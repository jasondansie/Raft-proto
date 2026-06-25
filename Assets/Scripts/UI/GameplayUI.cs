using RaftProto.Core;
using RaftProto.Items;
using UnityEngine;

namespace RaftProto.UI
{
    /// <summary>
    /// Builds the gameplay overlay: hotbar, inventory bag, and craft panel.
    /// </summary>
    public class GameplayUI : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private Hotbar hotbar;
        [SerializeField] private ItemCatalog itemCatalog;
        [SerializeField] private RecipeCatalog recipeCatalog;
        [SerializeField] private CraftingSystem craftingSystem;

        private HotbarPanelUI _hotbarPanel;
        private InventoryBagUI _inventoryBag;
        private CraftPanelUI _craftPanel;
        private ToolStatusUI _toolStatus;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<Inventory>();
            }

            if (hotbar == null)
            {
                hotbar = GetComponent<Hotbar>();
            }

            if (craftingSystem == null)
            {
                craftingSystem = GetComponent<CraftingSystem>();
            }

            IPlayerToolState toolState = GetComponent<IPlayerToolState>();
            IHotbarSlotSelector slotSelector = GetComponent<IHotbarSlotSelector>();

            Canvas canvas = UiFactory.CreateOverlayCanvas(transform, "GameplayUI_Canvas");

            GameObject hotbarHost = new GameObject("HotbarPanelHost");
            hotbarHost.transform.SetParent(canvas.transform, false);
            _hotbarPanel = hotbarHost.AddComponent<HotbarPanelUI>();
            _hotbarPanel.Initialize(canvas.transform, hotbar, itemCatalog, toolState);

            _hotbarPanel.SlotClicked += index =>
            {
                if (_inventoryBag != null && _inventoryBag.IsOpen)
                {
                    return;
                }

                slotSelector?.SelectSlot(index);
            };

            GameObject craftHost = new GameObject("CraftPanelHost");
            craftHost.transform.SetParent(canvas.transform, false);
            _craftPanel = craftHost.AddComponent<CraftPanelUI>();
            _craftPanel.Initialize(canvas.transform, craftingSystem, recipeCatalog, itemCatalog, inventory);

            GameObject bagHost = new GameObject("InventoryBagHost");
            bagHost.transform.SetParent(canvas.transform, false);
            _inventoryBag = bagHost.AddComponent<InventoryBagUI>();
            _inventoryBag.Initialize(canvas.transform, inventory, hotbar, itemCatalog, _hotbarPanel, _craftPanel);

            if (toolState != null)
            {
                GameObject toolHost = new GameObject("ToolStatusHost");
                toolHost.transform.SetParent(canvas.transform, false);
                _toolStatus = toolHost.AddComponent<ToolStatusUI>();
                _toolStatus.Initialize(canvas.transform, toolState, hotbar, itemCatalog);
            }
        }

        private void OnEnable()
        {
            _hotbarPanel?.BindEvents();
            _inventoryBag?.BindEvents();
            _craftPanel?.BindEvents();
        }

        private void OnDisable()
        {
            _hotbarPanel?.UnbindEvents();
            _inventoryBag?.UnbindEvents();
            _craftPanel?.UnbindEvents();
        }
    }
}
