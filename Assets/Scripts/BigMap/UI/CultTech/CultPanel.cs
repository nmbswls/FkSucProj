using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    public sealed class CultPanel : PanelBase
    {
        public const string Pid = "CultPanel";
        public static CultPanel Open() => UIManager.Instance.ShowPanel(Pid) as CultPanel;

        [SerializeField] Button closeButton;
        [SerializeField] CultOverviewView overviewView;
        [SerializeField] CultTechTreeView doctrineView;
        [SerializeField] AncientSeatTreeView seatView;
        Button _overviewTab;
        Button _doctrineTab;
        Button _seatTab;
        int _activeTab;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId)) panelId = Pid;
            overviewView ??= transform.Find("OverviewRoot")?.GetComponent<CultOverviewView>();
            doctrineView ??= GetComponent<CultTechTreeView>();
            seatView ??= GetComponent<AncientSeatTreeView>();
            EnsureTabs();
            EnsureCloseButton();
            SelectTab(_activeTab);
        }

        void EnsureTabs()
        {
            var root = transform.Find("CultTabs");
            if (root == null)
            {
                Debug.LogError("CultPanel requires CultTabs in its prefab.");
                return;
            }
            _overviewTab = root.Find("OverviewTab")?.GetComponent<Button>();
            _doctrineTab = root.Find("DoctrineTab")?.GetComponent<Button>();
            _seatTab = root.Find("AncientSeatTab")?.GetComponent<Button>();
            if (_overviewTab == null || _doctrineTab == null || _seatTab == null)
                Debug.LogError("CultPanel requires OverviewTab, DoctrineTab and AncientSeatTab in its prefab.");
            BindTab(_overviewTab, 0);
            BindTab(_doctrineTab, 1);
            BindTab(_seatTab, 2);
        }

        void BindTab(Button tab, int index)
        {
            if (tab == null) return;
            tab.onClick.RemoveAllListeners();
            tab.onClick.AddListener(() => SelectTab(index));
        }

        void EnsureCloseButton()
        {
            closeButton ??= transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton == null) return;
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            var cult = ResolveCult();
            overviewView?.Bind(cult);
            doctrineView?.Bind(cult);
            seatView?.Bind(cult);
            SelectTab(_activeTab);
        }

        public override void Show()
        {
            base.Show();
            overviewView?.Refresh();
            doctrineView?.Refresh();
            seatView?.Refresh();
            SelectTab(_activeTab);
        }

        void SelectTab(int tab)
        {
            _activeTab = Mathf.Clamp(tab, 0, 2);
            overviewView?.SetVisible(_activeTab == 0);
            doctrineView?.SetVisible(_activeTab == 1);
            seatView?.SetVisible(_activeTab == 2);
            SetTabState(_overviewTab, _activeTab == 0);
            SetTabState(_doctrineTab, _activeTab == 1);
            SetTabState(_seatTab, _activeTab == 2);
        }

        static void SetTabState(Button tab, bool active)
        {
            if (tab == null) return;
            var image = tab.GetComponent<Image>();
            if (image != null) image.color = active
                ? new Color(0.42f, 0.22f, 0.32f, 1f)
                : new Color(0.16f, 0.12f, 0.18f, 1f);
        }

        static DemonCultSystem ResolveCult() => MainGameManager.Instance?.gameLogicManager?.playerDataManager?.ProgressionSystem?.DemonCult;
    }
}