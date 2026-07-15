using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    public sealed class CultTechNodeView : MonoBehaviour
    {
        [SerializeField] Button selectButton;
        [SerializeField] Button actionButton;
        [SerializeField] Image nodeBackground;
        [SerializeField] Image selectionFrame;
        [SerializeField] Image nodeIcon;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] TextMeshProUGUI actionText;
        [SerializeField] CultTechNodeHoverView hoverView;

        CultTechTreeView _host;
        CultTechNodeBinder _binder;

        public int CultNodeId => _binder != null ? _binder.CultNodeId : 0;

        void Awake()
        {
            _binder = GetComponent<CultTechNodeBinder>();
            selectButton ??= GetComponent<Button>();
            actionButton ??= transform.Find("ActionButton")?.GetComponent<Button>();
            nodeBackground ??= GetComponent<Image>();
            selectionFrame ??= transform.Find("SelectionFrame")?.GetComponent<Image>();
            nodeIcon ??= transform.Find("NodeIcon")?.GetComponent<Image>();
            titleText ??= transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            levelText ??= transform.Find("Level")?.GetComponent<TextMeshProUGUI>();
            actionText ??= transform.Find("ActionButton/Label")?.GetComponent<TextMeshProUGUI>();
            hoverView ??= GetComponent<CultTechNodeHoverView>();
        }

        public void BindHost(CultTechTreeView host)
        {
            _host = host;
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSelectClicked);
            }

            if (actionButton != null && actionButton != selectButton)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnActionClicked);
            }
        }

        public void Refresh(DemonCultSystem cult, bool selected)
        {
            var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(CultNodeId);
            int current = cult?.GetTechNodeLevel(CultNodeId) ?? 0;
            int max = node?.MaxLevel ?? 1;
            var state = cult?.GetTechNodeVisualState(CultNodeId) ?? CultTechNodeVisualState.Locked;
            hoverView?.Configure(CultNodeId, cult);

            if (titleText != null) titleText.text = node?.DisplayName ?? $"Cult {CultNodeId}";
            if (levelText != null) levelText.text = $"Lv{current}/{max}";
            if (actionText != null) actionText.text = current >= max ? "已铭刻" : "铭刻";
            if (actionButton != null) actionButton.interactable = state == CultTechNodeVisualState.Unlockable;

            if (nodeBackground != null)
            {
                nodeBackground.color = state switch
                {
                    CultTechNodeVisualState.Unlocked => new Color(.58f, .25f, .7f, .98f),
                    CultTechNodeVisualState.Unlockable => new Color(.28f, .2f, .42f, .98f),
                    CultTechNodeVisualState.InsufficientFaith => new Color(.2f, .14f, .25f, .95f),
                    _ => new Color(.09f, .07f, .14f, .92f),
                };
            }

            if (nodeIcon != null)
            {
                nodeIcon.color = state == CultTechNodeVisualState.Unlocked
                    ? new Color(1f, .65f, .95f, 1f)
                    : new Color(.7f, .55f, .9f, .9f);
            }

            if (selectionFrame != null) selectionFrame.enabled = selected;
        }

        void OnSelectClicked() => _host?.OnNodeClicked(CultNodeId);

        void OnActionClicked() => _host?.TryUnlock(CultNodeId);
    }
}