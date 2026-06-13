using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public class SkillLoadoutPanel : PanelBase, IInputConsumer, IPlayerProgressionHubPage
    {
        public const string Pid = "SkillLoadoutPanel";

        public static SkillLoadoutPanel Current { get; private set; }

        // 与 PlayerSkillSystem.NormalSkillSlots 长度一致
        const int ActiveSlotCount = 8;

        enum ESkillLoadoutScreen
        {
            Entry,
            Equipped,
        }

        [SerializeField] Transform builtRoot;
        [SerializeField] GameObject entryPanel;
        [SerializeField] GameObject equippedPanel;
        [SerializeField] GameObject skillScrollRoot;
        [SerializeField] SkillSchoolEntryView schoolTabTemplate;
        [SerializeField] Transform schoolTabContainer;
        [SerializeField] SkillSlotView[] activeSlotViews;
        [SerializeField] SkillSlotView[] passiveSlotViews;
        [SerializeField] Transform skillGridContent;
        [SerializeField] SkillPoolEntryView skillCellTemplate;
        [SerializeField] TextMeshProUGUI skillGridEmptyHint;
        [SerializeField] Button closeButton;
        [SerializeField] Button blockerButton;
        [SerializeField] GameObject dragGhostRoot;
        [SerializeField] Image dragGhostIcon;
        [SerializeField] TextMeshProUGUI dragGhostLabel;
        [SerializeField] SkillLoadoutDetailView skillDetailView;

        [SerializeField] TextMeshProUGUI schoolLabel;
        [SerializeField] Button schoolBackBtn;


        IPlayerProgressionHubHost _progressionHubHost;
        ISkillDropBehavior _skillDropBehavior = new SchoolFilteredNormalSlotDropBehavior();

        readonly List<SkillPoolEntryView> _skillGridCells = new();
        readonly List<GameObject> _skillGridObjects = new();
        readonly List<SkillSchoolEntryView> _schoolEntries = new();
        readonly List<GameObject> _spawnedSchoolTabs = new();

        ESkillLoadoutScreen _screen = ESkillLoadoutScreen.Entry;
        int _activeTabIndex = -1;
        int _selectedEntryId;

        public int ActiveSchoolId { get; private set; }

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

        public override void Setup(object data = null)
        {
            base.Setup(data);

            if (builtRoot == null && transform.Find("BuiltRoot") == null)
            {
                Debug.LogError("[SkillLoadoutPanel] Prefab missing BuiltRoot.");
                return;
            }

            EnsurePanelRefs();
            EnsureSlotViews();
            WireUiEvents();
            EnsureSkillDetailView();
            WireSkillDetailView();
            ApplyHostedChromeIfNeeded();
            WireDragSession();
            InitSchoolEntries();
            ShowEntryScreen();
        }

        public override void Show()
        {
            base.Show();
            Current = this;
            EnsureSlotViews();
            SkillDragSession.SetCanvas(ResolveSkillDragCanvas());
            WireDragSession();
            ShowEntryScreen();
        }

        public override void Hide()
        {
            SkillDragSession.End();
            Current = null;
            base.Hide();
        }

        public override void Teardown()
        {
            SkillDragSession.End();
            ClearSchoolTabs();
            Current = null;
            base.Teardown();
        }

        void EnsurePanelRefs()
        {
            Transform root = builtRoot != null ? builtRoot : transform.Find("BuiltRoot");
            if (root == null)
            {
                return;
            }

            if (entryPanel == null)
            {
                entryPanel = root.Find("EntryPanel")?.gameObject;
            }

            if (equippedPanel == null)
            {
                equippedPanel = root.Find("EquippedPanel")?.gameObject;
            }

            if (skillScrollRoot == null)
            {
                skillScrollRoot = root.Find("SkillScroll")?.gameObject;
            }

            if (schoolBackBtn == null )
            {
                schoolBackBtn = root.Find("BtnBack")?.GetComponent<Button>();
            }

            if (schoolLabel == null)
            {
                schoolLabel = root.Find("SchoolLabel")?.GetComponent<TextMeshProUGUI>();
            }

            EnsureSchoolTabContainer();
        }

        void EnsureSchoolTabContainer()
        {
            if (schoolTabContainer != null)
            {
                return;
            }

            Transform root = builtRoot != null ? builtRoot : transform.Find("BuiltRoot");
            if (root == null)
            {
                return;
            }

            schoolTabContainer = root.Find("EntryPanel");
        }

        void EnsureSlotViews()
        {
            if (equippedPanel == null)
            {
                return;
            }

            var views = equippedPanel.GetComponentsInChildren<SkillSlotView>(true);
            if (views == null || views.Length == 0)
            {
                return;
            }

            var activeViews = new SkillSlotView[ActiveSlotCount];
            var passiveViews = new SkillSlotView[PlayerSkillSystem.PassiveSlotCount];

            foreach (var view in views)
            {
                if (view == null)
                {
                    continue;
                }

                if (view.slotKind == SkillLoadoutSlotKind.Passive)
                {
                    RegisterSlotView(passiveViews, view, PlayerSkillSystem.PassiveSlotCount, "passive");
                }
                else
                {
                    RegisterSlotView(activeViews, view, ActiveSlotCount, "active");
                }
            }

            activeSlotViews = activeViews;
            passiveSlotViews = passiveViews;
        }

        static void RegisterSlotView(SkillSlotView[] slots, SkillSlotView view, int capacity, string kindLabel)
        {
            if (view.SlotIndex < 0 || view.SlotIndex >= capacity)
            {
                Debug.LogWarning(
                    $"[SkillLoadoutPanel] {kindLabel} slot '{view.name}' SlotIndex={view.SlotIndex} is out of range [0,{capacity}), ignored.",
                    view);
                return;
            }

            if (slots[view.SlotIndex] != null)
            {
                Debug.LogError(
                    $"[SkillLoadoutPanel] Duplicate {kindLabel} SlotIndex={view.SlotIndex} between '{slots[view.SlotIndex].name}' and '{view.name}'.",
                    view);
                return;
            }

            slots[view.SlotIndex] = view;
        }

        void RefreshEquippedSlots()
        {
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            RefreshSlotDisplays(mgr?.SkillSystem);
        }

        void EnsureSkillDetailView()
        {
            if (skillDetailView != null)
            {
                return;
            }

            Transform root = builtRoot != null ? builtRoot : transform.Find("BuiltRoot");
            if (root == null)
            {
                return;
            }

            var detailTr = root.Find("SkillDetail");
            if (detailTr == null)
            {
                return;
            }

            skillDetailView = detailTr.GetComponent<SkillLoadoutDetailView>();
            if (skillDetailView == null)
            {
                skillDetailView = detailTr.gameObject.AddComponent<SkillLoadoutDetailView>();
            }
        }

        void WireSkillDetailView()
        {
            skillDetailView?.SetLearnHandler(OnLearnEntryClicked);
        }

        void WireUiEvents()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseSelfOrHub);
            }

            if (blockerButton != null)
            {
                blockerButton.onClick.RemoveAllListeners();
                blockerButton.onClick.AddListener(CloseSelfOrHub);
            }

            if (schoolBackBtn != null)
            {
                schoolBackBtn.onClick.RemoveAllListeners();
                schoolBackBtn.onClick.AddListener(ShowEntryScreen);
            }
        }

        void ApplyHostedChromeIfNeeded()
        {
            if (_progressionHubHost == null || builtRoot == null)
            {
                return;
            }

            if (blockerButton != null)
            {
                blockerButton.gameObject.SetActive(false);
            }
            else
            {
                var blocker = builtRoot.Find("BlockerButton");
                if (blocker != null)
                {
                    blocker.gameObject.SetActive(false);
                }
            }
        }

        void WireDragSession()
        {
            if (dragGhostRoot == null)
            {
                return;
            }

            SkillDragSession.Configure(
                dragGhostRoot,
                dragGhostIcon,
                dragGhostLabel,
                ResolveSkillDragCanvas());
        }

        Canvas ResolveSkillDragCanvas()
        {
            if (_progressionHubHost != null)
            {
                var c = _progressionHubHost.ResolveHubCanvas();
                if (c != null)
                {
                    return c;
                }
            }

            return GetComponentInParent<Canvas>();
        }

        public void CloseSelfOrHub()
        {
            if (_progressionHubHost != null)
            {
                _progressionHubHost.CloseHub();
                return;
            }

            Debug.LogError("[SkillLoadoutPanel] Not hosted by PlayerProgressionHubPanel.");
        }

        void InitSchoolEntries()
        {
            EnsureSchoolTabContainer();
            BuildSchoolTabs();
        }

        void BuildSchoolTabs()
        {
            ClearSchoolTabs();
            if (schoolTabTemplate == null || schoolTabContainer == null)
            {
                Debug.LogWarning("[SkillLoadoutPanel] schoolTabTemplate or schoolTabContainer is missing.");
                return;
            }

            var schools = SkillLearnCatalog.GetSchoolsSorted();
            for (var i = 0; i < schools.Count; i++)
            {
                var entryView = Instantiate(schoolTabTemplate, schoolTabContainer);
                entryView.gameObject.SetActive(true);
                entryView.name = $"SchoolTab_{i}";
                _schoolEntries.Add(entryView);
                _spawnedSchoolTabs.Add(entryView.gameObject);
                entryView.Bind(i, schools[i], OnSchoolEntryClicked);
            }
        }

        void ClearSchoolTabs()
        {
            for (var i = _spawnedSchoolTabs.Count - 1; i >= 0; i--)
            {
                if (_spawnedSchoolTabs[i] != null)
                {
                    Destroy(_spawnedSchoolTabs[i]);
                }
            }

            _spawnedSchoolTabs.Clear();
            _schoolEntries.Clear();
            ClearPreviewSchoolTabs();
        }

        // Prefab 内 EntryPanel 可能预置 SkillSchoolTabItem 仅作编辑器预览，运行时清掉后再 Instantiate。
        void ClearPreviewSchoolTabs()
        {
            if (schoolTabContainer == null)
            {
                return;
            }

            var previews = schoolTabContainer.GetComponentsInChildren<SkillSchoolEntryView>(true);
            for (var i = previews.Length - 1; i >= 0; i--)
            {
                var view = previews[i];
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
        }

        void RefreshSchoolEntries()
        {
            var schools = SkillLearnCatalog.GetSchoolsSorted();
            if (_schoolEntries.Count != schools.Count)
            {
                BuildSchoolTabs();
                return;
            }

            for (var i = 0; i < _schoolEntries.Count; i++)
            {
                var entryView = _schoolEntries[i];
                if (entryView == null)
                {
                    continue;
                }

                SkillSchool school = i < schools.Count ? schools[i] : null;
                entryView.Bind(i, school, OnSchoolEntryClicked);
            }
        }

        void OnSchoolEntryClicked(int schoolId)
        {
            if (schoolId <= 0)
            {
                ShowSchoolLockedTip(0);
                return;
            }

            if (!SkillSchoolAccessUtil.IsSchoolUnlocked(schoolId))
            {
                ShowSchoolLockedTip(schoolId);
                return;
            }

            EnterEquippedScreen(schoolId);
        }

        void ShowSchoolLockedTip(int schoolId)
        {
            string message = SkillSchoolAccessUtil.ResolveLockedHint(schoolId);
            var glm = MainGameManager.Instance?.gameLogicManager;
            var player = glm?.playerLogicEntity;
            if (player != null)
            {
                MainGameManager.Instance.ShowFakeFxEffect(message, player.Pos);
            }
            else
            {
                Debug.LogWarning(message);
            }
        }

        void ShowEntryScreen()
        {
            _screen = ESkillLoadoutScreen.Entry;
            _activeTabIndex = -1;
            ActiveSchoolId = 0;
            _selectedEntryId = 0;

            SkillDragSession.End();
            HideSkillDetail();
            ClearSkillGrid();

            if (schoolLabel != null)
            {
                schoolLabel.gameObject.SetActive(false);
            }
            if (schoolBackBtn != null)
            {
                schoolBackBtn.gameObject.SetActive(false);
            }

            if (skillScrollRoot != null)
            {
                skillScrollRoot.SetActive(false);
            }

            if (skillDetailView != null)
            {
                skillDetailView.gameObject.SetActive(false);
            }

            if (equippedPanel != null)
            {
                equippedPanel.SetActive(true);
            }

            RefreshSchoolEntries();
            RefreshEquippedSlots();
        }

        void EnterEquippedScreen(int schoolId)
        {
            _screen = ESkillLoadoutScreen.Equipped;

            if (equippedPanel != null)
            {
                equippedPanel.SetActive(true);
            }
            
            if (schoolLabel != null)
            {
                schoolLabel.gameObject.SetActive(true);
            }
            if (schoolBackBtn != null)
            {
                schoolBackBtn.gameObject.SetActive(true);
            }

            if (skillScrollRoot != null)
            {
                skillScrollRoot.SetActive(true);
            }

            SelectSchoolById(schoolId);
        }

        void SelectSchoolById(int schoolId)
        {
            if (schoolId <= 0)
            {
                return;
            }

            var tabIndex = FindTabIndexForSchool(schoolId);
            if (tabIndex < 0)
            {
                return;
            }

            _activeTabIndex = tabIndex;
            ActiveSchoolId = schoolId;
            _selectedEntryId = 0;
            HideSkillDetail();

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;
            var entries = SkillLearnCatalog.GetLearnEntriesBySchool(ActiveSchoolId);
            RefreshSkillGrid(entries, sys);
            RefreshSlotDisplays(sys);
        }

        void RefreshSkillGrid(IReadOnlyList<SkillLearnEntry> entries, PlayerSkillSystem sys)
        {
            ClearSkillGrid();
            if (skillGridContent == null || skillCellTemplate == null)
            {
                return;
            }

            int visibleCount = 0;
            if (entries != null)
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.SkillId))
                    {
                        continue;
                    }

                    if (sys != null &&
                        (sys.IsGrantedPassive(entry.SkillId) || sys.IsGrantedActive(entry.SkillId)))
                    {
                        continue;
                    }

                    visibleCount++;
                    var cell = Instantiate(skillCellTemplate, skillGridContent);
                    cell.gameObject.SetActive(true);
                    bool learned = sys != null && sys.IsSkillLearned(entry.SkillId);
                    bool selected = !learned && entry.EntryId == _selectedEntryId;
                    cell.Bind(
                        entry,
                        learned,
                        selected,
                        _skillDropBehavior,
                        OnSkillEntrySelected,
                        OnSkillEntryDetailClicked);
                    _skillGridObjects.Add(cell.gameObject);
                    _skillGridCells.Add(cell);
                }
            }

            if (skillGridEmptyHint != null)
            {
                skillGridEmptyHint.gameObject.SetActive(visibleCount <= 0);
            }
        }

        void ClearSkillGrid()
        {
            _skillGridObjects.Clear();
            _skillGridCells.Clear();

            if (skillGridContent == null)
            {
                return;
            }

            // 清除 prefab 中用于编辑器预览的占位 cell
            for (int i = skillGridContent.childCount - 1; i >= 0; i--)
            {
                Destroy(skillGridContent.GetChild(i).gameObject);
            }
        }

        void OnSkillEntrySelected(int entryId)
        {
            if (entryId <= 0)
            {
                return;
            }

            _selectedEntryId = entryId;
            RefreshSkillGridSelection();
            ShowSkillDetail(entryId);
        }

        void OnSkillEntryDetailClicked(int entryId)
        {
            if (entryId <= 0)
            {
                return;
            }

            _selectedEntryId = 0;
            RefreshSkillGridSelection();
            ShowSkillDetail(entryId);
        }

        void ShowSkillDetail(int entryId)
        {
            EnsureSkillDetailView();
            if (skillDetailView == null)
            {
                return;
            }

            var detailTr = skillDetailView.transform;
            if (detailTr.parent != null)
            {
                detailTr.SetAsLastSibling();
            }

            skillDetailView.Show(entryId);
        }

        void HideSkillDetail()
        {
            skillDetailView?.Hide();
        }

        void RefreshSkillGridSelection()
        {
            for (var i = 0; i < _skillGridCells.Count; i++)
            {
                var cell = _skillGridCells[i];
                if (cell == null)
                {
                    continue;
                }

                cell.SetSelected(cell.EntryId == _selectedEntryId);
            }
        }

        void OnLearnEntryClicked(int entryId)
        {
            var entry = SkillLearnCatalog.TryGetLearnEntry(entryId);
            if (entry == null)
            {
                return;
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr == null)
            {
                return;
            }

            if (!mgr.CanLearnSkillFromEntry(entryId, out var reason))
            {
                ShowLearnFailedTip(reason);
                return;
            }

            string skillName = ResolveSkillDisplayName(entry);
            YesNoMsgBox.Show(
                "学习技能",
                $"确定要学习「{skillName}」吗？",
                () => ConfirmLearnSkill(entryId),
                null);
        }

        void ConfirmLearnSkill(int entryId)
        {
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr == null)
            {
                return;
            }

            if (!mgr.TryLearnSkillFromEntry(entryId, out var reason))
            {
                ShowLearnFailedTip(reason);
                return;
            }

            _selectedEntryId = 0;
            HideSkillDetail();
            RefreshAll();
        }

        void ShowLearnFailedTip(string reason)
        {
            string message = string.IsNullOrEmpty(reason) ? "无法学习该技能" : reason;
            var glm = MainGameManager.Instance?.gameLogicManager;
            var player = glm?.playerLogicEntity;
            if (player != null)
            {
                MainGameManager.Instance.ShowFakeFxEffect(message, player.Pos);
            }
            else
            {
                Debug.LogWarning("Learn skill failed: " + message);
            }
        }

        static string ResolveSkillDisplayName(SkillLearnEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(entry.DisplayName))
            {
                return entry.DisplayName;
            }

            if (!string.IsNullOrEmpty(entry.SkillId))
            {
                var cfg = My.Map.Entity.SkillLibrary.GetSkillConfig(entry.SkillId);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Desc))
                {
                    return cfg.Desc;
                }

                return entry.SkillId;
            }

            return string.Empty;
        }

        void RefreshSlotDisplays(PlayerSkillSystem sys)
        {
            if (sys == null)
            {
                return;
            }

            if (activeSlotViews != null)
            {
                foreach (var slot in activeSlotViews)
                {
                    slot?.RefreshDisplay(sys);
                }
            }

            if (passiveSlotViews != null)
            {
                foreach (var slot in passiveSlotViews)
                {
                    slot?.RefreshDisplay(sys);
                }
            }
        }

        public void RefreshAll()
        {
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;

            if (_screen == ESkillLoadoutScreen.Entry)
            {
                RefreshSchoolEntries();
                RefreshEquippedSlots();
                return;
            }

            RefreshSlotDisplays(sys);

            if (ActiveSchoolId > 0)
            {
                SelectSchoolById(ActiveSchoolId);
            }
            else if (_activeTabIndex >= 0)
            {
                var schools = SkillLearnCatalog.GetSchoolsSorted();
                if (_activeTabIndex < schools.Count)
                {
                    SelectSchoolById(schools[_activeTabIndex].SchoolId);
                }
            }

            TryRefreshHudBar();
        }

        int FindTabIndexForSchool(int schoolId)
        {
            var schools = SkillLearnCatalog.GetSchoolsSorted();
            for (var i = 0; i < schools.Count; i++)
            {
                if (schools[i].SchoolId == schoolId)
                {
                    return i;
                }
            }

            return -1;
        }

        public void ApplyLoadoutToEntity()
        {
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            mgr?.SyncLearnedSkillsToPlayerEntity();
        }

        public void TryClearPassiveSlotFromUi(int slotIndex)
        {
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;
            if (sys == null)
            {
                return;
            }

            if (!sys.TryClearPassiveSlot(slotIndex, out _))
            {
                return;
            }

            ApplyLoadoutToEntity();
            RefreshAll();
        }

        static void TryRefreshHudBar()
        {
            var hud = OverworldHUDPanel.Instance;
            if (hud != null && hud.SkilBar != null)
            {
                hud.SkilBar.Refresh();
            }
        }

        public bool OnConfirm() => false;

        public bool OnCancel()
        {
            if (_screen == ESkillLoadoutScreen.Equipped)
            {
                if (_selectedEntryId > 0 || (skillDetailView != null && skillDetailView.gameObject.activeSelf))
                {
                    _selectedEntryId = 0;
                    HideSkillDetail();
                    RefreshSkillGridSelection();
                    return true;
                }

                ShowEntryScreen();
                return true;
            }

            return false;
        }

        public bool OnNavigate(Vector2 dir) => false;

        public bool OnHotkey(string keyName) => false;

        public bool OnScroll(float deltaY) => false;

        public bool OnClick(int button, Vector2 mousePos) => false;

        public bool OnHoldStart(string holdKey) => false;

        public bool OnHoldUpdate(string holdKey) => false;

        public bool OnHoldingEnd(string holdKey) => false;
    }
}
