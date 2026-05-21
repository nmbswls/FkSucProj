using UnityEngine;

namespace My.SecretBase
{
    // 设施/NPC 共用的点击检测与高亮
    public abstract class SecretBaseClickTargetBase : MonoBehaviour, ISecretBaseClickTarget
    {
        Collider2D _collider;
        SpriteRenderer _sprite;
        Color _normalColor;
        bool _hasNormalColor;
        int _sortOrder;

        public int SortOrder => _sortOrder;

        protected void CacheRefs()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider == null)
            {
                _collider = GetComponentInChildren<Collider2D>();
            }

            _sprite = GetComponent<SpriteRenderer>();
            if (_sprite == null)
            {
                _sprite = GetComponentInChildren<SpriteRenderer>();
            }

            if (_sprite != null)
            {
                _normalColor = _sprite.color;
                _hasNormalColor = true;
            }
        }

        protected void ApplySortOrder(int sortOrder)
        {
            _sortOrder = sortOrder;
            if (_sprite != null)
            {
                _sprite.sortingOrder = sortOrder;
            }
        }

        void OnDisable()
        {
            SetHighlight(false);
        }

        public bool ContainsPoint(Vector2 worldPos)
        {
            if (_collider != null)
            {
                return _collider.OverlapPoint(worldPos);
            }

            return _sprite != null && _sprite.sprite != null && _sprite.bounds.Contains(worldPos);
        }

        public void SetHighlight(bool on)
        {
            if (!_hasNormalColor || _sprite == null)
            {
                return;
            }

            _sprite.color = on ? Color.Lerp(_normalColor, Color.white, 0.25f) : _normalColor;
        }

        public abstract void OnClick();
    }
}
