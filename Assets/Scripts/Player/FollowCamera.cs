using UnityEngine;
using UnityEngine.InputSystem;

namespace RaftProto.Player
{
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson
    }

    /// <summary>
    /// First- or third-person camera driven by Look input. Runs in LateUpdate so it
    /// tracks after movement. Only the local owner's camera should run in MP later.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Mode")]
        [SerializeField] private CameraMode mode = CameraMode.FirstPerson;

        [Header("First Person")]
        [Tooltip("When set, eye height is derived from the target CharacterController instead of the manual offset.")]
        [SerializeField] private bool useCharacterControllerEyeHeight = true;

        [Tooltip("Fallback eye offset from the target origin when no CharacterController is present.")]
        [SerializeField] private float firstPersonEyeHeight = 0.8f;

        [SerializeField] private float firstPersonFieldOfView = 75f;
        [SerializeField] private float firstPersonMinPitch = -85f;
        [SerializeField] private float firstPersonMaxPitch = 85f;

        [Header("Third Person")]
        [SerializeField] private float thirdPersonDistance = 3f;
        [SerializeField] private float thirdPersonHeightOffset = 1.2f;
        [SerializeField] private float thirdPersonMinPitch = -20f;
        [SerializeField] private float thirdPersonMaxPitch = 60f;
        [SerializeField] private float thirdPersonPositionSmoothTime = 0.05f;

        [Header("Look")]
        [SerializeField] private float lookSensitivity = 0.15f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnPlay = true;

        private InputSystem_Actions _input;
        private Camera _camera;
        private float _yaw;
        private float _pitch;
        private Vector3 _positionVelocity;

        public CameraMode Mode => mode;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            InitializeAnglesFromTransform();

            if (_camera != null)
            {
                _camera.fieldOfView = firstPersonFieldOfView;
            }
        }

        private void OnEnable()
        {
            _input.Player.Enable();

            if (lockCursorOnPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            _input.Player.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            _input.Dispose();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            ApplyLookInput();

            if (mode == CameraMode.FirstPerson)
            {
                ApplyFirstPerson();
            }
            else
            {
                ApplyThirdPerson();
            }
        }

        /// <summary>
        /// Switch perspective at runtime. Hook to input/UI later.
        /// </summary>
        public void SetMode(CameraMode newMode)
        {
            if (mode == newMode)
            {
                return;
            }

            mode = newMode;
            _positionVelocity = Vector3.zero;
            SnapToMode();
        }

        private void ApplyLookInput()
        {
            Vector2 look = _input.Player.Look.ReadValue<Vector2>();
            _yaw += look.x * lookSensitivity;
            _pitch -= look.y * lookSensitivity;

            if (mode == CameraMode.FirstPerson)
            {
                _pitch = Mathf.Clamp(_pitch, firstPersonMinPitch, firstPersonMaxPitch);
            }
            else
            {
                _pitch = Mathf.Clamp(_pitch, thirdPersonMinPitch, thirdPersonMaxPitch);
            }
        }

        private void ApplyFirstPerson()
        {
            Vector3 eyePosition = target.position + Vector3.up * GetFirstPersonEyeHeight();
            transform.position = eyePosition;
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private float GetFirstPersonEyeHeight()
        {
            if (useCharacterControllerEyeHeight &&
                target.TryGetComponent(out CharacterController controller))
            {
                // Eye line ~65% up from the bottom of the capsule (typical for height = 2, center = 0).
                float bottom = controller.center.y - controller.height * 0.5f;
                return bottom + controller.height * 0.65f;
            }

            return firstPersonEyeHeight;
        }

        private void ApplyThirdPerson()
        {
            Vector3 pivot = target.position + Vector3.up * thirdPersonHeightOffset;
            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPosition = pivot + orbit * new Vector3(0f, 0f, -thirdPersonDistance);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                thirdPersonPositionSmoothTime);

            Vector3 lookDirection = pivot - transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        private void InitializeAnglesFromTransform()
        {
            Vector3 euler = transform.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;
            if (_pitch > 180f)
            {
                _pitch -= 360f;
            }
        }

        private void SnapToMode()
        {
            if (target == null)
            {
                return;
            }

            if (mode == CameraMode.FirstPerson)
            {
                ApplyFirstPerson();
            }
            else
            {
                _positionVelocity = Vector3.zero;
                ApplyThirdPerson();
            }
        }
    }
}
