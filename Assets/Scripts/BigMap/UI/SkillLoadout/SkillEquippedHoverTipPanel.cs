using My.UI;
using My.UI.SkillLoadout;
using TMPro;
using UnityEngine;

namespace My.UI.SkillLoadout
{
    public sealed class SkillEquippedHoverTipPanel : MonoBehaviour, IHoverTipPanel
    {
        [SerializeField] RectTransform root;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI summaryText;
        [SerializeField] TextMeshProUGUI stateText;
        [SerializeField] TextMeshProUGUI hintText;

        void Awake()
        {
            if (root == null)
            {
                root = transform as RectTransform;
            }
        }

        public void OnHoverTipUpdate(HoverTipParams tipParams, IHoverInfoProvider provider)
        {
            if (root == null)
            {
                root = transform as RectTransform;
            }

            Vector3 anchorPos = provider.TooltipPosition;
            anchorPos.z = 0;
            root.position = anchorPos;
            root.localPosition = new Vector3(root.localPosition.x, root.localPosition.y, 0);

            ClearTexts();

            if (provider is not SkillEquippedHoverProvider skillHover)
            {
                return;
            }

            RefreshContent(skillHover);
        }

        void RefreshContent(SkillEquippedHoverProvider provider)
        {
            SetText(titleText, provider.GetDisplayName());
            SetText(summaryText, provider.GetSummaryText());
            SetText(stateText, provider.GetStateText());
            SetText(hintText, provider.GetHintText());
        }

        void ClearTexts()
        {
            SetText(titleText, string.Empty);
            SetText(summaryText, string.Empty);
            SetText(stateText, string.Empty);
            SetText(hintText, string.Empty);
        }

        static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
