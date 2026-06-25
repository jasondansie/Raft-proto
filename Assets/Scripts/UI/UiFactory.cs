using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RaftProto.UI
{
    internal static class UiFactory
    {
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        public static Canvas CreateOverlayCanvas(Transform parent, string name, int sortOrder = 150)
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject(name);
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.sprite = UiSprites.White;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 size)
        {
            GameObject buttonObject = new GameObject("Button");
            buttonObject.transform.SetParent(parent, false);

            Image background = buttonObject.AddComponent<Image>();
            background.sprite = UiSprites.White;
            background.color = new Color(0.25f, 0.45f, 0.3f, 0.95f);
            background.raycastTarget = true;

            Button button = buttonObject.AddComponent<Button>();

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Text text = CreateText(buttonObject.transform, "Label", 20, TextAnchor.MiddleCenter);
            text.text = label;
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
