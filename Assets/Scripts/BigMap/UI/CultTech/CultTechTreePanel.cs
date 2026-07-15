using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    public sealed class CultTechTreePanel : PanelBase
    {
        public const string Pid = "CultTechTreePanel";
        public static CultTechTreePanel Open() => UIManager.Instance.ShowPanel(Pid) as CultTechTreePanel;

        [SerializeField] Button closeButton;
        [SerializeField] CultTechTreeView doctrineView;
        [SerializeField] AncientSeatTreeView seatView;
        Button _doctrineTab;
        Button _seatTab;
        int _activeTab;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId)) panelId = Pid;
            doctrineView ??= GetComponent<CultTechTreeView>();
            if (doctrineView == null) doctrineView = gameObject.AddComponent<CultTechTreeView>();
            EnsureSeatView();
            EnsureTabs();
            EnsureCloseButton();
            SelectTab(0);
        }

        void EnsureSeatView()
        {
            if (seatView != null) return;
            var root = transform.Find("AncientSeatViewRoot");
            if (root == null)
            {
                var go = new GameObject("AncientSeatViewRoot", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                root = go.transform;
            }
            seatView = root.GetComponent<AncientSeatTreeView>() ?? root.gameObject.AddComponent<AncientSeatTreeView>();
        }

        void EnsureTabs()
        {
            var root = transform.Find("CultTabs");
            if (root == null)
            {
                var go = new GameObject("CultTabs", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.02f, 0.94f); rect.anchorMax = new Vector2(0.68f, 0.995f); rect.offsetMin = rect.offsetMax = Vector2.zero;
                root = go.transform;
            }
            _doctrineTab = EnsureTab(root, "DoctrineTab", "????", 0, "UI/Cult/Icons/doctrine");
            _seatTab = EnsureTab(root, "AncientSeatTab", "?????", 1, "UI/Cult/Icons/ancient_seat");
            _doctrineTab.onClick.RemoveAllListeners(); _doctrineTab.onClick.AddListener(() => SelectTab(0));
            _seatTab.onClick.RemoveAllListeners(); _seatTab.onClick.AddListener(() => SelectTab(1));
        }

        Button EnsureTab(Transform root, string name, string text, int index, string iconPath)
        {
            var button = root.Find(name)?.GetComponent<Button>();
            if (button == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(root, false);
                var rect = (RectTransform)go.transform; rect.anchorMin = new Vector2(index * 0.5f, 0f); rect.anchorMax = new Vector2(index * 0.5f + 0.46f, 1f); rect.offsetMin = rect.offsetMax = Vector2.zero;
                button = go.GetComponent<Button>();
                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); label.transform.SetParent(go.transform, false);
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.text = text; label.alignment = TextAlignmentOptions.Center; label.fontSize = 16f; label.raycastTarget = false;
            }
            return button;
        }

        void EnsureCloseButton()
        {
            if (closeButton == null) closeButton = transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton == null)
            {
                var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform; rect.anchorMin = new Vector2(1f, 1f); rect.anchorMax = new Vector2(1f, 1f); rect.pivot = new Vector2(1f, 1f); rect.anchoredPosition = new Vector2(-18f, -18f); rect.sizeDelta = new Vector2(42f, 42f);
                closeButton = go.GetComponent<Button>();
                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); label.transform.SetParent(go.transform, false); label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero; label.text = "?"; label.fontSize = 24f; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            }
            closeButton.onClick.RemoveAllListeners(); closeButton.onClick.AddListener(Hide);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            var cult = ResolveCult();
            doctrineView?.Bind(cult);
            seatView?.Bind(cult);
            SelectTab(_activeTab);
        }

        public override void Show()
        {
            base.Show(); doctrineView?.Refresh(); seatView?.Refresh(); SelectTab(_activeTab);
        }

        void SelectTab(int tab)
        {
            _activeTab = Mathf.Clamp(tab, 0, 1);
            doctrineView?.SetVisible(_activeTab == 0);
            seatView?.SetVisible(_activeTab == 1);
            if (_doctrineTab != null) _doctrineTab.GetComponent<Image>().color = _activeTab == 0 ? new Color(0.42f, 0.22f, 0.32f, 1f) : new Color(0.16f, 0.12f, 0.18f, 1f);
            if (_seatTab != null) _seatTab.GetComponent<Image>().color = _activeTab == 1 ? new Color(0.42f, 0.22f, 0.32f, 1f) : new Color(0.16f, 0.12f, 0.18f, 1f);
        }

        static DemonCultSystem ResolveCult() => MainGameManager.Instance?.gameLogicManager?.playerDataManager?.ProgressionSystem?.DemonCult;
    }
}
