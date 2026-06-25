using RaftProto.Core;
using RaftProto.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RaftProto.Player
{
    /// <summary>
    /// Selects the active tool from the selected hotbar slot. Tools stay in inventory while equipped.
    /// </summary>
    [AddComponentMenu("RaftProto/Player Tool Controller")]
    public class PlayerToolController : MonoBehaviour, IPlayerToolState, IHotbarSlotSelector
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private Hotbar hotbar;

        private ISwimStateProvider _swimState;
        private int _selectedHotbarSlotIndex;

        public EquippedToolType ActiveTool => ResolveActiveTool();
        public bool IsHammerActive => ActiveTool == EquippedToolType.Hammer;
        public bool IsAxeActive => ActiveTool == EquippedToolType.Axe;
        public int SelectedHotbarSlotIndex => _selectedHotbarSlotIndex;

        public void SelectSlot(int index)
        {
            if (hotbar == null || index < 0 || index >= hotbar.SlotCount)
            {
                return;
            }

            _selectedHotbarSlotIndex = index;
        }

        private void Awake()
        {
            _swimState = GetComponent<ISwimStateProvider>();

            if (inventory == null)
            {
                inventory = GetComponent<Inventory>();
            }

            if (hotbar == null)
            {
                hotbar = GetComponent<Hotbar>();
            }
        }

        private void Update()
        {
            if (_swimState != null && _swimState.IsSwimming)
            {
                return;
            }

            if (Keyboard.current == null || hotbar == null)
            {
                return;
            }

            int slotCount = Mathf.Min(hotbar.SlotCount, 9);
            for (int i = 0; i < slotCount; i++)
            {
                Key key = Key.Digit1 + i;
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    _selectedHotbarSlotIndex = i;
                }
            }
        }

        private EquippedToolType ResolveActiveTool()
        {
            if (hotbar == null || inventory == null)
            {
                return EquippedToolType.None;
            }

            string itemId = hotbar.GetAssignedItemId(_selectedHotbarSlotIndex);
            if (string.IsNullOrWhiteSpace(itemId) || inventory.GetCount(itemId) <= 0)
            {
                return EquippedToolType.None;
            }

            if (itemId == ItemIds.Hammer)
            {
                return EquippedToolType.Hammer;
            }

            if (itemId == ItemIds.Axe)
            {
                return EquippedToolType.Axe;
            }

            return EquippedToolType.None;
        }
    }
}
