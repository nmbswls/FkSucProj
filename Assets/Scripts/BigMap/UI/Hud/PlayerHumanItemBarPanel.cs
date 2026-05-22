using My;
using UnityEngine;

namespace My.UI
{
    public class PlayerHumanItemBarPanel : PanelBase
    {
        public const string PanelIdConst = "PlayerHumanItemBarPanel";

        public static PlayerHumanItemBarPanel Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as PlayerHumanItemBarPanel;
            }
        }

        [SerializeField]
        RectTransform _contentRoot;

        [SerializeField]
        PlayerHumanItemBarController _controller;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelIdConst;
            }

            layer = UILayer.HUD;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_contentRoot == null)
            {
                _contentRoot = transform as RectTransform;
            }

            if (_controller == null)
            {
                _controller = GetComponent<PlayerHumanItemBarController>();
            }
        }

        public static void TryShow()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.ShowPanel(PanelIdConst);
        }

        public static void TryHide()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.HidePanel(PanelIdConst);
        }

        public static void RefreshFromGame()
        {
            Instance?.Refresh();
        }

        public void EnsureQuickItemBarReady()
        {
            _controller?.InitializeIfNeeded();
            _controller?.EnsureSlots();
            _controller?.RefreshFromPlayerData();
        }

        public override void Setup(object data = null)
        {
            _controller?.InitializeIfNeeded();
            _controller?.EnsureSlots();
            Refresh();
        }

        public override void Show()
        {
            base.Show();
            _controller?.EnsureSlots();
            Refresh();
        }

        public void Refresh()
        {
            var lgm = MainGameManager.Instance?.gameLogicManager;
            bool available = lgm != null && lgm.IsHumanQuickBarAvailable();

            if (_contentRoot != null)
            {
                _contentRoot.gameObject.SetActive(available);
            }

            if (!available)
            {
                return;
            }

            lgm.playerDataManager?.HumanQuickBar?.PruneInvalidSlots();
            _controller?.EnsureSlots();
            _controller?.RefreshFromPlayerData();
            OverworldHUDPanel.Instance?.SkilBar?.Refresh();
        }
    }
}
