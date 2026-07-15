using System.Collections;
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
        [SerializeField] CanvasGroup seatDetailCanvasGroup;
        [SerializeField] RectTransform seatDetailTreeRoot;
        Button _backButton;
        Button _previousSeatButton;
        Button _nextSeatButton;
        TextMeshProUGUI _seatHeader;
        TextMeshProUGUI _seatDescription;
        Coroutine _seatTransition;
        GameObject _seatCardTemplate;
        GameObject _seatLayoutInstance;
        readonly List<CultSeatTechNodeView> _nodes = new();
        DemonCultSystem _cult;
        int _seatId;
        int _selectedNodeId;
        bool _seatSelected;
        bool _panelVisible;
        public int SelectedNodeId => _selectedNodeId;

        public void SetVisible(bool visible)
        {
            _panelVisible = visible;
            EnsureLayout();
            ApplyLayerVisibility();
        }

        public void Bind(DemonCultSystem cult)
        {
            _cult = cult;
            _cult.RefreshAutoUnlockedSeats();
            EnsureLayout();
            WireNavigationButtons();
            BuildSeatButtons();
            if (_seatId <= 0) _seatId = FindFirstSeat();
            _seatSelected = false;
            ClearNodes();
            ApplyLayerVisibility();
        }

        public void SelectSeat(int seatId)
        {
            if (seatId <= 0) return;
            var direction = ResolveSeatDirection(seatId);
            _seatId = seatId;
            _selectedNodeId = 0;
            _seatSelected = true;
            Rebuild();
            ApplyLayerVisibility();
            PlaySeatTransition(direction);
        }

        public void ReturnToSeatStrip()
        {
            StopSeatTransition();
            _seatSelected = false;
            _selectedNodeId = 0;
            ClearNodes();
            ApplyLayerVisibility();
        }

        void Update()
        {
            if (!_panelVisible || !_seatSelected)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) SelectAdjacentSeat(-1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) SelectAdjacentSeat(1);
        }

        public void SelectAdjacentSeat(int direction)
        {
            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            if (seats == null || seats.Count == 0 || direction == 0)
            {
                return;
            }

            var currentIndex = seats.FindIndex(seat => seat != null && seat.SeatId == _seatId);
            if (currentIndex < 0)
            {
                currentIndex = direction > 0 ? -1 : seats.Count;
            }

            var nextIndex = Mathf.Clamp(currentIndex + (direction > 0 ? 1 : -1), 0, seats.Count - 1);
            var nextSeat = seats[nextIndex];
            if (nextSeat != null && nextSeat.SeatId != _seatId)
            {
                SelectSeat(nextSeat.SeatId);
            }
        }

        int ResolveSeatDirection(int nextSeatId)
        {
            if (_seatId <= 0 || nextSeatId == _seatId)
            {
                return 1;
            }

            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            if (seats == null) return nextSeatId >= _seatId ? 1 : -1;
            var current = seats.FindIndex(seat => seat != null && seat.SeatId == _seatId);
            var next = seats.FindIndex(seat => seat != null && seat.SeatId == nextSeatId);
            return next >= current ? 1 : -1;
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
            _cult.RefreshAutoUnlockedSeats();
            BuildSeatButtons();
            UpdateSeatHeader();
            UpdateNavigationButtons();
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
            if (root == null || _seatCardTemplate == null) return;
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                if (root.GetChild(index).gameObject != _seatCardTemplate)
                    Destroy(root.GetChild(index).gameObject);
            }
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
                var objectRoot = Instantiate(_seatCardTemplate, root);
                objectRoot.name = $"Seat_{seat.SeatId}";
                objectRoot.SetActive(true);
                var portrait = objectRoot.transform.Find("Portrait")?.GetComponent<Image>();
                var label = objectRoot.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                var status = objectRoot.transform.Find("StatusIcon")?.GetComponent<Image>();
                if (portrait == null || label == null || status == null)
                    Debug.LogError("AncientSeatTreeView requires Portrait, Label and StatusIcon in SeatCardTemplate.");
                var rect = (RectTransform)objectRoot.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(startX + index * (cardWidth + gap), 0f);
                rect.sizeDelta = new Vector2(cardWidth, 390f);
                objectRoot.GetComponent<Image>().color = unlocked
                    ? new Color(0.2f, 0.12f, 0.2f, 0.98f)
                    : new Color(0.09f, 0.08f, 0.11f, 0.98f);
                if (portrait != null)
                {
                    portrait.sprite = Resources.Load<Sprite>($"UI/Cult/Seats/seat_{seat.SeatId}");
                    portrait.color = unlocked ? Color.white : new Color(0.32f, 0.32f, 0.36f, 1f);
                }
                if (label != null) label.text = seat.DisplayName;
                if (status != null) status.sprite = Resources.Load<Sprite>(unlocked ? "UI/Cult/Icons/unlocked" : "UI/Cult/Icons/locked");
                var button = objectRoot.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectSeat(seat.SeatId));
            }
        }

        void Rebuild()
        {
            ClearNodes();
            if (_cult == null || nodeViewContainer == null) return;
            var layoutPrefab = Resources.Load<GameObject>($"UI/Prefabs/PlayerProgressionHubPanelSub/CultSeats/Seat_{_seatId}_Layout");
            if (layoutPrefab == null)
            {
                Debug.LogError($"AncientSeatTreeView requires seat layout prefab for seat {_seatId}.");
                return;
            }

            _seatLayoutInstance = Instantiate(layoutPrefab, nodeViewContainer, false);
            _seatLayoutInstance.name = layoutPrefab.name;
            var binders = _seatLayoutInstance.GetComponentsInChildren<CultSeatTechNodeBinder>(true);
            foreach (var binder in binders)
            {
                var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(binder.SeatTechNodeId);
                var view = binder.GetComponent<CultSeatTechNodeView>();
                if (node == null || view == null || node.SeatId != _seatId)
                {
                    Debug.LogError($"Invalid seat node binder {binder.SeatTechNodeId} in seat {_seatId} layout.");
                    continue;
                }
                view.Bind(_seatId, this);
                _nodes.Add(view);
            }

            var table = CfgMgr.Cfgs?.TbCultSeatTechNode?.DataList;
            if (_selectedNodeId <= 0 || CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(_selectedNodeId)?.SeatId != _seatId)
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
            UpdateSeatHeader();
        }

        void UpdateSeatHeader()
        {
            var seat = CfgMgr.Cfgs?.TbCultAncientSeat?.GetOrDefault(_seatId);
            if (_seatHeader != null)
            {
                _seatHeader.text = seat == null ? "????" : $"{seat.DisplayName} ? {seat.Title}";
            }
            if (_seatDescription != null)
            {
                _seatDescription.text = seat == null ? string.Empty : seat.Desc;
            }
        }

        void WireNavigationButtons()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(ReturnToSeatStrip);
            }
            if (_previousSeatButton != null)
            {
                _previousSeatButton.onClick.RemoveAllListeners();
                _previousSeatButton.onClick.AddListener(() => SelectAdjacentSeat(-1));
            }
            if (_nextSeatButton != null)
            {
                _nextSeatButton.onClick.RemoveAllListeners();
                _nextSeatButton.onClick.AddListener(() => SelectAdjacentSeat(1));
            }
        }

        void UpdateNavigationButtons()
        {
            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            var currentIndex = seats?.FindIndex(seat => seat != null && seat.SeatId == _seatId) ?? -1;
            if (_previousSeatButton != null) _previousSeatButton.interactable = _seatSelected && currentIndex > 0;
            if (_nextSeatButton != null)
            {
                _nextSeatButton.interactable = _seatSelected && seats != null
                    && currentIndex >= 0 && currentIndex < seats.Count - 1;
            }
        }

        void PlaySeatTransition(int direction)
        {
            StopSeatTransition();
            if (seatDetailCanvasGroup == null || seatDetailTreeRoot == null) return;
            _seatTransition = StartCoroutine(SeatTransitionRoutine(direction));
        }

        IEnumerator SeatTransitionRoutine(int direction)
        {
            var basePosition = seatDetailTreeRoot.anchoredPosition;
            var startPosition = basePosition + new Vector2(direction * 48f, 0f);
            seatDetailTreeRoot.anchoredPosition = startPosition;
            seatDetailCanvasGroup.alpha = 0f;
            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                seatDetailTreeRoot.anchoredPosition = Vector2.Lerp(startPosition, basePosition, t);
                seatDetailCanvasGroup.alpha = t;
                yield return null;
            }
            seatDetailTreeRoot.anchoredPosition = basePosition;
            seatDetailCanvasGroup.alpha = 1f;
            _seatTransition = null;
        }

        void StopSeatTransition()
        {
            if (_seatTransition != null)
            {
                StopCoroutine(_seatTransition);
                _seatTransition = null;
            }
            if (seatDetailCanvasGroup != null)
            {
                seatDetailCanvasGroup.alpha = _seatSelected ? 1f : 0f;
            }
        }

        void TryUnlock()
        {
            if (_cult == null)
            {
                Refresh();
                return;
            }
            if (!_cult.TryUnlockSeatTechNode(_seatId, _selectedNodeId, out var reason))
                Debug.LogWarning($"Cult seat tech unlock failed: {reason}");
            Refresh();
        }
        void ClearNodes()
        {
            _nodes.Clear();
            if (_seatLayoutInstance != null)
            {
                Destroy(_seatLayoutInstance);
                _seatLayoutInstance = null;
            }
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
            if (_previousSeatButton != null) _previousSeatButton.gameObject.SetActive(_panelVisible && _seatSelected);
            if (_nextSeatButton != null) _nextSeatButton.gameObject.SetActive(_panelVisible && _seatSelected);
            if (_seatHeader != null) _seatHeader.gameObject.SetActive(_panelVisible && _seatSelected);
            if (_seatDescription != null) _seatDescription.gameObject.SetActive(_panelVisible && _seatSelected);
            if (seatDetailCanvasGroup != null && !_seatSelected) seatDetailCanvasGroup.alpha = 0f;
            UpdateSeatHeader();
            UpdateNavigationButtons();
        }

        void EnsureLayout()
        {
            nodeViewContainer ??= transform.Find("TreeRoot");
            faithText ??= transform.Find("FaithText")?.GetComponent<TextMeshProUGUI>();
            detailTitle ??= transform.Find("DetailArea/DetailTitle")?.GetComponent<TextMeshProUGUI>()
                ?? transform.Find("DetailTitle")?.GetComponent<TextMeshProUGUI>();
            detailBody ??= transform.Find("DetailArea/DetailBody")?.GetComponent<TextMeshProUGUI>()
                ?? transform.Find("DetailBody")?.GetComponent<TextMeshProUGUI>();
            detailStatusHint ??= transform.Find("DetailArea/DetailStatusHint")?.GetComponent<TextMeshProUGUI>()
                ?? transform.Find("DetailStatusHint")?.GetComponent<TextMeshProUGUI>();
            unlockButton ??= transform.Find("DetailArea/UnlockButton")?.GetComponent<Button>()
                ?? transform.Find("UnlockButton")?.GetComponent<Button>();
            _backButton ??= transform.Find("BackToSeatsButton")?.GetComponent<Button>();
            _previousSeatButton ??= transform.Find("PreviousSeatButton")?.GetComponent<Button>();
            _nextSeatButton ??= transform.Find("NextSeatButton")?.GetComponent<Button>();
            _seatHeader ??= transform.Find("SeatHeader")?.GetComponent<TextMeshProUGUI>();
            _seatDescription ??= transform.Find("SeatDescription")?.GetComponent<TextMeshProUGUI>();
            seatDetailTreeRoot ??= transform.Find("TreeRoot") as RectTransform;
            seatDetailCanvasGroup ??= seatDetailTreeRoot?.GetComponent<CanvasGroup>();
            _seatCardTemplate ??= transform.Find("SeatRoot/SeatCardTemplate")?.gameObject;
            if (nodeViewContainer == null || faithText == null || detailTitle == null || detailBody == null
                || detailStatusHint == null || unlockButton == null || _backButton == null || _seatHeader == null
                || _seatDescription == null || _previousSeatButton == null || _nextSeatButton == null
                || seatDetailCanvasGroup == null || _seatCardTemplate == null)
                Debug.LogError("AncientSeatTreeView requires its layout nodes in the CultPanel prefab.");
        }

    }
}
