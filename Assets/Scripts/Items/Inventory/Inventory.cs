using System.Collections.Generic;
using UnityEngine;

namespace RaftProto.Items
{
    /// <summary>
    /// Stores item ID → count stacks for one owner (player). Server-authoritative in multiplayer later;
    /// for now local-only on the player object.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private ItemCatalog catalog;

        private readonly Dictionary<string, int> _stacks = new();

        public event System.Action Changed;

        /// <summary>Fired when an item type goes from zero to at least one in the bag.</summary>
        public event System.Action<string> ItemFirstAcquired;

        public IReadOnlyDictionary<string, int> Stacks => _stacks;

        public int GetCount(string itemId)
        {
            return _stacks.TryGetValue(itemId, out int count) ? count : 0;
        }

        public bool HasItem(string itemId, int amount)
        {
            return amount <= 0 || GetCount(itemId) >= amount;
        }

        public bool HasIngredients(RecipeIngredient[] ingredients)
        {
            if (ingredients == null)
            {
                return true;
            }

            foreach (RecipeIngredient ingredient in ingredients)
            {
                if (!HasItem(ingredient.ItemId, ingredient.Count))
                {
                    return false;
                }
            }

            return true;
        }

        public bool AddItem(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            _stacks.TryGetValue(itemId, out int current);
            int maxStack = catalog != null ? catalog.GetMaxStack(itemId) : 99;
            int updated = Mathf.Min(current + amount, maxStack);

            if (updated == current)
            {
                return false;
            }

            bool firstAcquire = current <= 0 && updated > 0;
            _stacks[itemId] = updated;

            if (firstAcquire)
            {
                ItemFirstAcquired?.Invoke(itemId);
            }

            Changed?.Invoke();
            return true;
        }

        public bool TryRemoveItem(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            if (!_stacks.TryGetValue(itemId, out int current) || current < amount)
            {
                return false;
            }

            int updated = current - amount;
            if (updated <= 0)
            {
                _stacks.Remove(itemId);
            }
            else
            {
                _stacks[itemId] = updated;
            }

            Changed?.Invoke();
            return true;
        }
    }
}
