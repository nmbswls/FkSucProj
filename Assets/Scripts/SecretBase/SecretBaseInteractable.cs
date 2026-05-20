using My.UI;
using UnityEngine;

namespace My.SecretBase
{
    // 场景交互点：挂 Collider2D + 填 panelId；无碰撞体时用 SpriteRenderer.bounds。
    public class SecretBaseInteractable : MonoBehaviour
    {
        [SerializeField] string panelId;

        Collider2D _collider;
        SpriteRenderer _sprite;
        Color _normalColor;
        bool _hasNormalColor;

        public int SortOrder => _sprite != null ? _sprite.sortingOrder : 0;

        void Awake()
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

        public void OpenPanel()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                return;
            }

            UIManager.Instance.ShowPanel(panelId);
        }
    }
}
