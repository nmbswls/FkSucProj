using System.Collections.Generic;
using cfg.demo;
using My;
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

        public bool IsHostedByHub => _progressionHubHost != null;

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

            var closeBtn = _builtRoot.Find("Window/Header/CloseBtn")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(CloseSelfOrHub);
            }

            var bl = _builtRoot.Find("BlockerButton")?.GetComponent<Button>();
            if (bl != null)
            {
                bl.onClick.RemoveAllListeners();
                bl.onClick.AddListener(CloseSelfOrHub);
            }
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

        IPlayerProgressionHubHost _progressionHubHost;

        public int ActiveSchoolId { get; private set; }

        Transform _builtRoot;
        SkillPoolEntryView[] _poolCells;
        ISkillDropBehavior _poolSkillDropBehavior = new SchoolFilteredNormalSlotDropBehavior();

        Button[] _tabButtons;
        SkillSlotView[] _activeSlotViews;
        SkillSlotView[] _passiveSlotViews;
        int _tabCount;
        int _activeTabIndex;

        Transform _learnContent;
        SkillLearnEntryView _learnRowTemplate;
        TextMeshProUGUI _poolEmptyHint;
        readonly List<SkillLearnEntryView> _learnRows = new();
        readonly List<GameObject> _learnRowObjects = new();

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (!IsHostedByHub)
            {
                Debug.LogError("[SkillLoadoutPanel] Setup without hub host.");
                return;
            }

            if (transform.Find("BuiltRoot") == null)
            {
                Debug.LogError("[SkillLoadoutPanel] Prefab 缺少 BuiltRoot。请检查 Resources/UI/Prefabs/SkillLoadoutPanel.prefab 层级或从版本库恢复。");
                return;
            }

            BindBuiltReferencesIfNeeded();
            ApplyHostedChromeIfNeeded();
            InitTabsFromTable();
            if (_tabCount > 0)
                SelectSchool(0);
            else
                RefreshAll();
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

        void BindBuiltReferencesIfNeeded()
        {
            if (_builtRoot != null) return;

            var root = transform.Find("BuiltRoot");
            if (root == null) return;

            _builtRoot = root;

            var poolRt = root.Find("Window/PoolScroll/Viewport/PoolContent");
            if (poolRt != null)
            {
                var cells = poolRt.GetComponentsInChildren<SkillPoolEntryView>(true);
                _poolCells = cells;
            }

            var tabRow = root.Find("Window/Tabs");
            if (tabRow != null)
                _tabButtons = tabRow.GetComponentsInChildren<Button>(true);

            var bar = root.Find("Window/BarRow");
            if (bar != null)
            {
                _activeSlotViews = bar.GetComponentsInChildren<SkillSlotView>(true);
                foreach (var v in _activeSlotViews)
                {
                    v.slotKind = SkillLoadoutSlotKind.Active;
                }
            }

            BindPassiveBar(root);
            BindLearnUi(root);
            WireSkillSlotDropZones();
            WireDragLayerFromHierarchy();

            var closeBtn = root.Find("Window/Header/CloseBtn")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(CloseSelfOrHub);
            }

            var bl = root.Find("BlockerButton")?.GetComponent<Button>();
            if (bl != null)
            {
                bl.onClick.RemoveAllListeners();
                bl.onClick.AddListener(CloseSelfOrHub);
            }
        }

        void BindPassiveBar(Transform root)
        {
            var window = root.Find("Window");
            if (window == null)
            {
                return;
            }

            var passiveRow = window.Find("PassiveBarRow");
            if (passiveRow != null)
            {
                _passiveSlotViews = passiveRow.GetComponentsInChildren<SkillSlotView>(true);
                foreach (var v in _passiveSlotViews)
                {
                    v.slotKind = SkillLoadoutSlotKind.Passive;
                }
            }
        }

        void BindLearnUi(Transform root)
        {
            var window = root.Find("Window");
            if (window == null)
            {
                return;
            }

            _learnContent = window.Find("LearnSection/Viewport/Content");
            _learnRowTemplate = _learnContent != null
                ? _learnContent.Find("LearnRow_Template")?.GetComponent<SkillLearnEntryView>()
                : null;
            if (_learnRowTemplate != null)
            {
                _learnRowTemplate.gameObject.SetActive(false);
            }

            _poolEmptyHint = window.Find("PoolScroll/PoolEmptyHint")?.GetComponent<TextMeshProUGUI>();
        }

        void RefreshLearnList(int tabIndex)
        {
            if (_learnContent == null || _learnRowTemplate == null)
            {
                return;
            }

            ClearLearnRows();
            if (ActiveSchoolId <= 0 || CfgMgr.Cfgs == null)
            {
                return;
            }

            foreach (var entry in CfgMgr.Cfgs.TbSkillLearnEntry.DataList)
            {
                if (entry == null || string.IsNullOrEmpty(entry.SkillId))
                {
                    continue;
                }

                if (entry.SchoolId != ActiveSchoolId)
                {
                    continue;
                }

                var rowGo = Instantiate(_learnRowTemplate.gameObject, _learnContent);
                rowGo.SetActive(true);
                var view = rowGo.GetComponent<SkillLearnEntryView>();
                view.Bind(entry, OnLearnEntryClicked);
                _learnRowObjects.Add(rowGo);
                _learnRows.Add(view);
            }
        }

        void ClearLearnRows()
        {
            for (int i = 0; i < _learnRowObjects.Count; i++)
            {
                if (_learnRowObjects[i] != null)
                {
                    Destroy(_learnRowObjects[i]);
                }
            }

            _learnRowObjects.Clear();
            _learnRows.Clear();
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

        void WireSkillSlotDropZones()
        {
            if (_activeSlotViews != null)
            {
                foreach (var view in _activeSlotViews)
                {
                    if (view == null)
                    {
                        continue;
                    }

                    var drop = view.GetComponent<SkillSlotDropZone>();
                    if (drop == null)
                    {
                        drop = view.gameObject.AddComponent<SkillSlotDropZone>();
                    }

                    drop.view = view;
                    drop.mode = view.SlotIndex >= 3 && view.SlotIndex <= 7
                        ? SkillSlotDropMode.CustomNormal
                        : SkillSlotDropMode.Fixed;

                    var img = view.GetComponent<Image>();
                    if (img == null)
                    {
                        img = view.GetComponentInChildren<Image>(true);
                    }

                    if (img != null && drop.mode == SkillSlotDropMode.CustomNormal)
                    {
                        img.raycastTarget = true;
                    }
                }
            }

            if (_passiveSlotViews == null)
            {
                return;
            }

            foreach (var view in _passiveSlotViews)
            {
                if (view == null)
                {
                    continue;
                }

                var drop = view.GetComponent<SkillSlotDropZone>();
                if (drop == null)
                {
                    drop = view.gameObject.AddComponent<SkillSlotDropZone>();
                }

                drop.view = view;
                drop.mode = SkillSlotDropMode.CustomNormal;

                var img = view.GetComponent<Image>();
                if (img == null)
                {
                    img = view.GetComponentInChildren<Image>(true);
                }

                if (img != null)
                {
                    img.raycastTarget = true;
                }
            }
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

        void WireDragLayerFromHierarchy()
        {
            var dragLayer = transform.Find("DragLayer");
            if (dragLayer == null) return;

            var ghostTf = dragLayer.Find("Ghost");
            if (ghostTf == null) return;

            var ghostImg = ghostTf.GetComponent<Image>();
            var ghostLbl = ghostTf.GetComponentInChildren<TextMeshProUGUI>();
            SkillDragSession.Configure(ghostTf.gameObject, ghostImg, ghostLbl, GetComponentInParent<Canvas>());
        }

        void InitTabsFromTable()
        {
            BindBuiltReferencesIfNeeded();

            var schools = SkillLearnCatalog.GetSchoolsSorted();
            _tabCount = 0;
            if (_tabButtons == null) return;

            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var b = _tabButtons[i];
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
                    lbl.text = nm;
                var captured = i;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => SelectSchool(captured));
            }
        }

        void SelectSchool(int tabIndex)
        {
            var schools = SkillLearnCatalog.GetSchoolsSorted();
            if (tabIndex < 0 || tabIndex >= schools.Count) return;
            _activeTabIndex = tabIndex;
            ActiveSchoolId = schools[tabIndex].SchoolId;

            if (_tabButtons != null)
            {
                for (var i = 0; i < _tabButtons.Length; i++)
                {
                    if (!_tabButtons[i].gameObject.activeSelf) continue;
                    var img = _tabButtons[i].GetComponent<Image>();
                    if (img == null) continue;
                    img.color = i == tabIndex
                        ? new Color(0.38f, 0.55f, 0.72f, 1f)
                        : new Color(0.28f, 0.3f, 0.35f, 1f);
                }
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;

            var entries = SkillLearnCatalog.GetLearnEntriesBySchool(ActiveSchoolId);

            if (_poolCells != null)
            {
                int visibleCount = 0;
                for (var i = 0; i < _poolCells.Length; i++)
                {
                    if (i >= entries.Count)
                    {
                        _poolCells[i].SetVisible(false);
                        continue;
                    }

                    var entry = entries[i];
                    if (sys != null && (sys.IsGrantedPassive(entry.SkillId) || sys.IsGrantedActive(entry.SkillId)))
                    {
                        _poolCells[i].SetVisible(false);
                        continue;
                    }

                    visibleCount++;
                    _poolCells[i].SetVisible(true);
                    bool learned = sys != null && sys.IsSkillLearned(entry.SkillId);
                    _poolCells[i].Bind(entry, learned, _poolSkillDropBehavior);
                }

                if (_poolEmptyHint != null)
                {
                    _poolEmptyHint.gameObject.SetActive(visibleCount <= 0);
                }
            }

            RefreshLearnList(tabIndex);
            RefreshSlotDisplays(sys);
        }

        void RefreshSlotDisplays(PlayerSkillSystem sys)
        {
            if (sys == null)
            {
                return;
            }

            if (_activeSlotViews != null)
            {
                foreach (var slot in _activeSlotViews)
                {
                    slot.RefreshDisplay(sys);
                }
            }

            if (_passiveSlotViews != null)
            {
                foreach (var slot in _passiveSlotViews)
                {
                    slot.RefreshDisplay(sys);
                }
            }
        }

        public void RefreshAll()
        {
            BindBuiltReferencesIfNeeded();
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;
            RefreshSlotDisplays(sys);

            if (ActiveSchoolId > 0)
            {
                var idx = FindTabIndexForSchool(ActiveSchoolId);
                if (idx >= 0)
                    SelectSchool(idx);
            }
            else if (_tabCount > 0)
                SelectSchool(0);

            TryRefreshHudBar();
        }

        int FindTabIndexForSchool(int schoolId)
        {
            var schools = SkillLearnCatalog.GetSchoolsSorted();
            for (var i = 0; i < schools.Count; i++)
            {
                if (schools[i].SchoolId == schoolId)
                    return i;
            }
            return -1;
        }

        static void TryRefreshHudBar()
        {
            var hud = OverworldHUDPanel.Instance;
            if (hud != null && hud.SkilBar != null)
                hud.SkilBar.Refresh();
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
