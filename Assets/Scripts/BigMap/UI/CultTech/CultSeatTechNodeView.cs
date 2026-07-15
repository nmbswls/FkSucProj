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

        public static CultSeatTechNodeView Create(Transform parent, int seatId, int nodeId, AncientSeatTreeView host)
        {
            var go = new GameObject($"CultSeatTechNode_{nodeId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(170f, 96f);
            go.GetComponent<Image>().color = new Color(0.08f, 0.16f, 0.2f, 0.96f);
            AddText(go.transform, "Title", new Vector2(4f, 52f), new Vector2(-4f, -6f), 15f);
            AddText(go.transform, "Level", new Vector2(4f, 28f), new Vector2(-4f, -34f), 13f);
            var hint = AddText(go.transform, "SeatHint", new Vector2(4f, 4f), new Vector2(-4f, -58f), 11f); hint.text = "????";
            var view = go.AddComponent<CultSeatTechNodeView>(); view.Bind(seatId, nodeId, host); return view;
        }

        static TextMeshProUGUI AddText(Transform parent, string name, Vector2 min, Vector2 max, float size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max;
            var text = go.AddComponent<TextMeshProUGUI>(); text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }

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
