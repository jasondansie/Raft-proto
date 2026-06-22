using System.Collections.Generic;
using UnityEngine;

namespace RaftProto.Resources
{
    /// <summary>
    /// Spawns drifting resources upstream of a target and sends them across on a shared current.
    /// Uses a simple object pool per visual prefab. In multiplayer this is server-only.
    /// </summary>
    public class ResourceSpawner : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Resources spawn around and drift past this point. Defaults to this object.")]
        [SerializeField] private Transform target;

        [Header("Prefabs")]
        [Tooltip("Kit prefabs mapped to resource types. If empty, a fallback cube is used.")]
        [SerializeField] private ResourceSpawnEntry[] spawnEntries;

        [Header("Spawning")]
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int maxAlive = 20;
        [SerializeField] private int prewarmPerType = 4;
        [Tooltip("Distance from target where resources appear (upstream of the current).")]
        [SerializeField] private float spawnRadius = 30f;
        [Tooltip("Distance from target beyond which resources are returned to the pool.")]
        [SerializeField] private float despawnRadius = 45f;

        [Header("Current")]
        [Tooltip("When enabled, debris spawns on the target's forward horizon and drifts toward the raft.")]
        [SerializeField] private bool deriveCurrentFromTargetForward = true;
        [Tooltip("Manual drift direction (world XZ) when not deriving from the target.")]
        [SerializeField] private Vector3 currentDirection = new Vector3(0f, 0f, -1f);
        [SerializeField] private float driftSpeed = 1.5f;

        [Header("Float")]
        [SerializeField] private float waterLevel = 0f;

        private readonly List<FloatingResource> _alive = new();
        private ResourcePool _pool;
        private float _timer;
        private float _totalSpawnWeight;

        private void Awake()
        {
            if (target == null)
            {
                target = transform;
            }

            _pool = new ResourcePool(transform, prewarmPerType);
            RecalculateSpawnWeights();

            if (spawnEntries == null || spawnEntries.Length == 0 || _totalSpawnWeight <= 0f)
            {
                Debug.LogWarning(
                    $"{nameof(ResourceSpawner)} on '{name}' has no Spawn Entries — using grey cube fallbacks. " +
                    "Assign kit prefabs on the ResourceSpawner component (not ResourceCollector).",
                    this);
            }
        }

        private void OnValidate()
        {
            RecalculateSpawnWeights();
        }

        private void RecalculateSpawnWeights()
        {
            _totalSpawnWeight = 0f;
            if (spawnEntries == null)
            {
                return;
            }

            foreach (ResourceSpawnEntry entry in spawnEntries)
            {
                if (entry.visualPrefab != null && entry.spawnWeight > 0f)
                {
                    _totalSpawnWeight += entry.spawnWeight;
                }
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= spawnInterval)
            {
                _timer = 0f;
                TrySpawn();
            }

            CullOutOfBounds();
        }

        private void TrySpawn()
        {
            _alive.RemoveAll(r => r == null || !r.IsAvailable);
            if (_alive.Count >= maxAlive)
            {
                return;
            }

            if (!TryPickSpawnEntry(out ResourceSpawnEntry entry))
            {
                return;
            }

            Vector3 drift = GetDriftVelocity();

            Vector3 flow = drift.sqrMagnitude > 0.0001f ? drift.normalized : Vector3.forward;
            Vector3 lateral = Vector3.Cross(Vector3.up, flow);

            Vector3 spawnPos = target.position
                - flow * spawnRadius
                + lateral * Random.Range(-spawnRadius, spawnRadius);
            spawnPos.y = waterLevel;

            FloatingResource resource = _pool.Acquire(entry.visualPrefab, entry.resourceType);
            resource.transform.position = spawnPos;
            resource.Initialize(entry.resourceType, drift);
            _alive.Add(resource);
        }

        private bool TryPickSpawnEntry(out ResourceSpawnEntry picked)
        {
            picked = default;

            if (spawnEntries == null || spawnEntries.Length == 0 || _totalSpawnWeight <= 0f)
            {
                picked = new ResourceSpawnEntry
                {
                    visualPrefab = null,
                    resourceType = RandomFallbackType(),
                    spawnWeight = 1f
                };
                return true;
            }

            float roll = Random.Range(0f, _totalSpawnWeight);
            float cumulative = 0f;

            foreach (ResourceSpawnEntry entry in spawnEntries)
            {
                if (entry.visualPrefab == null || entry.spawnWeight <= 0f)
                {
                    continue;
                }

                cumulative += entry.spawnWeight;
                if (roll <= cumulative)
                {
                    picked = entry;
                    return true;
                }
            }

            return false;
        }

        private static ResourceType RandomFallbackType()
        {
            System.Array values = System.Enum.GetValues(typeof(ResourceType));
            return (ResourceType)values.GetValue(Random.Range(0, values.Length));
        }

        private void CullOutOfBounds()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                FloatingResource resource = _alive[i];
                if (resource == null || !resource.gameObject.activeInHierarchy)
                {
                    _alive.RemoveAt(i);
                    continue;
                }

                Vector3 flat = resource.transform.position - target.position;
                flat.y = 0f;
                if (flat.magnitude > despawnRadius)
                {
                    resource.Recycle();
                    _alive.RemoveAt(i);
                }
            }
        }

        private Vector3 GetDriftVelocity()
        {
            Vector3 direction = GetDriftDirection();
            return direction * driftSpeed;
        }

        private Vector3 GetDriftDirection()
        {
            if (deriveCurrentFromTargetForward && target != null)
            {
                // Spawn upstream at target.forward * radius; drift travels toward the raft.
                Vector3 towardRaft = -target.forward;
                towardRaft.y = 0f;
                if (towardRaft.sqrMagnitude > 0.0001f)
                {
                    return towardRaft.normalized;
                }
            }

            Vector3 manual = currentDirection;
            manual.y = 0f;
            return manual.sqrMagnitude > 0.0001f ? manual.normalized : Vector3.back;
        }
    }
}
