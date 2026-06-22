using System.Collections.Generic;
using UnityEngine;

namespace RaftProto.Resources
{
    /// <summary>
    /// Simple stack pool for <see cref="FloatingResource"/> instances keyed by visual prefab.
    /// Avoids Instantiate/Destroy churn while resources drift and get collected.
    /// </summary>
    public class ResourcePool
    {
        private readonly Transform _parent;
        private readonly int _prewarmCount;
        private readonly Dictionary<GameObject, Stack<FloatingResource>> _stacks = new();
        private readonly Dictionary<FloatingResource, GameObject> _prefabByInstance = new();

        public ResourcePool(Transform parent, int prewarmCount)
        {
            _parent = parent;
            _prewarmCount = Mathf.Max(0, prewarmCount);
        }

        public FloatingResource Acquire(GameObject visualPrefab, ResourceType type)
        {
            GameObject prefabKey = visualPrefab != null ? visualPrefab : GetFallbackPrefabKey();

            if (!_stacks.TryGetValue(prefabKey, out Stack<FloatingResource> stack))
            {
                stack = new Stack<FloatingResource>();
                _stacks[prefabKey] = stack;

                for (int i = 0; i < _prewarmCount; i++)
                {
                    stack.Push(CreateInstance(prefabKey, type));
                }
            }

            FloatingResource resource = stack.Count > 0 ? stack.Pop() : CreateInstance(prefabKey, type);
            resource.gameObject.SetActive(true);
            resource.PrepareForSpawn(type);
            return resource;
        }

        public void Release(FloatingResource resource)
        {
            if (resource == null)
            {
                return;
            }

            if (!_prefabByInstance.TryGetValue(resource, out GameObject prefabKey))
            {
                Object.Destroy(resource.gameObject);
                return;
            }

            resource.gameObject.SetActive(false);
            resource.transform.SetParent(_parent, false);

            if (!_stacks.TryGetValue(prefabKey, out Stack<FloatingResource> stack))
            {
                stack = new Stack<FloatingResource>();
                _stacks[prefabKey] = stack;
            }

            stack.Push(resource);
        }

        private FloatingResource CreateInstance(GameObject prefabKey, ResourceType type)
        {
            GameObject instance;

            if (prefabKey == GetFallbackPrefabKey())
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.name = "FloatingResource_Fallback";
                instance.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            }
            else
            {
                instance = Object.Instantiate(prefabKey, _parent);
                instance.name = prefabKey.name;
            }

            FloatingResourceSetup.Configure(instance, type);
            FloatingResource resource = instance.GetComponent<FloatingResource>();
            resource.BindPool(this);
            instance.SetActive(false);
            _prefabByInstance[resource] = prefabKey;
            return resource;
        }

        private static GameObject _fallbackPrefabKey;

        private static GameObject GetFallbackPrefabKey()
        {
            if (_fallbackPrefabKey == null)
            {
                _fallbackPrefabKey = new GameObject("ResourcePool_FallbackKey");
                _fallbackPrefabKey.hideFlags = HideFlags.HideAndDontSave;
            }

            return _fallbackPrefabKey;
        }
    }
}
