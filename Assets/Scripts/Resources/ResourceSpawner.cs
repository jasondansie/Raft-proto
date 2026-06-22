using System.Collections.Generic;
using UnityEngine;

namespace RaftProto.Resources
{
    /// <summary>
    /// Spawns drifting resources upstream of a target and sends them across on a shared current.
    /// In multiplayer this is server-only and the spawned objects are networked; running it on
    /// every client would create duplicate/ghost resources. Local for now.
    /// </summary>
    public class ResourceSpawner : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Resources spawn around and drift past this point. Defaults to this object.")]
        [SerializeField] private Transform target;

        [Header("Prefabs")]
        [Tooltip("Optional resource prefabs (each needs a FloatingResource + collider). If empty, a primitive cube is used.")]
        [SerializeField] private FloatingResource[] resourcePrefabs;

        [Header("Spawning")]
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int maxAlive = 20;
        [Tooltip("Distance from target where resources appear (upstream of the current).")]
        [SerializeField] private float spawnRadius = 30f;
        [Tooltip("Distance from target beyond which resources despawn.")]
        [SerializeField] private float despawnRadius = 45f;

        [Header("Current")]
        [Tooltip("World-space direction the resources drift along.")]
        [SerializeField] private Vector3 currentDirection = new Vector3(-1f, 0f, -0.3f);
        [SerializeField] private float driftSpeed = 1.5f;

        [Header("Float")]
        [SerializeField] private float waterLevel = 0f;

        private readonly List<FloatingResource> _alive = new();
        private float _timer;

        private void Awake()
        {
            if (target == null)
            {
                target = transform;
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
            _alive.RemoveAll(r => r == null);
            if (_alive.Count >= maxAlive)
            {
                return;
            }

            Vector3 drift = currentDirection.sqrMagnitude > 0.0001f
                ? currentDirection.normalized * driftSpeed
                : Vector3.zero;

            Vector3 flow = drift.sqrMagnitude > 0.0001f ? drift.normalized : Vector3.forward;
            Vector3 lateral = Vector3.Cross(Vector3.up, flow);

            // Start upstream and offset sideways so it drifts across the target area.
            Vector3 spawnPos = target.position
                - flow * spawnRadius
                + lateral * Random.Range(-spawnRadius, spawnRadius);
            spawnPos.y = waterLevel;

            FloatingResource resource = CreateResource();
            resource.transform.position = spawnPos;
            resource.Initialize(RandomType(), drift);
            _alive.Add(resource);
        }

        private FloatingResource CreateResource()
        {
            if (resourcePrefabs != null && resourcePrefabs.Length > 0)
            {
                FloatingResource prefab = resourcePrefabs[Random.Range(0, resourcePrefabs.Length)];
                if (prefab != null)
                {
                    return Instantiate(prefab);
                }
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "FloatingResource";
            go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            // Trigger so it can be detected by the collector but doesn't shove the player or raft.
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            return go.AddComponent<FloatingResource>();
        }

        private static ResourceType RandomType()
        {
            System.Array values = System.Enum.GetValues(typeof(ResourceType));
            return (ResourceType)values.GetValue(Random.Range(0, values.Length));
        }

        private void CullOutOfBounds()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                FloatingResource resource = _alive[i];
                if (resource == null)
                {
                    _alive.RemoveAt(i);
                    continue;
                }

                Vector3 flat = resource.transform.position - target.position;
                flat.y = 0f;
                if (flat.magnitude > despawnRadius)
                {
                    Destroy(resource.gameObject);
                    _alive.RemoveAt(i);
                }
            }
        }
    }
}
