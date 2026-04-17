using UnityEngine;

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
    }
}
