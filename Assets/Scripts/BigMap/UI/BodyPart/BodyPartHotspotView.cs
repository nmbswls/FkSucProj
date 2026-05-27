using cfg.demo;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    public sealed class BodyPartHotspotView : MonoBehaviour
    {
        static readonly Color SelectedHighlightColor = new Color(0.55f, 0.42f, 0.78f, 0.85f);
        static readonly Color NormalHighlightColor = new Color(0.2f, 0.18f, 0.28f, 0.55f);
        static readonly Color LockedHighlightColor = new Color(0.15f, 0.14f, 0.18f, 0.4f);

        public EBodyPart PartId = EBodyPart.None;
        public Button ClickButton;
        public Image HighlightImage;
        public TextMeshProUGUI LabelText;

        [SerializeField] RectTransform focusAnchor;

        bool _selected;
        bool _locked;
        System.Action<EBodyPart> _onClick;

        public RectTransform FocusRect => focusAnchor != null ? focusAnchor : transform as RectTransform;

        public void Bind(EBodyPart partId, System.Action<EBodyPart> onClick)
        {
            PartId = partId;
            _onClick = onClick;

            if (LabelText != null)
            {
                var def = BodyPartCatalog.GetPartDef(partId);
                LabelText.text = def != null && !string.IsNullOrEmpty(def.DisplayName)
                    ? def.DisplayName
                    : partId.ToString();
            }

            if (ClickButton != null)
            {
                ClickButton.onClick.RemoveAllListeners();
                ClickButton.onClick.AddListener(() => _onClick?.Invoke(PartId));
            }
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyHighlightColor();
        }

        public void SetLocked(bool locked)
        {
            _locked = locked;
            if (ClickButton != null)
            {
                ClickButton.interactable = !locked;
            }

            ApplyHighlightColor();
        }

        void ApplyHighlightColor()
        {
            if (HighlightImage == null)
            {
                return;
            }

            if (_locked)
            {
                HighlightImage.color = LockedHighlightColor;
                return;
            }

            HighlightImage.color = _selected ? SelectedHighlightColor : NormalHighlightColor;
        }
    }
}
