using System.Collections.Generic;
using UnityEngine;

namespace RaftProto.Raft
{
    /// <summary>
    /// Grid-driven spring-damper buoyancy. One float sample is generated per occupied
    /// <see cref="RaftGrid"/> cell, and the rigidbody mass + center of mass scale with the
    /// raft as it grows, so any size or shape floats level at the same ride height. Because
    /// both the per-point force budget and the mass scale with tile count, the tuned
    /// equilibrium depth is independent of how big the raft gets.
    ///
    /// Pure local physics: in multiplayer this runs on the server only and is disabled on clients.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Buoyancy : MonoBehaviour
    {
        [Header("Water")]
        [Tooltip("World-space height of the ocean surface. Convention for the whole project is 0.")]
        [SerializeField] private float waterLevel = 0f;

        [Header("Grid Source")]
        [Tooltip("Float samples, mass and center of mass are derived from this grid's occupied cells. Auto-found on this object if left empty.")]
        [SerializeField] private RaftGrid raftGrid;

        [Header("Tuning")]
        [Tooltip("Buoyancy as a multiple of weight at full submersion. >1 means it floats up.")]
        [SerializeField] private float buoyancyStrength = 1.6f;

        [Tooltip("Depth (m) at which a point produces maximum buoyancy. Larger = softer spring.")]
        [SerializeField] private float maxSubmergenceDepth = 0.8f;

        [Tooltip("Opposes vertical velocity at each point for a smooth settle. Higher = less bob/dip.")]
        [SerializeField] private float buoyancyDamping = 3f;

        [Header("Per-Tile Physics")]
        [Tooltip("Mass (kg) added to the rigidbody for each placed tile.")]
        [SerializeField] private float massPerTile = 10f;

        [Tooltip("Local Y of each float sample below the deck plane. Lower = more freeboard.")]
        [SerializeField] private float floatSampleLocalY = -0.6f;

        [Tooltip("Local Y of the center of mass below the deck. Keep below the samples for roll stability.")]
        [SerializeField] private float ballastLocalY = -0.9f;

        private Rigidbody _body;
        private readonly List<Vector3> _localSamples = new();

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();

            if (raftGrid == null)
            {
                raftGrid = GetComponent<RaftGrid>();
            }
        }

        private void OnEnable()
        {
            if (raftGrid != null)
            {
                raftGrid.TilesChanged += Rebuild;
            }
        }

        private void OnDisable()
        {
            if (raftGrid != null)
            {
                raftGrid.TilesChanged -= Rebuild;
            }
        }

        private void Start()
        {
            // Initial tiles are registered in RaftGrid.Awake, so build once after all Awakes.
            Rebuild();
        }

        /// <summary>
        /// Recompute float samples, mass and center of mass from the current occupied cells.
        /// Cheap and only called when the tile set changes.
        /// </summary>
        private void Rebuild()
        {
            _localSamples.Clear();

            if (raftGrid == null)
            {
                return;
            }

            Vector3 centroid = Vector3.zero;
            foreach (Vector2Int cell in raftGrid.Tiles.Keys)
            {
                Vector3 deckLocal = raftGrid.CellToLocal(cell);
                centroid += deckLocal;
                _localSamples.Add(new Vector3(deckLocal.x, floatSampleLocalY, deckLocal.z));
            }

            int count = _localSamples.Count;
            if (count == 0)
            {
                return;
            }

            centroid /= count;
            _body.mass = count * massPerTile;
            _body.centerOfMass = new Vector3(centroid.x, ballastLocalY, centroid.z);
        }

        private void FixedUpdate()
        {
            int count = _localSamples.Count;
            if (count == 0)
            {
                return;
            }

            float gravity = Mathf.Abs(Physics.gravity.y);
            float maxForcePerPoint = _body.mass * gravity * buoyancyStrength / count;
            float dampingPerPoint = _body.mass * buoyancyDamping / count;

            for (int i = 0; i < count; i++)
            {
                Vector3 worldPoint = transform.TransformPoint(_localSamples[i]);

                float depth = waterLevel - worldPoint.y;
                if (depth <= 0f)
                {
                    continue;
                }

                float submersion = Mathf.Clamp01(depth / maxSubmergenceDepth);
                float verticalSpeed = _body.GetPointVelocity(worldPoint).y;

                float springForce = maxForcePerPoint * submersion;
                float dampingForce = verticalSpeed * dampingPerPoint * submersion;

                _body.AddForceAtPosition(Vector3.up * (springForce - dampingForce), worldPoint, ForceMode.Force);
            }
        }
    }
}
