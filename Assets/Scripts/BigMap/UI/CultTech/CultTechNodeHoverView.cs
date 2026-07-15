using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace My.UI.CultTech
{
    public sealed class CultTechNodeHoverView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] GameObject hoverTip;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI summaryText;
        [SerializeField] TextMeshProUGUI statusText;

        int _nodeId;
        DemonCultSystem _cult;

        void Awake()
        {
            hoverTip ??= transform.Find("HoverTip")?.gameObject;
            titleText ??= transform.Find("HoverTip/Title")?.GetComponent<TextMeshProUGUI>();
            summaryText ??= transform.Find("HoverTip/Summary")?.GetComponent<TextMeshProUGUI>();
            statusText ??= transform.Find("HoverTip/Status")?.GetComponent<TextMeshProUGUI>();
            SetTipVisible(false);
        }

        public void Configure(int nodeId, DemonCultSystem cult)
        {
            _nodeId = nodeId;
            _cult = cult;
            if (hoverTip != null && hoverTip.activeSelf)
            {
                RefreshContent();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_nodeId <= 0)
            {
                return;
            }

            RefreshContent();
            SetTipVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetTipVisible(false);
        }

        void RefreshContent()
        {
            var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(_nodeId);
            var state = _cult?.GetTechNodeVisualState(_nodeId) ?? CultTechNodeVisualState.Locked;
            if (titleText != null)
            {
                titleText.text = node?.DisplayName ?? $"教团节点 {_nodeId}";
            }

            if (summaryText != null)
            {
                summaryText.text = TrimSummary(node?.Desc);
            }

            if (statusText != null)
            {
                statusText.text = state switch
                {
                    CultTechNodeVisualState.Unlocked => "已铭刻",
                    CultTechNodeVisualState.Unlockable => "可铭刻",
                    CultTechNodeVisualState.InsufficientFaith => "信仰不足",
                    _ => "尚未满足前置",
                };
            }
        }

        static string TrimSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                return "暂无教义摘要";
            }

            summary = summary.Replace("\n", " ").Trim();
            return summary.Length <= 36 ? summary : summary.Substring(0, 35) + "…";
        }

        void SetTipVisible(bool visible)
        {
            if (hoverTip != null)
            {
                hoverTip.SetActive(visible);
            }
        }
    }
}