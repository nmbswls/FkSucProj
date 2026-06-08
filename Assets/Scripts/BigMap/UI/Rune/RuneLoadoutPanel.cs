using System.Collections.Generic;
using System.Linq;
using cfg.demo;
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

        [SerializeField] Transform builtRoot;
        [SerializeField] RectTransform slotGrid;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailBody;
        [SerializeField] TextMeshProUGUI runeDescText;
        [SerializeField] RuneUpgradeLayoutHost layoutHost;
        [SerializeField] RuneUpgradeDetailSection upgradeDetail;
        [SerializeField] RectTransform ownedArea;
        [SerializeField] RectTransform ownedGrid;
        [SerializeField] RuneOwnedCell ownedCellTemplate;
        [SerializeField] TextMeshProUGUI ownedHint;
        [SerializeField] RuneDragDropController dragController;

        IPlayerProgressionHubHost _progressionHubHost;

        readonly List<RuneSlotView> _slotViews = new();
        readonly List<RuneOwnedCell> _ownedCells = new();

        RuneSlotView _selectedSlot;
        ERuneEquipSlot _selectedEquipSlot = ERuneEquipSlot.None;
        string _selectedFixedRuneId;
        string _selectedOwnedRuneId;
        bool _slotViewsCached;

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

            if (upgradeDetail != null)
            {
                upgradeDetail.SetHostPanel(this);
            }
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
            ApplyHostedChromeIfNeeded();
        }

        void ApplyHostedChromeIfNeeded()
        {
            if (_progressionHubHost == null)
            {
                return;
            }
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

            if (ownedCellTemplate != null)
            {
                ownedCellTemplate.gameObject.SetActive(false);
            }

            if (ownedArea != null)
            {
                ownedArea.gameObject.SetActive(false);
            }

            if (upgradeDetail != null)
            {
                upgradeDetail.SetHostPanel(this);
            }

            ApplyHostedChromeIfNeeded();
            RefreshAll();
        }

        public override void Show()
        {
            base.Show();
            RefreshAll();
        }

        bool ValidatePrefabBindings()
        {
            if (builtRoot == null)
            {
                Debug.LogError("[RunePanel] Prefab missing builtRoot.");
                return false;
            }

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

            if (ownedGrid == null || ownedCellTemplate == null)
            {
                Debug.LogError("[RunePanel] Prefab missing ownedGrid or ownedCellTemplate.");
                return false;
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
            RefreshOwnedGrid();
            RefreshDetailPanel();
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

            _selectedSlot = FindSelectedSlotView();
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
            if (slot?.Binder == null)
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

        public void OnSlotClicked(RuneSlotView slot)
        {
            if (slot == null || slot.Binder == null)
            {
                return;
            }

            _selectedSlot = slot;
            if (slot.Binder.SlotKind == RuneSlotKind.Equippable)
            {
                _selectedEquipSlot = slot.Binder.EquipSlot;
                _selectedFixedRuneId = null;
                if (ownedArea != null)
                {
                    ownedArea.gameObject.SetActive(slot.State != RuneSlotVisualState.Locked);
                }
            }
            else
            {
                _selectedEquipSlot = ERuneEquipSlot.None;
                _selectedFixedRuneId = slot.Binder.FixedRuneId;
                if (ownedArea != null)
                {
                    ownedArea.gameObject.SetActive(false);
                }
            }

            RefreshAll();
        }

        void RefreshDetailPanel()
        {
            if (_selectedSlot == null || _selectedSlot.Binder == null)
            {
                ClearDetailPanel("点击槽位查看符文详情。");
                return;
            }

            var provider = _selectedSlot.GetComponent<RuneInfoProvider>();
            if (provider == null)
            {
                ClearDetailPanel(string.Empty);
                return;
            }

            if (detailTitle != null)
            {
                detailTitle.text = provider.GetDisplayName();
            }

            if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Fixed
                && _selectedSlot.State == RuneSlotVisualState.Locked)
            {
                ClearDetailPanel("该常驻符文尚未解锁。");
                return;
            }

            if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Equippable
                && _selectedSlot.State == RuneSlotVisualState.Locked)
            {
                ClearDetailPanel("尚未获得可用于该槽位的符文。");
                return;
            }

            if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Equippable
                && _selectedSlot.State == RuneSlotVisualState.Empty)
            {
                ClearDetailPanel("从下方列表选择或拖拽符文进行装配。");
                return;
            }

            string runeId = ResolveEffectiveRuneId();
            var runeDef = RuneCatalog.GetOrDefault(runeId);
            var runeSystem = GetRuneSystem();
            if (runeDef == null || runeSystem == null || !runeSystem.OwnsRune(runeId))
            {
                ClearDetailPanel(provider.GetDetailText());
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

        void ClearDetailPanel(string message)
        {
            if (detailTitle != null)
            {
                detailTitle.text = string.Empty;
            }

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
            if (_selectedEquipSlot == ERuneEquipSlot.None)
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
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.RuneId)
                .ToList();

            ClearOwnedCells();
            for (int i = 0; i < owned.Count; i++)
            {
                var def = owned[i];
                var cell = Instantiate(ownedCellTemplate, ownedGrid, false);
                cell.gameObject.SetActive(true);

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

            LayoutRebuilder.ForceRebuildLayoutImmediate(ownedGrid);
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

            if (pdm.TryEquipRune(slot, runeId))
            {
                controller?.MarkDropHandled();
                _selectedEquipSlot = slot;
                RefreshAll();
            }
        }

        static PlayerRuneSystem GetRuneSystem()
        {
            return MainGameManager.Instance?.gameLogicManager?.playerDataManager?.RuneSystem;
        }


        public RuneUpgradeTreeView BuildUpgradeTreeView(string baseRuneId)
        {
            var view = new RuneUpgradeTreeView
            {
                BaseRuneId = baseRuneId,
                BaseRune = RuneCatalog.GetOrDefault(baseRuneId),
            };

            if (string.IsNullOrEmpty(baseRuneId))
            {
                return view;
            }

            foreach (var branchId in RuneUpgradeCatalog.GetBranchIdsForRune(baseRuneId))
            {
                var branch = new RuneUpgradeBranchView { BranchId = branchId };
                foreach (var def in RuneUpgradeCatalog.GetUpgradesInBranch(baseRuneId, branchId))
                {
                    branch.Nodes.Add(BuildNodeView(def));
                }

                view.Branches.Add(branch);
            }

            foreach (var def in RuneUpgradeCatalog.GetUpgradesInBranch(baseRuneId, null))
            {
                view.RootUpgrades.Add(BuildNodeView(def));
            }

            return view;
        }

        RuneUpgradeNodeView BuildNodeView(RuneUpgradeInfo def)
        {
            var runeSystem = MainGameManager.Instance.gameLogicManager.playerDataManager.RuneSystem;
            var state = runeSystem.GetUpgradeNodeState(def.UpgradeId, out var lockReason);

            return new RuneUpgradeNodeView
            {
                Def = def,
                State = state,
                LockReason = lockReason,
            };
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
