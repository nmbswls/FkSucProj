using System.Collections.Generic;
using My.Config;
using My.Player;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public class SkillLoadoutPanel : PanelBase, IInputConsumer
    {
        public const string Pid = "SkillLoadoutPanel";

        public static SkillLoadoutPanel Current { get; private set; }

        public static SkillLoadoutPanel Open()
        {
            PlayerProgressionHubPanel.OpenSkills();
            return SkillLoadoutPanel.Current;
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
            }
            else
            {
                UIManager.Instance.HidePanel(Pid);
            }
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
        SkillSlotView[] _slotViews;
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
                _slotViews = bar.GetComponentsInChildren<SkillSlotView>(true);

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
                    if (sys.IsLearned(id))
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

            if (sys != null && _slotViews != null)
            {
                foreach (var slot in _slotViews)
                    slot.RefreshDisplay(sys);
            }
        }

        public void RefreshAll()
        {
            BindBuiltReferencesIfNeeded();
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var sys = mgr?.SkillSystem;
            if (sys != null && _slotViews != null)
            {
                foreach (var slot in _slotViews)
                    slot.RefreshDisplay(sys);
            }

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
