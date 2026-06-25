using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaftProto.Items
{
    [Serializable]
    public struct HotbarSlotBinding
    {
        public string ItemId;
        public bool IsPinned;
    }

    /// <summary>
    /// Quick-access row bound to the same item stacks as <see cref="Inventory"/>.
    /// Unpinned slots auto-fill in pickup order; pinned slots stay on a chosen item type.
    /// </summary>
    public class Hotbar : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private int slotCount = 8;

        private HotbarSlotBinding[] _slots;
        private readonly List<string> _acquisitionOrder = new();

        public event Action Changed;

        public int SlotCount => slotCount;
        public IReadOnlyList<string> AcquisitionOrder => _acquisitionOrder;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<Inventory>();
            }

            _slots = new HotbarSlotBinding[slotCount];
        }

        private void OnEnable()
        {
            if (inventory == null)
            {
                return;
            }

            inventory.ItemFirstAcquired += OnItemFirstAcquired;
            inventory.Changed += OnInventoryChanged;
        }

        private void OnDisable()
        {
            if (inventory == null)
            {
                return;
            }

            inventory.ItemFirstAcquired -= OnItemFirstAcquired;
            inventory.Changed -= OnInventoryChanged;
        }

        public HotbarSlotBinding GetSlot(int index)
        {
            if (index < 0 || index >= slotCount)
            {
                return default;
            }

            return _slots[index];
        }

        public string GetAssignedItemId(int index)
        {
            return GetSlot(index).ItemId;
        }

        public bool IsSlotPinned(int index)
        {
            return GetSlot(index).IsPinned;
        }

        public int GetDisplayCount(int index)
        {
            string itemId = GetAssignedItemId(index);
            if (string.IsNullOrWhiteSpace(itemId) || inventory == null)
            {
                return 0;
            }

            return inventory.GetCount(itemId);
        }

        public void PinItemToSlot(int index, string itemId)
        {
            if (index < 0 || index >= slotCount || string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            if (inventory == null || inventory.GetCount(itemId) <= 0)
            {
                return;
            }

            _slots[index].ItemId = itemId;
            _slots[index].IsPinned = true;
            RemoveFromOtherUnpinnedSlots(itemId, index);
            Changed?.Invoke();
        }

        public void ClearSlot(int index)
        {
            if (index < 0 || index >= slotCount)
            {
                return;
            }

            _slots[index] = default;
            AutoFillSlots();
            Changed?.Invoke();
        }

        private void OnItemFirstAcquired(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || _acquisitionOrder.Contains(itemId))
            {
                return;
            }

            _acquisitionOrder.Add(itemId);
            TryAutoAssign(itemId);
        }

        private void OnInventoryChanged()
        {
            PruneEmptyAssignments();
            AutoFillSlots();
            Changed?.Invoke();
        }

        private void PruneEmptyAssignments()
        {
            for (int i = 0; i < slotCount; i++)
            {
                string itemId = _slots[i].ItemId;
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                if (inventory.GetCount(itemId) <= 0 && !_slots[i].IsPinned)
                {
                    _slots[i] = default;
                }
            }
        }

        private void AutoFillSlots()
        {
            foreach (string itemId in _acquisitionOrder)
            {
                if (inventory.GetCount(itemId) > 0 && !IsItemAssigned(itemId))
                {
                    TryAutoAssign(itemId);
                }
            }
        }

        private void TryAutoAssign(string itemId)
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (_slots[i].IsPinned || !string.IsNullOrWhiteSpace(_slots[i].ItemId))
                {
                    continue;
                }

                _slots[i].ItemId = itemId;
                _slots[i].IsPinned = false;
                return;
            }
        }

        private bool IsItemAssigned(string itemId)
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (_slots[i].ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveFromOtherUnpinnedSlots(string itemId, int keepIndex)
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (i == keepIndex || _slots[i].IsPinned)
                {
                    continue;
                }

                if (_slots[i].ItemId == itemId)
                {
                    _slots[i] = default;
                }
            }
        }
    }
}
