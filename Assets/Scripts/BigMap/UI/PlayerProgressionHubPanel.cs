using My;
using My.Map.Logic;
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

        public ProgressionHubTab CurrentTab { get; private set; }

        public string HubPanelId => Pid;

        RectTransform _contentRect;
        RectTransform _skillHost;
        RectTransform _talentHost;
        RectTransform _gearHost;
        RectTransform _worldHost;
        RectTransform _runeHost;
        Button _tabSkill;
        Button _tabTalent;
        Button _tabGear;
        Button _tabWorld;
        Button _tabRune;
        SkillLoadoutPanel _skill;
        TalentTreePanel _talent;
        PlayerGearEquipPanel _gear;
        GlobalWorldPanel _world;
        RunePanel _rune;

        static readonly Color TabSelectedColor = new Color(0.38f, 0.55f, 0.72f, 1f);
        static readonly Color TabNormalColor = new Color(0.22f, 0.24f, 0.3f, 1f);

        // 与 UIManager 路径对齐：Hub 重复 ShowPanel 时传入的 data；子页按需 Setup
        object _lastHubSetupData;

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
            if (!BodyPartUiRules.HasAnySelectablePart(ResolveGameLogic()))
            {
                Debug.LogWarning("[PlayerProgressionHubPanel] BodyPart page unavailable: no selectable part.");
                Open(ProgressionHubTab.Skills);
                return;
            }

            Open(ProgressionHubTab.BodyPart);
        }

        public static void OpenWorld() => Open(ProgressionHubTab.World);

        public static void OpenRunes() => Open(ProgressionHubTab.Runes);

        public SkillLoadoutPanel SkillPage => _skill;

        public TalentTreePanel TalentPage => _talent;

        public PlayerGearEquipPanel GearPage => _gear;

        public GlobalWorldPanel WorldPage => _world;

        public RunePanel RunePage => _rune;

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
            if (data is ProgressionHubOpenArgs args)
            {
                SelectTab(args.InitialTab);
            }
            else
            {
                SelectTab(ProgressionHubTab.Skills);
            }
        }

        public override void Show()
        {
            base.Show();
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
            base.Hide();
        }

        public override void Teardown()
        {
            _skill?.Teardown();
            _talent?.Teardown();
            _gear?.Teardown();
            _world?.Teardown();
            _rune?.Teardown();
            _skill = null;
            _talent = null;
            _gear = null;
            _world = null;
            _rune = null;
            base.Teardown();
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
            _tabSkill = root.Find("Window/HubTabs/TabSkill")?.GetComponent<Button>();
            _tabTalent = root.Find("Window/HubTabs/TabTalent")?.GetComponent<Button>();
            _tabGear = root.Find("Window/HubTabs/TabGear")?.GetComponent<Button>();
            _tabWorld = root.Find("Window/HubTabs/TabWorld")?.GetComponent<Button>();
            _tabRune = root.Find("Window/HubTabs/TabRune")?.GetComponent<Button>();

            if (_tabSkill != null)
            {
                _tabSkill.onClick.RemoveAllListeners();
                _tabSkill.onClick.AddListener(() => SelectTab(ProgressionHubTab.Skills));
            }

            if (_tabTalent != null)
            {
                _tabTalent.onClick.RemoveAllListeners();
                _tabTalent.onClick.AddListener(() => SelectTab(ProgressionHubTab.Talents));
            }

            if (_tabGear != null)
            {
                _tabGear.onClick.RemoveAllListeners();
                _tabGear.onClick.AddListener(() => SelectTab(ProgressionHubTab.BodyPart));
            }

            if (_tabWorld != null)
            {
                _tabWorld.onClick.RemoveAllListeners();
                _tabWorld.onClick.AddListener(() => SelectTab(ProgressionHubTab.World));
            }

            if (_tabRune != null)
            {
                _tabRune.onClick.RemoveAllListeners();
                _tabRune.onClick.AddListener(() => SelectTab(ProgressionHubTab.Runes));
            }

            WireShellChrome(root);
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

        // Hub 托管子页：Resources 实例化不会自动走 UIManager；此处统一 Instantiate → 绑 Host → Hide。
        // 子页 Setup 由 ApplySetupForTab 在选中 Tab 时调用，与 UIManager 每次 Show 前再 Setup 的习惯对齐。
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
            if (tab == ProgressionHubTab.BodyPart && !BodyPartUiRules.HasAnySelectablePart(ResolveGameLogic()))
            {
                Debug.LogWarning("[PlayerProgressionHubPanel] BodyPart tab unavailable: no selectable part.");
                tab = ProgressionHubTab.Skills;
            }

            CurrentTab = tab;
            EnsurePages();
            _skill?.Hide();
            _talent?.Hide();
            _gear?.Hide();
            _world?.Hide();
            _rune?.Hide();

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

            ApplySetupForTab(CurrentTab, _lastHubSetupData);
            RefreshHubTabVisuals();
            RefreshActivePage();
        }

        void RefreshHubTabVisuals()
        {
            bool bodyPartAvailable = BodyPartUiRules.HasAnySelectablePart(ResolveGameLogic());
            ApplyTabVisual(_tabSkill, CurrentTab == ProgressionHubTab.Skills);
            ApplyTabVisual(_tabTalent, CurrentTab == ProgressionHubTab.Talents);
            ApplyTabVisual(_tabGear, CurrentTab == ProgressionHubTab.BodyPart);
            if (_tabGear != null)
            {
                _tabGear.interactable = bodyPartAvailable;
            }
            ApplyTabVisual(_tabWorld, CurrentTab == ProgressionHubTab.World);
            ApplyTabVisual(_tabRune, CurrentTab == ProgressionHubTab.Runes);
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
