using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    public static class DreamUISpriteUtil
    {
        private static Sprite _white;

        public static Sprite WhiteSprite()
        {
            if (_white != null) return _white;
            var tex = Texture2D.whiteTexture;
            _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _white.name = "DreamWhiteSpriteRuntime";
            return _white;
        }

        // Prefab 上 Image 可不填 sprite，运行时补白块
        public static void EnsureWhiteSprite(Image image)
        {
            if (image == null || image.sprite != null) return;
            image.sprite = WhiteSprite();
        }
    }
}
