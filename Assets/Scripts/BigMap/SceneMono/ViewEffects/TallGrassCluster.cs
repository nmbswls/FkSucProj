using Animancer;
using UnityEngine;

namespace My.Map
{
    // 单簇高草：由 TallGrassPatch 在初始化时设置形态、排序与动画相位
    public class TallGrassCluster : MonoBehaviour
    {
        SpriteRenderer _spriteRenderer;
        SoloAnimation _soloAnimation;

        void Awake()
        {
            CacheComponents();
        }

        void CacheComponents()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (_soloAnimation == null)
            {
                _soloAnimation = GetComponentInChildren<SoloAnimation>(true);
            }
        }

        public void Setup(Sprite sprite, int sortingOrder, float normalizedPhase, bool flipX)
        {
            CacheComponents();

            if (_spriteRenderer != null)
            {
                if (sprite != null)
                {
                    _spriteRenderer.sprite = sprite;
                }

                _spriteRenderer.flipX = flipX;
                _spriteRenderer.sortingOrder = sortingOrder;
            }

            if (_soloAnimation == null || _soloAnimation.Clip == null)
            {
                return;
            }

            if (!_soloAnimation.IsPlaying)
            {
                _soloAnimation.Play();
            }

            _soloAnimation.NormalizedTime = Mathf.Repeat(normalizedPhase, 1f);
        }
    }
}
