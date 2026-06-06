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
    public class RunePanel : PanelBase, IInputConsumer, IPlayerProgressionHubPage
    {
        public const string Pid = "RuneLoadoutPanel";

        [SerializeField] Transform builtRoot;
        [SerializeField] RectTransform slotGrid;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailBody;
        [SerializeField] RectTransform ownedArea;
        [SerializeField] RectTransform ownedGrid;
        [SerializeField] RuneOwnedCell ownedCellTemplate;
        [SerializeField] TextMeshProUGUI ownedHint;
        [SerializeField] RuneSlotView slotPrefab;
        [SerializeField] Button closeButton;
        [SerializeField] Button blockerButton;
        [SerializeField] RuneDragDropController dragController;

        IPlayerProgressionHubHost _progressionHubHost;

        readonly List<RuneSlotView> _slotViews = new();
        readonly List<RuneOwnedCell> _ownedCells = new();

        RuneSlotView _selectedSlot;
        ERuneEquipSlot _selectedEquipSlot = ERuneEquipSlot.None;
        string _selectedFixedRuneId;
        string _selectedOwnedRuneId;

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
        }

        void OnEnable()
        {
            PlayerEventBus.Subscribe<PlayerRuneGrantedEvent>(OnRuneGranted);
        }

        void OnDisable()
        {
            PlayerEventBus.Unsubscribe<PlayerRuneGrantedEvent>(OnRuneGranted);
        }

        void OnRuneGranted(PlayerRuneGrantedEvent e)
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

            if (blockerButton != null)
            {
                blockerButton.gameObject.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseSelfOrHub);
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

            if (slotPrefab == null)
            {
                Debug.LogError("[RunePanel] Prefab missing slotPrefab.");
                return false;
            }

            if (ownedGrid == null || ownedCellTemplate == null)
            {
                Debug.LogError("[RunePanel] Prefab missing ownedGrid or ownedCellTemplate.");
                return false;
            }

            if (detailTitle == null || detailBody == null)
            {
                Debug.LogError("[RunePanel] Prefab missing detailTitle or detailBody.");
                return false;
            }

            if (dragController == null)
            {
                Debug.LogError("[RunePanel] Prefab missing dragController.");
                return false;
            }

            return true;
        }

        public void RefreshAll()
        {
            RefreshSlots();
            RefreshOwnedGrid();
            RefreshDetailPanel();
        }

        void RefreshSlots()
        {
            var runeSystem = GetRuneSystem();
            if (runeSystem == null)
            {
                return;
            }

            ClearSlotViews();
            foreach (var def in RuneCatalog.GetPermanentCatalog())
            {
                var slot = CreateSlotView();
                if (slot == null)
                {
                    continue;
                }

                bool unlocked = runeSystem.OwnsRune(def.RuneId);
                slot.BindPanel(this);
                slot.RefreshFixed(def.RuneId, unlocked, _selectedFixedRuneId == def.RuneId);
                _slotViews.Add(slot);
            }

            foreach (var equipSlot in RuneCatalog.EquipSlots)
            {
                var slot = CreateSlotView();
                if (slot == null)
                {
                    continue;
                }

                bool slotUnlocked = IsEquipSlotUnlocked(runeSystem, equipSlot);
                string equippedId = runeSystem.GetEquipped(equipSlot);
                slot.BindPanel(this);
                slot.RefreshEquippable(equipSlot, slotUnlocked, equippedId, _selectedEquipSlot == equipSlot);
                _slotViews.Add(slot);
            }

            _selectedSlot = FindSelectedSlotView();
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotGrid);
        }

        static bool IsEquipSlotUnlocked(PlayerRuneSystem runeSystem, ERuneEquipSlot slot)
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

        RuneSlotView CreateSlotView()
        {
            var view = Instantiate(slotPrefab, slotGrid, false);
            view.gameObject.SetActive(true);
            return view;
        }

        void ClearSlotViews()
        {
            foreach (var slot in _slotViews)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _slotViews.Clear();
        }

        RuneSlotView FindSelectedSlotView()
        {
            foreach (var slot in _slotViews)
            {
                if (slot?.Binder == null)
                {
                    continue;
                }

                if (slot.Binder.SlotKind == RuneSlotKind.Fixed
                    && !string.IsNullOrEmpty(_selectedFixedRuneId)
                    && slot.Binder.FixedRuneId == _selectedFixedRuneId)
                {
                    return slot;
                }

                if (slot.Binder.SlotKind == RuneSlotKind.Equippable
                    && _selectedEquipSlot != ERuneEquipSlot.None
                    && slot.Binder.EquipSlot == _selectedEquipSlot)
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
                if (detailTitle != null)
                {
                    detailTitle.text = string.Empty;
                }

                if (detailBody != null)
                {
                    detailBody.text = "点击槽位查看符文详情。";
                }

                return;
            }

            var provider = _selectedSlot.GetComponent<RuneInfoProvider>();
            if (provider == null)
            {
                return;
            }

            if (detailTitle != null)
            {
                detailTitle.text = provider.GetDisplayName();
            }

            if (detailBody != null)
            {
                if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Fixed
                    && _selectedSlot.State == RuneSlotVisualState.Locked)
                {
                    detailBody.text = "该常驻符文尚未解锁。";
                }
                else if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Equippable
                         && _selectedSlot.State == RuneSlotVisualState.Locked)
                {
                    detailBody.text = "尚未获得可用于该槽位的符文。";
                }
                else if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Equippable
                         && _selectedSlot.State == RuneSlotVisualState.Empty)
                {
                    detailBody.text = string.IsNullOrEmpty(provider.GetDetailText())
                        ? "从下方列表选择或拖拽符文进行装配。"
                        : provider.GetDetailText();
                }
                else
                {
                    detailBody.text = provider.GetDetailText();
                }
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
