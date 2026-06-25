using System.Collections.Generic;
using RaftProto.Core;
using RaftProto.Items;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RaftProto.UI
{
    /// <summary>
    /// Raft-style inventory panel: wood board, 5-column grid, Tab to toggle.
    /// Click an item, then click a hotbar slot to pin it.
    /// </summary>
    public class InventoryBagUI : MonoBehaviour
    {
        private const int GridColumns = 5;
        private const int GridRows = 3;

        [SerializeField] private Key toggleKey = Key.Tab;

        private Inventory _inventory;
        private Hotbar _hotbar;
        private ItemCatalog _catalog;
        private HotbarPanelUI _hotbarPanel;
        private CraftPanelUI _craftPanel;

        private GameObject _panelRoot;
        private Text _hintLabel;
        private readonly List<BagSlotView> _slots = new();
        private string _selectedItemId;
        private bool _isOpen;

        private struct BagSlotView
        {
            public Button Button;
            public Image Icon;
            public Text CountLabel;
            public Image Selection;
            public string ItemId;
        }

        public bool IsOpen => _isOpen;

        public void Initialize(
            Transform canvasRoot,
            Inventory inventory,
            Hotbar hotbar,
            ItemCatalog catalog,
            HotbarPanelUI hotbarPanel,
            CraftPanelUI craftPanel)
        {
            _inventory = inventory;
            _hotbar = hotbar;
            _catalog = catalog;
            _hotbarPanel = hotbarPanel;
            _craftPanel = craftPanel;
            BuildPanel(canvasRoot);
            ShowEmptySlots();
            _isOpen = false;
            _panelRoot.SetActive(false);
            _craftPanel?.SetVisible(false);
        }

        public void BindEvents()
        {
            if (_inventory != null)
            {
                _inventory.Changed += Refresh;
            }

            if (_hotbarPanel != null)
            {
                _hotbarPanel.SlotClicked += OnHotbarSlotClicked;
            }
        }

        public void UnbindEvents()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
            }

            if (_hotbarPanel != null)
            {
                _hotbarPanel.SlotClicked -= OnHotbarSlotClicked;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                SetOpen(!_isOpen);
            }
        }

        private void BuildPanel(Transform canvasRoot)
        {
            _panelRoot = new GameObject("InventoryBagPanel");
            _panelRoot.transform.SetParent(canvasRoot, false);

            RectTransform panelRect = _panelRoot.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(280f, 120f);
            panelRect.sizeDelta = new Vector2(430f, 500f);

            Image border = UiFactory.CreateImage(_panelRoot.transform, "Border", RaftUiTheme.PanelBorder);
            border.raycastTarget = true;
            StretchRect(border.rectTransform, 0f);

            Image panelBackground = UiFactory.CreateImage(_panelRoot.transform, "Background", RaftUiTheme.PanelFill);
            panelBackground.raycastTarget = false;
            StretchRect(panelBackground.rectTransform, 6f);

            Text title = UiFactory.CreateText(_panelRoot.transform, "Title", 30, TextAnchor.UpperCenter);
            title.text = "INVENTORY";
            title.fontStyle = FontStyle.Bold;
            title.color = RaftUiTheme.TitleText;
            title.raycastTarget = false;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -18f);
            titleRect.sizeDelta = new Vector2(-32f, 40f);

            Image divider = UiFactory.CreateImage(_panelRoot.transform, "Divider", RaftUiTheme.Divider);
            divider.raycastTarget = false;
            RectTransform dividerRect = divider.rectTransform;
            dividerRect.anchorMin = new Vector2(0f, 1f);
            dividerRect.anchorMax = new Vector2(1f, 1f);
            dividerRect.pivot = new Vector2(0.5f, 1f);
            dividerRect.anchoredPosition = new Vector2(0f, -58f);
            dividerRect.sizeDelta = new Vector2(-40f, 3f);

            GameObject gridObject = new GameObject("ItemGrid");
            gridObject.transform.SetParent(_panelRoot.transform, false);
            RectTransform gridRect = gridObject.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 1f);
            gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = new Vector2(0f, -72f);
            gridRect.sizeDelta = new Vector2(370f, 330f);

            GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;
            grid.cellSize = new Vector2(68f, 68f);
            grid.spacing = new Vector2(8f, 8f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(4, 4, 4, 4);

            int slotCount = GridColumns * GridRows;
            for (int i = 0; i < slotCount; i++)
            {
                _slots.Add(CreateBagSlot(gridObject.transform, i));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

            _hintLabel = UiFactory.CreateText(_panelRoot.transform, "Hint", 16, TextAnchor.LowerCenter);
            _hintLabel.text = "Tab to close · click item, then hotbar slot to assign";
            _hintLabel.color = RaftUiTheme.BodyText;
            _hintLabel.raycastTarget = false;
            RectTransform hintRect = _hintLabel.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 14f);
            hintRect.sizeDelta = new Vector2(-24f, 36f);
        }

        private BagSlotView CreateBagSlot(Transform parent, int index)
        {
            GameObject slotObject = new GameObject($"BagSlot_{index}");
            slotObject.transform.SetParent(parent, false);

            RectTransform slotRect = slotObject.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(68f, 68f);

            Image slotBorder = slotObject.AddComponent<Image>();
            slotBorder.sprite = UiSprites.White;
            slotBorder.color = RaftUiTheme.SlotBorder;
            slotBorder.raycastTarget = true;

            Image slotFill = UiFactory.CreateImage(slotObject.transform, "Fill", RaftUiTheme.SlotFill);
            slotFill.raycastTarget = false;
            StretchRect(slotFill.rectTransform, 3f);

            Image selection = UiFactory.CreateImage(slotObject.transform, "Selection", RaftUiTheme.SlotSelected);
            selection.raycastTarget = false;
            StretchRect(selection.rectTransform, 1f);
            selection.enabled = false;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(slotObject.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.enabled = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Text countLabel = UiFactory.CreateText(slotObject.transform, "Count", 18, TextAnchor.UpperLeft);
            countLabel.fontStyle = FontStyle.Bold;
            countLabel.color = RaftUiTheme.TitleText;
            countLabel.raycastTarget = false;
            RectTransform countRect = countLabel.rectTransform;
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(6f, 0f);
            countRect.offsetMax = new Vector2(-4f, -4f);

            Button button = slotObject.AddComponent<Button>();
            button.targetGraphic = slotBorder;
            int capturedIndex = index;
            button.onClick.AddListener(() => OnBagSlotClicked(capturedIndex));

            return new BagSlotView
            {
                Button = button,
                Icon = icon,
                CountLabel = countLabel,
                Selection = selection
            };
        }

        private void Refresh()
        {
            if (_inventory == null || _hotbar == null)
            {
                return;
            }

            var itemIds = new List<string>();
            foreach (string itemId in GetDisplayOrder())
            {
                if (_inventory.GetCount(itemId) > 0)
                {
                    itemIds.Add(itemId);
                }
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < itemIds.Count)
                {
                    ApplyItemToSlot(i, itemIds[i]);
                }
                else
                {
                    ClearSlotAt(i);
                }
            }

            if (!string.IsNullOrWhiteSpace(_selectedItemId) && _inventory.GetCount(_selectedItemId) <= 0)
            {
                _selectedItemId = null;
            }

            UpdateSelectionVisuals();
            UpdateHint();
        }

        private void ApplyItemToSlot(int index, string itemId)
        {
            BagSlotView slot = _slots[index];
            slot.ItemId = itemId;
            int count = _inventory.GetCount(itemId);

            if (_catalog != null && _catalog.TryGetDefinition(itemId, out ItemDefinition definition))
            {
                slot.Icon.sprite = definition.Icon != null ? definition.Icon : UiSprites.White;
                slot.Icon.color = definition.Icon != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
            }
            else
            {
                slot.Icon.sprite = UiSprites.White;
                slot.Icon.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            }

            slot.Icon.enabled = true;
            slot.CountLabel.text = count > 1 ? count.ToString() : string.Empty;
            slot.Button.interactable = true;
            _slots[index] = slot;
        }

        private void ClearSlotAt(int index)
        {
            BagSlotView slot = _slots[index];
            slot.ItemId = null;
            slot.Icon.enabled = false;
            slot.CountLabel.text = string.Empty;
            slot.Selection.enabled = false;
            slot.Button.interactable = false;
            _slots[index] = slot;
        }

        private void ShowEmptySlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                ClearSlotAt(i);
            }
        }

        private IEnumerable<string> GetDisplayOrder()
        {
            var seen = new HashSet<string>();

            foreach (string itemId in _hotbar.AcquisitionOrder)
            {
                if (!string.IsNullOrWhiteSpace(itemId) && seen.Add(itemId))
                {
                    yield return itemId;
                }
            }

            foreach (KeyValuePair<string, int> stack in _inventory.Stacks)
            {
                if (stack.Value > 0 && seen.Add(stack.Key))
                {
                    yield return stack.Key;
                }
            }
        }

        private void OnBagSlotClicked(int index)
        {
            if (index < 0 || index >= _slots.Count)
            {
                return;
            }

            string itemId = _slots[index].ItemId;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            SelectItem(itemId);
        }

        private void SelectItem(string itemId)
        {
            _selectedItemId = itemId;
            UpdateSelectionVisuals();
            UpdateHint();
        }

        private void OnHotbarSlotClicked(int slotIndex)
        {
            if (!_isOpen || string.IsNullOrWhiteSpace(_selectedItemId) || _hotbar == null)
            {
                return;
            }

            _hotbar.PinItemToSlot(slotIndex, _selectedItemId);
            _selectedItemId = null;
            UpdateSelectionVisuals();
            UpdateHint();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                BagSlotView slot = _slots[i];
                slot.Selection.enabled = !string.IsNullOrWhiteSpace(slot.ItemId) && slot.ItemId == _selectedItemId;
                _slots[i] = slot;
            }
        }

        private void UpdateHint()
        {
            if (_hintLabel == null)
            {
                return;
            }

            _hintLabel.text = string.IsNullOrWhiteSpace(_selectedItemId)
                ? "Tab to close · click item, then hotbar slot to assign"
                : $"Selected: {ResolveDisplayName(_selectedItemId)} · click a hotbar slot";
        }

        private string ResolveDisplayName(string itemId)
        {
            if (_catalog != null && _catalog.TryGetDefinition(itemId, out ItemDefinition definition))
            {
                return definition.DisplayName;
            }

            return itemId;
        }

        private void SetOpen(bool open)
        {
            if (_isOpen == open)
            {
                return;
            }

            _isOpen = open;
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(open);
            }

            _craftPanel?.SetVisible(open);

            if (open)
            {
                Refresh();
                GameplayUiGate.SetPanelOpen(true);
            }
            else
            {
                _selectedItemId = null;
                UpdateHint();
                GameplayUiGate.SetPanelOpen(false);
            }
        }

        private static void StretchRect(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
