using My.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class WorldMapLiveMarkerView : MonoBehaviour
    {
        const float MarkerSize = 14f;

        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] GameObject labelRoot;

        RectTransform _rt;

        public long SourceEntityId { get; private set; }

        void Awake()
        {
            _rt = transform as RectTransform;
            if (icon == null)
            {
                icon = GetComponent<Image>();
            }

            if (label == null && labelRoot != null)
            {
                label = labelRoot.GetComponent<TextMeshProUGUI>();
            }
        }

        public void Bind(WorldMapMarkerData data, Vector2 anchoredPos)
        {
            SourceEntityId = data.sourceEntityId;
            if (_rt != null)
            {
                _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
                _rt.pivot = new Vector2(0.5f, 0.5f);
                _rt.sizeDelta = new Vector2(MarkerSize, MarkerSize);
                _rt.anchoredPosition = anchoredPos;
            }

            if (icon != null)
            {
                icon.color = ColorForKind(data.kind);
            }

            var hasLabel = !string.IsNullOrEmpty(data.label);
            if (labelRoot != null)
            {
                labelRoot.SetActive(hasLabel);
            }

            if (label != null && hasLabel)
            {
                label.text = data.label;
            }
        }

        public void SetAnchoredPosition(Vector2 pos)
        {
            if (_rt != null)
            {
                _rt.anchoredPosition = pos;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        static Color ColorForKind(WorldMapLandmarkKind k)
        {
            return k switch
            {
                WorldMapLandmarkKind.Player => new Color(0.3f, 1f, 0.45f),
                WorldMapLandmarkKind.MajorInteract => new Color(0.45f, 0.75f, 1f),
                WorldMapLandmarkKind.MajorBoss => new Color(1f, 0.45f, 0.35f),
                _ => Color.gray
            };
        }
    }
}
