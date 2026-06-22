using UnityEngine;

namespace RaftProto.Resources
{
    /// <summary>
    /// Proximity pickup attached to the player. In multiplayer this becomes a CollectServerRpc:
    /// the owning client asks, the server validates range + availability, despawns once, and
    /// credits the owner's inventory. For now it runs locally and auto-collects in range.
    ///
    /// The <see cref="Collected"/> event is the seam Phase 4 inventory will subscribe to.
    /// </summary>
    public class ResourceCollector : MonoBehaviour
    {
        [Tooltip("Pickup radius around the player. Allow for the ~1.3m gap between the capsule center and the water surface.")]
        [SerializeField] private float collectRange = 2.5f;

        [Tooltip("Max colliders considered per frame (non-alloc buffer size).")]
        [SerializeField] private int maxConsidered = 16;

        public event System.Action<ResourceType> Collected;

        private Collider[] _buffer;

        private void Awake()
        {
            _buffer = new Collider[Mathf.Max(1, maxConsidered)];
        }

        private void Update()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, collectRange, _buffer, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider col = _buffer[i];
                if (col == null)
                {
                    continue;
                }

                FloatingResource resource = col.GetComponentInParent<FloatingResource>();
                if (resource == null)
                {
                    continue;
                }

                ResourceType type = resource.ResourceType;
                if (resource.TryConsume())
                {
                    Collected?.Invoke(type);
                }
            }
        }
    }
}
