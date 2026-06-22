using UnityEngine;

namespace My.Map.Scene
{
    public static class RuntimeCircleVisualUtil
    {
        static Sprite _circleSprite;

        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                {
                    _circleSprite = CreateCircleSprite();
                }

                return _circleSprite;
            }
        }

        public static Color ParseColor(string htmlColor, Color fallback)
        {
            if (!string.IsNullOrEmpty(htmlColor) && ColorUtility.TryParseHtmlString(htmlColor, out var color))
            {
                return color;
            }

            return fallback;
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "RuntimeCircleSprite";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = (size - 2) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
