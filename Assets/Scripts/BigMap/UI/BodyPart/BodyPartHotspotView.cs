using cfg.demo;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    public sealed class BodyPartHotspotView : MonoBehaviour
    {
        public EBodyPart PartId = EBodyPart.None;
        public Button ClickButton;
        public Image HighlightImage;
        public TextMeshProUGUI LabelText;

        System.Action<EBodyPart> _onClick;

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
            if (HighlightImage != null)
            {
                HighlightImage.color = selected
                    ? new Color(0.55f, 0.42f, 0.78f, 0.85f)
                    : new Color(0.2f, 0.18f, 0.28f, 0.55f);
            }
        }
    }
}
