using TMPro;
using UnityEngine;

namespace My.UI
{
    public class ItemHoverTipPanel : MonoBehaviour, IHoverTipPanel
    {
        public TextMeshProUGUI TitleText;
        public RectTransform Root;

        [SerializeField] Vector2 compactSize = new(160f, 36f);
        [SerializeField] float compactFontSize = 16f;
        [SerializeField] float normalFontSize = 16f;

        Vector2 _defaultRootSize;
        float _defaultFontSize;
        bool _defaultsCaptured;

        void Awake()
        {
            if (Root == null)
            {
                Root = transform as RectTransform;
            }

            CaptureDefaults();
        }

        void CaptureDefaults()
        {
            if (_defaultsCaptured || Root == null)
            {
                return;
            }

            _defaultRootSize = Root.sizeDelta;
            if (TitleText != null)
            {
                _defaultFontSize = TitleText.fontSize;
            }
            else
            {
                _defaultFontSize = normalFontSize;
            }

            _defaultsCaptured = true;
        }

        public void OnHoverTipUpdate(HoverTipParams tipParams, IHoverInfoProvider provider)
        {
            if (Root == null)
            {
                Root = transform as RectTransform;
            }

            CaptureDefaults();

            Vector3 anchorPos = provider.TooltipPosition;
            anchorPos.z = 0;
            Root.position = anchorPos;
            Root.localPosition = new Vector3(Root.localPosition.x, Root.localPosition.y, 0);

            if (TitleText == null)
            {
                return;
            }

            string title = string.Empty;
            string detail = string.Empty;
            bool nameOnly = false;
            if (provider is ItemCellHoverProvider itemHover)
            {
                title = itemHover.GetDisplayName();
                detail = itemHover.GetDetailText();
                nameOnly = itemHover.IsNameOnlyTip;
            }
            else if (provider is ItemIdHoverProvider itemIdHover)
            {
                title = itemIdHover.GetDisplayName();
                detail = itemIdHover.GetDetailText();
                nameOnly = itemIdHover.IsNameOnlyTip;
            }

            if (nameOnly)
            {
                TitleText.text = title;
                TitleText.fontSize = compactFontSize;
                TitleText.enableWordWrapping = false;
                Root.sizeDelta = compactSize;
            }
            else
            {
                TitleText.text = string.IsNullOrEmpty(detail) ? title : $"{title}\n{detail}";
                TitleText.fontSize = _defaultFontSize > 0f ? _defaultFontSize : normalFontSize;
                TitleText.enableWordWrapping = true;
                Root.sizeDelta = _defaultRootSize;
            }
        }
    }
}
