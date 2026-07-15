using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    public sealed class CultTechNodeView : MonoBehaviour
    {
        int _nodeId;
        CultTechTreeView _host;
        Button _actionButton;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _levelText;
        TextMeshProUGUI _actionText;
        Image _bgImage;

        public void Bind(int nodeId, CultTechTreeView host)
        {
            _nodeId = nodeId;
            _host = host;
            var buttons = GetComponentsInChildren<Button>(true);
            _actionButton = buttons.Length > 0 ? buttons[0] : null;
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            _titleText = texts.Length > 0 ? texts[0] : null;
            _levelText = texts.Length > 1 ? texts[1] : null;
            _actionText = texts.Length > 2 ? texts[2] : null;
            _bgImage = GetComponent<Image>() ?? transform.Find("Image")?.GetComponent<Image>();

            if (_actionButton != null)
            {
                _actionButton.onClick.RemoveAllListeners();
                _actionButton.onClick.AddListener(OnActionClicked);
            }
        }

        public void Refresh(DemonCultSystem cult)
        {
            var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(_nodeId);
            int current = cult?.GetTechNodeLevel(_nodeId) ?? 0;
            int max = node?.MaxLevel ?? 1;
            var state = cult?.GetTechNodeVisualState(_nodeId) ?? CultTechNodeVisualState.Locked;

            if (_titleText != null)
            {
                _titleText.text = node?.DisplayName ?? $"Cult {_nodeId}";
            }

            if (_levelText != null)
            {
                _levelText.text = $"Lv{current}/{max}";
            }

            if (_actionText != null)
            {
                _actionText.text = current >= max ? "Max" : "Unlock";
            }

            if (_actionButton != null)
            {
                _actionButton.interactable = state == CultTechNodeVisualState.Unlockable
                    || state == CultTechNodeVisualState.InsufficientFaith
                    || state == CultTechNodeVisualState.Unlocked;
            }

            if (_bgImage != null)
            {
                _bgImage.color = state switch
                {
                    CultTechNodeVisualState.Unlocked => new Color(0.55f, 0.22f, 0.32f, 0.95f),
                    CultTechNodeVisualState.Unlockable => new Color(0.42f, 0.28f, 0.55f, 0.95f),
                    CultTechNodeVisualState.InsufficientFaith => new Color(0.28f, 0.16f, 0.22f, 0.9f),
                    _ => new Color(0.12f, 0.1f, 0.14f, 0.85f),
                };
            }
        }

        void OnActionClicked()
        {
            _host?.OnNodeClicked(_nodeId);
        }
    }
}
