using System.Collections.Generic;
using UnityEngine;

namespace RaftProto.Items
{
    /// <summary>
    /// Registry of all <see cref="ItemDefinition"/> assets. Used for stack limits, display names, and UI.
    /// </summary>
    [CreateAssetMenu(menuName = "RaftProto/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items;

        private Dictionary<string, ItemDefinition> _lookup;

        public IReadOnlyList<ItemDefinition> Items => items;

        public bool TryGetDefinition(string itemId, out ItemDefinition definition)
        {
            BuildLookup();
            return _lookup.TryGetValue(itemId, out definition);
        }

        public int GetMaxStack(string itemId)
        {
            return TryGetDefinition(itemId, out ItemDefinition definition) ? definition.MaxStackSize : 99;
        }

        private void BuildLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, ItemDefinition>();
            if (items == null)
            {
                return;
            }

            foreach (ItemDefinition item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                _lookup[item.ItemId] = item;
            }
        }

        private void OnEnable()
        {
            _lookup = null;
        }
    }
}
