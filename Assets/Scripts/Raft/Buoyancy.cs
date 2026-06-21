using UnityEngine;

namespace RaftProto.Raft
{
    /// <summary>
    /// Applies a spring-damper buoyancy force at several sample points so the body
    /// floats at the water line and self-levels. Pure local physics: in multiplayer
    /// this component is expected to run on the server only and be disabled on clients.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Buoyancy : MonoBehaviour
    {
        [Header("Water")]
        [Tooltip("World-space height of the ocean surface. Convention for the whole project is 0.")]
        [SerializeField] private float waterLevel = 0f;

        [Header("Float Sampling")]
        [Tooltip("Points sampled for submersion. Spread them to the corners for self-leveling torque.")]
        [SerializeField] private Transform[] floatPoints;

        [Header("Tuning")]
        [Tooltip("Buoyancy as a multiple of weight at full submersion. >1 means it floats up.")]
        [SerializeField] private float buoyancyStrength = 1.6f;

        [Tooltip("Depth (m) at which a point produces maximum buoyancy. Larger = softer spring.")]
        [SerializeField] private float maxSubmergenceDepth = 0.8f;

        [Tooltip("Opposes vertical velocity at each point for a smooth settle. Higher = less bob/dip.")]
        [SerializeField] private float buoyancyDamping = 3f;

        [Header("Stability")]
        [Tooltip("Override the rigidbody center of mass. Placing it below the float points makes the raft self-right (ballast effect).")]
        [SerializeField] private bool overrideCenterOfMass = true;

        [Tooltip("Local-space center of mass. Keep its Y below the float points for roll stability.")]
        [SerializeField] private Vector3 centerOfMass = new Vector3(0f, -0.9f, 0f);

        private Rigidbody _body;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();

            if (overrideCenterOfMass)
            {
                _body.centerOfMass = centerOfMass;
            }
        }

        private void FixedUpdate()
        {
            if (floatPoints == null || floatPoints.Length == 0)
            {
                return;
            }

            float gravity = Mathf.Abs(Physics.gravity.y);
            int pointCount = floatPoints.Length;
            float maxForcePerPoint = _body.mass * gravity * buoyancyStrength / pointCount;
            float dampingPerPoint = _body.mass * buoyancyDamping / pointCount;

            foreach (Transform point in floatPoints)
            {
                if (point == null)
                {
                    continue;
                }

                float depth = waterLevel - point.position.y;
                if (depth <= 0f)
                {
                    continue;
                }

                float submersion = Mathf.Clamp01(depth / maxSubmergenceDepth);
                float verticalSpeed = _body.GetPointVelocity(point.position).y;

                float springForce = maxForcePerPoint * submersion;
                float dampingForce = verticalSpeed * dampingPerPoint * submersion;

                _body.AddForceAtPosition(Vector3.up * (springForce - dampingForce), point.position, ForceMode.Force);
            }
        }
    }
}
