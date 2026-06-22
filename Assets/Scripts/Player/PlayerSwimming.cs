using UnityEngine;

namespace RaftProto.Player
{
    /// <summary>
    /// Buoyancy and swim movement when the player is in the water. Uses the project-wide
    /// water surface at <see cref="waterLevel"/> and defers to <see cref="CharacterController.isGrounded"/>
    /// so standing on the raft always wins over swim mode. In multiplayer this runs only for
    /// the owning client; the server still owns authoritative position via the networked controller.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerSwimming : MonoBehaviour
    {
        [Header("Water")]
        [Tooltip("World-space height of the ocean surface. Project convention is 0.")]
        [SerializeField] private float waterLevel = 0f;

        [Header("Movement")]
        [SerializeField] private float swimMoveSpeed = 2.5f;
        [Tooltip("Upward boost when pressing Jump while swimming.")]
        [SerializeField] private float swimJumpSpeed = 4f;

        [Header("Buoyancy")]
        [Tooltip("World Y of the capsule centre when floating at the surface.")]
        [SerializeField] private float swimCenterHeight = 0.55f;
        [SerializeField] private float buoyancyStrength = 12f;
        [SerializeField] private float waterDrag = 4f;
        [SerializeField] private float maxSinkSpeed = 2.5f;

        private CharacterController _controller;

        public bool IsSwimming { get; private set; }
        public float SwimMoveSpeed => swimMoveSpeed;
        public float SwimJumpSpeed => swimJumpSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        /// <summary>Call once per frame before applying vertical velocity.</summary>
        public void UpdateSwimState()
        {
            float feetY = transform.position.y + _controller.center.y - _controller.height * 0.5f;
            bool feetInWater = feetY <= waterLevel + 0.05f;
            IsSwimming = feetInWater && !_controller.isGrounded;
        }

        /// <summary>
        /// Spring-damper buoyancy toward the swim surface. Replaces normal gravity while swimming.
        /// </summary>
        public float ApplySwimVertical(float verticalVelocity, float deltaTime)
        {
            float targetY = waterLevel + swimCenterHeight;
            float depthError = targetY - transform.position.y;

            float acceleration = depthError * buoyancyStrength - verticalVelocity * waterDrag;
            verticalVelocity += acceleration * deltaTime;
            return Mathf.Max(verticalVelocity, -maxSinkSpeed);
        }
    }
}
