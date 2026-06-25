using RaftProto.Core;
using RaftProto.Items;
using RaftProto.Raft;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RaftProto.Building
{
    /// <summary>
    /// Local building preview and placement. Requires build mode (B key for now; later a
    /// hammer tool) before showing the ghost or accepting place/remove input.
    /// </summary>
    public class BuildingSystem : MonoBehaviour, IBlocksResourcePickup
    {
        bool IBlocksResourcePickup.BlocksResourcePickup => _buildModeActive;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("References")]
        [SerializeField] private RaftGrid raftGrid;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform playerTransform;

        [Header("Tile")]
        [SerializeField] private GameObject deckTilePrefab;
        [SerializeField] private Vector3 deckLocalScale = new Vector3(1f, 0.25f, 1f);

        [Header("Placement")]
        [Tooltip("Max distance along the aim ray to the deck plane. Caps how far you can point.")]
        [SerializeField] private float maxRayDistance = 8f;
        [Tooltip("Max horizontal distance from the player to the target cell centre.")]
        [SerializeField] private float maxPlacementDistanceFromPlayer = 3.5f;

        [Header("Build Mode")]
        [Tooltip("Toggle build mode (stands in for having a hammer equipped until the tool system exists).")]
        [SerializeField] private Key buildModeKey = Key.B;

        [Header("Removal (testing)")]
        [Tooltip("Test-only key to remove the targeted tile. Later this will be gated behind an axe tool.")]
        [SerializeField] private Key removeKey = Key.X;

        [Header("Placement Cost")]
        [SerializeField] private Inventory inventory;
        [SerializeField] private bool requirePlacementCost = true;
        [SerializeField] private string placementItemId = ItemIds.Plank;
        [SerializeField] private int placementItemCost = 1;

        [Header("Ghost Colors")]
        [SerializeField] private Color validGhostColor = new Color(0.2f, 0.9f, 0.3f, 0.45f);
        [SerializeField] private Color invalidGhostColor = new Color(0.9f, 0.2f, 0.2f, 0.35f);

        private InputSystem_Actions _input;
        private Transform _ghostRoot;
        private Renderer _ghostRenderer;
        private MaterialPropertyBlock _ghostPropertyBlock;
        private Vector2Int _previewCell;
        private bool _hasPreview;
        private bool _hasTargetCell;
        private bool _buildModeActive;
        private ISwimStateProvider _swimState;
        private IResourceInteractPickup _resourceInteractPickup;

        /// <summary>True while the player is in build mode (hammer equipped). Later driven by the tool system.</summary>
        public bool IsBuildModeActive => _buildModeActive;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            _ghostPropertyBlock = new MaterialPropertyBlock();
            _swimState = GetComponent<ISwimStateProvider>();
            _resourceInteractPickup = GetComponent<IResourceInteractPickup>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (playerTransform == null)
            {
                playerTransform = transform;
            }

            if (inventory == null)
            {
                inventory = GetComponent<Inventory>();
            }

            CreateGhost();
        }

        private void OnEnable()
        {
            _input.Player.Enable();
        }

        private void OnDisable()
        {
            _input.Player.Disable();
            _buildModeActive = false;
            SetGhostVisible(false);
        }

        private void OnDestroy()
        {
            _input.Dispose();

            if (_ghostRoot != null)
            {
                Destroy(_ghostRoot.gameObject);
            }
        }

        private void Update()
        {
            if (raftGrid == null || cameraTransform == null)
            {
                SetGhostVisible(false);
                return;
            }

            if (Keyboard.current != null && Keyboard.current[buildModeKey].wasPressedThisFrame)
            {
                if (_swimState == null || !_swimState.IsSwimming)
                {
                    SetBuildModeActive(!_buildModeActive);
                }
            }

            if (_swimState != null && _swimState.IsSwimming && _buildModeActive)
            {
                SetBuildModeActive(false);
            }

            if (!_buildModeActive)
            {
                _hasTargetCell = false;
                _hasPreview = false;
                SetGhostVisible(false);
                return;
            }

            UpdatePreview();

            if (Keyboard.current != null && Keyboard.current[removeKey].wasPressedThisFrame)
            {
                TryRemoveTargetedTile();
            }
        }

        private void LateUpdate()
        {
            if (!_buildModeActive || raftGrid == null || cameraTransform == null)
            {
                return;
            }

            if (!_input.Player.Interact.WasPressedThisFrame())
            {
                return;
            }

            // Pickup runs in ResourceCollector.Update; defer placement if E collected something.
            if (_resourceInteractPickup != null && _resourceInteractPickup.LastInteractCollected)
            {
                return;
            }

            TryPlacePreviewedTile();
        }

        private void SetBuildModeActive(bool active)
        {
            _buildModeActive = active;

            if (!_buildModeActive)
            {
                _hasTargetCell = false;
                _hasPreview = false;
                SetGhostVisible(false);
            }
        }

        private void UpdatePreview()
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

            if (!TryGetTargetCell(ray, out Vector2Int targetCell))
            {
                _hasTargetCell = false;
                SetGhostVisible(false);
                return;
            }

            _previewCell = targetCell;
            _hasTargetCell = true;

            bool isValid = IsValidPlacement(targetCell);
            _hasPreview = isValid;

            _ghostRoot.SetParent(raftGrid.transform, false);
            _ghostRoot.localPosition = raftGrid.CellToLocal(targetCell);
            _ghostRoot.localRotation = Quaternion.identity;

            SetGhostColor(isValid ? validGhostColor : invalidGhostColor);
            SetGhostVisible(true);
        }

    private bool TryGetTargetCell(Ray ray, out Vector2Int targetCell)
    {
        targetCell = default;

        // Project the aim ray onto the raft's deck plane. Using the grid's own up/origin
        // keeps this correct while the raft bobs and tilts, and lets us aim at empty water
        // cells next to the raft instead of needing the crosshair on an existing tile.
        Plane deckPlane = new Plane(raftGrid.transform.up, raftGrid.transform.position);

        if (!deckPlane.Raycast(ray, out float enter) || enter > maxRayDistance)
        {
            return false;
        }

        targetCell = raftGrid.WorldToCell(ray.GetPoint(enter));
        return true;
    }

        private bool IsValidPlacement(Vector2Int cell)
        {
            return raftGrid.CanPlaceTile(cell) && IsWithinReach(cell) && CanAffordPlacementCost();
        }

        private bool CanAffordPlacementCost()
        {
            if (!requirePlacementCost || inventory == null || placementItemCost <= 0)
            {
                return true;
            }

            return inventory.HasItem(placementItemId, placementItemCost);
        }

        private bool TryConsumePlacementCost()
        {
            if (!requirePlacementCost || inventory == null || placementItemCost <= 0)
            {
                return true;
            }

            return inventory.TryRemoveItem(placementItemId, placementItemCost);
        }

        // Horizontal reach only: the player capsule sits ~1m above the deck, so including
        // the vertical gap would shrink the usable reach and make edge tiles feel unreachable.
        private bool IsWithinReach(Vector2Int cell)
        {
            Vector3 toCell = raftGrid.CellToWorld(cell) - playerTransform.position;
            toCell.y = 0f;
            return toCell.magnitude <= maxPlacementDistanceFromPlayer;
        }

        private void TryPlacePreviewedTile()
        {
            if (!_hasPreview || !IsValidPlacement(_previewCell))
            {
                return;
            }

            GameObject tileObject = CreateDeckTile(_previewCell);
            if (tileObject == null)
            {
                return;
            }

            if (!raftGrid.RegisterTile(_previewCell, tileObject.transform))
            {
                Destroy(tileObject);
                return;
            }

            if (!TryConsumePlacementCost())
            {
                raftGrid.RemoveTile(_previewCell, out Transform rollbackRoot);
                if (rollbackRoot != null)
                {
                    Destroy(rollbackRoot.gameObject);
                }
            }
        }

        private void TryRemoveTargetedTile()
        {
            if (!_hasTargetCell || !raftGrid.HasTile(_previewCell))
            {
                return;
            }

            if (!IsWithinReach(_previewCell))
            {
                return;
            }

            // Keep at least one tile so the raft doesn't lose all buoyancy and sink.
            if (raftGrid.Tiles.Count <= 1)
            {
                return;
            }

            if (raftGrid.RemoveTile(_previewCell, out Transform removedRoot) && removedRoot != null)
            {
                Destroy(removedRoot.gameObject);
            }
        }

        private GameObject CreateDeckTile(Vector2Int cell)
        {
            GameObject tileObject;

            if (deckTilePrefab != null)
            {
                tileObject = Instantiate(deckTilePrefab, raftGrid.transform);
            }
            else
            {
                tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObject.transform.SetParent(raftGrid.transform, false);
                tileObject.name = $"Deck_{cell.x}_{cell.y}";
                tileObject.transform.localScale = deckLocalScale;
            }

            tileObject.transform.localPosition = raftGrid.CellToLocal(cell);
            tileObject.transform.localRotation = Quaternion.identity;
            return tileObject;
        }

    private void CreateGhost()
        {
            if (deckTilePrefab != null)
            {
                _ghostRoot = Instantiate(deckTilePrefab).transform;
                _ghostRoot.name = "DeckGhost";
            }
            else
            {
                _ghostRoot = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                _ghostRoot.name = "DeckGhost";
                _ghostRoot.localScale = deckLocalScale;
            }

            foreach (Collider collider in _ghostRoot.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }

            _ghostRenderer = _ghostRoot.GetComponentInChildren<Renderer>();
            SetGhostVisible(false);
        }

        private void SetGhostVisible(bool visible)
        {
            if (_ghostRoot != null)
            {
                _ghostRoot.gameObject.SetActive(visible);
            }
        }

        private void SetGhostColor(Color color)
        {
            if (_ghostRenderer == null)
            {
                return;
            }

            _ghostRenderer.GetPropertyBlock(_ghostPropertyBlock);
            _ghostPropertyBlock.SetColor(BaseColorId, color);
            _ghostRenderer.SetPropertyBlock(_ghostPropertyBlock);
        }
    }
}
