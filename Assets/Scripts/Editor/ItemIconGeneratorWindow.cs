using System.Collections.Generic;
using RaftProto.Items;
using UnityEditor;
using UnityEngine;

namespace RaftProto.Editor
{
    public class ItemIconGeneratorWindow : EditorWindow
    {
        private const string DefaultConfigPath = "Assets/Data/ItemIconGenerationConfig.asset";

        private ItemIconGenerationConfig _config;
        private Vector2 _scroll;
        private SerializedObject _serializedConfig;

        [MenuItem("RaftProto/Item Icon Generator")]
        public static void Open()
        {
            ItemIconGeneratorWindow window = GetWindow<ItemIconGeneratorWindow>("Item Icon Generator");
            window.minSize = new Vector2(420f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateConfig();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Item Icon Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Renders kit prefabs with an orthographic preview camera, saves PNG sprites, " +
                "and optionally assigns them to ItemDefinition assets.",
                MessageType.Info);

            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            _config = (ItemIconGenerationConfig)EditorGUILayout.ObjectField(
                "Config",
                _config,
                typeof(ItemIconGenerationConfig),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                BindSerializedConfig();
            }

            if (_config == null)
            {
                if (GUILayout.Button("Create Config Asset"))
                {
                    CreateConfigAsset();
                }

                return;
            }

            if (_serializedConfig == null)
            {
                BindSerializedConfig();
            }

            _serializedConfig.Update();

            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("resolution"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("backgroundColor"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("padding"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("outputFolder"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("assignToItemDefinitions"));

            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Raft Proto Defaults"))
            {
                LoadRaftProtoDefaults();
            }

            if (GUILayout.Button("Generate Enabled Icons"))
            {
                GenerateEnabledIcons();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("entries"), true);
            EditorGUILayout.EndScrollView();

            _serializedConfig.ApplyModifiedProperties();
        }

        private void LoadOrCreateConfig()
        {
            _config = AssetDatabase.LoadAssetAtPath<ItemIconGenerationConfig>(DefaultConfigPath);
            BindSerializedConfig();
        }

        private void BindSerializedConfig()
        {
            _serializedConfig = _config != null ? new SerializedObject(_config) : null;
        }

        private void CreateConfigAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            _config = CreateInstance<ItemIconGenerationConfig>();
            AssetDatabase.CreateAsset(_config, DefaultConfigPath);
            AssetDatabase.SaveAssets();
            LoadRaftProtoDefaults();
            BindSerializedConfig();
        }

        private void LoadRaftProtoDefaults()
        {
            if (_config == null)
            {
                return;
            }

            Undo.RecordObject(_config, "Load Item Icon Defaults");

            _config.entries = new List<ItemIconGenerationEntry>
            {
                CreateDefaultEntry("Assets/Data/Items/Item_Wood.asset", "Assets/Raft Builder Kit/Prefabs/LargePlatform1/FloatingRoll.prefab", new Vector3(10f, -35f, 0f)),
                CreateDefaultEntry("Assets/Data/Items/Item_Plastic.asset", "Assets/Raft Builder Kit/Prefabs/SmallitemsTools/Bottle.prefab", new Vector3(-10f, 35f, 0f)),
                CreateDefaultEntry("Assets/Data/Items/Item_Scrap.asset", "Assets/Raft Builder Kit/Prefabs/Resources/CircuitBoard.prefab", new Vector3(20f, -25f, 0f)),
                CreateDefaultEntry("Assets/Data/Items/Item_Plank.asset", "Assets/Prefabs/DeckTile.prefab", new Vector3(55f, -35f, 0f)),
                CreateDefaultEntry("Assets/Data/Items/Item_Hammer.asset", "Assets/Raft Builder Kit/Prefabs/SmallitemsTools/Hammer.prefab", new Vector3(-15f, 35f, 0f)),
                CreateDefaultEntry("Assets/Data/Items/Item_Axe.asset", "Assets/Raft Builder Kit/Prefabs/SmallitemsTools/Axe.prefab", new Vector3(-15f, 35f, 0f))
            };

            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            BindSerializedConfig();
        }

        private static ItemIconGenerationEntry CreateDefaultEntry(string itemPath, string prefabPath, Vector3 rotationEuler)
        {
            return new ItemIconGenerationEntry
            {
                enabled = true,
                item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath),
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),
                rotationEuler = rotationEuler
            };
        }

        private void GenerateEnabledIcons()
        {
            if (_config == null)
            {
                return;
            }

            int generated = 0;

            foreach (ItemIconGenerationEntry entry in _config.entries)
            {
                if (!entry.enabled || entry.item == null || entry.prefab == null)
                {
                    continue;
                }

                Texture2D texture = ItemIconRendererUtility.RenderPrefabIcon(
                    entry.prefab,
                    _config.resolution,
                    entry.rotationEuler,
                    _config.padding,
                    _config.backgroundColor);

                if (texture == null)
                {
                    Debug.LogWarning($"Item Icon Generator: failed to render {entry.item.name}.");
                    continue;
                }

                string fileName = $"Icon_{entry.item.ItemId}.png";
                string assetPath = ItemIconRendererUtility.SaveIconTexture(texture, _config.outputFolder, fileName);

                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (_config.assignToItemDefinitions)
                {
                    AssignIcon(entry.item, assetPath);
                }

                generated++;
                Debug.Log($"Item Icon Generator: wrote {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Item Icon Generator", $"Generated {generated} icon(s).", "OK");
        }

        private static void AssignIcon(ItemDefinition item, string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning($"Item Icon Generator: sprite import missing for {assetPath}.");
                return;
            }

            SerializedObject serializedItem = new SerializedObject(item);
            SerializedProperty iconProperty = serializedItem.FindProperty("icon");
            iconProperty.objectReferenceValue = sprite;
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }
    }
}
