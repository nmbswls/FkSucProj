using My.MiniGame.Dream;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class UIBedroomDreamPanel : PanelWithInput
    {
        public const string PanelIdConst = "UIBedroomDream";

        [SerializeField] private Button btnDream;
        [SerializeField] private Button btnClose;

        public override int FocusPriority => 805;

        private void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
                panelId = PanelIdConst;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            layer = UILayer.Popup;

            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(TryCloseSelf);
            }

            if (btnDream != null)
            {
                btnDream.onClick.RemoveAllListeners();
                btnDream.onClick.AddListener(OnClickOpenDream);
            }
        }

        private void Update()
        {
            if (!IsVisible) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                TryCloseSelf();
        }

        public override bool OnCancel()
        {
            TryCloseSelf();
            return true;
        }

        private void TryCloseSelf()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.HidePanel(panelId);
        }

        private void OnClickOpenDream()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.HidePanel(panelId);
            DreamInfiltrationBootstrap.OpenEntry();
        }
    }
}
