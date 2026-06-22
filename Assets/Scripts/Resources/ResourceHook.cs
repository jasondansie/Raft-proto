using RaftProto.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RaftProto.Resources
{
    /// <summary>
    /// Throwable hook on Attack: raycast to a floating resource, reel it in, then hand off to
    /// <see cref="ResourceCollector"/>. The rope line runs from the hook visual tip to the target.
    /// In multiplayer the cast + attach + collect become server-validated RPCs.
    /// </summary>
    public class ResourceHook : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private ResourceCollector collector;

        [Header("Hook Visual")]
        [Tooltip("Kit hook prefab, parented to the mount while reeling.")]
        [SerializeField] private GameObject hookVisualPrefab;
        [Tooltip("Usually the camera for first-person. Falls back to the main camera.")]
        [SerializeField] private Transform hookMount;
        [Tooltip("Optional explicit rope start on the prefab. If empty, one is created at Line Origin Local Offset.")]
        [SerializeField] private Transform lineOrigin;
        [SerializeField] private Vector3 hookLocalPosition = new Vector3(0.35f, -0.3f, 0.55f);
        [SerializeField] private Vector3 hookLocalEuler = new Vector3(15f, -25f, 0f);
        [SerializeField] private Vector3 lineOriginLocalOffset = new Vector3(0f, 0.05f, 0.45f);

        [Header("Hook")]
        [SerializeField] private float hookRange = 14f;
        [SerializeField] private float pullSpeed = 5f;
        [SerializeField] private float collectRange = 2f;
        [Tooltip("Half-angle (degrees) of the aim cone used when the ray hits the raft first.")]
        [SerializeField] private float aimConeHalfAngle = 12f;
        [SerializeField] private LayerMask hookMask = Physics.DefaultRaycastLayers;

        [Header("Line")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float lineWidth = 0.03f;

        private InputSystem_Actions _input;
        private IBlocksResourcePickup _pickupBlocker;
        private FloatingResource _hooked;
        private Collider[] _aimBuffer;
        private readonly RaycastHit[] _rayHits = new RaycastHit[32];
        private Transform _hookVisualRoot;
        private Transform _lineOriginTransform;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            _pickupBlocker = GetComponent<IBlocksResourcePickup>();
            _aimBuffer = new Collider[32];

            if (collector == null)
            {
                collector = GetComponent<ResourceCollector>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (hookMount == null)
            {
                hookMount = cameraTransform;
            }

            EnsureHookVisual();
            EnsureLineRenderer();
            SetHookVisualVisible(false);
        }

        private void OnEnable()
        {
            _input.Player.Enable();
        }

        private void OnDisable()
        {
            _input.Player.Disable();
            ReleaseHook();
            SetLineVisible(false);
            SetHookVisualVisible(false);
        }

        private void OnDestroy()
        {
            _input.Dispose();
        }

        private void Update()
        {
            if (_hooked != null)
            {
                ReelHookedResource();
                UpdateLine();
                return;
            }

            SetLineVisible(false);
            SetHookVisualVisible(false);

            if (IsHookBlocked())
            {
                return;
            }

            if (_input.Player.Attack.WasPressedThisFrame())
            {
                TryCastHook();
            }
        }

        private void TryCastHook()
        {
            if (cameraTransform == null)
            {
                return;
            }

            FloatingResource resource = FindResourceAlongRay();
            if (resource == null)
            {
                resource = FindResourceInAimCone();
            }

            if (resource == null)
            {
                return;
            }

            _hooked = resource;
            _hooked.SetDriftEnabled(false);
            SetHookVisualVisible(true);
            SetLineVisible(true);
        }

        private FloatingResource FindResourceAlongRay()
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            int hitCount = Physics.RaycastNonAlloc(
                ray, _rayHits, hookRange, hookMask, QueryTriggerInteraction.Collide);

            System.Array.Sort(_rayHits, 0, hitCount, RayHitDistanceComparer.Instance);

            for (int i = 0; i < hitCount; i++)
            {
                FloatingResource resource = _rayHits[i].collider.GetComponentInParent<FloatingResource>();
                if (resource != null && resource.IsAvailable)
                {
                    return resource;
                }
            }

            return null;
        }

        private FloatingResource FindResourceInAimCone()
        {
            int count = Physics.OverlapSphereNonAlloc(
                cameraTransform.position, hookRange, _aimBuffer, ~0, QueryTriggerInteraction.Collide);

            FloatingResource best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = _aimBuffer[i];
                if (col == null)
                {
                    continue;
                }

                FloatingResource resource = col.GetComponentInParent<FloatingResource>();
                if (resource == null || !resource.IsAvailable)
                {
                    continue;
                }

                Vector3 toResource = resource.transform.position - cameraTransform.position;
                float distance = toResource.magnitude;
                if (distance <= 0.001f)
                {
                    continue;
                }

                float angle = Vector3.Angle(cameraTransform.forward, toResource);
                if (angle > aimConeHalfAngle)
                {
                    continue;
                }

                float score = angle + distance * 0.02f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = resource;
                }
            }

            return best;
        }

        private void ReelHookedResource()
        {
            if (_hooked == null || !_hooked.IsAvailable)
            {
                ReleaseHook();
                return;
            }

            Vector3 toPlayer = transform.position - _hooked.transform.position;
            toPlayer.y = 0f;

            if (toPlayer.magnitude <= collectRange)
            {
                if (collector != null && collector.TryCollect(_hooked))
                {
                    _hooked = null;
                    SetLineVisible(false);
                    SetHookVisualVisible(false);
                    return;
                }

                ReleaseHook();
                return;
            }

            _hooked.transform.position += toPlayer.normalized * (pullSpeed * Time.deltaTime);
        }

        private void ReleaseHook()
        {
            if (_hooked != null && _hooked.IsAvailable)
            {
                _hooked.SetDriftEnabled(true);
            }

            _hooked = null;
            SetLineVisible(false);
            SetHookVisualVisible(false);
        }

        private void UpdateLine()
        {
            if (lineRenderer == null || _hooked == null)
            {
                return;
            }

            lineRenderer.SetPosition(0, GetLineOriginWorldPosition());
            lineRenderer.SetPosition(1, _hooked.transform.position);
        }

        private Vector3 GetLineOriginWorldPosition()
        {
            if (_lineOriginTransform != null)
            {
                return _lineOriginTransform.position;
            }

            if (hookMount != null)
            {
                return hookMount.TransformPoint(lineOriginLocalOffset);
            }

            return transform.position + Vector3.up;
        }

        private void EnsureHookVisual()
        {
            if (_hookVisualRoot != null || hookVisualPrefab == null || hookMount == null)
            {
                ResolveLineOriginTransform();
                return;
            }

            GameObject instance = Instantiate(hookVisualPrefab, hookMount);
            instance.name = "HookVisual";
            _hookVisualRoot = instance.transform;
            _hookVisualRoot.localPosition = hookLocalPosition;
            _hookVisualRoot.localRotation = Quaternion.Euler(hookLocalEuler);

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }

            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>())
            {
                Destroy(body);
            }

            ResolveLineOriginTransform();
        }

        private void ResolveLineOriginTransform()
        {
            if (lineOrigin != null)
            {
                _lineOriginTransform = lineOrigin;
                return;
            }

            if (_hookVisualRoot == null)
            {
                return;
            }

            Transform existing = _hookVisualRoot.Find("LineOrigin");
            if (existing != null)
            {
                _lineOriginTransform = existing;
                return;
            }

            GameObject originObject = new GameObject("LineOrigin");
            originObject.transform.SetParent(_hookVisualRoot, false);
            originObject.transform.localPosition = lineOriginLocalOffset;
            _lineOriginTransform = originObject.transform;
        }

        private void EnsureLineRenderer()
        {
            if (lineRenderer != null)
            {
                return;
            }

            GameObject lineObject = new GameObject("HookLine");
            lineObject.transform.SetParent(transform, false);
            lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth * 0.5f;
            lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineRenderer.startColor = new Color(0.85f, 0.75f, 0.55f, 1f);
            lineRenderer.endColor = lineRenderer.startColor;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
        }

        private void SetLineVisible(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }
        }

        private void SetHookVisualVisible(bool visible)
        {
            if (_hookVisualRoot != null)
            {
                _hookVisualRoot.gameObject.SetActive(visible);
            }
        }

        private bool IsHookBlocked()
        {
            return _pickupBlocker != null && _pickupBlocker.BlocksResourcePickup;
        }

        private sealed class RayHitDistanceComparer : System.Collections.IComparer
        {
            public static readonly RayHitDistanceComparer Instance = new();

            public int Compare(object x, object y)
            {
                return ((RaycastHit)x).distance.CompareTo(((RaycastHit)y).distance);
            }
        }
    }
}
