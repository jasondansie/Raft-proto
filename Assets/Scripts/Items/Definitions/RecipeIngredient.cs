using System;
using UnityEngine;

namespace RaftProto.Items
{
    [Serializable]
    public struct RecipeIngredient
    {
        [SerializeField] private string itemId;
        [SerializeField] private int count;

        public string ItemId => itemId;
        public int Count => count;

        public RecipeIngredient(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }
}
