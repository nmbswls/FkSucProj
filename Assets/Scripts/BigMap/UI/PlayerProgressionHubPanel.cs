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
        Gear = 2,
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

        public ProgressionHubTab CurrentTab { get; private set; }

        public string HubPanelId => Pid;

        RectTransform _contentRect;
        RectTransform _skillHost;
        RectTransform _talentHost;
        RectTransform _gearHost;
        Button _tabSkill;
        Button _tabTalent;
        Button _tabGear;
        SkillLoadoutPanel _skill;
        TalentTreePanel _talent;
        PlayerGearEquipPanel _gear;

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

        public static void OpenSkills() => Open(ProgressionHubTab.Skills);

        public static void OpenTalents() => Open(ProgressionHubTab.Talents);

        public static void OpenGear() => Open(ProgressionHubTab.Gear);

        // 旧 panelId 仍可能被外部 ShowPanel 调用时转发到统一 Hub
        public static string RemapLegacyCatalogId(string panelId, ref object data)
        {
            if (panelId == SkillLoadoutPanel.Pid)
            {
                data = new ProgressionHubOpenArgs { InitialTab = ProgressionHubTab.Skills };
                return Pid;
            }

            if (panelId == TalentTreePanel.Pid)
            {
                data = new ProgressionHubOpenArgs { InitialTab = ProgressionHubTab.Talents };
                return Pid;
            }

            if (panelId == PlayerGearEquipPanel.Pid)
            {
                data = new ProgressionHubOpenArgs { InitialTab = ProgressionHubTab.Gear };
                return Pid;
            }

            return panelId;
        }

        public static void ToggleTalents()
        {
            if (UIManager.Instance.IsPanelVisible(Pid))
            {
                var hub = UIManager.Instance.GetShowingPanel(Pid) as PlayerProgressionHubPanel;
                if (hub != null && hub.CurrentTab == ProgressionHubTab.Talents)
                {
                    UIManager.Instance.HidePanel(Pid);
                }
                else
                {
                    hub?.SelectTab(ProgressionHubTab.Talents);
                }

                return;
            }

            Open(ProgressionHubTab.Talents);
        }

        public void CloseHub() => UIManager.Instance.HidePanel(Pid);

        public Canvas ResolveHubCanvas() => GetComponentInParent<Canvas>();

        public override void Setup(object data = null)
        {
            base.Setup(data);
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
            RefreshActivePage();
        }

        public override void Hide()
        {
            _skill?.Hide();
            _talent?.Hide();
            _gear?.Hide();
            base.Hide();
        }

        public override void Teardown()
        {
            _skill?.Teardown();
            _talent?.Teardown();
            _gear?.Teardown();
            _skill = null;
            _talent = null;
            _gear = null;
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
            _tabSkill = root.Find("Window/HubTabs/TabSkill")?.GetComponent<Button>();
            _tabTalent = root.Find("Window/HubTabs/TabTalent")?.GetComponent<Button>();
            _tabGear = root.Find("Window/HubTabs/TabGear")?.GetComponent<Button>();
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
                _tabGear.onClick.AddListener(() => SelectTab(ProgressionHubTab.Gear));
            }

            WireShellChrome(root);
        }

        void WireShellChrome(Transform root)
        {
            var blockerBtn = root.Find("BlockerButton")?.GetComponent<Button>();
            if (blockerBtn != null)
            {
                blockerBtn.onClick.RemoveAllListeners();
                blockerBtn.onClick.AddListener(CloseHub);
            }

            var closeBtn = root.Find("Window/HubHeader/HubClose")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(CloseHub);
            }
        }

        void EnsurePages()
        {
            BindShellRefs();
            if (_skillHost == null)
            {
                return;
            }

            if (_skill == null)
            {
                var pf = Resources.Load<GameObject>(PathSkill);
                if (pf != null)
                {
                    var go = Instantiate(pf, _skillHost, false);
                    StretchToParent(go);
                    _skill = go.GetComponent<SkillLoadoutPanel>();
                    if (_skill != null)
                    {
                        _skill.SetProgressionHubHost(this);
                        _skill.Setup(null);
                        _skill.Hide();
                    }
                }
            }

            if (_talent == null)
            {
                var pf = Resources.Load<GameObject>(PathTalent);
                if (pf != null)
                {
                    var go = Instantiate(pf, _talentHost, false);
                    StretchToParent(go);
                    _talent = go.GetComponent<TalentTreePanel>();
                    if (_talent != null)
                    {
                        _talent.SetProgressionHubHost(this);
                        _talent.Setup(null);
                        _talent.Hide();
                    }
                }
            }

            if (_gear == null)
            {
                var pf = Resources.Load<GameObject>(PathGear);
                if (pf != null)
                {
                    var go = Instantiate(pf, _gearHost, false);
                    StretchToParent(go);
                    _gear = go.GetComponent<PlayerGearEquipPanel>();
                    if (_gear != null)
                    {
                        _gear.SetProgressionHubHost(this);
                        _gear.Setup(null);
                        _gear.Hide();
                    }
                }
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
            CurrentTab = tab;
            EnsurePages();
            _skill?.Hide();
            _talent?.Hide();
            _gear?.Hide();

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
                _gearHost.gameObject.SetActive(tab == ProgressionHubTab.Gear);
            }

            RefreshActivePage();
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
                case ProgressionHubTab.Gear:
                    _gear?.Show();
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
                    case ProgressionHubTab.Gear:
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
    }
}
