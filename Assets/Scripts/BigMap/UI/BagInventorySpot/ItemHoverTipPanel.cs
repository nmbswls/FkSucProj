using TMPro;
using UnityEngine;

namespace My.UI
{
    public class ItemHoverTipPanel : MonoBehaviour, IHoverTipPanel
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

            if (TitleText == null)
            {
                return;
            }

            if (provider is ItemCellHoverProvider itemHover)
            {
                TitleText.text = itemHover.GetDisplayName();
                return;
            }

            TitleText.text = string.Empty;
        }
    }
}
