using UnityEngine;

namespace RaftProto.UI
{
    internal static class UiSprites
    {
        private static Sprite _white;

        public static Sprite White
        {
            get
            {
                if (_white != null)
                {
                    return _white;
                }

                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _white = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                return _white;
            }
        }
    }
}
