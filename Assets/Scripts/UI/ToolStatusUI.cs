using RaftProto.Core;
using RaftProto.Items;
using UnityEngine;
using UnityEngine.UI;

namespace RaftProto.UI
{
    /// <summary>
    /// Shows the active hotbar slot and equipped tool. Local-only feedback for the owner.
    /// </summary>
    public class ToolStatusUI : MonoBehaviour
    {
        private IPlayerToolState _toolState;
        private Hotbar _hotbar;
        private ItemCatalog _catalog;
        private Text _label;

        public void Initialize(
            Transform canvasRoot,
            IPlayerToolState toolState,
            Hotbar hotbar,
            ItemCatalog catalog)
        {
            _toolState = toolState;
            _hotbar = hotbar;
            _catalog = catalog;
            BuildLabel(canvasRoot);
        }

        private void Update()
        {
            if (_label == null || _toolState == null)
            {
                return;
            }

            string activeName = _toolState.ActiveTool switch
            {
                EquippedToolType.Hammer => "Hammer",
                EquippedToolType.Axe => "Axe",
                _ => "Hands"
            };

            string slotItem = "empty";
            if (_hotbar != null)
            {
                string itemId = _hotbar.GetAssignedItemId(_toolState.SelectedHotbarSlotIndex);
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    slotItem = ResolveDisplayName(itemId);
                }
            }

            _label.text =
                $"Slot {_toolState.SelectedHotbarSlotIndex + 1}: {slotItem}  |  Tool: {activeName}  |  [1-8] hotbar  |  [Tab] inventory & craft";
        }

        private string ResolveDisplayName(string itemId)
        {
            if (_catalog != null && _catalog.TryGetDefinition(itemId, out ItemDefinition definition))
            {
                return definition.DisplayName;
            }

            return itemId;
        }

        private void BuildLabel(Transform canvasRoot)
        {
            GameObject labelObject = new GameObject("ToolStatus");
            labelObject.transform.SetParent(canvasRoot, false);

            RectTransform rect = labelObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -16f);
            rect.sizeDelta = new Vector2(960f, 32f);

            _label = labelObject.AddComponent<Text>();
            _label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 18;
            _label.alignment = TextAnchor.UpperCenter;
            _label.color = Color.white;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }
}
