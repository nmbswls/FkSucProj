using My;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SecretBaseHudPanel : PanelBase
    {
        public const string PanelIdConst = "SecretBaseHudPanel";

        [SerializeField] Button btnExit;
        [SerializeField] Button btnBuild;

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
        }

        public static void TryShow()
        {
            UIManager.Instance.ShowPanel(PanelIdConst);
        }

        public static void TryHide()
        {
            UIManager.Instance.HidePanel(PanelIdConst);
        }
    }
}
