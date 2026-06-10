using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Player;
using My.Quest;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public class RuneLoadoutPanel : PanelBase, IInputConsumer, IPlayerProgressionHubPage
    {
        public const string Pid = "RuneLoadoutPanel";

        [SerializeField] RectTransform slotGrid;
        [SerializeField] Button interactionBackdrop;
        [SerializeField] RuneLoadoutDetailView detailView;
        [SerializeField] RuneLoadoutBottomView bottomView;
        [SerializeField] RuneDragDropController dragController;

        IPlayerProgressionHubHost _progressionHubHost;

        readonly List<RuneSlotView> _slotViews = new();

        RuneSlotView _selectedSlot;
        ERuneEquipSlot _selectedEquipSlot = ERuneEquipSlot.None;
        string _selectedFixedRuneId;
        string _selectedOwnedRuneId;
        bool _slotViewsCached;
        bool _interactionOpen;

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

            detailView?.BindDismissCallback(DismissInteraction);
            WireBackdrop();
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
        }

        void WireBackdrop()
        {
            if (interactionBackdrop != null)
            {
                interactionBackdrop.onClick.RemoveAllListeners();
                interactionBackdrop.onClick.AddListener(DismissInteraction);
            }
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

            CacheSubviewLayoutAnchors();
            InitializePanelVisualState();
            RefreshAll();
        }

        public override void Show()
        {
            base.Show();
            CacheSubviewLayoutAnchors();
            RefreshAll();
        }

        void CacheSubviewLayoutAnchors()
        {
            detailView?.CacheLayoutAnchors();
            bottomView?.CacheLayoutAnchors();
        }

        void InitializePanelVisualState()
        {
            _interactionOpen = false;
            SetBackdropActive(false);
            detailView?.ResetVisualState();
            bottomView?.ResetVisualState();
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

            if (detailView == null)
            {
                Debug.LogError("[RunePanel] Prefab missing detailView.");
                return false;
            }

            if (bottomView == null)
            {
                Debug.LogError("[RunePanel] Prefab missing bottomView.");
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
            if (!_interactionOpen)
            {
                return;
            }

            var runeSystem = GetRuneSystem();
            if (detailView != null && detailView.IsVisible)
            {
                detailView.Refresh(_selectedSlot, runeSystem);
            }

            if (bottomView != null && bottomView.IsVisible)
            {
                bottomView.Refresh(runeSystem);
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

            if (_interactionOpen)
            {
                _selectedSlot = FindSelectedSlotView();
            }
        }

        bool IsSlotSelected(RuneSlotView slot)
        {
            if (!_interactionOpen || slot?.Binder == null)
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

            OpenSlotInteraction(slot);
        }

        void OpenSlotInteraction(RuneSlotView slot)
        {
            bool wasOpen = _interactionOpen;
            ApplySelection(slot);
            _interactionOpen = true;
            RefreshSlots();

            var runeSystem = GetRuneSystem();
            var mode = ResolveInteractionMode(slot, runeSystem);
            switch (mode)
            {
                case SlotInteractionMode.Detail:
                    bottomView?.Hide(wasOpen && bottomView.IsVisible);
                    detailView?.Show(slot, runeSystem, animate: !wasOpen || !detailView.IsVisible);
                    break;
                case SlotInteractionMode.OwnedPicker:
                    detailView?.Hide(wasOpen && detailView.IsVisible);
                    bottomView?.ShowOwnedPicker(this, _selectedEquipSlot, runeSystem);
                    break;
                case SlotInteractionMode.SlotLocked:
                    detailView?.Hide(wasOpen && detailView.IsVisible);
                    bottomView?.ShowSlotLocked(_selectedEquipSlot, runeSystem);
                    break;
            }

            SetBackdropActive(true);
        }

        enum SlotInteractionMode
        {
            Detail,
            OwnedPicker,
            SlotLocked,
        }

        static SlotInteractionMode ResolveInteractionMode(RuneSlotView slot, PlayerRuneSystem runeSystem)
        {
            if (slot?.Binder == null)
            {
                return SlotInteractionMode.Detail;
            }

            if (slot.Binder.SlotKind == RuneSlotKind.Fixed)
            {
                return SlotInteractionMode.Detail;
            }

            if (slot.State == RuneSlotVisualState.Locked)
            {
                return SlotInteractionMode.SlotLocked;
            }

            if (slot.State == RuneSlotVisualState.Equipped)
            {
                return SlotInteractionMode.Detail;
            }

            return SlotInteractionMode.OwnedPicker;
        }

        void ApplySelection(RuneSlotView slot)
        {
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
        }

        void DismissInteraction()
        {
            if (!_interactionOpen)
            {
                return;
            }

            _interactionOpen = false;
            detailView?.Hide();
            bottomView?.Hide();
            SetBackdropActive(false);
            ClearSelectionState();
            RefreshSlots();
        }

        void SetBackdropActive(bool active)
        {
            if (interactionBackdrop != null)
            {
                interactionBackdrop.gameObject.SetActive(active);
            }
        }

        void ClearSelectionState()
        {
            _selectedSlot = null;
            _selectedEquipSlot = ERuneEquipSlot.None;
            _selectedFixedRuneId = null;
            _selectedOwnedRuneId = null;
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
            bottomView?.NotifyOwnedRuneSelected(def.RuneId);
            if (pdm.TryEquipRune(_selectedEquipSlot, def.RuneId))
            {
                var slotView = FindSlotViewForEquipSlot(_selectedEquipSlot);
                if (slotView != null)
                {
                    OpenSlotInteraction(slotView);
                }
                else
                {
                    RefreshAll();
                }
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
                OpenSlotInteraction(slotView);
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
