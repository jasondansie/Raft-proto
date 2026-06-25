using System.Collections.Generic;
using RaftProto.Items;
using UnityEngine;
using UnityEngine.UI;

namespace RaftProto.UI
{
    /// <summary>
    /// Bottom inventory bar with icon slots and stack counts. Local-only; binds to the owner's inventory.
    /// </summary>
    public class InventoryPanelUI : MonoBehaviour
    {
        private Inventory _inventory;
        private ItemCatalog _catalog;
        private readonly List<SlotView> _slots = new();

        private struct SlotView
        {
            public ItemDefinition Item;
            public Image Icon;
            public Text CountLabel;
            public Image Background;
        }

        public void Initialize(Transform canvasRoot, Inventory inventory, ItemCatalog catalog)
        {
            _inventory = inventory;
            _catalog = catalog;
            BuildPanel(canvasRoot);
            Refresh();
        }

        public void BindEvents()
        {
            if (_inventory != null)
            {
                _inventory.Changed += Refresh;
            }
        }

        public void UnbindEvents()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
            }
        }

        private void BuildPanel(Transform canvasRoot)
        {
            GameObject barObject = new GameObject("InventoryBar");
            barObject.transform.SetParent(canvasRoot, false);

            RectTransform barRect = barObject.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 36f);

            HorizontalLayoutGroup layout = barObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (_catalog == null || _catalog.Items == null)
            {
                return;
            }

            foreach (ItemDefinition item in _catalog.Items)
            {
                if (item == null)
                {
                    continue;
                }

                _slots.Add(CreateSlot(barObject.transform, item));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
        }

        private SlotView CreateSlot(Transform parent, ItemDefinition item)
        {
            GameObject slotObject = new GameObject($"Slot_{item.ItemId}");
            slotObject.transform.SetParent(parent, false);

            RectTransform slotRect = slotObject.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(96f, 96f);

            Image background = UiFactory.CreateImage(slotObject.transform, "Background", new Color(0f, 0f, 0f, 0.55f));
            background.raycastTarget = false;
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(slotObject.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = item.Icon != null ? item.Icon : UiSprites.White;
            icon.color = item.Icon != null ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Text countLabel = UiFactory.CreateText(slotObject.transform, "Count", 22, TextAnchor.LowerRight);
            countLabel.fontStyle = FontStyle.Bold;
            RectTransform countRect = countLabel.rectTransform;
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(4f, 4f);
            countRect.offsetMax = new Vector2(-6f, -4f);

            return new SlotView
            {
                Item = item,
                Icon = icon,
                CountLabel = countLabel,
                Background = background
            };
        }

        private void Refresh()
        {
            if (_inventory == null)
            {
                return;
            }

            foreach (SlotView slot in _slots)
            {
                int count = _inventory.GetCount(slot.Item.ItemId);
                slot.CountLabel.text = count > 0 ? count.ToString() : string.Empty;
                slot.Icon.color = count > 0
                    ? (slot.Item.Icon != null ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f))
                    : new Color(1f, 1f, 1f, 0.35f);
            }
        }
    }
}
