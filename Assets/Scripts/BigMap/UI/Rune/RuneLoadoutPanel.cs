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
        const string SlotPrefabPath = "UI/Prefabs/PlayerProgressionHubPanelSub/RuneSlot";

        Transform _builtRoot;
        RectTransform _slotGrid;
        RectTransform _detailArea;
        TextMeshProUGUI _detailTitle;
        TextMeshProUGUI _detailBody;
        RectTransform _ownedArea;
        RectTransform _ownedGrid;
        RuneOwnedCell _ownedCellTemplate;
        TextMeshProUGUI _ownedHint;
        RuneDragDropController _dragController;

        GameObject _slotPrefab;
        RuneSlotView _slotTemplate;
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

            _dragController = GetComponent<RuneDragDropController>();
            if (_dragController == null)
            {
                _dragController = gameObject.AddComponent<RuneDragDropController>();
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

            Debug.LogError("[RunePanel] Not hosted by PlayerProgressionHubPanel.");
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);

            if (transform.Find("BuiltRoot") == null)
            {
                Debug.LogError("[RunePanel] Prefab missing BuiltRoot.");
                return;
            }

            BindBuiltReferencesIfNeeded();
            ApplyHostedChromeIfNeeded();
            HideLegacyTabUi();
            RefreshAll();
        }

        public override void Show()
        {
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
            EnsureLayout(window);
            _ownedGrid = window != null ? window.Find("OwnedArea/OwnedScroll/Viewport/OwnedGrid") as RectTransform : null;
            _ownedHint = window != null ? window.Find("OwnedArea/EmptyHint")?.GetComponent<TextMeshProUGUI>() : null;
            _ownedCellTemplate = _ownedGrid != null ? _ownedGrid.Find("RuneCell_Template")?.GetComponent<RuneOwnedCell>() : null;

            WireOwnedCellTemplateFromLegacy();
            if (_ownedCellTemplate != null)
            {
                _ownedCellTemplate.gameObject.SetActive(false);
            }

            _slotPrefab = Resources.Load<GameObject>(SlotPrefabPath);
            if (_slotPrefab == null)
            {
                _slotTemplate = window != null ? window.Find("RuneSlot_Template")?.GetComponent<RuneSlotView>() : null;
            }
        }

        void WireOwnedCellTemplateFromLegacy()
        {
            if (_ownedCellTemplate != null || _ownedGrid == null)
            {
                return;
            }

            var legacy = _ownedGrid.Find("RuneCell_Template");
            if (legacy == null)
            {
                return;
            }

            var owned = legacy.GetComponent<RuneOwnedCell>();
            if (owned == null)
            {
                owned = legacy.GetComponent<RuneCellView>();
            }

            if (owned == null)
            {
                owned = legacy.gameObject.AddComponent<RuneOwnedCell>();
            }

            var legacyView = legacy.GetComponent<RuneCellView>();
            if (legacyView != null)
            {
                owned.bg = owned.bg != null ? owned.bg : legacy.Find("Bg")?.GetComponent<Image>();
                owned.icon = owned.icon != null ? owned.icon : legacyView.IconImage;
                owned.nameText = owned.nameText != null ? owned.nameText : legacyView.NameText;
                owned.equippedMark = owned.equippedMark != null ? owned.equippedMark : legacyView.EquippedMark;
            }
            else
            {
                owned.bg = owned.bg != null ? owned.bg : legacy.Find("Bg")?.GetComponent<Image>();
                owned.icon = owned.icon != null ? owned.icon : legacy.Find("Icon")?.GetComponent<Image>();
                owned.nameText = owned.nameText != null ? owned.nameText : legacy.Find("Name")?.GetComponent<TextMeshProUGUI>();
                owned.equippedMark = owned.equippedMark != null ? owned.equippedMark : legacy.Find("EquippedMark")?.GetComponent<Image>();
            }

            _ownedCellTemplate = owned;
        }

        void EnsureLayout(Transform window)
        {
            if (window == null)
            {
                return;
            }

            var tabBar = window.Find("TabBar");
            if (tabBar != null)
            {
                tabBar.gameObject.SetActive(false);
            }

            _detailArea = window.Find("DetailArea") as RectTransform;
            if (_detailArea == null)
            {
                var detailGo = new GameObject("DetailArea", typeof(RectTransform));
                _detailArea = detailGo.GetComponent<RectTransform>();
                _detailArea.SetParent(window, false);
                _detailArea.anchorMin = new Vector2(0.68f, 0.12f);
                _detailArea.anchorMax = new Vector2(0.98f, 0.88f);
                _detailArea.offsetMin = Vector2.zero;
                _detailArea.offsetMax = Vector2.zero;

                var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleGo.transform.SetParent(_detailArea, false);
                _detailTitle = titleGo.GetComponent<TextMeshProUGUI>();
                _detailTitle.fontSize = 22;
                _detailTitle.alignment = TextAlignmentOptions.TopLeft;
                var titleRect = titleGo.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.sizeDelta = new Vector2(0f, 36f);
                titleRect.anchoredPosition = Vector2.zero;

                var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
                bodyGo.transform.SetParent(_detailArea, false);
                _detailBody = bodyGo.GetComponent<TextMeshProUGUI>();
                _detailBody.fontSize = 16;
                _detailBody.alignment = TextAlignmentOptions.TopLeft;
                var bodyRect = bodyGo.GetComponent<RectTransform>();
                bodyRect.anchorMin = new Vector2(0f, 0f);
                bodyRect.anchorMax = new Vector2(1f, 1f);
                bodyRect.offsetMin = new Vector2(0f, 0f);
                bodyRect.offsetMax = new Vector2(0f, -44f);
            }
            else
            {
                _detailTitle = _detailArea.Find("Title")?.GetComponent<TextMeshProUGUI>();
                _detailBody = _detailArea.Find("Body")?.GetComponent<TextMeshProUGUI>();
            }

            _slotGrid = window.Find("SlotArea/Viewport/Content/SlotGrid") as RectTransform;
            if (_slotGrid == null)
            {
                var slotAreaGo = new GameObject("SlotArea", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                var slotArea = slotAreaGo.GetComponent<RectTransform>();
                slotArea.SetParent(window, false);
                slotArea.anchorMin = new Vector2(0.02f, 0.12f);
                slotArea.anchorMax = new Vector2(0.66f, 0.88f);
                slotArea.offsetMin = Vector2.zero;
                slotArea.offsetMax = Vector2.zero;

                var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
                var viewport = viewportGo.GetComponent<RectTransform>();
                viewport.SetParent(slotArea, false);
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = Vector2.zero;
                viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

                var contentGo = new GameObject("Content", typeof(RectTransform));
                var content = contentGo.GetComponent<RectTransform>();
                content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = new Vector2(0f, 0f);

                var gridGo = new GameObject("SlotGrid", typeof(RectTransform), typeof(GridLayoutGroup));
                _slotGrid = gridGo.GetComponent<RectTransform>();
                _slotGrid.SetParent(content, false);
                _slotGrid.anchorMin = new Vector2(0f, 1f);
                _slotGrid.anchorMax = new Vector2(1f, 1f);
                _slotGrid.pivot = new Vector2(0.5f, 1f);
                _slotGrid.anchoredPosition = Vector2.zero;
                _slotGrid.sizeDelta = new Vector2(0f, 0f);

                var layout = gridGo.GetComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(120f, 140f);
                layout.spacing = new Vector2(12f, 12f);
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 4;
                layout.childAlignment = TextAnchor.UpperCenter;

                var scroll = slotAreaGo.GetComponent<ScrollRect>();
                scroll.viewport = viewport;
                scroll.content = content;
                scroll.horizontal = false;
                scroll.vertical = true;
            }

            _ownedArea = window.Find("OwnedArea") as RectTransform;
            if (_ownedArea != null)
            {
                _ownedArea.gameObject.SetActive(false);
            }
        }

        void HideLegacyTabUi()
        {
            if (_builtRoot == null)
            {
                return;
            }

            var window = _builtRoot.Find("Window");
            if (window == null)
            {
                return;
            }

            var permanent = window.Find("PermanentArea");
            if (permanent != null)
            {
                permanent.gameObject.SetActive(false);
            }

            var equip = window.Find("EquipArea");
            if (equip != null)
            {
                equip.gameObject.SetActive(false);
            }
        }

        public void RefreshAll()
        {
            RefreshSlots();
            RefreshOwnedGrid();
            RefreshDetailPanel();
        }

        void RefreshSlots()
        {
            if (_slotGrid == null)
            {
                return;
            }

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

            LayoutRebuilder.ForceRebuildLayoutImmediate(_slotGrid);
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
            GameObject go;
            if (_slotPrefab != null)
            {
                go = Instantiate(_slotPrefab, _slotGrid, false);
            }
            else if (_slotTemplate != null)
            {
                go = Instantiate(_slotTemplate.gameObject, _slotGrid, false);
            }
            else
            {
                go = BuildRuntimeSlot(_slotGrid);
            }

            go.SetActive(true);
            var view = go.GetComponent<RuneSlotView>();
            if (view == null)
            {
                view = go.AddComponent<RuneSlotView>();
            }

            return view;
        }

        static GameObject BuildRuntimeSlot(Transform parent)
        {
            var go = new GameObject("RuneSlot", typeof(RectTransform), typeof(Image), typeof(RuneSlotBinder), typeof(RuneInfoProvider), typeof(RuneSlotView), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 140f);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.14f, 0.12f, 0.20f, 1f);

            var view = go.GetComponent<RuneSlotView>();
            view.Bg = bg;
            view.ClickButton = go.GetComponent<Button>();

            var labelGo = new GameObject("SlotLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            view.SlotLabel = labelGo.GetComponent<TextMeshProUGUI>();
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(0f, 22f);
            labelRect.anchoredPosition = new Vector2(0f, -4f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            view.Icon = iconGo.GetComponent<Image>();
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(72f, 72f);

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(go.transform, false);
            view.NameText = nameGo.GetComponent<TextMeshProUGUI>();
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.sizeDelta = new Vector2(0f, 28f);
            nameRect.anchoredPosition = new Vector2(0f, 8f);

            return go;
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
                if (_ownedArea != null)
                {
                    _ownedArea.gameObject.SetActive(slot.State != RuneSlotVisualState.Locked);
                }
            }
            else
            {
                _selectedEquipSlot = ERuneEquipSlot.None;
                _selectedFixedRuneId = slot.Binder.FixedRuneId;
                if (_ownedArea != null)
                {
                    _ownedArea.gameObject.SetActive(false);
                }
            }

            RefreshAll();
        }

        void RefreshDetailPanel()
        {
            if (_detailArea == null)
            {
                return;
            }

            if (_selectedSlot == null || _selectedSlot.Binder == null)
            {
                if (_detailTitle != null)
                {
                    _detailTitle.text = string.Empty;
                }

                if (_detailBody != null)
                {
                    _detailBody.text = "点击槽位查看符文详情。";
                }

                return;
            }

            var provider = _selectedSlot.GetComponent<RuneInfoProvider>();
            if (provider == null)
            {
                return;
            }

            if (_detailTitle != null)
            {
                _detailTitle.text = provider.GetDisplayName();
            }

            if (_detailBody != null)
            {
                if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Fixed
                    && _selectedSlot.State == RuneSlotVisualState.Locked)
                {
                    _detailBody.text = "该常驻符文尚未解锁。";
                }
                else if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Equippable
                         && _selectedSlot.State == RuneSlotVisualState.Locked)
                {
                    _detailBody.text = "尚未获得可用于该槽位的符文。";
                }
                else if (_selectedSlot.Binder.SlotKind == RuneSlotKind.Equippable
                         && _selectedSlot.State == RuneSlotVisualState.Empty)
                {
                    _detailBody.text = string.IsNullOrEmpty(provider.GetDetailText())
                        ? "从下方列表选择或拖拽符文进行装配。"
                        : provider.GetDetailText();
                }
                else
                {
                    _detailBody.text = provider.GetDetailText();
                }
            }
        }

        void RefreshOwnedGrid()
        {
            if (_ownedGrid == null || _ownedCellTemplate == null)
            {
                return;
            }

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
                .Where(x => x.EquipSlot == _selectedEquipSlot)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.RuneId)
                .ToList();

            ClearOwnedCells();
            string equippedId = runeSystem.GetEquipped(_selectedEquipSlot);
            for (int i = 0; i < owned.Count; i++)
            {
                var def = owned[i];
                var go = Instantiate(_ownedCellTemplate.gameObject, _ownedGrid, false);
                go.SetActive(true);
                var cell = go.GetComponent<RuneOwnedCell>();
                if (cell == null)
                {
                    cell = go.AddComponent<RuneOwnedCell>();
                }

                bool isEquipped = equippedId == def.RuneId;
                bool selected = _selectedOwnedRuneId == def.RuneId;
                cell.Bind(this, def, isEquipped, selected, i);
                _ownedCells.Add(cell);
            }

            if (_ownedHint != null)
            {
                _ownedHint.gameObject.SetActive(owned.Count == 0);
                _ownedHint.text = owned.Count == 0 ? "暂无该槽位可用符文" : string.Empty;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_ownedGrid);
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

    // prefab / 旧引用兼容
    public class RuneLoadoutPanel : RunePanel
    {
    }
}
