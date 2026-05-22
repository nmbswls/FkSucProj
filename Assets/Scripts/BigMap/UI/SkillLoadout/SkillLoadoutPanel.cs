using System.Collections.Generic;
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

        public string ActiveSchoolId { get; private set; }

        Transform _builtRoot;
        SkillPoolEntryView[] _poolCells;
        ISkillDropBehavior _poolSkillDropBehavior = new SchoolFilteredNormalSlotDropBehavior();

        Button[] _tabButtons;
        SkillSlotView[] _activeSlotViews;
        SkillSlotView[] _passiveSlotViews;
        int _tabCount;

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

            EnsurePassiveBarUi(root);
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

        void EnsurePassiveBarUi(Transform root)
        {
            var window = root.Find("Window");
            if (window == null)
            {
                return;
            }

            var passiveRow = window.Find("PassiveBarRow");
            if (passiveRow == null)
            {
                var barRow = window.Find("BarRow");
                var template = barRow != null ? barRow.Find("Slot_3") : null;
                if (template == null)
                {
                    return;
                }

                int insertAt = barRow.GetSiblingIndex() + 1;

                var labelGo = new GameObject("PassiveBarLabel", typeof(RectTransform));
                labelGo.transform.SetParent(window, false);
                labelGo.transform.SetSiblingIndex(insertAt);
                var labelLe = labelGo.AddComponent<LayoutElement>();
                labelLe.preferredHeight = 24;
                var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
                labelTmp.text = "被动技能（右键槽位卸下）";
                labelTmp.fontSize = 18;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

                var rowGo = new GameObject("PassiveBarRow", typeof(RectTransform));
                rowGo.transform.SetParent(window, false);
                rowGo.transform.SetSiblingIndex(insertAt + 1);
                passiveRow = rowGo.transform;
                var rowLe = rowGo.AddComponent<LayoutElement>();
                rowLe.preferredHeight = 92;
                var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8;
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = false;
                rowLayout.childControlHeight = false;

                for (int i = 0; i < PlayerSkillSystem.PassiveSlotCount; i++)
                {
                    var clone = Instantiate(template.gameObject, passiveRow);
                    clone.name = "PassiveSlot_" + i;
                    var view = clone.GetComponent<SkillSlotView>();
                    if (view != null)
                    {
                        view.slotKind = SkillLoadoutSlotKind.Passive;
                        view.SlotIndex = i;
                    }
                }
            }

            if (passiveRow != null)
            {
                _passiveSlotViews = passiveRow.GetComponentsInChildren<SkillSlotView>(true);
                foreach (var v in _passiveSlotViews)
                {
                    v.slotKind = SkillLoadoutSlotKind.Passive;
                }
            }
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

            var schools = SkillSchoolTable.Instance.AllSchools;
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
            var schools = SkillSchoolTable.Instance.AllSchools;
            if (tabIndex < 0 || tabIndex >= schools.Count) return;
            ActiveSchoolId = schools[tabIndex].Id;

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

            SkillSchoolTable.Instance.TryGetSchool(ActiveSchoolId, out var sch);
            var list = new List<string>();
            if (sch != null && sch.SkillIds != null && sys != null)
            {
                foreach (var id in sch.SkillIds)
                {
                    if (sys.IsSkillLearned(id) && !sys.IsGrantedPassive(id) && !sys.IsGrantedActive(id))
                        list.Add(id);
                }
            }

            if (_poolCells != null)
            {
                for (var i = 0; i < _poolCells.Length; i++)
                {
                    if (i >= list.Count)
                    {
                        _poolCells[i].SetVisible(false);
                        continue;
                    }

                    _poolCells[i].SetVisible(true);
                    _poolCells[i].Bind(list[i], _poolSkillDropBehavior);
                }
            }

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

            if (!string.IsNullOrEmpty(ActiveSchoolId))
            {
                var idx = FindTabIndexForSchool(ActiveSchoolId);
                if (idx >= 0)
                    SelectSchool(idx);
            }
            else if (_tabCount > 0)
                SelectSchool(0);

            TryRefreshHudBar();
        }

        int FindTabIndexForSchool(string id)
        {
            var schools = SkillSchoolTable.Instance.AllSchools;
            for (var i = 0; i < schools.Count; i++)
            {
                if (schools[i].Id == id)
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
