using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    public sealed class AncientSeatTreeView : MonoBehaviour
    {
        static readonly float[] RingRadius = { 0f, 180f, 300f, 400f };
        [SerializeField] Transform nodeViewContainer;
        [SerializeField] TextMeshProUGUI faithText;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailBody;
        [SerializeField] TextMeshProUGUI detailStatusHint;
        [SerializeField] Button unlockButton;
        readonly List<CultSeatTechNodeView> _nodes = new();
        DemonCultSystem _cult;
        int _seatId;
        int _selectedNodeId;

        public void Bind(DemonCultSystem cult)
        {
            _cult = cult; EnsureLayout(); BuildSeatButtons(); SelectSeat(_seatId > 0 ? _seatId : FindFirstSeat());
        }

        public void SelectSeat(int seatId)
        {
            _seatId = seatId; _selectedNodeId = 0; Rebuild();
        }

        public void SelectNode(int nodeId)
        {
            _selectedNodeId = nodeId; ShowDetail();
            if (_cult?.GetSeatTechNodeVisualState(_seatId, nodeId) == CultTechNodeVisualState.Unlockable) TryUnlock();
        }

        public void Refresh()
        {
            if (_cult == null) return;
            if (faithText != null) faithText.text = $"信仰 {_cult.Faith}  ·  火团 {_cult.GetTechNodeCount(_seatId)}";
            foreach (var node in _nodes) node?.Refresh(_cult);
            ShowDetail();
        }

        int FindFirstSeat()
        {
            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            return seats != null && seats.Count > 0 ? seats[0].SeatId : 0;
        }

        void BuildSeatButtons()
        {
            var root = transform.Find("SeatRoot") as RectTransform;
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            if (seats == null) return;
            for (int i = 0; i < seats.Count; i++)
            {
                var seat = seats[i]; var go = new GameObject($"Seat_{seat.SeatId}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(root, false); var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0f, 0.5f); rect.anchorMax = new Vector2(0f, 0.5f); rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(i * 142f, 0f); rect.sizeDelta = new Vector2(132f, 42f);
                go.GetComponent<Image>().color = new Color(0.18f, 0.12f, 0.15f, 1f);
                var text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); text.transform.SetParent(go.transform, false);
                text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = new Vector2(4f, 2f); text.rectTransform.offsetMax = new Vector2(-4f, -2f);
                text.text = $"{(_cult.IsSeatUnlocked(seat.SeatId) ? "火" : "封")} {seat.Title}"; text.fontSize = 13f; text.alignment = TextAlignmentOptions.Center;
                go.GetComponent<Button>().onClick.AddListener(() => SelectSeat(seat.SeatId));
            }
        }

        void Rebuild()
        {
            EnsureLayout(); ClearNodes();
            var prefab = Resources.Load<GameObject>("UI/Prefabs/PlayerProgressionHubPanelSub/TalentNode_View");
            var table = CfgMgr.Cfgs?.TbCultSeatTechNode?.DataList;
            if (_cult == null || prefab == null || table == null) return;
            var positions = new Dictionary<int, Vector2>();
            foreach (var row in table)
            {
                if (row == null || row.SeatId != _seatId) continue;
                var go = Instantiate(prefab, nodeViewContainer); go.name = $"CultSeatTechNode_{row.NodeId}";
                var pos = new Vector2(Mathf.Cos(row.AngleDeg * Mathf.Deg2Rad) * RingRadius[Mathf.Clamp(row.Ring, 0, RingRadius.Length - 1)], Mathf.Sin(row.AngleDeg * Mathf.Deg2Rad) * RingRadius[Mathf.Clamp(row.Ring, 0, RingRadius.Length - 1)]);
                if (go.transform is RectTransform rect) { rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = pos; }
                var old = go.GetComponent<My.UI.Talent.TalentTreeNodeView>(); if (old != null) old.enabled = false;
                var view = go.AddComponent<CultSeatTechNodeView>(); view.Bind(_seatId, row.NodeId, this); _nodes.Add(view); positions[row.NodeId] = pos;
            }
            if (_selectedNodeId <= 0) foreach (var row in table) if (row != null && row.SeatId == _seatId) { _selectedNodeId = row.NodeId; break; }
            Refresh();
        }

        void TryUnlock()
        {
            if (_cult != null && !_cult.TryUnlockSeatTechNode(_seatId, _selectedNodeId, out var reason)) Debug.LogWarning($"Seat tech unlock failed: {reason}");
            Refresh();
        }

        void ShowDetail()
        {
            var seat = CfgMgr.Cfgs?.TbCultAncientSeat?.GetOrDefault(_seatId); var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(_selectedNodeId);
            if (seat == null || node == null) { if (detailTitle != null) detailTitle.text = seat?.Title ?? "古老者之座"; if (detailBody != null) detailBody.text = seat?.Desc ?? string.Empty; return; }
            int current = _cult.GetSeatTechNodeLevel(_seatId, node.NodeId); var state = _cult.GetSeatTechNodeVisualState(_seatId, node.NodeId); var next = CfgMgr.Cfgs?.TbCultSeatTechNodeLevel?.Get(node.NodeId, current + 1);
            if (detailTitle != null) detailTitle.text = node.DisplayName;
            if (detailBody != null) detailBody.text = $"{node.Desc}\n当前等级：{current}/{node.MaxLevel}\n效果：{next?.EffectDesc ?? "—"}\n信仰消耗：{next?.FaithCost.ToString() ?? "—"}";
            if (detailStatusHint != null) detailStatusHint.text = state == CultTechNodeVisualState.Unlocked ? "火团已点亮" : state == CultTechNodeVisualState.Unlockable ? "可以供奉" : "尚未满足供奉条件";
            if (unlockButton != null) unlockButton.interactable = state == CultTechNodeVisualState.Unlockable;
        }

        void ClearNodes() { _nodes.Clear(); if (nodeViewContainer == null) return; for (int i = nodeViewContainer.childCount - 1; i >= 0; i--) Destroy(nodeViewContainer.GetChild(i).gameObject); }
        void EnsureLayout()
        {
            if (transform.Find("Background") == null)
            {
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); background.transform.SetParent(transform, false); background.transform.SetAsFirstSibling();
                var backgroundRect = (RectTransform)background.transform; backgroundRect.anchorMin = Vector2.zero; backgroundRect.anchorMax = Vector2.one; backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;
                background.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.08f, 0.96f);
            }
            if (nodeViewContainer == null) nodeViewContainer = transform.Find("TreeRoot");
            if (nodeViewContainer == null)
            {
                var go = new GameObject("TreeRoot", typeof(RectTransform)); go.transform.SetParent(transform, false); var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.02f, 0.08f); rect.anchorMax = new Vector2(0.68f, 0.84f); rect.offsetMin = rect.offsetMax = Vector2.zero; nodeViewContainer = go.transform;
            }
            if (transform.Find("SeatRoot") == null)
            {
                var go = new GameObject("SeatRoot", typeof(RectTransform)); go.transform.SetParent(transform, false); var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.02f, 0.86f); rect.anchorMax = new Vector2(0.68f, 0.94f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            if (faithText == null) faithText = EnsureText("FaithText", new Vector2(0.02f, 0.94f), new Vector2(0.68f, 0.99f), 20f);
            if (detailTitle == null) detailTitle = EnsureText("DetailTitle", new Vector2(0.72f, 0.78f), new Vector2(0.98f, 0.94f), 22f);
            if (detailBody == null) detailBody = EnsureText("DetailBody", new Vector2(0.72f, 0.3f), new Vector2(0.98f, 0.76f), 15f);
            if (detailStatusHint == null) detailStatusHint = EnsureText("DetailStatusHint", new Vector2(0.72f, 0.18f), new Vector2(0.98f, 0.28f), 15f);
            if (unlockButton == null)
            {
                var go = new GameObject("UnlockButton", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(transform, false); var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.76f, 0.08f); rect.anchorMax = new Vector2(0.94f, 0.16f); rect.offsetMin = rect.offsetMax = Vector2.zero; unlockButton = go.GetComponent<Button>();
                var label = EnsureText("Label", Vector2.zero, Vector2.one, 16f, go.transform); label.text = "供奉火团"; label.alignment = TextAlignmentOptions.Center;
            }
            unlockButton.onClick.RemoveAllListeners(); unlockButton.onClick.AddListener(TryUnlock);
        }

        TextMeshProUGUI EnsureText(string name, Vector2 min, Vector2 max, float size, Transform parent = null)
        {
            parent ??= transform; var existing = parent.Find(name)?.GetComponent<TextMeshProUGUI>(); if (existing != null) return existing;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<TextMeshProUGUI>(); text.fontSize = size; text.enableWordWrapping = true; text.raycastTarget = false; return text;
        }
    }
}
