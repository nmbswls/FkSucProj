using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Map.Logic;
using My.Player;
using My.Quest;
using My.UI.BodyPart;
using My.UI.Rune;
using My.UI.SkillLoadout;
using My.UI.Talent;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public enum ProgressionHubTab
    {
        Skills = 0,
        Talents = 1,
        BodyPart = 2,
        World = 3,
        Runes = 4,
        JingYuanTune = 5,
    }

    public sealed class ProgressionHubOpenArgs
    {
        public ProgressionHubTab InitialTab { get; set; } = ProgressionHubTab.Skills;
    }

    public interface IPlayerProgressionHubHost
    {
        string HubPanelId { get; }

        void CloseHub();

        Canvas ResolveHubCanvas();
    }

    public class PlayerProgressionHubPanel : PanelBase, IInputConsumer, IPlayerProgressionHubHost
    {
        public const string Pid = "PlayerProgressionHub";

        const string PathSkill = "UI/Prefabs/PlayerProgressionHubPanelSub/SkillLoadoutPanel";
        const string PathTalent = "UI/Prefabs/PlayerProgressionHubPanelSub/TalentTreePanel";
        const string PathGear = "UI/Prefabs/PlayerProgressionHubPanelSub/PlayerGearEquipPanel";
        const string PathWorld = "UI/Prefabs/PlayerProgressionHubPanelSub/GlobalWorldPanel";
        const string PathRune = "UI/Prefabs/PlayerProgressionHubPanelSub/RuneLoadoutPanel";
        const string PathJingYuanTune = "UI/Prefabs/PlayerProgressionHubPanelSub/JingYuanTunePanel";

        public ProgressionHubTab CurrentTab { get; private set; }

        public string HubPanelId => Pid;

        RectTransform _contentRect;
        RectTransform _skillHost;
        RectTransform _talentHost;
        RectTransform _gearHost;
        RectTransform _worldHost;
        RectTransform _runeHost;
        RectTransform _jingYuanHost;
        Transform _hubTabsRoot;
        Button _tabSkill;
        Button _tabTalent;
        Button _tabGear;
        Button _tabWorld;
        Button _tabRune;
        Button _tabJingYuan;
        readonly List<Button> _orderedTabButtons = new List<Button>();
        ProgressionHubTabBar _tabBar;
        SkillLoadoutPanel _skill;
        TalentTreePanel _talent;
        PlayerGearEquipPanel _gear;
        GlobalWorldPanel _world;
        RuneLoadoutPanel _rune;
        JingYuanTunePanel _jingYuan;

        static readonly Color TabSelectedColor = new Color(0.38f, 0.55f, 0.72f, 1f);
        static readonly Color TabNormalColor = new Color(0.22f, 0.24f, 0.3f, 1f);

        object _lastHubSetupData;
        bool _funcOpenSubscribed;

        public Button BtnClose;
        public Button BtnBlocker;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            layer = UILayer.Popup;
        }

        public static PlayerProgressionHubPanel Open(ProgressionHubTab tab = ProgressionHubTab.Skills)
        {
            return UIManager.Instance.ShowPanel(Pid, new ProgressionHubOpenArgs { InitialTab = tab }) as PlayerProgressionHubPanel;
        }

        public static void OpenTab(ProgressionHubTab tab) => Open(tab);

        public static void OpenSkills() => Open(ProgressionHubTab.Skills);

        public static void OpenTalents() => Open(ProgressionHubTab.Talents);

        public static void OpenBodyPart()
        {
            if (!ProgressionHubTabRules.IsTabOpen(ProgressionHubTab.BodyPart, ResolveGameLogic()))
            {
                Debug.LogWarning("[PlayerProgressionHubPanel] BodyPart page unavailable.");
                Open(ProgressionHubTab.Skills);
                return;
            }

            Open(ProgressionHubTab.BodyPart);
        }

        public static void OpenWorld() => Open(ProgressionHubTab.World);

        public static void OpenRunes() => Open(ProgressionHubTab.Runes);

        public static void OpenJingYuanTune() => Open(ProgressionHubTab.JingYuanTune);

        public SkillLoadoutPanel SkillPage => _skill;

        public TalentTreePanel TalentPage => _talent;

        public PlayerGearEquipPanel GearPage => _gear;

        public GlobalWorldPanel WorldPage => _world;

        public RuneLoadoutPanel RunePage => _rune;

        public JingYuanTunePanel JingYuanPage => _jingYuan;

        public static void ToggleTab(ProgressionHubTab tab)
        {
            if (UIManager.Instance.IsPanelVisible(Pid))
            {
                var hub = UIManager.Instance.GetShowingPanel(Pid) as PlayerProgressionHubPanel;
                if (hub != null && hub.CurrentTab == tab)
                {
                    UIManager.Instance.HidePanel(Pid);
                }
                else
                {
                    hub?.SelectTab(tab);
                }

                return;
            }

            Open(tab);
        }

        public static void ToggleTalents() => ToggleTab(ProgressionHubTab.Talents);

        public static void ToggleRunes() => ToggleTab(ProgressionHubTab.Runes);

        public void CloseHub() => UIManager.Instance.HidePanel(Pid);

        public Canvas ResolveHubCanvas() => GetComponentInParent<Canvas>();

        public override void Setup(object data = null)
        {
            base.Setup(data);
            _lastHubSetupData = data;
            BindShellRefs();
            var logic = ResolveGameLogic();
            var initialTab = data is ProgressionHubOpenArgs args
                ? args.InitialTab
                : ProgressionHubTab.Skills;
            SelectTab(ProgressionHubTabRules.ResolveInitialTab(initialTab, logic));
        }

        public override void Show()
        {
            base.Show();
            EnsureFuncOpenSubscription();
            EnsurePages();
            RefreshHubTabVisuals();
            RefreshActivePage();
        }

        public override void Hide()
        {
            _skill?.Hide();
            _talent?.Hide();
            _gear?.Hide();
            _world?.Hide();
            _rune?.Hide();
            _jingYuan?.Hide();
            base.Hide();
        }

        public override void Teardown()
        {
            UnsubscribeFuncOpen();
            _skill?.Teardown();
            _talent?.Teardown();
            _gear?.Teardown();
            _world?.Teardown();
            _rune?.Teardown();
            _jingYuan?.Teardown();
            _skill = null;
            _talent = null;
            _gear = null;
            _world = null;
            _rune = null;
            _jingYuan = null;
            base.Teardown();
        }

        void EnsureFuncOpenSubscription()
        {
            if (_funcOpenSubscribed)
            {
                return;
            }

            PlayerEventBus.Subscribe<PlayerFuncUnlockEvent>(HandlePlayerFuncUnlock);
            _funcOpenSubscribed = true;
        }

        void UnsubscribeFuncOpen()
        {
            if (!_funcOpenSubscribed)
            {
                return;
            }

            PlayerEventBus.Unsubscribe<PlayerFuncUnlockEvent>(HandlePlayerFuncUnlock);
            _funcOpenSubscribed = false;
        }

        void HandlePlayerFuncUnlock(PlayerFuncUnlockEvent e)
        {
            if (!IsVisible)
            {
                return;
            }

            RefreshHubTabVisuals();
            if (!ProgressionHubTabRules.IsTabOpen(CurrentTab, ResolveGameLogic()))
            {
                SelectTab(ProgressionHubTabRules.ResolveInitialTab(CurrentTab, ResolveGameLogic()));
            }
        }

        void BindShellRefs()
        {
            var root = transform.Find("BuiltRoot");
            if (root == null)
            {
                return;
            }

            _contentRect = root.Find("Window/ContentHost") as RectTransform;
            _skillHost = root.Find("Window/ContentHost/SkillHost") as RectTransform;
            _talentHost = root.Find("Window/ContentHost/TalentHost") as RectTransform;
            _gearHost = root.Find("Window/ContentHost/GearHost") as RectTransform;
            _worldHost = root.Find("Window/ContentHost/WorldHost") as RectTransform;
            _runeHost = root.Find("Window/ContentHost/RuneHost") as RectTransform;
            _jingYuanHost = root.Find("Window/ContentHost/JingYuanHost") as RectTransform;
            _hubTabsRoot = root.Find("Window/HubTabs");
            _tabSkill = _hubTabsRoot?.Find("TabSkill")?.GetComponent<Button>();
            if (_tabSkill == null)
            {
                _tabSkill = _hubTabsRoot?.Find("TabScrollArea/Viewport/Content/TabSkill")?.GetComponent<Button>();
            }

            _tabTalent = FindTabButton("TabTalent");
            _tabGear = FindTabButton("TabGear");
            _tabWorld = FindTabButton("TabWorld");
            _tabRune = FindTabButton("TabRune");
            _tabJingYuan = FindTabButton("TabJingYuan");

            WireTabButton(_tabSkill, ProgressionHubTab.Skills);
            WireTabButton(_tabTalent, ProgressionHubTab.Talents);
            WireTabButton(_tabGear, ProgressionHubTab.BodyPart);
            WireTabButton(_tabWorld, ProgressionHubTab.World);
            WireTabButton(_tabRune, ProgressionHubTab.Runes);
            WireTabButton(_tabJingYuan, ProgressionHubTab.JingYuanTune);

            EnsureTabBar();
            WireShellChrome(root);
        }

        Button FindTabButton(string tabName)
        {
            if (_hubTabsRoot == null)
            {
                return null;
            }

            var direct = _hubTabsRoot.Find(tabName);
            if (direct != null)
            {
                return direct.GetComponent<Button>();
            }

            return _hubTabsRoot.Find($"TabScrollArea/Viewport/Content/{tabName}")?.GetComponent<Button>();
        }

        void WireTabButton(Button button, ProgressionHubTab tab)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectTab(tab));
        }

        void EnsureTabBar()
        {
            if (_hubTabsRoot == null)
            {
                return;
            }

            if (_tabBar == null)
            {
                _tabBar = _hubTabsRoot.GetComponent<ProgressionHubTabBar>();
                if (_tabBar == null)
                {
                    _tabBar = _hubTabsRoot.gameObject.AddComponent<ProgressionHubTabBar>();
                }
            }

            BuildOrderedTabButtons();
            _tabBar.BuildIfNeeded(_hubTabsRoot, _orderedTabButtons);
            _tabBar.NotifyTabSelected(GetTabIndex(CurrentTab));
        }

        void BuildOrderedTabButtons()
        {
            _orderedTabButtons.Clear();
            var boundTabs = new Dictionary<ProgressionHubTab, Button>
            {
                { ProgressionHubTab.Skills, _tabSkill },
                { ProgressionHubTab.Talents, _tabTalent },
                { ProgressionHubTab.BodyPart, _tabGear },
                { ProgressionHubTab.World, _tabWorld },
                { ProgressionHubTab.Runes, _tabRune },
                { ProgressionHubTab.JingYuanTune, _tabJingYuan },
            };

            var defs = ProgressionHubTabRules.GetSortedTabDefs();
            if (defs.Count == 0)
            {
                _orderedTabButtons.Add(_tabSkill);
                _orderedTabButtons.Add(_tabTalent);
                _orderedTabButtons.Add(_tabGear);
                _orderedTabButtons.Add(_tabWorld);
                _orderedTabButtons.Add(_tabRune);
                _orderedTabButtons.Add(_tabJingYuan);
                return;
            }

            for (int i = 0; i < defs.Count; i++)
            {
                var tab = ProgressionHubTabRules.FromCfgTab(defs[i].TabId);
                if (boundTabs.TryGetValue(tab, out var button) && button != null && button.gameObject.activeInHierarchy)
                {
                    _orderedTabButtons.Add(button);
                }
            }
        }

        void WireShellChrome(Transform root)
        {
            if (BtnBlocker != null)
            {
                BtnBlocker.onClick.RemoveAllListeners();
                BtnBlocker.onClick.AddListener(CloseHub);
            }

            if (BtnClose != null)
            {
                BtnClose.onClick.RemoveAllListeners();
                BtnClose.onClick.AddListener(CloseHub);
            }
        }

        void EnsureEmbeddedPage<T>(
            ref T cache,
            string resourcePath,
            RectTransform host,
            System.Action<T, IPlayerProgressionHubHost> bindHost)
            where T : PanelBase, IPlayerProgressionHubPage
        {
            if (cache != null || host == null)
            {
                return;
            }

            var pf = Resources.Load<GameObject>(resourcePath);
            if (pf == null)
            {
                return;
            }

            var go = Instantiate(pf, host, false);
            StretchToParent(go);
            cache = go.GetComponent<T>();
            if (cache == null)
            {
                return;
            }

            bindHost?.Invoke(cache, this);
            cache.Hide();
        }

        void EnsurePages()
        {
            BindShellRefs();
            if (_skillHost == null)
            {
                return;
            }

            EnsureEmbeddedPage(ref _skill, PathSkill, _skillHost, (p, h) => p.SetProgressionHubHost(h));
            EnsureEmbeddedPage(ref _talent, PathTalent, _talentHost, (p, h) => p.SetProgressionHubHost(h));
            EnsureEmbeddedPage(ref _gear, PathGear, _gearHost, (p, h) => p.SetProgressionHubHost(h));
            if (_worldHost != null)
            {
                EnsureEmbeddedPage(ref _world, PathWorld, _worldHost, (p, h) => p.SetProgressionHubHost(h));
            }

            if (_runeHost != null)
            {
                EnsureEmbeddedPage(ref _rune, PathRune, _runeHost, (p, h) => p.SetProgressionHubHost(h));
            }

            if (_jingYuanHost != null)
            {
                EnsureEmbeddedPage(ref _jingYuan, PathJingYuanTune, _jingYuanHost, (p, h) => p.SetProgressionHubHost(h));
            }
        }

        void ApplySetupForTab(ProgressionHubTab tab, object data)
        {
            switch (tab)
            {
                case ProgressionHubTab.Skills:
                    _skill?.Setup(data);
                    break;
                case ProgressionHubTab.Talents:
                    _talent?.Setup(data);
                    break;
                case ProgressionHubTab.BodyPart:
                    _gear?.Setup(data);
                    break;
                case ProgressionHubTab.World:
                    _world?.Setup(data);
                    break;
                case ProgressionHubTab.Runes:
                    _rune?.Setup(data);
                    break;
                case ProgressionHubTab.JingYuanTune:
                    _jingYuan?.Setup(data);
                    break;
            }
        }

        static void StretchToParent(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        public void SelectTab(ProgressionHubTab tab)
        {
            var logic = ResolveGameLogic();
            if (!ProgressionHubTabRules.IsTabOpen(tab, logic))
            {
                Debug.LogWarning($"[PlayerProgressionHubPanel] Tab unavailable: {tab}");
                tab = ProgressionHubTabRules.ResolveInitialTab(tab, logic);
            }

            CurrentTab = tab;
            EnsurePages();
            _skill?.Hide();
            _talent?.Hide();
            _gear?.Hide();
            _world?.Hide();
            _rune?.Hide();
            _jingYuan?.Hide();

            if (_skillHost != null)
            {
                _skillHost.gameObject.SetActive(tab == ProgressionHubTab.Skills);
            }

            if (_talentHost != null)
            {
                _talentHost.gameObject.SetActive(tab == ProgressionHubTab.Talents);
            }

            if (_gearHost != null)
            {
                _gearHost.gameObject.SetActive(tab == ProgressionHubTab.BodyPart);
            }

            if (_worldHost != null)
            {
                _worldHost.gameObject.SetActive(tab == ProgressionHubTab.World);
            }

            if (_runeHost != null)
            {
                _runeHost.gameObject.SetActive(tab == ProgressionHubTab.Runes);
            }

            if (_jingYuanHost != null)
            {
                _jingYuanHost.gameObject.SetActive(tab == ProgressionHubTab.JingYuanTune);
            }

            ApplySetupForTab(CurrentTab, _lastHubSetupData);
            RefreshHubTabVisuals();
            RefreshActivePage();
        }

        void RefreshHubTabVisuals()
        {
            var logic = ResolveGameLogic();
            ApplyTabEntryVisual(_tabSkill, ProgressionHubTab.Skills, logic);
            ApplyTabEntryVisual(_tabTalent, ProgressionHubTab.Talents, logic);
            ApplyTabEntryVisual(_tabGear, ProgressionHubTab.BodyPart, logic);
            ApplyTabEntryVisual(_tabWorld, ProgressionHubTab.World, logic);
            ApplyTabEntryVisual(_tabRune, ProgressionHubTab.Runes, logic);
            ApplyTabEntryVisual(_tabJingYuan, ProgressionHubTab.JingYuanTune, logic);
            EnsureTabBar();
            _tabBar?.NotifyTabSelected(GetTabIndex(CurrentTab));
        }

        void ApplyTabEntryVisual(Button tab, ProgressionHubTab tabId, GameLogicManager logic)
        {
            if (tab == null)
            {
                return;
            }

            bool open = ProgressionHubTabRules.IsTabOpen(tabId, logic);
            var def = CfgMgr.Cfgs?.TbProgressionHubTab?.GetOrDefault(ProgressionHubTabRules.ToCfgTab(tabId));
            bool hideWhenLocked = def != null && def.FuncOpenType != EFuncOpenType.Invalid;
            tab.gameObject.SetActive(!hideWhenLocked || open);
            tab.interactable = open;
            ApplyTabVisual(tab, CurrentTab == tabId);
        }

        static void ApplyTabVisual(Button tab, bool selected)
        {
            if (tab == null)
            {
                return;
            }

            var img = tab.GetComponent<Image>();
            if (img != null)
            {
                img.color = selected ? TabSelectedColor : TabNormalColor;
            }
        }

        int GetTabIndex(ProgressionHubTab tab)
        {
            for (int i = 0; i < _orderedTabButtons.Count; i++)
            {
                var button = _orderedTabButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (TabFromButton(button) == tab)
                {
                    return i;
                }
            }

            return 0;
        }

        static ProgressionHubTab TabFromButton(Button button)
        {
            switch (button.name)
            {
                case "TabSkill":
                    return ProgressionHubTab.Skills;
                case "TabTalent":
                    return ProgressionHubTab.Talents;
                case "TabGear":
                    return ProgressionHubTab.BodyPart;
                case "TabWorld":
                    return ProgressionHubTab.World;
                case "TabRune":
                    return ProgressionHubTab.Runes;
                case "TabJingYuan":
                    return ProgressionHubTab.JingYuanTune;
                default:
                    return ProgressionHubTab.Skills;
            }
        }

        void RefreshActivePage()
        {
            if (!IsVisible)
            {
                return;
            }

            switch (CurrentTab)
            {
                case ProgressionHubTab.Skills:
                    _skill?.Show();
                    break;
                case ProgressionHubTab.Talents:
                    _talent?.Show();
                    break;
                case ProgressionHubTab.BodyPart:
                    _gear?.Show();
                    break;
                case ProgressionHubTab.World:
                    _world?.Show();
                    break;
                case ProgressionHubTab.Runes:
                    _rune?.Show();
                    break;
                case ProgressionHubTab.JingYuanTune:
                    _jingYuan?.Show();
                    break;
            }
        }

        IInputConsumer ActiveConsumer
        {
            get
            {
                switch (CurrentTab)
                {
                    case ProgressionHubTab.Skills:
                        return _skill;
                    case ProgressionHubTab.BodyPart:
                        return _gear;
                    default:
                        return null;
                }
            }
        }

        public bool CapturesNavigateAxisForWorld
        {
            get
            {
                var c = ActiveConsumer;
                return c != null && c.CapturesNavigateAxisForWorld;
            }
        }

        public override int FocusPriority => 24;

        public bool OnConfirm() => TryChild(c => c.OnConfirm());

        public bool OnCancel()
        {
            if (TryChild(c => c.OnCancel()))
            {
                return true;
            }

            CloseHub();
            return true;
        }

        public bool OnNavigate(Vector2 dir) => TryChild(c => c.OnNavigate(dir));

        public bool OnHotkey(string keyName) => TryChild(c => c.OnHotkey(keyName));

        public bool OnScroll(float deltaY) => TryChild(c => c.OnScroll(deltaY));

        public bool OnClick(int button, Vector2 mousePos) => TryChild(c => c.OnClick(button, mousePos));

        public bool OnHoldStart(string holdKey) => TryChild(c => c.OnHoldStart(holdKey));

        public bool OnHoldUpdate(string holdKey) => TryChild(c => c.OnHoldUpdate(holdKey));

        public bool OnHoldingEnd(string holdKey) => TryChild(c => c.OnHoldingEnd(holdKey));

        bool TryChild(System.Func<IInputConsumer, bool> fn)
        {
            var c = ActiveConsumer;
            if (c == null)
            {
                return false;
            }

            return fn(c);
        }

        static GameLogicManager ResolveGameLogic()
        {
            return MainGameManager.Instance?.gameLogicManager;
        }
    }
}
