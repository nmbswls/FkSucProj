using My;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SecretBaseHudPanel : PanelBase
    {
        public const string PanelIdConst = "SecretBaseHudPanel";

        [SerializeField] Button btnExit;
        [SerializeField] Button btnBuild;

        [SerializeField] Button btnSwitch;
        [SerializeField] TextMeshProUGUI txtFakeState;

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.HUD;

            if (btnExit != null)
            {
                btnExit.onClick.AddListener(() => MainGameManager.Instance?.gameLogicManager?.ExitSecretBase());
            }

            if (btnBuild != null)
            {
                btnBuild.onClick.AddListener(() => UIManager.Instance.ShowPanel(SecretBaseBuildPanel.PanelIdConst));
            }

            if(btnSwitch != null)
            {
                btnSwitch.onClick.AddListener(() => SwitchFakeState());
            }
        }

        /// <summary>
        /// ÇÐ»»
        /// </summary>
        private void SwitchFakeState()
        {
            MainGameManager.Instance.gameLogicManager.ForcePlayerHumanMode(!MainGameManager.Instance.gameLogicManager.GameSession.PlayerHumanMode);
            RefreshUI();
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

        public override void Show()
        {
            base.Show();

            RefreshUI();
        }

        public void RefreshUI()
        {
            if (MainGameManager.Instance.gameLogicManager.GameSession.PlayerHumanMode)
            {
                txtFakeState.text = "Î±×°";
            }
            else
            {
                txtFakeState.text = "ÕæÉí";
            }
        }
    }
}
