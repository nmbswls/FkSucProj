using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public enum RuneLoadoutTab
    {
        Permanent = 0,
        Equippable = 1,
    }

    public sealed class RuneLoadoutPanel : PanelBase, IInputConsumer, IPlayerProgressionHubPage
    {
        public const string Pid = "RuneLoadoutPanel";

        Transform _builtRoot;
        RectTransform _equipArea;
        RectTransform _ownedGrid;
        RuneCellView _cellTemplate;
        TextMeshProUGUI _emptyHint;
        Button _tabPermanent;
        Button _tabEquippable;
        RuneEquipSlotView[] _slotViews;
        IPlayerProgressionHubHost _progressionHubHost;

        readonly List<RuneCellView> _spawnedCells = new();

        public RuneLoadoutTab CurrentTab { get; private set; } = RuneLoadoutTab.Permanent;

        public bool IsHostedByHub => _progressionHubHost != null;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }
        }

        public void SetProgressionHubHost(IPlayerProgressionHubHost host)
        {
            _progressionHubHost = host;
            ApplyHostedChromeIfNeeded();
        }

        void ApplyHostedChromeIfNeeded()
        {
            if (_progressionHubHost == null || _builtRoot == null)
            {
                return;
            }

            var blocker = _builtRoot.Find("BlockerButton");
            if (blocker != null)
            {
                blocker.gameObject.SetActive(false);
            }

            var closeTr = _builtRoot.Find("Window/Header/CloseBtn");
            var closeBtn = closeTr != null ? closeTr.GetComponent<Button>() : null;
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(CloseSelfOrHub);
            }
        }

        public void CloseSelfOrHub()
        {
            if (_progressionHubHost != null)
            {
                _progressionHubHost.CloseHub();
                return;
            }

            Debug.LogError("[RuneLoadoutPanel] Not hosted by PlayerProgressionHubPanel.");
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (!IsHostedByHub)
            {
                Debug.LogError("[RuneLoadoutPanel] Setup without hub host.");
                return;
            }

            if (transform.Find("BuiltRoot") == null)
            {
                Debug.LogError("[RuneLoadoutPanel] Prefab missing BuiltRoot.");
                return;
            }

            BindBuiltReferencesIfNeeded();
            ApplyHostedChromeIfNeeded();
            SelectTab(RuneLoadoutTab.Permanent);
        }

        public override void Show()
        {
            if (!IsHostedByHub)
            {
                Debug.LogError("[RuneLoadoutPanel] Show without hub host.");
                return;
            }

            base.Show();
            RefreshAll();
        }

        void BindBuiltReferencesIfNeeded()
        {
            if (_builtRoot != null)
            {
                return;
            }

            var root = transform.Find("BuiltRoot");
            if (root == null)
            {
                return;
            }

            _builtRoot = root;
            var window = root.Find("Window");
            _equipArea = window != null ? window.Find("EquipArea") as RectTransform : null;
            _ownedGrid = window != null ? window.Find("OwnedArea/OwnedScroll/Viewport/OwnedGrid") as RectTransform : null;
            _emptyHint = window != null ? window.Find("OwnedArea/EmptyHint")?.GetComponent<TextMeshProUGUI>() : null;
            _cellTemplate = _ownedGrid != null ? _ownedGrid.Find("RuneCell_Template")?.GetComponent<RuneCellView>() : null;

            var tabBar = window != null ? window.Find("TabBar") : null;
            if (tabBar != null)
            {
                _tabPermanent = tabBar.Find("TabPermanent")?.GetComponent<Button>();
                _tabEquippable = tabBar.Find("TabEquippable")?.GetComponent<Button>();
            }

            if (_equipArea != null)
            {
                _slotViews = _equipArea.GetComponentsInChildren<RuneEquipSlotView>(true);
            }

            if (_tabPermanent != null)
            {
                _tabPermanent.onClick.RemoveAllListeners();
                _tabPermanent.onClick.AddListener(() => SelectTab(RuneLoadoutTab.Permanent));
            }

            if (_tabEquippable != null)
            {
                _tabEquippable.onClick.RemoveAllListeners();
                _tabEquippable.onClick.AddListener(() => SelectTab(RuneLoadoutTab.Equippable));
            }

            if (_cellTemplate != null)
            {
                _cellTemplate.gameObject.SetActive(false);
            }
        }

        public void SelectTab(RuneLoadoutTab tab)
        {
            CurrentTab = tab;
            if (_equipArea != null)
            {
                _equipArea.gameObject.SetActive(tab == RuneLoadoutTab.Equippable);
            }

            RefreshAll();
        }

        void RefreshAll()
        {
            RefreshEquipSlots();
            RefreshOwnedGrid();
        }

        void RefreshEquipSlots()
        {
            if (_slotViews == null)
            {
                return;
            }

            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var runeSystem = pdm?.RuneSystem;
            if (runeSystem == null)
            {
                return;
            }

            foreach (var slotView in _slotViews)
            {
                if (slotView == null)
                {
                    continue;
                }

                var equippedId = runeSystem.GetEquipped(slotView.Slot);
                slotView.Bind(slotView.Slot, equippedId, OnEquipSlotClicked);
            }
        }

        void RefreshOwnedGrid()
        {
            if (_ownedGrid == null || _cellTemplate == null)
            {
                return;
            }

            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var runeSystem = pdm?.RuneSystem;
            if (runeSystem == null)
            {
                return;
            }

            var targetType = CurrentTab == RuneLoadoutTab.Permanent
                ? ERuneType.Permanent
                : ERuneType.Equippable;

            var owned = runeSystem.GetOwnedByType(targetType)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.RuneId)
                .ToList();

            ClearSpawnedCells();
            foreach (var def in owned)
            {
                var go = Instantiate(_cellTemplate.gameObject, _ownedGrid, false);
                go.SetActive(true);
                var cell = go.GetComponent<RuneCellView>();
                if (cell == null)
                {
                    continue;
                }

                bool isEquipped = false;
                if (def.RuneType == ERuneType.Equippable)
                {
                    var equippedId = runeSystem.GetEquipped(def.EquipSlot);
                    isEquipped = equippedId == def.RuneId;
                }

                cell.Bind(def, isEquipped, OnOwnedCellClicked);
                _spawnedCells.Add(cell);
            }

            if (_emptyHint != null)
            {
                _emptyHint.gameObject.SetActive(owned.Count == 0);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_ownedGrid);
        }

        void ClearSpawnedCells()
        {
            foreach (var cell in _spawnedCells)
            {
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }

            _spawnedCells.Clear();
        }

        void OnOwnedCellClicked(RuneData def)
        {
            if (def == null || def.RuneType != ERuneType.Equippable)
            {
                return;
            }

            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (pdm == null)
            {
                return;
            }

            if (pdm.TryEquipRune(def.EquipSlot, def.RuneId))
            {
                RefreshAll();
            }
        }

        void OnEquipSlotClicked(ERuneEquipSlot slot)
        {
            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (pdm == null)
            {
                return;
            }

            if (pdm.TryUnequipRune(slot))
            {
                RefreshAll();
            }
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
