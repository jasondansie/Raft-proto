using System;
using System.Collections.Generic;
using RaftProto.Items;
using UnityEngine;

namespace RaftProto.Editor
{
    [CreateAssetMenu(menuName = "RaftProto/Item Icon Generation Config", fileName = "ItemIconGenerationConfig")]
    public class ItemIconGenerationConfig : ScriptableObject
    {
        [Min(64)]
        public int resolution = 256;

        public Color backgroundColor = new Color(0f, 0f, 0f, 0f);

        [Range(1f, 2f)]
        public float padding = 1.15f;

        public string outputFolder = "Assets/Data/Icons";

        public bool assignToItemDefinitions = true;

        public List<ItemIconGenerationEntry> entries = new();
    }

    [Serializable]
    public class ItemIconGenerationEntry
    {
        public bool enabled = true;
        public ItemDefinition item;
        public GameObject prefab;
        public Vector3 rotationEuler = new Vector3(-20f, 35f, 0f);
    }
}
