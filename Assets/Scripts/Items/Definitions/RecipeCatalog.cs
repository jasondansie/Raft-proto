using UnityEngine;

namespace RaftProto.Items
{
    /// <summary>
    /// Registry of craft recipes shown in the craft UI.
    /// </summary>
    [CreateAssetMenu(menuName = "RaftProto/Recipe Catalog", fileName = "RecipeCatalog")]
    public class RecipeCatalog : ScriptableObject
    {
        [SerializeField] private RecipeDefinition[] recipes;

        public RecipeDefinition[] Recipes => recipes;
    }
}
