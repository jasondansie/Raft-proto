using System.Collections.Generic;
using System.Text;
using RaftProto.Items;
using UnityEngine;
using UnityEngine.UI;

namespace RaftProto.UI
{
    /// <summary>
    /// Raft-style craft panel on the left: category sidebar + recipe list.
    /// Visibility is driven by <see cref="InventoryBagUI"/> (Tab), not its own hotkey.
    /// </summary>
    public class CraftPanelUI : MonoBehaviour
    {
        private CraftingSystem _craftingSystem;
        private RecipeCatalog _recipeCatalog;
        private ItemCatalog _itemCatalog;
        private Inventory _inventory;

        private GameObject _panelRoot;
        private Transform _recipeListRoot;
        private readonly List<RecipeRowView> _rows = new();

        private struct RecipeRowView
        {
            public RecipeDefinition Recipe;
            public Button Button;
            public Image Icon;
            public Text NameLabel;
        }

        public void Initialize(
            Transform canvasRoot,
            CraftingSystem craftingSystem,
            RecipeCatalog recipeCatalog,
            ItemCatalog itemCatalog,
            Inventory inventory)
        {
            _craftingSystem = craftingSystem;
            _recipeCatalog = recipeCatalog;
            _itemCatalog = itemCatalog;
            _inventory = inventory;
            BuildPanel(canvasRoot);
            SetVisible(false);
        }

        public void BindEvents()
        {
            if (_inventory != null)
            {
                _inventory.Changed += RefreshRows;
            }

            if (_craftingSystem != null)
            {
                _craftingSystem.Crafted += OnCrafted;
            }
        }

        public void UnbindEvents()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= RefreshRows;
            }

