using My.UI;
using TMPro;
using UnityEngine;

namespace My.UI.Rune
{
    public sealed class RuneUpgradeHoverTipPanel : MonoBehaviour, IHoverTipPanel
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

            if (provider is not RuneUpgradeHoverProvider runeHover)
            {
                return;
            }

            RefreshContent(runeHover);
        }

        // prefab 拼装定稿后，在此接入各字段赋值
        void RefreshContent(RuneUpgradeHoverProvider provider)
        {
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
