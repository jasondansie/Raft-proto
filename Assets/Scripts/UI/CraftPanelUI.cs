using System.Collections.Generic;
using System.Text;
using RaftProto.Items;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RaftProto.UI
{
    /// <summary>
    /// Toggleable craft menu listing recipes from a catalog. Press C to open/close.
    /// </summary>
    public class CraftPanelUI : MonoBehaviour
    {
        [SerializeField] private Key toggleKey = Key.C;

        private CraftingSystem _craftingSystem;
        private RecipeCatalog _recipeCatalog;
        private ItemCatalog _itemCatalog;
        private Inventory _inventory;

        private GameObject _panelRoot;
        private readonly List<RecipeRowView> _rows = new();
        private bool _isOpen;

        private struct RecipeRowView
        {
            public RecipeDefinition Recipe;
            public Text Description;
            public Button CraftButton;
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
            SetOpen(false);
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
            _panelRoot = new GameObject("CraftPanel");
            _panelRoot.transform.SetParent(canvasRoot, false);

            RectTransform panelRect = _panelRoot.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-32f, 0f);
            panelRect.sizeDelta = new Vector2(440f, 500f);

            Image panelBackground = UiFactory.CreateImage(_panelRoot.transform, "Background", new Color(0f, 0f, 0f, 0.75f));
            panelBackground.raycastTarget = true;
            RectTransform backgroundRect = panelBackground.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Text title = UiFactory.CreateText(_panelRoot.transform, "Title", 28, TextAnchor.UpperLeft);
            title.text = "Crafting  (C to close)";
            title.raycastTarget = false;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(20f, -16f);
            titleRect.sizeDelta = new Vector2(-40f, 40f);

            GameObject listObject = new GameObject("RecipeList");
            listObject.transform.SetParent(_panelRoot.transform, false);
            RectTransform listRect = listObject.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.offsetMin = new Vector2(20f, 20f);
            listRect.offsetMax = new Vector2(-20f, -56f);

            VerticalLayoutGroup layout = listObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

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

                _rows.Add(CreateRecipeRow(listObject.transform, recipe));
            }
        }

        private RecipeRowView CreateRecipeRow(Transform parent, RecipeDefinition recipe)
        {
            GameObject rowObject = new GameObject($"Recipe_{recipe.RecipeId}");
            rowObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 128f;

            Image rowBackground = UiFactory.CreateImage(rowObject.transform, "RowBackground", new Color(1f, 1f, 1f, 0.08f));
            rowBackground.raycastTarget = false;
            RectTransform rowBackgroundRect = rowBackground.rectTransform;
            rowBackgroundRect.anchorMin = Vector2.zero;
            rowBackgroundRect.anchorMax = Vector2.one;
            rowBackgroundRect.offsetMin = Vector2.zero;
            rowBackgroundRect.offsetMax = Vector2.zero;

            Text description = UiFactory.CreateText(rowObject.transform, "Description", 20, TextAnchor.UpperLeft);
            description.raycastTarget = false;
            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.offsetMin = new Vector2(16f, 52f);
            descriptionRect.offsetMax = new Vector2(-16f, -12f);

            Button craftButton = UiFactory.CreateButton(rowObject.transform, "Craft", new Vector2(128f, 40f));
            RectTransform buttonRect = craftButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-16f, 12f);

            RecipeDefinition capturedRecipe = recipe;
            craftButton.onClick.AddListener(() => TryCraft(capturedRecipe));

            return new RecipeRowView
            {
                Recipe = recipe,
                Description = description,
                CraftButton = craftButton
            };
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
                row.Description.text = BuildRecipeDescription(row.Recipe);
                bool canCraft = _craftingSystem != null && _craftingSystem.CanCraft(row.Recipe);
                row.CraftButton.interactable = canCraft;
            }
        }

        private string BuildRecipeDescription(RecipeDefinition recipe)
        {
            var builder = new StringBuilder();
            builder.Append(string.IsNullOrWhiteSpace(recipe.DisplayName) ? recipe.RecipeId : recipe.DisplayName);
            builder.Append("\n");
            builder.Append(FormatIngredients(recipe.Inputs));
            builder.Append("  →  ");
            builder.Append(FormatIngredients(recipe.Outputs));
            return builder.ToString();
        }

        private string FormatIngredients(RecipeIngredient[] ingredients)
        {
            if (ingredients == null || ingredients.Length == 0)
            {
                return "-";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < ingredients.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(ingredients[i].Count);
                builder.Append(' ');
                builder.Append(ResolveItemName(ingredients[i].ItemId));
            }

            return builder.ToString();
        }

        private string ResolveItemName(string itemId)
        {
            if (_itemCatalog != null && _itemCatalog.TryGetDefinition(itemId, out ItemDefinition definition))
            {
                return definition.DisplayName;
            }

            return itemId;
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(open);
            }

            if (open)
            {
                RefreshRows();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
