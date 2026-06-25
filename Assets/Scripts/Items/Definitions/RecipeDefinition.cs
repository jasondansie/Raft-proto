using UnityEngine;

namespace RaftProto.Items
{
    /// <summary>
    /// Designer-facing craft recipe: consumes input item IDs, produces output item IDs.
    /// Crafting logic comes in a later step; this is the data shape only.
    /// </summary>
    [CreateAssetMenu(menuName = "RaftProto/Recipe Definition", fileName = "Recipe_")]
    public class RecipeDefinition : ScriptableObject
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
        [SerializeField] private RecipeIngredient[] inputs;
        [SerializeField] private RecipeIngredient[] outputs;

        public string RecipeId => recipeId;
        public string DisplayName => displayName;
        public RecipeIngredient[] Inputs => inputs;
        public RecipeIngredient[] Outputs => outputs;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(recipeId))
            {
                displayName = recipeId;
            }
        }
    }
}
