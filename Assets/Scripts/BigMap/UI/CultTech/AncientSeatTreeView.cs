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

        public void SetVisible(bool visible)
        {
            if (nodeViewContainer != null) nodeViewContainer.gameObject.SetActive(visible);
            if (faithText != null) faithText.gameObject.SetActive(visible);
            if (detailTitle != null) detailTitle.gameObject.SetActive(visible);
            if (detailBody != null) detailBody.gameObject.SetActive(visible);
            if (detailStatusHint != null) detailStatusHint.gameObject.SetActive(visible);
            if (unlockButton != null) unlockButton.gameObject.SetActive(visible);
            var seatRoot = transform.Find("SeatRoot"); if (seatRoot != null) seatRoot.gameObject.SetActive(visible);
        }

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
                var seat = seats[i];
                var unlocked = _cult.IsSeatUnlocked(seat.SeatId);
                var go = new GameObject($"Seat_{seat.SeatId}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(root, false);
                var rect = (RectTransform)go.transform; rect.anchorMin = new Vector2(0f, 0.5f); rect.anchorMax = new Vector2(0f, 0.5f); rect.pivot = new Vector2(0f, 0.5f); rect.anchoredPosition = new Vector2(i * 168f, 0f); rect.sizeDelta = new Vector2(158f, 62f);
                go.GetComponent<Image>().color = unlocked ? new Color(0.2f, 0.13f, 0.18f, 1f) : new Color(0.08f, 0.08f, 0.1f, 1f);
                var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image)); portraitGo.transform.SetParent(go.transform, false);
                var portraitRect = (RectTransform)portraitGo.transform; portraitRect.anchorMin = new Vector2(0f, 0.5f); portraitRect.anchorMax = new Vector2(0f, 0.5f); portraitRect.pivot = new Vector2(0f, 0.5f); portraitRect.anchoredPosition = new Vector2(7f, 0f); portraitRect.sizeDelta = new Vector2(48f, 48f);
                var portrait = portraitGo.GetComponent<Image>(); portrait.sprite = Resources.Load<Sprite>($"UI/Cult/Seats/seat_{seat.SeatId}"); portrait.color = unlocked ? Color.white : new Color(0.32f, 0.32f, 0.36f, 1f);
                var statusGo = new GameObject("StatusIcon", typeof(RectTransform), typeof(Image)); statusGo.transform.SetParent(go.transform, false);
                var statusRect = (RectTransform)statusGo.transform; statusRect.anchorMin = new Vector2(1f, 1f); statusRect.anchorMax = new Vector2(1f, 1f); statusRect.pivot = new Vector2(1f, 1f); statusRect.anchoredPosition = new Vector2(-4f, -4f); statusRect.sizeDelta = new Vector2(20f, 20f); statusGo.GetComponent<Image>().sprite = Resources.Load<Sprite>(unlocked ? "UI/Cult/Icons/unlocked" : "UI/Cult/Icons/locked");
                var text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); text.transform.SetParent(go.transform, false); text.rectTransform.anchorMin = new Vector2(0f, 0f); text.rectTransform.anchorMax = new Vector2(1f, 1f); text.rectTransform.offsetMin = new Vector2(60f, 4f); text.rectTransform.offsetMax = new Vector2(-4f, -4f); text.text = $"{(unlocked ? "???" : "???")}\n{seat.Title}"; text.fontSize = 13f; text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
                go.GetComponent<Button>().onClick.AddListener(() => SelectSeat(seat.SeatId));
            }
        }

        void Rebuild()
        {
            EnsureLayout(); ClearNodes();
            var table = CfgMgr.Cfgs?.TbCultSeatTechNode?.DataList;
            if (_cult == null || table == null) return;
            var positions = new Dictionary<int, Vector2>();
            foreach (var row in table)
            {
                if (row == null || row.SeatId != _seatId) continue;
                var radius = RingRadius[Mathf.Clamp(row.Ring, 0, RingRadius.Length - 1)];
                var rad = row.AngleDeg * Mathf.Deg2Rad;
                var pos = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
                var view = CultSeatTechNodeView.Create(nodeViewContainer, _seatId, row.NodeId, this);
                if (view.transform is RectTransform rect) { rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = pos; }
                _nodes.Add(view); positions[row.NodeId] = pos;
            }
            if (_selectedNodeId <= 0) foreach (var row in table) if (row != null && row.SeatId == _seatId) { _selectedNodeId = row.NodeId; break; }
            Refresh();
        }

        void TryUnlock()
        {
            if (_cult == null) return;
            if (!_cult.IsSeatUnlocked(_seatId))
            {
                if (!_cult.TryUnlockSeat(_seatId, out var seatReason)) Debug.LogWarning($"Seat unlock failed: {seatReason}");
            }
            else if (!_cult.TryUnlockSeatTechNode(_seatId, _selectedNodeId, out var reason))
            {
                Debug.LogWarning($"Seat tech unlock failed: {reason}");
            }
            BuildSeatButtons(); Refresh();
        }

        void ShowDetail()
        {
            var seat = CfgMgr.Cfgs?.TbCultAncientSeat?.GetOrDefault(_seatId);
            var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(_selectedNodeId);
            if (seat == null) return;
            if (!_cult.IsSeatUnlocked(_seatId))
            {
                if (detailTitle != null) detailTitle.text = seat.Title;
                if (detailBody != null) detailBody.text = $"{seat.Desc}\n\n???????{seat.UnlockFaithCost}";
                if (detailStatusHint != null) detailStatusHint.text = "??????";
                if (unlockButton != null) unlockButton.interactable = _cult.Faith >= seat.UnlockFaithCost;
                return;
            }
            if (node == null)
            {
                if (detailTitle != null) detailTitle.text = seat.Title;
                if (detailBody != null) detailBody.text = seat.Desc;
                if (detailStatusHint != null) detailStatusHint.text = "?????";
                if (unlockButton != null) unlockButton.interactable = false;
                return;
            }
            int current = _cult.GetSeatTechNodeLevel(_seatId, node.NodeId);
            var state = _cult.GetSeatTechNodeVisualState(_seatId, node.NodeId);
            var next = CfgMgr.Cfgs?.TbCultSeatTechNodeLevel?.Get(node.NodeId, current + 1);
            if (detailTitle != null) detailTitle.text = node.DisplayName;
            if (detailBody != null) detailBody.text = $"{node.Desc}\n?????{current}/{node.MaxLevel}\n???{next?.EffectDesc ?? "?"}\n?????{next?.FaithCost.ToString() ?? "?"}";
            if (detailStatusHint != null) detailStatusHint.text = state == CultTechNodeVisualState.Unlocked ? "???" : state == CultTechNodeVisualState.Unlockable ? "????" : "????????";
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
            EnsureFaithIcon();
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

        void EnsureFaithIcon()
        {
            if (transform.Find("FaithIcon") != null) return;
            var go = new GameObject("FaithIcon", typeof(RectTransform), typeof(Image)); go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = new Vector2(0.02f, 0.94f); rect.anchorMax = new Vector2(0.02f, 0.94f); rect.pivot = new Vector2(0f, 0.5f); rect.anchoredPosition = new Vector2(0f, -12f); rect.sizeDelta = new Vector2(28f, 28f);
            go.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI/Cult/Icons/faith");
        }

        TextMeshProUGUI EnsureText(string name, Vector2 min, Vector2 max, float size, Transform parent = null)
        {
            parent ??= transform; var existing = parent.Find(name)?.GetComponent<TextMeshProUGUI>(); if (existing != null) return existing;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<TextMeshProUGUI>(); text.fontSize = size; text.enableWordWrapping = true; text.raycastTarget = false; return text;
        }
    }
}
