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
        static readonly float[] RingRadius = { 0f, 170f, 290f, 390f };

        [SerializeField] Transform nodeViewContainer;
        [SerializeField] TextMeshProUGUI faithText;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailBody;
        [SerializeField] TextMeshProUGUI detailStatusHint;
        [SerializeField] Button unlockButton;
        Button _backButton;
        TextMeshProUGUI _seatHeader;
        readonly List<CultSeatTechNodeView> _nodes = new();
        DemonCultSystem _cult;
        int _seatId;
        int _selectedNodeId;
        bool _seatSelected;
        bool _panelVisible;

        public void SetVisible(bool visible)
        {
            _panelVisible = visible;
            EnsureLayout();
            ApplyLayerVisibility();
        }

        public void Bind(DemonCultSystem cult)
        {
            _cult = cult;
            EnsureLayout();
            BuildSeatButtons();
            if (_seatId <= 0) _seatId = FindFirstSeat();
            _seatSelected = false;
            ClearNodes();
            ApplyLayerVisibility();
        }

        public void SelectSeat(int seatId)
        {
            if (seatId <= 0) return;
            _seatId = seatId;
            _selectedNodeId = 0;
            _seatSelected = true;
            Rebuild();
            ApplyLayerVisibility();
        }

        public void ReturnToSeatStrip()
        {
            _seatSelected = false;
            _selectedNodeId = 0;
            ClearNodes();
            ApplyLayerVisibility();
        }

        public void SelectNode(int nodeId)
        {
            _selectedNodeId = nodeId;
            ShowDetail();
            if (_cult?.GetSeatTechNodeVisualState(_seatId, nodeId) == CultTechNodeVisualState.Unlockable)
                TryUnlock();
        }

        public void Refresh()
        {
            if (_cult == null) return;
            BuildSeatButtons();
            if (!_seatSelected)
            {
                ApplyLayerVisibility();
                return;
            }
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
            for (var index = root.childCount - 1; index >= 0; index--) Destroy(root.GetChild(index).gameObject);
            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            if (seats == null) return;
            var cardWidth = Mathf.Clamp(760f / Mathf.Max(1, seats.Count), 86f, 132f);
            var gap = 16f;
            var totalWidth = seats.Count * cardWidth + Mathf.Max(0, seats.Count - 1) * gap;
            var startX = -totalWidth * 0.5f + cardWidth * 0.5f;
            for (var index = 0; index < seats.Count; index++)
            {
                var seat = seats[index];
                var unlocked = _cult != null && _cult.IsSeatUnlocked(seat.SeatId);
                var objectRoot = new GameObject($"Seat_{seat.SeatId}", typeof(RectTransform), typeof(Image), typeof(Button));
                objectRoot.transform.SetParent(root, false);
                var rect = (RectTransform)objectRoot.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(startX + index * (cardWidth + gap), 0f);
                rect.sizeDelta = new Vector2(cardWidth, 390f);
                objectRoot.GetComponent<Image>().color = unlocked
                    ? new Color(0.2f, 0.12f, 0.2f, 0.98f)
                    : new Color(0.09f, 0.08f, 0.11f, 0.98f);
                var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                portraitObject.transform.SetParent(objectRoot.transform, false);
                var portraitRect = (RectTransform)portraitObject.transform;
                portraitRect.anchorMin = new Vector2(0.12f, 0.2f);
                portraitRect.anchorMax = new Vector2(0.88f, 0.92f);
                portraitRect.offsetMin = portraitRect.offsetMax = Vector2.zero;
                var portrait = portraitObject.GetComponent<Image>();
                portrait.sprite = Resources.Load<Sprite>($"UI/Cult/Seats/seat_{seat.SeatId}");
                portrait.color = unlocked ? Color.white : new Color(0.32f, 0.32f, 0.36f, 1f);
                var label = CreateText(objectRoot.transform, "Label", seat.DisplayName, 14f, TextAlignmentOptions.Center);
                label.rectTransform.anchorMin = new Vector2(0.05f, 0.04f);
                label.rectTransform.anchorMax = new Vector2(0.95f, 0.17f);
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                var status = new GameObject("StatusIcon", typeof(RectTransform), typeof(Image));
                status.transform.SetParent(objectRoot.transform, false);
                var statusRect = (RectTransform)status.transform;
                statusRect.anchorMin = new Vector2(1f, 1f);
                statusRect.anchorMax = new Vector2(1f, 1f);
                statusRect.pivot = new Vector2(1f, 1f);
                statusRect.anchoredPosition = new Vector2(-6f, -6f);
                statusRect.sizeDelta = new Vector2(24f, 24f);
                status.GetComponent<Image>().sprite = Resources.Load<Sprite>(unlocked ? "UI/Cult/Icons/unlocked" : "UI/Cult/Icons/locked");
                var button = objectRoot.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectSeat(seat.SeatId));
            }
        }

        void Rebuild()
        {
            ClearNodes();
            if (_cult == null || nodeViewContainer == null) return;
            var table = CfgMgr.Cfgs?.TbCultSeatTechNode?.DataList;
            if (table == null) return;
            var positions = new Dictionary<int, Vector2>();
            foreach (var row in table)
            {
                if (row == null || row.SeatId != _seatId || row.NodeId <= 0) continue;
                var nodeView = CultSeatTechNodeView.Create(nodeViewContainer, _seatId, row.NodeId, this);
                if (nodeView.transform is RectTransform rect)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = ResolveRadialPosition(row);
                }
                _nodes.Add(nodeView);
                positions[row.NodeId] = ((RectTransform)nodeView.transform).anchoredPosition;
            }
            BuildConnections(positions);
            if (_selectedNodeId <= 0 || CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(_selectedNodeId) == null)
                _selectedNodeId = FindInitialNode(table);
            Refresh();
        }

        static Vector2 ResolveRadialPosition(CultSeatTechNode node)
        {
            var ring = Mathf.Clamp(node.Ring, 0, RingRadius.Length - 1);
            var radius = RingRadius[ring];
            var radians = node.AngleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        int FindInitialNode(List<CultSeatTechNode> table)
        {
            foreach (var row in table)
            {
                if (row != null && row.SeatId == _seatId && row.Ring == 0) return row.NodeId;
            }
            foreach (var row in table)
            {
                if (row != null && row.SeatId == _seatId) return row.NodeId;
            }
            return 0;
        }

        void BuildConnections(Dictionary<int, Vector2> positions)
        {
            var lineRoot = nodeViewContainer.Find("Connections") as RectTransform;
            if (lineRoot == null)
            {
                var objectRoot = new GameObject("Connections", typeof(RectTransform));
                objectRoot.transform.SetParent(nodeViewContainer, false);
                lineRoot = (RectTransform)objectRoot.transform;
                lineRoot.anchorMin = Vector2.zero;
                lineRoot.anchorMax = Vector2.one;
                lineRoot.offsetMin = lineRoot.offsetMax = Vector2.zero;
                lineRoot.SetAsFirstSibling();
            }
            for (var index = lineRoot.childCount - 1; index >= 0; index--) Destroy(lineRoot.GetChild(index).gameObject);
            var table = CfgMgr.Cfgs?.TbCultSeatTechNode?.DataList;
            if (table == null) return;
            foreach (var node in table)
            {
                if (node == null || node.SeatId != _seatId || !positions.TryGetValue(node.NodeId, out var start)) continue;
                var next = table.Find(candidate => candidate != null && candidate.SeatId == _seatId && candidate.Ring == node.Ring + 1);
                if (next == null || !positions.TryGetValue(next.NodeId, out var end)) continue;
                CreateLine(lineRoot, start, end);
            }
        }

        static void CreateLine(RectTransform parent, Vector2 start, Vector2 end)
        {
            var objectRoot = new GameObject("Connection", typeof(RectTransform), typeof(Image));
            objectRoot.transform.SetParent(parent, false);
            var rect = (RectTransform)objectRoot.transform;
            var delta = end - start;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = start;
            rect.sizeDelta = new Vector2(delta.magnitude, 3f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            objectRoot.GetComponent<Image>().color = new Color(0.45f, 0.25f, 0.65f, 0.8f);
        }

        void ShowDetail()
        {
            var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(_selectedNodeId);
            if (node == null || _cult == null) return;
            var current = _cult.GetSeatTechNodeLevel(_seatId, _selectedNodeId);
            var state = _cult.GetSeatTechNodeVisualState(_seatId, _selectedNodeId);
            var next = CfgMgr.Cfgs?.TbCultSeatTechNodeLevel?.Get(node.NodeId, current + 1);
            if (detailTitle != null) detailTitle.text = node.DisplayName;
            if (detailBody != null) detailBody.text = $"{node.Desc}\n等级 {current}/{node.MaxLevel}\n下一阶段 {next?.EffectDesc ?? "已达上限"}\n所需信仰 {next?.FaithCost.ToString() ?? "—"}";
            if (detailStatusHint != null) detailStatusHint.text = state switch
            {
                CultTechNodeVisualState.Unlocked => "已点亮",
                CultTechNodeVisualState.Unlockable => "可以点亮",
                CultTechNodeVisualState.InsufficientFaith => "信仰不足",
                _ => "尚未满足前置条件",
            };
            if (unlockButton != null) unlockButton.interactable = state == CultTechNodeVisualState.Unlockable;
            if (_seatHeader != null)
            {
                var seat = CfgMgr.Cfgs?.TbCultAncientSeat?.GetOrDefault(_seatId);
                _seatHeader.text = seat == null ? "座席火团" : $"{seat.DisplayName} · {seat.Title}";
            }
        }

        void TryUnlock()
        {
            if (_cult == null || !_cult.TryUnlockSeatTechNode(_seatId, _selectedNodeId, out var reason))
                Debug.LogWarning($"Cult seat tech unlock failed: {reason}");
            Refresh();
        }

        void ClearNodes()
        {
            _nodes.Clear();
            if (nodeViewContainer == null) return;
            for (var index = nodeViewContainer.childCount - 1; index >= 0; index--)
                Destroy(nodeViewContainer.GetChild(index).gameObject);
        }

        void ApplyLayerVisibility()
        {
            var seatRoot = transform.Find("SeatRoot");
            if (seatRoot != null) seatRoot.gameObject.SetActive(_panelVisible && !_seatSelected);
            if (nodeViewContainer != null) nodeViewContainer.gameObject.SetActive(_panelVisible && _seatSelected);
            if (detailTitle != null) detailTitle.gameObject.SetActive(_panelVisible && _seatSelected);
            if (detailBody != null) detailBody.gameObject.SetActive(_panelVisible && _seatSelected);
            if (detailStatusHint != null) detailStatusHint.gameObject.SetActive(_panelVisible && _seatSelected);
            if (unlockButton != null) unlockButton.gameObject.SetActive(_panelVisible && _seatSelected);
            if (faithText != null)
            {
                faithText.gameObject.SetActive(_panelVisible);
                if (_cult != null) faithText.text = $"信仰 {_cult.Faith}  ·  火团 {_cult.GetTechNodeCount(_seatId)}";
            }
            if (_backButton != null) _backButton.gameObject.SetActive(_panelVisible && _seatSelected);
            if (_seatHeader != null) _seatHeader.gameObject.SetActive(_panelVisible && _seatSelected);
        }

        void EnsureLayout()
        {
            if (transform.Find("Background") == null)
            {
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
                background.transform.SetParent(transform, false);
                background.transform.SetAsFirstSibling();
                var backgroundRect = (RectTransform)background.transform;
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;
                background.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.08f, 0.96f);
            }
            nodeViewContainer ??= transform.Find("TreeRoot");
            if (nodeViewContainer == null)
            {
                var objectRoot = new GameObject("TreeRoot", typeof(RectTransform));
                objectRoot.transform.SetParent(transform, false);
                nodeViewContainer = objectRoot.transform;
            }
            var treeRect = (RectTransform)nodeViewContainer;
            treeRect.anchorMin = new Vector2(0.03f, 0.1f);
            treeRect.anchorMax = new Vector2(0.68f, 0.88f);
            treeRect.offsetMin = treeRect.offsetMax = Vector2.zero;
            var seatRoot = transform.Find("SeatRoot") as RectTransform;
            if (seatRoot == null)
            {
                var objectRoot = new GameObject("SeatRoot", typeof(RectTransform));
                objectRoot.transform.SetParent(transform, false);
                seatRoot = (RectTransform)objectRoot.transform;
            }
            seatRoot.anchorMin = new Vector2(0.04f, 0.12f);
            seatRoot.anchorMax = new Vector2(0.96f, 0.86f);
            seatRoot.offsetMin = seatRoot.offsetMax = Vector2.zero;
            faithText ??= EnsureText("FaithText", new Vector2(0.04f, 0.9f), new Vector2(0.68f, 0.97f), 20f);
            detailTitle ??= EnsureText("DetailTitle", new Vector2(0.72f, 0.72f), new Vector2(0.97f, 0.9f), 22f);
            detailBody ??= EnsureText("DetailBody", new Vector2(0.72f, 0.3f), new Vector2(0.97f, 0.7f), 15f);
            detailStatusHint ??= EnsureText("DetailStatusHint", new Vector2(0.72f, 0.2f), new Vector2(0.97f, 0.28f), 15f);
            unlockButton ??= EnsureButton("UnlockButton", "点亮火团", new Vector2(0.76f, 0.08f), new Vector2(0.94f, 0.16f));
            _backButton ??= EnsureButton("BackToSeatsButton", "返回座席", new Vector2(0.04f, 0.92f), new Vector2(0.18f, 0.98f));
            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(ReturnToSeatStrip);
            _seatHeader ??= EnsureText("SeatHeader", "座席火团", new Vector2(0.2f, 0.92f), new Vector2(0.68f, 0.98f), 20f);
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(TryUnlock);
        }

        Button EnsureButton(string name, string labelText, Vector2 min, Vector2 max)
        {
            var button = transform.Find(name)?.GetComponent<Button>();
            if (button == null)
            {
                var objectRoot = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                objectRoot.transform.SetParent(transform, false);
                button = objectRoot.GetComponent<Button>();
                var label = CreateText(objectRoot.transform, "Label", labelText, 15f, TextAlignmentOptions.Center);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            }
            var rect = (RectTransform)button.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return button;
        }

        TextMeshProUGUI EnsureText(string name, Vector2 min, Vector2 max, float size)
        {
            var text = transform.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (text == null) text = CreateText(transform, name, name, size, TextAlignmentOptions.Left);
            text.rectTransform.anchorMin = min;
            text.rectTransform.anchorMax = max;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            return text;
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment)
        {
            var objectRoot = new GameObject(name, typeof(RectTransform));
            objectRoot.transform.SetParent(parent, false);
            var text = objectRoot.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }
    }
}