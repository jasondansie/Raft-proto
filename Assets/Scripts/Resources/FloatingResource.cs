using UnityEngine;

namespace RaftProto.Resources
{
    public enum ResourceType
    {
        Wood,
        Plastic,
        Scrap
    }

    /// <summary>
    /// A single drifting resource. In multiplayer the server owns spawn + drift and replicates
    /// the object; for now it runs locally. Movement is a constant current plus a gentle bob so
    /// it reads as floating on the surface.
    /// </summary>
    public class FloatingResource : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Wood;

        [Header("Float")]
        [Tooltip("World height of the ocean surface. Project convention is 0.")]
        [SerializeField] private float waterLevel = 0f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 1f;

        private Vector3 _driftVelocity;
        private float _bobPhase;
        private bool _consumed;

        public ResourceType ResourceType => resourceType;

        public void Initialize(ResourceType type, Vector3 driftVelocity)
        {
            resourceType = type;
            _driftVelocity = driftVelocity;
            _bobPhase = Random.value * Mathf.PI * 2f;
        }

        public void SetDrift(Vector3 driftVelocity)
        {
            _driftVelocity = driftVelocity;
        }

        private void Update()
        {
            Vector3 pos = transform.position + _driftVelocity * Time.deltaTime;
            _bobPhase += bobFrequency * Time.deltaTime;
            pos.y = waterLevel + Mathf.Sin(_bobPhase) * bobAmplitude;
            transform.position = pos;
        }

        /// <summary>
        /// Claims and despawns the resource. Returns false if it was already taken, so two
        /// collectors can't both score it — the single-winner rule the server will own later.
        /// </summary>
        public bool TryConsume()
        {
            if (_consumed)
            {
                return false;
            }

            _consumed = true;
            Destroy(gameObject);
            return true;
        }
    }
}
