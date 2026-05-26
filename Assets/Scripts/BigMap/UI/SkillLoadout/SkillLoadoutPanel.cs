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

        [SerializeField] Transform builtRoot;
        [SerializeField] Button[] tabButtons;
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

        public bool IsHostedByHub => _progressionHubHost != null;

        IPlayerProgressionHubHost _progressionHubHost;
        ISkillDropBehavior _skillDropBehavior = new SchoolFilteredNormalSlotDropBehavior();

        readonly List<SkillPoolEntryView> _skillGridCells = new();
        readonly List<GameObject> _skillGridObjects = new();

        int _tabCount;
        int _activeTabIndex;

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
            if (!IsHostedByHub)
            {
                Debug.LogError("[SkillLoadoutPanel] Setup without hub host.");
                return;
            }

            if (builtRoot == null && transform.Find("BuiltRoot") == null)
            {
                Debug.LogError("[SkillLoadoutPanel] Prefab missing BuiltRoot. Run Tools/ProgressionHub/Setup Hub UI Prefabs.");
                return;
            }

            WireUiEvents();
            ApplyHostedChromeIfNeeded();
            WireDragSession();
            InitTabsFromTable();
            if (_tabCount > 0)
            {
                SelectSchool(0);
            }
            else
            {
                RefreshAll();
            }
        }

        public override void Show()
        {
            if (!IsHostedByHub)
            {
                Debug.LogError("[SkillLoadoutPanel] Show without hub host.");
                return;
            }

            base.Show();
            Current = this;
            SkillDragSession.SetCanvas(ResolveSkillDragCanvas());
            WireDragSession();
            RefreshAll();
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
            Current = null;
            base.Teardown();
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

        void InitTabsFromTable()
        {
            var schools = SkillLearnCatalog.GetSchoolsSorted();
            _tabCount = 0;
            if (tabButtons == null)
            {
                return;
            }

            for (var i = 0; i < tabButtons.Length; i++)
            {
                var b = tabButtons[i];
                if (b == null)
                {
                    continue;
                }

                if (i >= schools.Count)
                {
                    b.gameObject.SetActive(false);
                    continue;
                }

                b.gameObject.SetActive(true);
                _tabCount++;
                var nm = schools[i].DisplayName;
                var lbl = b.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null)
                {
                    lbl.text = nm;
                }

                var captured = i;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => SelectSchool(captured));
            }
        }

        void SelectSchool(int tabIndex)
        {
            var schools = SkillLearnCatalog.GetSchoolsSorted();
            if (tabIndex < 0 || tabIndex >= schools.Count)
            {
                return;
            }

            _activeTabIndex = tabIndex;
            ActiveSchoolId = schools[tabIndex].SchoolId;
            UpdateTabVisuals(tabIndex);

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;
            var entries = SkillLearnCatalog.GetLearnEntriesBySchool(ActiveSchoolId);
            RefreshSkillGrid(entries, sys);
            RefreshSlotDisplays(sys);
        }

        void UpdateTabVisuals(int tabIndex)
        {
            if (tabButtons == null)
            {
                return;
            }

            for (var i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null || !tabButtons[i].gameObject.activeSelf)
                {
                    continue;
                }

                var img = tabButtons[i].GetComponent<Image>();
                if (img == null)
                {
                    continue;
                }

                img.color = i == tabIndex
                    ? new Color(0.38f, 0.55f, 0.72f, 1f)
                    : new Color(0.28f, 0.3f, 0.35f, 1f);
            }
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
                    var cellGo = Instantiate(skillCellTemplate.gameObject, skillGridContent);
                    cellGo.SetActive(true);
                    var cell = cellGo.GetComponent<SkillPoolEntryView>();
                    bool learned = sys != null && sys.IsSkillLearned(entry.SkillId);
                    cell.Bind(entry, learned, _skillDropBehavior, OnLearnEntryClicked);
                    _skillGridObjects.Add(cellGo);
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
            for (int i = 0; i < _skillGridObjects.Count; i++)
            {
                if (_skillGridObjects[i] != null)
                {
                    Destroy(_skillGridObjects[i]);
                }
            }

            _skillGridObjects.Clear();
            _skillGridCells.Clear();
        }

        void OnLearnEntryClicked(int entryId)
        {
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr == null)
            {
                return;
            }

            if (!mgr.TryLearnSkillFromEntry(entryId, out var reason))
            {
                Debug.LogWarning("Learn skill failed: " + reason);
            }

            RefreshAll();
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
            RefreshSlotDisplays(sys);

            if (ActiveSchoolId > 0)
            {
                var idx = FindTabIndexForSchool(ActiveSchoolId);
                if (idx >= 0)
                {
                    SelectSchool(idx);
                }
            }
            else if (_tabCount > 0)
            {
                SelectSchool(0);
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
            CloseSelfOrHub();
            return true;
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
