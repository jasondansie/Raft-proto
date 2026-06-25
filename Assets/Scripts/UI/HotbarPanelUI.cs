using System;
using RaftProto.Core;
using RaftProto.Items;
using UnityEngine;
using UnityEngine.UI;

namespace RaftProto.UI
{
    /// <summary>
    /// Bottom hotbar: fixed empty slots that fill in pickup order unless pinned by the player.
    /// </summary>
    public class HotbarPanelUI : MonoBehaviour
    {
        private Hotbar _hotbar;
        private ItemCatalog _catalog;
        private IPlayerToolState _toolState;
        private SlotView[] _slotViews = Array.Empty<SlotView>();

        public event Action<int> SlotClicked;

        private struct SlotView
        {
            public Button Button;
            public Image Icon;
            public Text CountLabel;
            public Text SlotNumberLabel;
            public Image Highlight;
        }

        public void Initialize(
            Transform canvasRoot,
            Hotbar hotbar,
            ItemCatalog catalog,
            IPlayerToolState toolState)
        {
            _hotbar = hotbar;
            _catalog = catalog;
            _toolState = toolState;
            BuildPanel(canvasRoot);
            Refresh();
        }

        public void BindEvents()
        {
            if (_hotbar != null)
            {
                _hotbar.Changed += Refresh;
            }
        }

        public void UnbindEvents()
        {
            if (_hotbar != null)
            {
                _hotbar.Changed -= Refresh;
            }
        }

        private void Update()
        {
            RefreshSelectionHighlight();
        }

        private void BuildPanel(Transform canvasRoot)
        {
            GameObject barObject = new GameObject("Hotbar");
            barObject.transform.SetParent(canvasRoot, false);

            RectTransform barRect = barObject.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 0f);
            barRect.pivot = new Vector2(0f, 0f);
            barRect.anchoredPosition = new Vector2(24f, 36f);

            HorizontalLayoutGroup layout = barObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (_hotbar == null)
            {
                return;
            }

            _slotViews = new SlotView[_hotbar.SlotCount];
            for (int i = 0; i < _hotbar.SlotCount; i++)
            {
                _slotViews[i] = CreateSlot(barObject.transform, i);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
        }

        private SlotView CreateSlot(Transform parent, int index)
        {
            GameObject slotObject = new GameObject($"HotbarSlot_{index + 1}");
            slotObject.transform.SetParent(parent, false);

            RectTransform slotRect = slotObject.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(96f, 96f);

            Image background = UiFactory.CreateImage(slotObject.transform, "Background", new Color(0f, 0f, 0f, 0.55f));
            background.raycastTarget = true;

            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Image highlight = UiFactory.CreateImage(slotObject.transform, "Highlight", new Color(0.95f, 0.85f, 0.2f, 0.85f));
            highlight.raycastTarget = false;
            RectTransform highlightRect = highlight.rectTransform;
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            highlight.enabled = false;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(slotObject.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = UiSprites.White;
            icon.color = new Color(1f, 1f, 1f, 0.15f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Text slotNumber = UiFactory.CreateText(slotObject.transform, "SlotNumber", 18, TextAnchor.UpperLeft);
            slotNumber.text = (index + 1).ToString();
            slotNumber.color = new Color(1f, 1f, 1f, 0.45f);
            RectTransform slotNumberRect = slotNumber.rectTransform;
            slotNumberRect.anchorMin = Vector2.zero;
            slotNumberRect.anchorMax = Vector2.one;
            slotNumberRect.offsetMin = new Vector2(8f, 0f);
            slotNumberRect.offsetMax = new Vector2(-4f, -4f);

            Text countLabel = UiFactory.CreateText(slotObject.transform, "Count", 22, TextAnchor.LowerRight);
            countLabel.fontStyle = FontStyle.Bold;
            RectTransform countRect = countLabel.rectTransform;
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(4f, 4f);
            countRect.offsetMax = new Vector2(-6f, -4f);

            Button button = slotObject.AddComponent<Button>();
            button.targetGraphic = background;
            int capturedIndex = index;
            button.onClick.AddListener(() => SlotClicked?.Invoke(capturedIndex));

            return new SlotView
            {
                Button = button,
                Icon = icon,
                CountLabel = countLabel,
                SlotNumberLabel = slotNumber,
                Highlight = highlight
            };
        }

        private void Refresh()
        {
            if (_hotbar == null)
            {
                return;
            }

            for (int i = 0; i < _slotViews.Length; i++)
            {
                SlotView slot = _slotViews[i];
                string itemId = _hotbar.GetAssignedItemId(i);
                int count = _hotbar.GetDisplayCount(i);
                bool hasAssignment = !string.IsNullOrWhiteSpace(itemId);
                bool hasItems = count > 0;

                if (hasAssignment && _catalog != null && _catalog.TryGetDefinition(itemId, out ItemDefinition definition))
                {
                    slot.Icon.sprite = definition.Icon != null ? definition.Icon : UiSprites.White;
                    slot.Icon.color = hasItems
                        ? (definition.Icon != null ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f))
                        : new Color(1f, 1f, 1f, 0.25f);
                }
                else
                {
                    slot.Icon.sprite = UiSprites.White;
                    slot.Icon.color = new Color(1f, 1f, 1f, 0.12f);
                }

                slot.CountLabel.text = hasItems ? count.ToString() : string.Empty;
                slot.SlotNumberLabel.text = (i + 1).ToString();
            }

            RefreshSelectionHighlight();
        }

        private void RefreshSelectionHighlight()
        {
            if (_toolState == null)
            {
                return;
            }

            int selectedIndex = _toolState.SelectedHotbarSlotIndex;
            for (int i = 0; i < _slotViews.Length; i++)
            {
                _slotViews[i].Highlight.enabled = i == selectedIndex;
            }
        }
    }
}
