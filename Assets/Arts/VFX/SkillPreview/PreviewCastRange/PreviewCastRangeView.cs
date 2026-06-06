using UnityEngine;

namespace My
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PreviewCastRangeView : MonoBehaviour
    {
        float _spriteExtent = 1f;
        float _logicRadius;

        void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                var size = sr.sprite.bounds.size;
                _spriteExtent = Mathf.Max(size.x, size.y);
            }
        }

        public void SetRadius(float worldRadius)
        {
            _logicRadius = worldRadius;
            ApplyVisualScale();
        }

        void ApplyVisualScale()
        {
            if (_logicRadius <= 1e-4f)
            {
                return;
            }

            float visScale = (_logicRadius * 2f) / _spriteExtent;
            transform.localScale = new Vector3(visScale, visScale, 1f);
        }
    }
}
