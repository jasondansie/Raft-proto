using RaftProto.Items;
using UnityEngine;

namespace RaftProto.Resources
{
    /// <summary>
    /// Maps floating resource pickups into inventory stacks via stable item IDs.
    /// In multiplayer the server validates pickup then credits the requesting player's inventory.
    /// </summary>
    public class ResourceInventoryBridge : MonoBehaviour
    {
        [SerializeField] private ResourceCollector collector;
        [SerializeField] private Inventory inventory;

        private void Awake()
        {
            if (collector == null)
            {
                collector = GetComponent<ResourceCollector>();
            }

            if (inventory == null)
            {
                inventory = GetComponent<Inventory>();
            }
        }

        private void OnEnable()
        {
            if (collector != null)
            {
                collector.Collected += OnCollected;
            }
        }

        private void OnDisable()
        {
            if (collector != null)
            {
                collector.Collected -= OnCollected;
            }
        }

        private void OnCollected(ResourceType type)
        {
            if (inventory == null)
            {
                return;
            }

            if (!TryMapResourceType(type, out string itemId))
            {
                Debug.LogWarning($"{nameof(ResourceInventoryBridge)}: no item ID mapped for {type}.", this);
                return;
            }

            inventory.AddItem(itemId, 1);
        }

        private static bool TryMapResourceType(ResourceType type, out string itemId)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    itemId = ItemIds.Wood;
                    return true;
                case ResourceType.Plastic:
                    itemId = ItemIds.Plastic;
                    return true;
                case ResourceType.Scrap:
                    itemId = ItemIds.Scrap;
                    return true;
                default:
                    itemId = null;
                    return false;
            }
        }
    }
}
