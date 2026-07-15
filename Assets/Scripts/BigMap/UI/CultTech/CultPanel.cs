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
            overviewView ??= GetComponentInChildren<CultOverviewView>(true);
            doctrineView ??= GetComponentInChildren<CultTechTreeView>(true);
            seatView ??= GetComponentInChildren<AncientSeatTreeView>(true);
            EnsureOverviewView();
            EnsureTabs();
            EnsureCloseButton();
            SelectTab(_activeTab);
        }

        void EnsureOverviewView()
        {
            if (overviewView != null) return;
            var root = transform.Find("OverviewRoot");
            if (root == null)
            {
                var objectRoot = new GameObject("OverviewRoot", typeof(RectTransform));
                objectRoot.transform.SetParent(transform, false);
                var rect = (RectTransform)objectRoot.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                root = objectRoot.transform;
            }
            overviewView = root.GetComponent<CultOverviewView>() ?? root.gameObject.AddComponent<CultOverviewView>();
        }

        void EnsureTabs()
        {
            var root = transform.Find("CultTabs");
            if (root == null)
            {
                var objectRoot = new GameObject("CultTabs", typeof(RectTransform));
                objectRoot.transform.SetParent(transform, false);
                var rect = (RectTransform)objectRoot.transform;
                rect.anchorMin = new Vector2(0.04f, 0.92f);
                rect.anchorMax = new Vector2(0.78f, 0.985f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                root = objectRoot.transform;
            }
            _overviewTab = EnsureTab(root, "OverviewTab", "教团概览", 0);
            _doctrineTab = EnsureTab(root, "DoctrineTab", "教义科技", 1);
            _seatTab = EnsureTab(root, "AncientSeatTab", "古老者之座", 2);
            BindTab(_overviewTab, 0);
            BindTab(_doctrineTab, 1);
            BindTab(_seatTab, 2);
        }

        Button EnsureTab(Transform root, string name, string text, int index)
        {
            var button = root.Find(name)?.GetComponent<Button>();
            if (button == null)
            {
                var objectRoot = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                objectRoot.transform.SetParent(root, false);
                var rect = (RectTransform)objectRoot.transform;
                rect.anchorMin = new Vector2(index / 3f, 0f);
                rect.anchorMax = new Vector2((index + 1) / 3f - 0.01f, 1f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                button = objectRoot.GetComponent<Button>();
                var labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(objectRoot.transform, false);
                var label = labelObject.AddComponent<TextMeshProUGUI>();
                label.text = text;
                label.fontSize = 16f;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            }
            return button;
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