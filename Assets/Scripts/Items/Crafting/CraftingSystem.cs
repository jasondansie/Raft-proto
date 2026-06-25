using UnityEngine;

namespace RaftProto.Items
{
    /// <summary>
    /// Validates and executes craft recipes against a local inventory. In multiplayer the server
    /// runs this after a CraftServerRpc. Crafting is driven by <see cref="RaftProto.UI.CraftPanelUI"/>.
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;

        public event System.Action<RecipeDefinition> Crafted;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<Inventory>();
            }
        }

        public bool CanCraft(RecipeDefinition recipe)
        {
            if (recipe == null || inventory == null)
            {
                return false;
            }

            return inventory.HasIngredients(recipe.Inputs);
        }

        public bool TryCraft(RecipeDefinition recipe)
        {
            if (!CanCraft(recipe))
            {
                return false;
            }

            foreach (RecipeIngredient input in recipe.Inputs)
            {
                if (!inventory.TryRemoveItem(input.ItemId, input.Count))
                {
                    return false;
                }
            }

            foreach (RecipeIngredient output in recipe.Outputs)
            {
                inventory.AddItem(output.ItemId, output.Count);
            }

            Crafted?.Invoke(recipe);
            return true;
        }
    }
}
