using RaftProto.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RaftProto.Resources
{
    public enum ResourcePickupMode
    {
        Interact,
        Proximity,
        Both
    }

    /// <summary>
    /// Collects floating resources via Interact (default), optional proximity, or both.
    /// Blocked while <see cref="IBlocksResourcePickup"/> is active (build mode). In multiplayer
    /// this becomes a server-validated CollectServerRpc.
    ///
    /// Prefabs are assigned on <see cref="ResourceSpawner"/> spawn entries, not here.
    /// </summary>
    public class ResourceCollector : MonoBehaviour
    {
        [SerializeField] private ResourcePickupMode pickupMode = ResourcePickupMode.Interact;

        [Tooltip("Pickup radius around the player.")]
        [SerializeField] private float collectRange = 2.5f;

        [Tooltip("Max colliders considered per query (non-alloc buffer size).")]
        [SerializeField] private int maxConsidered = 16;

        [Header("Interact Aim")]
        [Tooltip("Prefer resources closer to the camera forward axis when pressing Interact.")]
        [SerializeField] private Transform cameraTransform;

        public event System.Action<ResourceType> Collected;

        private InputSystem_Actions _input;
        private Collider[] _buffer;
        private IBlocksResourcePickup _pickupBlocker;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            _buffer = new Collider[Mathf.Max(1, maxConsidered)];
            _pickupBlocker = GetComponent<IBlocksResourcePickup>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            _input.Player.Enable();
        }

        private void OnDisable()
        {
            _input.Player.Disable();
        }

        private void OnDestroy()
        {
            _input.Dispose();
        }

        private void Update()
        {
            if (IsPickupBlocked())
            {
                return;
            }

            if (pickupMode == ResourcePickupMode.Interact || pickupMode == ResourcePickupMode.Both)
            {
                if (_input.Player.Interact.WasPressedThisFrame())
                {
                    TryCollectBestInRange(preferAim: true);
                }
            }

            if (pickupMode == ResourcePickupMode.Proximity || pickupMode == ResourcePickupMode.Both)
            {
                TryCollectBestInRange(preferAim: false);
            }
        }

        /// <summary>Used by <see cref="ResourceHook"/> when a reeled resource reaches the player.</summary>
        public bool TryCollect(FloatingResource resource)
        {
            if (resource == null || IsPickupBlocked())
            {
                return false;
            }

            ResourceType type = resource.ResourceType;
            if (resource.TryConsume())
            {
                Collected?.Invoke(type);
                return true;
            }

            return false;
        }

        private void TryCollectBestInRange(bool preferAim)
        {
            FloatingResource best = FindBestResource(preferAim);
            if (best != null)
            {
                TryCollect(best);
            }
        }

        private FloatingResource FindBestResource(bool preferAim)
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, collectRange, _buffer, ~0, QueryTriggerInteraction.Collide);

            FloatingResource best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = _buffer[i];
                if (col == null)
                {
                    continue;
                }

                FloatingResource resource = col.GetComponentInParent<FloatingResource>();
                if (resource == null || !resource.IsAvailable)
                {
                    continue;
                }

                Vector3 toResource = resource.transform.position - transform.position;
                float distance = toResource.magnitude;

                float score = distance;
                if (preferAim && cameraTransform != null)
                {
                    Vector3 flatToResource = toResource;
                    flatToResource.y = 0f;
                    if (flatToResource.sqrMagnitude > 0.0001f)
                    {
                        float angle = Vector3.Angle(cameraTransform.forward, flatToResource.normalized);
                        score += angle * 0.05f;
                    }
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = resource;
                }
            }

            return best;
        }

        private bool IsPickupBlocked()
        {
            return _pickupBlocker != null && _pickupBlocker.BlocksResourcePickup;
        }
    }
}
