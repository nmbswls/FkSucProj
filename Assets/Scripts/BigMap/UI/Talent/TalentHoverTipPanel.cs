using My.UI;
using TMPro;
using UnityEngine;

namespace My.UI.Talent
{
    public sealed class TalentHoverTipPanel : MonoBehaviour, IHoverTipPanel
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

            if (provider is not TalentNodeHoverProvider talentHover)
            {
                return;
            }

            RefreshContent(talentHover);
        }

        void RefreshContent(TalentNodeHoverProvider provider)
        {
            if (!TalentNodeDisplayHelper.TryBuildSnapshot(
                    provider.NodeId,
                    provider.Progression,
                    out var snapshot))
            {
                return;
            }

            SetText(titleText, TalentNodeDisplayHelper.GetDisplayName(snapshot));
            SetText(summaryText, TalentNodeDisplayHelper.BuildHoverSummary(snapshot));
            SetText(stateText, TalentNodeDisplayHelper.BuildHoverStateLabel(snapshot));
            SetText(hintText, TalentNodeDisplayHelper.BuildHoverHint(snapshot, provider.Progression));
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
