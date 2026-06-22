using UnityEngine;
using UnityEngine.UI;

namespace RaftProto.UI
{
    /// <summary>
    /// Screen-centre crosshair for hook, interact, and build aiming. Local-only HUD element.
    /// </summary>
    public class CrosshairHUD : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private float armLength = 10f;
        [SerializeField] private float thickness = 2f;
        [SerializeField] private float gap = 4f;

        private static Sprite _whiteSprite;

        private void Awake()
        {
            BuildCrosshair();
        }

        private void BuildCrosshair()
        {
            GameObject canvasObject = new GameObject("Crosshair_Canvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvasObject.AddComponent<CanvasScaler>();

            GameObject root = new GameObject("Crosshair");
            root.transform.SetParent(canvasObject.transform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;

            CreateArm(root.transform, "Top", new Vector2(0f, gap), new Vector2(thickness, armLength), new Vector2(0.5f, 0f));
            CreateArm(root.transform, "Bottom", new Vector2(0f, -gap), new Vector2(thickness, armLength), new Vector2(0.5f, 1f));
            CreateArm(root.transform, "Left", new Vector2(-gap, 0f), new Vector2(armLength, thickness), new Vector2(1f, 0.5f));
            CreateArm(root.transform, "Right", new Vector2(gap, 0f), new Vector2(armLength, thickness), new Vector2(0f, 0.5f));
        }

        private void CreateArm(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            GameObject armObject = new GameObject(name);
            armObject.transform.SetParent(parent, false);

            Image image = armObject.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
            {
                return _whiteSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}
