using UnityEngine;
using UnityEngine.InputSystem;

namespace RaftProto.Player
{
    /// <summary>
    /// Camera-relative character movement driven by the Input System. Uses a
    /// CharacterController, so gravity and jumping are integrated manually.
    /// Input reading and movement are intentionally kept together so the network
    /// layer can later gate the whole component to the owning client.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Jump & Gravity")]
        [Tooltip("Peak height (m) of a jump from flat ground.")]
        [SerializeField] private float jumpHeight = 1.2f;

        [Tooltip("Downward acceleration (m/s^2). More negative = heavier feel.")]
        [SerializeField] private float gravity = -20f;

        [Header("Camera")]
        [Tooltip("Transform whose yaw defines movement direction. Defaults to the main camera.")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController _controller;
        private InputSystem_Actions _input;
        private float _verticalVelocity;
        private bool _jumpQueued;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = new InputSystem_Actions();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            _input.Player.Enable();
            _input.Player.Jump.performed += OnJump;
        }

        private void OnDisable()
        {
            _input.Player.Jump.performed -= OnJump;
            _input.Player.Disable();
        }

        private void OnDestroy()
        {
            _input.Dispose();
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            if (_controller.isGrounded)
            {
                _jumpQueued = true;
            }
        }

        private void Update()
        {
            Vector2 moveInput = _input.Player.Move.ReadValue<Vector2>();
            bool sprinting = _input.Player.Sprint.IsPressed();

            Vector3 moveDirection = CameraRelativeDirection(moveInput);
            float speed = sprinting ? sprintSpeed : moveSpeed;

            UpdateVerticalVelocity();

            Vector3 velocity = moveDirection * speed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            FaceMoveDirection(moveDirection);
        }

        private Vector3 CameraRelativeDirection(Vector2 input)
        {
            if (cameraTransform == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private void UpdateVerticalVelocity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                // Small constant keeps the controller pressed onto the ground for a reliable isGrounded.
                _verticalVelocity = -2f;
            }

            if (_jumpQueued)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _jumpQueued = false;
            }

            _verticalVelocity += gravity * Time.deltaTime;
        }

        private void FaceMoveDirection(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }
}
