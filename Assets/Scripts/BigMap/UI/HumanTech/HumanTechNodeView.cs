using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.HumanTech
{
    public sealed class HumanTechNodeView : MonoBehaviour
    {
        int _nodeId;
        HumanTechTreeView _host;
        Button _actionButton;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _levelText;
        TextMeshProUGUI _actionText;

        public void Bind(int nodeId, HumanTechTreeView host)
        {
            _nodeId = nodeId;
            _host = host;
            var buttons = GetComponentsInChildren<Button>(true);
            _actionButton = buttons.Length > 0 ? buttons[0] : null;
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            _titleText = texts.Length > 0 ? texts[0] : null;
            _levelText = texts.Length > 1 ? texts[1] : null;
            _actionText = texts.Length > 2 ? texts[2] : null;
            if (_actionButton != null)
            {
                _actionButton.onClick.RemoveAllListeners();
                _actionButton.onClick.AddListener(OnActionClicked);
            }
        }

        public void Refresh(HumanCivilizationSystem progression)
        {
            var node = CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(_nodeId);
            int current = progression?.GetTechNodeLevel(_nodeId) ?? 0;
            int max = node?.MaxLevel ?? 1;
            var state = progression?.GetTechNodeVisualState(_nodeId) ?? HumanTechNodeVisualState.Locked;
            if (_titleText != null) _titleText.text = node?.DisplayName ?? $"Tech {_nodeId}";
            if (_levelText != null) _levelText.text = $"Lv{current}/{max}";
            if (_actionText != null) _actionText.text = current >= max ? "Max" : "Unlock";
            if (_actionButton != null) _actionButton.interactable = state == HumanTechNodeVisualState.Unlockable;
        }

        void OnActionClicked() { _host?.TryUnlock(_nodeId); }
    }
}