using RaftProto.Raft;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RaftProto.Building
{
    /// <summary>
    /// Local building preview and placement. Raycasts from the camera, shows a ghost tile,
    /// and registers new deck tiles on the raft grid. In multiplayer this becomes client
    /// preview + ServerRpc; only the server mutates RaftGrid.
    /// </summary>
    public class BuildingSystem : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("References")]
        [SerializeField] private RaftGrid raftGrid;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform playerTransform;

        [Header("Tile")]
        [SerializeField] private GameObject deckTilePrefab;
        [SerializeField] private Vector3 deckLocalScale = new Vector3(1f, 0.25f, 1f);

        [Header("Placement")]
        [SerializeField] private float maxRayDistance = 5f;
        [SerializeField] private float maxPlacementDistanceFromPlayer = 3.5f;
        [SerializeField] private LayerMask placementMask = Physics.DefaultRaycastLayers;

        [Header("Ghost Colors")]
        [SerializeField] private Color validGhostColor = new Color(0.2f, 0.9f, 0.3f, 0.45f);
        [SerializeField] private Color invalidGhostColor = new Color(0.9f, 0.2f, 0.2f, 0.35f);

        private InputSystem_Actions _input;
        private Transform _ghostRoot;
        private Renderer _ghostRenderer;
        private MaterialPropertyBlock _ghostPropertyBlock;
        private Vector2Int _previewCell;
        private bool _hasPreview;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            _ghostPropertyBlock = new MaterialPropertyBlock();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (playerTransform == null)
            {
                playerTransform = transform;
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

            UpdatePreview();

            if (_input.Player.Interact.WasPressedThisFrame())
            {
                TryPlacePreviewedTile();
            }
        }

        private void UpdatePreview()
        {
            Ray ray = cameraTransform.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (!TryGetTargetCell(ray, out Vector2Int targetCell))
            {
                SetGhostVisible(false);
                return;
            }

            bool isValid = IsValidPlacement(targetCell);
            _previewCell = targetCell;
            _hasPreview = isValid;

            _ghostRoot.SetParent(raftGrid.transform, false);
            _ghostRoot.localPosition = raftGrid.CellToLocal(targetCell);
            _ghostRoot.localRotation = Quaternion.identity;
            _ghostRoot.localScale = deckLocalScale;

            SetGhostColor(isValid ? validGhostColor : invalidGhostColor);
            SetGhostVisible(true);
        }

        private bool TryGetTargetCell(Ray ray, out Vector2Int targetCell)
        {
            targetCell = default;

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (!IsRaftCollider(hit.collider))
            {
                return false;
            }

            Vector2Int hitCell = raftGrid.WorldToCell(hit.point);
            Vector2Int offset = GetPlacementOffset(hit);
            targetCell = hitCell + offset;
            return true;
        }

        private Vector2Int GetPlacementOffset(RaycastHit hit)
        {
            if (Mathf.Abs(hit.normal.y) < 0.5f)
            {
                return new Vector2Int(
                    Mathf.RoundToInt(hit.normal.x),
                    Mathf.RoundToInt(hit.normal.z));
            }

            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return Vector2Int.zero;
            }

            forward.Normalize();
            return new Vector2Int(Mathf.RoundToInt(forward.x), Mathf.RoundToInt(forward.z));
        }

        private bool IsValidPlacement(Vector2Int cell)
        {
            if (!raftGrid.CanPlaceTile(cell))
            {
                return false;
            }

            Vector3 cellWorld = raftGrid.CellToWorld(cell);
            float distance = Vector3.Distance(playerTransform.position, cellWorld);
            return distance <= maxPlacementDistanceFromPlayer;
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
            }

            tileObject.transform.localPosition = raftGrid.CellToLocal(cell);
            tileObject.transform.localRotation = Quaternion.identity;
            tileObject.transform.localScale = deckLocalScale;
            return tileObject;
        }

        private bool IsRaftCollider(Collider collider)
        {
            return collider.transform.IsChildOf(raftGrid.transform);
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
