using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using DG.Tweening;
using My;
using My.Config;
using My.Player;
using My.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public class RuneLoadoutPanel : PanelBase, IInputConsumer, IPlayerProgressionHubPage
    {
        public const string Pid = "RuneLoadoutPanel";

        const float DetailSlideDuration = 0.22f;
        const float OwnedSlideDuration = 0.28f;
        const float DetailSlideOffsetX = 72f;
        const float OwnedHiddenExtraOffset = 24f;

        [SerializeField] RectTransform slotGrid;
        [SerializeField] RectTransform detailArea;
        [SerializeField] Button detailCloseButton;
        [SerializeField] Button detailBackdrop;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailBody;
        [SerializeField] TextMeshProUGUI runeDescText;
        [SerializeField] RuneUpgradeLayoutHost layoutHost;
        [SerializeField] RuneUpgradeDetailSection upgradeDetail;
        [SerializeField] RectTransform ownedArea;
        [SerializeField] ScrollRect ownedScroll;
        [SerializeField] RectTransform ownedGrid;
        [SerializeField] RuneOwnedCell ownedCellTemplate;
        [SerializeField] TextMeshProUGUI ownedHint;
        [SerializeField] RuneDragDropController dragController;

        const string OwnedCellPrefabPath = "UI/Prefabs/PlayerProgressionHubPanelSub/RuneOwnedCell";

        IPlayerProgressionHubHost _progressionHubHost;

        readonly List<RuneSlotView> _slotViews = new();
        readonly List<RuneOwnedCell> _ownedCells = new();

        RuneSlotView _selectedSlot;
        ERuneEquipSlot _selectedEquipSlot = ERuneEquipSlot.None;
        string _selectedFixedRuneId;
        string _selectedOwnedRuneId;
        bool _slotViewsCached;
        bool _detailVisible;
        bool _ownedAreaVisible;

        Vector2 _detailShownAnchoredPos;
        Vector2 _ownedAreaShownAnchoredPos;
        Vector2 _ownedAreaHiddenAnchoredPos;

        Tweener _detailTween;
        Tweener _ownedAreaTween;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            if (dragController == null)
            {
                dragController = GetComponent<RuneDragDropController>();
            }

            WireDetailChrome();
            CacheLayoutAnchors();
        }

        void OnEnable()
        {
            PlayerEventBus.Subscribe<PlayerRuneGrantedEvent>(OnRuneGranted);
            PlayerEventBus.Subscribe<PlayerRuneUpgradeUnlockedEvent>(OnRuneUpgradeUnlocked);
        }

        void OnDisable()
        {
            PlayerEventBus.Unsubscribe<PlayerRuneGrantedEvent>(OnRuneGranted);
            PlayerEventBus.Unsubscribe<PlayerRuneUpgradeUnlockedEvent>(OnRuneUpgradeUnlocked);
            KillPanelTweens();
        }

        void OnRuneGranted(PlayerRuneGrantedEvent e)
        {
            RefreshAll();
        }

        void OnRuneUpgradeUnlocked(PlayerRuneUpgradeUnlockedEvent e)
        {
            RefreshAll();
        }

        public void SetProgressionHubHost(IPlayerProgressionHubHost host)
        {
            _progressionHubHost = host;
        }

        public void CloseSelfOrHub()
        {
            if (_progressionHubHost != null)
            {
                _progressionHubHost.CloseHub();
                return;
            }

            Debug.LogError("[RunePanel] Not hosted by PlayerProgressionHubPanel.");
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);

            if (!ValidatePrefabBindings())
            {
                return;
            }

            EnsureOwnedCellTemplate();
            if (ownedCellTemplate != null)
            {
                ownedCellTemplate.gameObject.SetActive(false);
            }

            if (ownedArea != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(ownedArea);
            }

            CacheLayoutAnchors();
            InitializePanelVisualState();
            RefreshAll();
        }

        public override void Show()
        {
            base.Show();

            if (ownedArea != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(ownedArea);
            }

            CacheLayoutAnchors();
            if (!_detailVisible && ownedArea != null)
            {
                ownedArea.anchoredPosition = _ownedAreaHiddenAnchoredPos;
            }

            RefreshAll();
        }

        void WireDetailChrome()
        {
            if (detailCloseButton != null)
            {
                detailCloseButton.onClick.RemoveAllListeners();
                detailCloseButton.onClick.AddListener(DismissDetail);
            }

            if (detailBackdrop != null)
            {
                detailBackdrop.onClick.RemoveAllListeners();
                detailBackdrop.onClick.AddListener(DismissDetail);
            }
        }

        void CacheLayoutAnchors()
        {
            if (detailArea != null)
            {
                // 设计位为 (0,0)；勿用当前 anchoredPosition，避免关闭 detail 后再次 Setup 误缓存滑出位置
                _detailShownAnchoredPos = Vector2.zero;
            }

            if (ownedArea != null)
            {
                var savedPos = ownedArea.anchoredPosition;
                _ownedAreaShownAnchoredPos = Vector2.zero;
                ownedArea.anchoredPosition = _ownedAreaShownAnchoredPos;
                LayoutRebuilder.ForceRebuildLayoutImmediate(ownedArea);

                float height = ownedArea.rect.height;
                if (height <= 1f)
                {
                    height = 156f;
                }

                _ownedAreaHiddenAnchoredPos = _ownedAreaShownAnchoredPos
                    + new Vector2(0f, -(height + OwnedHiddenExtraOffset));
                ownedArea.anchoredPosition = savedPos;
            }
        }

        void InitializePanelVisualState()
        {
            KillPanelTweens();
            _detailVisible = false;
            _ownedAreaVisible = false;

            if (detailBackdrop != null)
            {
                detailBackdrop.gameObject.SetActive(false);
            }

            if (detailArea != null)
            {
                detailArea.gameObject.SetActive(false);
                detailArea.anchoredPosition = _detailShownAnchoredPos;
            }

            if (ownedArea != null)
            {
                ownedArea.gameObject.SetActive(true);
                ownedArea.anchoredPosition = _ownedAreaHiddenAnchoredPos;
            }

            ClearSelectionState();
        }

        bool ValidatePrefabBindings()
        {
            if (slotGrid == null)
            {
                Debug.LogError("[RunePanel] Prefab missing slotGrid.");
                return false;
            }

            EnsureSlotViews();
            if (_slotViews.Count == 0)
            {
                Debug.LogWarning("[RunePanel] SlotGrid has no RuneSlotView. Place RuneSlot prefabs in editor.");
            }

            if (detailArea == null)
            {
                Debug.LogError("[RunePanel] Prefab missing detailArea.");
                return false;
            }

            if (ownedGrid == null)
            {
                Debug.LogError("[RunePanel] Prefab missing ownedGrid.");
                return false;
            }

            EnsureOwnedCellTemplate();
            if (ownedCellTemplate == null)
            {
                Debug.LogError("[RunePanel] Prefab missing ownedCellTemplate.");
                return false;
            }

            if (ownedScroll == null && ownedGrid != null)
            {
                ownedScroll = ownedGrid.GetComponentInParent<ScrollRect>();
            }

            if (detailTitle == null)
            {
                Debug.LogError("[RunePanel] Prefab missing detailTitle.");
                return false;
            }

            if (runeDescText == null)
            {
                Debug.LogError("[RunePanel] Prefab missing runeDescText.");
                return false;
            }

            if (layoutHost == null)
            {
                Debug.LogError("[RunePanel] Prefab missing layoutHost.");
                return false;
            }

            if (upgradeDetail == null)
            {
                Debug.LogError("[RunePanel] Prefab missing upgradeDetail.");
                return false;
            }

            if (dragController == null)
            {
                Debug.LogError("[RunePanel] Prefab missing dragController.");
                return false;
            }

            return true;
        }

        void EnsureSlotViews()
        {
            if (_slotViewsCached)
            {
                return;
            }

            _slotViews.Clear();
            if (slotGrid == null)
            {
                return;
            }

            var views = slotGrid.GetComponentsInChildren<RuneSlotView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view.GetComponent<RuneSlotBinder>() == null)
                {
                    continue;
                }

                view.BindPanel(this);
                _slotViews.Add(view);
            }

            _slotViewsCached = true;
        }

        public void RefreshAll()
        {
            RefreshSlots();
            if (_detailVisible)
            {
                RefreshOwnedGrid();
                RefreshDetailPanel();
            }
            else
            {
                ClearOwnedCells();
            }
        }

        void RefreshSlots()
        {
            EnsureSlotViews();
            var runeSystem = GetRuneSystem();
            if (runeSystem == null)
            {
                return;
            }

            for (int i = 0; i < _slotViews.Count; i++)
            {
                var slot = _slotViews[i];
                if (slot == null)
                {
                    continue;
                }

                slot.Refresh(runeSystem, IsSlotSelected(slot));
            }

            if (_detailVisible)
            {
                _selectedSlot = FindSelectedSlotView();
            }
        }

        public static bool IsEquipSlotUnlocked(PlayerRuneSystem runeSystem, ERuneEquipSlot slot)
        {
            foreach (var def in runeSystem.GetOwnedByType(ERuneType.Equippable))
            {
                if (def.EquipSlot == slot)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsSlotSelected(RuneSlotView slot)
        {
            if (!_detailVisible || slot?.Binder == null)
            {
                return false;
            }

            if (slot.Binder.SlotKind == RuneSlotKind.Fixed)
            {
                return !string.IsNullOrEmpty(_selectedFixedRuneId)
                       && slot.Binder.FixedRuneId == _selectedFixedRuneId;
            }

            return _selectedEquipSlot != ERuneEquipSlot.None
                   && slot.Binder.EquipSlot == _selectedEquipSlot;
        }

        RuneSlotView FindSelectedSlotView()
        {
            foreach (var slot in _slotViews)
            {
                if (IsSlotSelected(slot))
                {
                    return slot;
                }
            }

            return null;
        }

        RuneSlotView FindSlotViewForEquipSlot(ERuneEquipSlot slot)
        {
            foreach (var view in _slotViews)
            {
                if (view?.Binder != null
                    && view.Binder.SlotKind == RuneSlotKind.Equippable
                    && view.Binder.EquipSlot == slot)
                {
                    return view;
                }
            }

            return null;
        }

        public void OnSlotClicked(RuneSlotView slot)
        {
            if (slot == null || slot.Binder == null)
            {
                return;
            }

            OpenDetail(slot);
        }

        void OpenDetail(RuneSlotView slot)
        {
            bool wasVisible = _detailVisible;
            _selectedSlot = slot;
            if (slot.Binder.SlotKind == RuneSlotKind.Equippable)
            {
                _selectedEquipSlot = slot.Binder.EquipSlot;
                _selectedFixedRuneId = null;
            }
            else
            {
                _selectedEquipSlot = ERuneEquipSlot.None;
                _selectedFixedRuneId = slot.Binder.FixedRuneId;
            }

            _detailVisible = true;
            RefreshSlots();
            RefreshDetailPanel();

            if (detailBackdrop != null)
            {
                detailBackdrop.gameObject.SetActive(true);
            }

            if (!wasVisible)
            {
                PlayDetailShow();
            }

            if (ShouldShowOwnedArea(slot))
            {
                if (_ownedAreaVisible)
                {
                    RefreshOwnedGrid();
                }
                else
                {
                    PlayOwnedAreaShow();
                }
            }
            else
            {
                PlayOwnedAreaHide();
            }
        }

        void DismissDetail()
        {
            if (!_detailVisible)
            {
                return;
            }

            _detailVisible = false;
            PlayOwnedAreaHide();
            PlayDetailHide();
            ClearSelectionState();
            RefreshSlots();
            ClearOwnedCells();
            layoutHost?.Hide();
            upgradeDetail?.Clear();
        }

        bool ShouldShowOwnedArea(RuneSlotView slot)
        {
            return slot != null
                   && slot.Binder != null
                   && slot.Binder.SlotKind == RuneSlotKind.Equippable
                   && slot.State != RuneSlotVisualState.Locked;
        }

        bool ShouldShowOwnedArea()
        {
            return _detailVisible && ShouldShowOwnedArea(_selectedSlot);
        }

        void PlayDetailShow()
        {
            if (detailArea == null)
            {
                return;
            }

            KillDetailTween();
            detailArea.gameObject.SetActive(true);
            detailArea.anchoredPosition = _detailShownAnchoredPos + new Vector2(DetailSlideOffsetX, 0f);
            _detailTween = detailArea
                .DOAnchorPos(_detailShownAnchoredPos, DetailSlideDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        void PlayDetailHide()
        {
            if (detailBackdrop != null)
            {
                detailBackdrop.gameObject.SetActive(false);
            }

            if (detailArea == null)
            {
                return;
            }

            KillDetailTween();
            var target = _detailShownAnchoredPos + new Vector2(DetailSlideOffsetX, 0f);
            _detailTween = detailArea
                .DOAnchorPos(target, DetailSlideDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (detailArea != null)
                    {
                        detailArea.gameObject.SetActive(false);
                        detailArea.anchoredPosition = _detailShownAnchoredPos;
                    }
                });
        }

        void PlayOwnedAreaShow()
        {
            if (ownedArea == null)
            {
                return;
            }

            ownedArea.gameObject.SetActive(true);

            if (_ownedAreaVisible)
            {
                ownedArea.anchoredPosition = _ownedAreaShownAnchoredPos;
                RefreshOwnedGrid();
                return;
            }

            _ownedAreaVisible = true;
            KillOwnedAreaTween();
            ownedArea.anchoredPosition = _ownedAreaHiddenAnchoredPos;
            _ownedAreaTween = ownedArea
                .DOAnchorPos(_ownedAreaShownAnchoredPos, OwnedSlideDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(OnOwnedAreaShowComplete);
            RefreshOwnedGrid();
        }

        void OnOwnedAreaShowComplete()
        {
            if (ownedArea != null)
            {
                ownedArea.anchoredPosition = _ownedAreaShownAnchoredPos;
            }

            RefreshOwnedGrid();
        }

        void PlayOwnedAreaHide()
        {
            if (ownedArea == null || !_ownedAreaVisible)
            {
                if (ownedArea != null)
                {
                    ownedArea.anchoredPosition = _ownedAreaHiddenAnchoredPos;
                }

                _ownedAreaVisible = false;
                return;
            }

            _ownedAreaVisible = false;
            KillOwnedAreaTween();
            _ownedAreaTween = ownedArea
                .DOAnchorPos(_ownedAreaHiddenAnchoredPos, OwnedSlideDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true);
        }

        void KillPanelTweens()
        {
            KillDetailTween();
            KillOwnedAreaTween();
        }

        void KillDetailTween()
        {
            if (_detailTween != null && _detailTween.IsActive())
            {
                _detailTween.Kill();
            }

            _detailTween = null;
            detailArea?.DOKill();
        }

        void KillOwnedAreaTween()
        {
            if (_ownedAreaTween != null && _ownedAreaTween.IsActive())
            {
                _ownedAreaTween.Kill();
            }

            _ownedAreaTween = null;
            ownedArea?.DOKill();
        }

        void ClearSelectionState()
        {
            _selectedSlot = null;
            _selectedEquipSlot = ERuneEquipSlot.None;
            _selectedFixedRuneId = null;
            _selectedOwnedRuneId = null;
        }

        void RefreshDetailPanel()
        {
            if (!_detailVisible || _selectedSlot == null || _selectedSlot.Binder == null)
            {
                return;
            }

            var provider = _selectedSlot.GetComponent<RuneInfoProvider>();
            if (provider == null)
            {
                ShowDetailPlaceholder(string.Empty);
                return;
            }

            if (detailTitle != null)
            {
                detailTitle.text = provider.GetDisplayName();
            }

            if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Fixed
                && _selectedSlot.State == RuneSlotVisualState.Locked)
            {
                ShowDetailPlaceholder("该常驻符文尚未解锁。");
                return;
            }

            if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Equippable
                && _selectedSlot.State == RuneSlotVisualState.Locked)
            {
                ShowDetailPlaceholder("槽位锁定：尚未获得可用于该槽位的符文。");
                return;
            }

            if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Equippable
                && _selectedSlot.State == RuneSlotVisualState.Empty)
            {
                ShowDetailPlaceholder("请装配符文。");
                return;
            }

            string runeId = ResolveEffectiveRuneId();
            var runeDef = RuneCatalog.GetOrDefault(runeId);
            var runeSystem = GetRuneSystem();
            if (runeDef == null || runeSystem == null || !runeSystem.OwnsRune(runeId))
            {
                ShowDetailPlaceholder(provider.GetDetailText());
                return;
            }

            if (runeDescText != null)
            {
                runeDescText.text = runeDef.Desc ?? string.Empty;
            }

            if (detailBody != null)
            {
                detailBody.gameObject.SetActive(false);
            }

            if (layoutHost != null)
            {
                layoutHost.ShowForRune(runeId, runeSystem, OnUpgradeSelected);
            }

            string selectedUpgradeId = layoutHost != null ? layoutHost.SelectedUpgradeId : null;
            if (upgradeDetail != null)
            {
                if (!string.IsNullOrEmpty(selectedUpgradeId))
                {
                    upgradeDetail.ShowUpgrade(selectedUpgradeId, runeSystem);
                }
                else
                {
                    upgradeDetail.Clear();
                }
            }
        }

        void ShowDetailPlaceholder(string message)
        {
            if (runeDescText != null)
            {
                runeDescText.text = string.Empty;
            }

            if (detailBody != null)
            {
                detailBody.gameObject.SetActive(true);
                detailBody.text = message ?? string.Empty;
            }

            layoutHost?.Hide();
            upgradeDetail?.Clear();
        }

        string ResolveEffectiveRuneId()
        {
            if (_selectedSlot?.Binder == null)
            {
                return null;
            }

            if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Fixed)
            {
                return _selectedSlot.Binder.FixedRuneId;
            }

            var runeSystem = GetRuneSystem();
            if (runeSystem == null)
            {
                return null;
            }

            return runeSystem.GetEquipped(_selectedSlot.Binder.EquipSlot);
        }

        void OnUpgradeSelected(string upgradeId)
        {
            var runeSystem = GetRuneSystem();
            if (upgradeDetail != null && runeSystem != null)
            {
                upgradeDetail.ShowUpgrade(upgradeId, runeSystem);
            }
        }

        void RefreshOwnedGrid()
        {
            if (!ShouldShowOwnedArea())
            {
                ClearOwnedCells();
                return;
            }

            var runeSystem = GetRuneSystem();
            if (runeSystem == null)
            {
                return;
            }

            var owned = runeSystem.GetOwnedByType(ERuneType.Equippable)
                .OrderBy(x => x.RuneId)
                .ToList();

            if (!EnsureOwnedCellTemplate())
            {
                return;
            }

            ClearOwnedCells();
            for (int i = 0; i < owned.Count; i++)
            {
                var def = owned[i];
                var cellGo = Instantiate(ownedCellTemplate.gameObject, ownedGrid, false);
                cellGo.SetActive(true);
                var cell = cellGo.GetComponent<RuneOwnedCell>();
                if (cell == null)
                {
                    Destroy(cellGo);
                    continue;
                }

                bool canEquip = def.EquipSlot == _selectedEquipSlot;
                string equippedId = runeSystem.GetEquipped(def.EquipSlot);
                bool isEquipped = equippedId == def.RuneId;
                bool selected = _selectedOwnedRuneId == def.RuneId;
                cell.Bind(this, def, isEquipped, selected, i, canEquip);
                _ownedCells.Add(cell);
            }

            if (ownedHint != null)
            {
                ownedHint.gameObject.SetActive(owned.Count == 0);
                ownedHint.text = owned.Count == 0 ? "暂无已拥有的装配符文" : string.Empty;
            }

            RebuildOwnedGridLayout();
        }

        bool EnsureOwnedCellTemplate()
        {
            if (ownedCellTemplate != null)
            {
                return true;
            }

            var prefab = Resources.Load<GameObject>(OwnedCellPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[RunePanel] Failed to load RuneOwnedCell prefab.");
                return false;
            }

            ownedCellTemplate = prefab.GetComponent<RuneOwnedCell>();
            if (ownedCellTemplate == null)
            {
                Debug.LogError("[RunePanel] RuneOwnedCell prefab missing RuneOwnedCell component.");
            }

            return ownedCellTemplate != null;
        }

        void RebuildOwnedGridLayout()
        {
            if (ownedGrid == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(ownedGrid);

            if (ownedScroll != null)
            {
                if (ownedScroll.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(ownedScroll.content);
                }

                if (ownedScroll.viewport != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(ownedScroll.viewport);
                }

                ownedScroll.verticalNormalizedPosition = 1f;
            }
        }

        void ClearOwnedCells()
        {
            foreach (var cell in _ownedCells)
            {
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }

            _ownedCells.Clear();
        }

        public void TryEquipOwnedRune(RuneData def)
        {
            if (def == null || _selectedEquipSlot == ERuneEquipSlot.None)
            {
                return;
            }

            if (def.EquipSlot != _selectedEquipSlot)
            {
                return;
            }

            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (pdm == null)
            {
                return;
            }

            _selectedOwnedRuneId = def.RuneId;
            if (pdm.TryEquipRune(_selectedEquipSlot, def.RuneId))
            {
                RefreshAll();
            }
        }

        public void TryEquipFromDrag(ERuneEquipSlot slot, string runeId, RuneDragDropController controller)
        {
            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (pdm == null || string.IsNullOrEmpty(runeId))
            {
                return;
            }

            if (!pdm.TryEquipRune(slot, runeId))
            {
                return;
            }

            controller?.MarkDropHandled();
            var slotView = FindSlotViewForEquipSlot(slot);
            if (slotView != null)
            {
                OpenDetail(slotView);
            }
            else
            {
                RefreshAll();
            }
        }

        static PlayerRuneSystem GetRuneSystem()
        {
            return MainGameManager.Instance?.gameLogicManager?.playerDataManager?.RuneSystem;
        }

        public bool OnConfirm() => false;
        public bool OnCancel() => false;
        public bool OnNavigate(Vector2 dir) => false;
        public bool OnHotkey(string keyName) => false;
        public bool OnScroll(float deltaY) => false;
        public bool OnClick(int button, Vector2 mousePos) => false;
        public bool OnHoldStart(string holdKey) => false;
        public bool OnHoldUpdate(string holdKey) => false;
        public bool OnHoldingEnd(string holdKey) => false;
    }
}
