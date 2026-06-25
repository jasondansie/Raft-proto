using UnityEngine;

namespace RaftProto.Items
{
    /// <summary>
    /// Designer-facing static data for one item type. Referenced everywhere by <see cref="ItemId"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "RaftProto/Item Definition", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private int maxStackSize = 20;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxStackSize => maxStackSize;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(itemId))
            {
                displayName = itemId;
            }
        }
    }
}