            if (_craftingSystem != null)
            {
                _craftingSystem.Crafted -= OnCrafted;
            }
        }

        public void SetVisible(bool visible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }

            if (visible)
            {
                RefreshRows();
            }
        }

        private void BuildPanel(Transform canvasRoot)
        {
            _panelRoot = new GameObject("CraftPanel");
            _panelRoot.transform.SetParent(canvasRoot, false);

            RectTransform panelRect = _panelRoot.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-300f, 120f);
            panelRect.sizeDelta = new Vector2(460f, 500f);

            BuildCategorySidebar(_panelRoot.transform);
            BuildRecipePanel(_panelRoot.transform);
        }

        private void BuildCategorySidebar(Transform parent)
        {
            GameObject sidebarObject = new GameObject("CategorySidebar");
            sidebarObject.transform.SetParent(parent, false);

            RectTransform sidebarRect = sidebarObject.AddComponent<RectTransform>();
            sidebarRect.anchorMin = new Vector2(0f, 0f);
            sidebarRect.anchorMax = new Vector2(0f, 1f);
            sidebarRect.pivot = new Vector2(0f, 0.5f);
            sidebarRect.anchoredPosition = Vector2.zero;
            sidebarRect.sizeDelta = new Vector2(72f, 0f);

            Image sidebarBorder = sidebarObject.AddComponent<Image>();
            sidebarBorder.sprite = UiSprites.White;
            sidebarBorder.color = RaftUiTheme.PanelBorder;
            sidebarBorder.raycastTarget = true;

            Image sidebarFill = UiFactory.CreateImage(sidebarObject.transform, "Fill", RaftUiTheme.PanelFill);
            StretchRect(sidebarFill.rectTransform, 3f);

            CreateCategoryButton(sidebarObject.transform, "Tools", true, 0);
            CreateCategoryButton(sidebarObject.transform, "·", false, 1);
            CreateCategoryButton(sidebarObject.transform, "·", false, 2);
            CreateCategoryButton(sidebarObject.transform, "·", false, 3);
        }

        private void CreateCategoryButton(Transform parent, string label, bool active, int index)
        {
            GameObject buttonObject = new GameObject($"Category_{index}");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -16f - index * 64f);
            rect.sizeDelta = new Vector2(56f, 56f);

            Color fillColor = active ? RaftUiTheme.SlotFill : new Color(0.35f, 0.24f, 0.14f, 0.65f);
            Image background = buttonObject.AddComponent<Image>();
            background.sprite = UiSprites.White;
            background.color = fillColor;
            background.raycastTarget = active;

            Text text = UiFactory.CreateText(buttonObject.transform, "Label", 16, TextAnchor.MiddleCenter);
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            text.color = RaftUiTheme.TitleText;
            text.raycastTarget = false;
            StretchRect(text.rectTransform, 0f);

            if (!active)
            {
                Button button = buttonObject.AddComponent<Button>();
                button.interactable = false;
                button.targetGraphic = background;
            }
        }

        private void BuildRecipePanel(Transform parent)
        {
            GameObject contentObject = new GameObject("RecipePanel");
            contentObject.transform.SetParent(parent, false);

            RectTransform contentRect = contentObject.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(72f, 0f);
            contentRect.offsetMax = Vector2.zero;

            Image border = UiFactory.CreateImage(contentObject.transform, "Border", RaftUiTheme.PanelBorder);
            border.raycastTarget = true;
            StretchRect(border.rectTransform, 0f);

            Image fill = UiFactory.CreateImage(contentObject.transform, "Fill", RaftUiTheme.PanelFill);
            fill.raycastTarget = false;
            StretchRect(fill.rectTransform, 6f);

            Text title = UiFactory.CreateText(contentObject.transform, "Title", 28, TextAnchor.UpperCenter);
            title.text = "TOOLS";
            title.fontStyle = FontStyle.Bold;
            title.color = RaftUiTheme.TitleText;
            title.raycastTarget = false;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -18f);
            titleRect.sizeDelta = new Vector2(-32f, 40f);

            Image divider = UiFactory.CreateImage(contentObject.transform, "Divider", RaftUiTheme.Divider);
            divider.raycastTarget = false;
            RectTransform dividerRect = divider.rectTransform;
            dividerRect.anchorMin = new Vector2(0f, 1f);
            dividerRect.anchorMax = new Vector2(1f, 1f);
            dividerRect.pivot = new Vector2(0.5f, 1f);
            dividerRect.anchoredPosition = new Vector2(0f, -58f);
            dividerRect.sizeDelta = new Vector2(-40f, 3f);

            GameObject scrollObject = new GameObject("RecipeScroll");
            scrollObject.transform.SetParent(contentObject.transform, false);
            RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(16f, 16f);
            scrollRect.offsetMax = new Vector2(-16f, -72f);

            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObject = new GameObject("Viewport");
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportObject.AddComponent<Mask>().showMaskGraphic = false;
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);

            GameObject listObject = new GameObject("RecipeList");
            listObject.transform.SetParent(viewportObject.transform, false);
            RectTransform listRect = listObject.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            _recipeListRoot = listObject.transform;

            VerticalLayoutGroup layout = listObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = listObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = listRect;

            if (_recipeCatalog == null || _recipeCatalog.Recipes == null)
            {
                return;
            }

            foreach (RecipeDefinition recipe in _recipeCatalog.Recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                _rows.Add(CreateRecipeRow(_recipeListRoot, recipe));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }

        private RecipeRowView CreateRecipeRow(Transform parent, RecipeDefinition recipe)
        {
            GameObject rowObject = new GameObject($"Recipe_{recipe.RecipeId}");
            rowObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 64f;

            Image rowBackground = rowObject.AddComponent<Image>();
            rowBackground.sprite = UiSprites.White;
            rowBackground.color = new Color(0.35f, 0.24f, 0.14f, 0.45f);
            rowBackground.raycastTarget = true;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(rowObject.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(10f, 0f);
            iconRect.sizeDelta = new Vector2(48f, 48f);
            ApplyOutputIcon(icon, recipe);

            Text nameLabel = UiFactory.CreateText(rowObject.transform, "Name", 20, TextAnchor.MiddleLeft);
            nameLabel.raycastTarget = false;
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.color = RaftUiTheme.TitleText;
            RectTransform nameRect = nameLabel.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(68f, 0f);
            nameRect.offsetMax = new Vector2(-8f, 0f);
            nameLabel.text = (string.IsNullOrWhiteSpace(recipe.DisplayName) ? recipe.RecipeId : recipe.DisplayName).ToUpperInvariant();

            Button button = rowObject.AddComponent<Button>();
            button.targetGraphic = rowBackground;
            RecipeDefinition capturedRecipe = recipe;
            button.onClick.AddListener(() => TryCraft(capturedRecipe));

            return new RecipeRowView
            {
                Recipe = recipe,
                Button = button,
                Icon = icon,
                NameLabel = nameLabel
            };
        }

        private void ApplyOutputIcon(Image icon, RecipeDefinition recipe)
        {
            string outputId = GetPrimaryOutputId(recipe);
            if (string.IsNullOrWhiteSpace(outputId))
            {
                icon.sprite = UiSprites.White;
                icon.color = new Color(0.75f, 0.75f, 0.75f, 1f);
                return;
            }

            if (_itemCatalog != null && _itemCatalog.TryGetDefinition(outputId, out ItemDefinition definition))
            {
                icon.sprite = definition.Icon != null ? definition.Icon : UiSprites.White;
                icon.color = definition.Icon != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
            }
        }

        private static string GetPrimaryOutputId(RecipeDefinition recipe)
        {
            if (recipe.Outputs == null || recipe.Outputs.Length == 0)
            {
                return null;
            }

            return recipe.Outputs[0].ItemId;
        }

        private void TryCraft(RecipeDefinition recipe)
        {
            if (_craftingSystem != null)
            {
                _craftingSystem.TryCraft(recipe);
            }
        }

        private void OnCrafted(RecipeDefinition recipe)
        {
            RefreshRows();
        }

        private void RefreshRows()
        {
            foreach (RecipeRowView row in _rows)
            {
                bool canCraft = _craftingSystem != null && _craftingSystem.CanCraft(row.Recipe);
                row.Button.interactable = canCraft;
                row.NameLabel.color = canCraft
                    ? RaftUiTheme.TitleText
                    : new Color(RaftUiTheme.TitleText.r, RaftUiTheme.TitleText.g, RaftUiTheme.TitleText.b, 0.45f);
                row.Icon.color = new Color(row.Icon.color.r, row.Icon.color.g, row.Icon.color.b, canCraft ? 1f : 0.45f);
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
