using TMPro;
using UnityEngine;

namespace My.UI.Talent
{
    public sealed class TalentHoverTipPanel : MonoBehaviour, IHoverTipPanel
    {
        public TextMeshProUGUI TitleText;
        public RectTransform Root;

        void Awake()
        {
            if (Root == null)
            {
                Root = transform as RectTransform;
            }
        }

        public void OnHoverTipUpdate(HoverTipParams tipParams, IHoverInfoProvider provider)
        {
            if (Root == null)
            {
                Root = transform as RectTransform;
            }

            Vector3 anchorPos = provider.TooltipPosition;
            anchorPos.z = 0;
            Root.position = anchorPos;
            Root.localPosition = new Vector3(Root.localPosition.x, Root.localPosition.y, 0);

            if (TitleText == null || provider is not TalentNodeHoverProvider talentHover)
            {
                if (TitleText != null)
                {
                    TitleText.text = string.Empty;
                }

                return;
            }

            string title = talentHover.GetDisplayName();
            string detail = talentHover.GetDetailText();
            TitleText.text = string.IsNullOrEmpty(detail) ? title : $"{title}\n{detail}";
        }
    }
}
