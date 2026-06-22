using System;
using UnityEngine;

namespace RaftProto.Resources
{
    [Serializable]
    public struct ResourceSpawnEntry
    {
        [Tooltip("Kit visual prefab (FloatingResource is added at runtime if missing).")]
        public GameObject visualPrefab;

        public ResourceType resourceType;

        [Min(0f)]
        public float spawnWeight;
    }
}
