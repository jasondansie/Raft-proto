using RaftProto.Items;
using UnityEngine;

namespace RaftProto.UI
{
    /// <summary>
    /// Builds the gameplay overlay: inventory bar + craft panel. Add this to the player instead of InventoryHUD.
    /// </summary>
    public class GameplayUI : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private ItemCatalog itemCatalog;
        [SerializeField] private RecipeCatalog recipeCatalog;
        [SerializeField] private CraftingSystem craftingSystem;

        private InventoryPanelUI _inventoryPanel;
        private CraftPanelUI _craftPanel;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<Inventory>();
            }

            if (craftingSystem == null)
            {
                craftingSystem = GetComponent<CraftingSystem>();
            }

            Canvas canvas = UiFactory.CreateOverlayCanvas(transform, "GameplayUI_Canvas");

            GameObject inventoryHost = new GameObject("InventoryPanelHost");
            inventoryHost.transform.SetParent(canvas.transform, false);
            _inventoryPanel = inventoryHost.AddComponent<InventoryPanelUI>();
            _inventoryPanel.Initialize(canvas.transform, inventory, itemCatalog);

            GameObject craftHost = new GameObject("CraftPanelHost");
            craftHost.transform.SetParent(canvas.transform, false);
            _craftPanel = craftHost.AddComponent<CraftPanelUI>();
            _craftPanel.Initialize(canvas.transform, craftingSystem, recipeCatalog, itemCatalog, inventory);
        }

        private void OnEnable()
        {
            _inventoryPanel?.BindEvents();
            _craftPanel?.BindEvents();
        }

        private void OnDisable()
        {
            _inventoryPanel?.UnbindEvents();
            _craftPanel?.UnbindEvents();
        }
    }
}
