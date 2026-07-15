using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    public sealed class CultSeatTechNodeView : MonoBehaviour
    {
        [SerializeField] Button selectButton;
        [SerializeField] Image nodeBackground;
        [SerializeField] Image selectionFrame;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] TextMeshProUGUI hintText;
        CultSeatTechNodeBinder _binder;
        AncientSeatTreeView _host;
        int _seatId;

        public int NodeId => _binder != null ? _binder.SeatTechNodeId : 0;

        void Awake()
        {
            _binder ??= GetComponent<CultSeatTechNodeBinder>();
            selectButton ??= GetComponent<Button>();
            nodeBackground ??= GetComponent<Image>();
            titleText ??= transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            levelText ??= transform.Find("Level")?.GetComponent<TextMeshProUGUI>();
            hintText ??= transform.Find("SeatHint")?.GetComponent<TextMeshProUGUI>();
            selectionFrame ??= transform.Find("SelectionFrame")?.GetComponent<Image>();
        }

        public void Bind(int seatId, AncientSeatTreeView host)
        {
            Awake();
            _seatId = seatId;
            _host = host;
            if (selectButton == null) return;
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _host?.SelectNode(NodeId));
        }

        public void Refresh(DemonCultSystem cult)
        {
            var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(NodeId);
            var current = cult?.GetSeatTechNodeLevel(_seatId, NodeId) ?? 0;
            var state = cult?.GetSeatTechNodeVisualState(_seatId, NodeId) ?? CultTechNodeVisualState.Locked;
            if (titleText != null) titleText.text = node?.DisplayName ?? $"Seat {NodeId}";
            if (levelText != null) levelText.text = $"Lv{current}/{node?.MaxLevel ?? 1}";
            if (hintText != null) hintText.text = state switch
            {
                CultTechNodeVisualState.Unlocked => "???",
                CultTechNodeVisualState.Unlockable => "???",
                CultTechNodeVisualState.InsufficientFaith => "????",
                _ => "?????",
            };
            if (nodeBackground != null)
            {
                nodeBackground.color = state switch
                {
                    CultTechNodeVisualState.Unlocked => new Color(0.55f, 0.22f, 0.32f, 0.95f),
                    CultTechNodeVisualState.Unlockable => new Color(0.42f, 0.28f, 0.55f, 0.95f),
                    _ => new Color(0.12f, 0.1f, 0.14f, 0.85f),
                };
            }
            if (selectionFrame != null) selectionFrame.enabled = _host != null && _host.SelectedNodeId == NodeId;
        }
    }
}
