using My.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace My.SecretBase
{
    public class SecretBaseInteractable : MonoBehaviour
    {
        [SerializeField] private string panelId;
        [SerializeField] private Rect worldBounds = new Rect(-1f, -1f, 2f, 2f);

        [Header("可选：Sprite 命中")]
        [SerializeField] private bool useSpriteHit;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D hitCollider;

        [Header("可选：悬浮高亮")]
        [SerializeField] private bool enableHoverHighlight;
        [SerializeField] [Range(0f, 1f)] private float hoverBrighten = 0.25f;
        [SerializeField] private GameObject highlightOutline;

        Color _normalColor;
        bool _normalColorCached;
        bool _highlighted;

        public string PanelId => panelId;

        public int HitSortOrder
        {
            get
            {
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sortingOrder;
                }

                return 0;
            }
        }

        void Awake()
        {
            ResolveSpriteRefs();
            CacheNormalColor();
        }

        void OnValidate()
        {
            ResolveSpriteRefs();
        }

        void OnDisable()
        {
            SetHighlighted(false);
        }

        void ResolveSpriteRefs()
        {
            if (!useSpriteHit)
            {
                return;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (hitCollider == null)
            {
                hitCollider = GetComponent<Collider2D>();
            }
        }

        void CacheNormalColor()
        {
            if (spriteRenderer == null)
            {
                _normalColorCached = false;
                return;
            }

            _normalColor = spriteRenderer.color;
            _normalColorCached = true;
        }

        public bool HitTest(Vector2 worldPos)
        {
            if (useSpriteHit)
            {
                if (hitCollider != null)
                {
                    return hitCollider.OverlapPoint(worldPos);
                }

                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    return spriteRenderer.bounds.Contains(worldPos);
                }
            }

            return ContainsWorldPoint(worldPos);
        }

        public bool ContainsWorldPoint(Vector2 worldPos)
        {
            var c = (Vector2)transform.position + worldBounds.position;
            var r = new Rect(c, worldBounds.size);
            return r.Contains(worldPos);
        }

        public void SetHighlighted(bool on)
        {
            if (_highlighted == on)
            {
                return;
            }

            _highlighted = on;

            if (highlightOutline != null)
            {
                highlightOutline.SetActive(on);
            }

            if (!enableHoverHighlight || spriteRenderer == null || !_normalColorCached)
            {
                return;
            }

            spriteRenderer.color = on
                ? Color.Lerp(_normalColor, Color.white, hoverBrighten)
                : _normalColor;
        }

        public void TryOpenPanel()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            UIManager.Instance.ShowPanel(panelId);
        }
    }
}
