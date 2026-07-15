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

        public static CultTechNodeView Create(Transform parent, int nodeId, CultTechTreeView host)
        {
            var go = new GameObject($"CultTechNode_{nodeId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(150f, 86f);
            go.GetComponent<Image>().color = new Color(0.18f, 0.08f, 0.2f, 0.95f);
            AddText(go.transform, "Title", new Vector2(4f, 48f), new Vector2(-4f, -6f), 16f);
            AddText(go.transform, "Level", new Vector2(4f, 24f), new Vector2(-4f, -30f), 13f);
            var action = AddText(go.transform, "Action", new Vector2(4f, 3f), new Vector2(-4f, -54f), 12f);
            action.alignment = TextAlignmentOptions.Center;
            var view = go.AddComponent<CultTechNodeView>(); view.Bind(nodeId, host); return view;
        }

        static TextMeshProUGUI AddText(Transform parent, string name, Vector2 min, Vector2 max, float size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max;
            var text = go.AddComponent<TextMeshProUGUI>(); text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }

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
