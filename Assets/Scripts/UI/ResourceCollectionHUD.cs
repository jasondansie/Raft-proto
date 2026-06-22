using System.Collections.Generic;
using System.Text;
using RaftProto.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace RaftProto.UI
{
    /// <summary>
    /// Temporary on-screen tally for Phase 3 playtesting. Subscribes to
    /// <see cref="ResourceCollector.Collected"/> and builds its own overlay canvas at runtime.
    /// Phase 4 inventory UI will replace this.
    /// </summary>
    public class ResourceCollectionHUD : MonoBehaviour
    {
        [SerializeField] private ResourceCollector collector;

        private readonly Dictionary<ResourceType, int> _counts = new();
        private Text _label;

        private void Awake()
        {
            if (collector == null)
            {
                collector = GetComponent<ResourceCollector>();
            }

            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                _counts[type] = 0;
            }

            BuildOverlay();
            RefreshLabel();
        }

        private void OnEnable()
        {
            if (collector != null)
            {
                collector.Collected += OnCollected;
            }
        }

        private void OnDisable()
        {
            if (collector != null)
            {
                collector.Collected -= OnCollected;
            }
        }

        private void OnCollected(ResourceType type)
        {
            _counts[type]++;
            RefreshLabel();
        }

        private void BuildOverlay()
        {
            GameObject canvasObject = new GameObject("ResourceHUD_Canvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject textObject = new GameObject("ResourceHUD_Label");
            textObject.transform.SetParent(canvasObject.transform, false);

            _label = textObject.AddComponent<Text>();
            _label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 18;
            _label.color = Color.white;
            _label.alignment = TextAnchor.UpperLeft;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = _label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -16f);
            rect.sizeDelta = new Vector2(400f, 120f);
        }

        private void RefreshLabel()
        {
            if (_label == null)
            {
                return;
            }

            var builder = new StringBuilder("Resources\n");
            foreach (KeyValuePair<ResourceType, int> entry in _counts)
            {
                builder.Append(entry.Key);
                builder.Append(": ");
                builder.Append(entry.Value);
                builder.Append('\n');
            }

            _label.text = builder.ToString().TrimEnd();
        }
    }
}
