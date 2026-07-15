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
        int _seatId;
        int _nodeId;
        AncientSeatTreeView _host;
        TextMeshProUGUI _title;
        TextMeshProUGUI _level;
        Image _background;

        public void Bind(int seatId, int nodeId, AncientSeatTreeView host)
        {
            _seatId = seatId; _nodeId = nodeId; _host = host;
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            _title = texts.Length > 0 ? texts[0] : null;
            _level = texts.Length > 1 ? texts[1] : null;
            _background = GetComponent<Image>() ?? transform.Find("Image")?.GetComponent<Image>();
            var button = GetComponentInChildren<Button>(true);
            if (button != null) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => _host.SelectNode(_nodeId)); }
        }

        public void Refresh(DemonCultSystem cult)
        {
            var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(_nodeId);
            var current = cult?.GetSeatTechNodeLevel(_seatId, _nodeId) ?? 0;
            var state = cult?.GetSeatTechNodeVisualState(_seatId, _nodeId) ?? CultTechNodeVisualState.Locked;
            if (_title != null) _title.text = node?.DisplayName ?? $"Seat {_nodeId}";
            if (_level != null) _level.text = $"Lv{current}/{node?.MaxLevel ?? 1}";
            if (_background != null)
            {
                _background.color = state switch
                {
                    CultTechNodeVisualState.Unlocked => new Color(0.55f, 0.22f, 0.32f, 0.95f),
                    CultTechNodeVisualState.Unlockable => new Color(0.42f, 0.28f, 0.55f, 0.95f),
                    _ => new Color(0.12f, 0.1f, 0.14f, 0.85f),
                };
            }
        }
    }
}
