using My;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SecretBaseHudPanel : PanelBase
    {
        public const string PanelIdConst = "SecretBaseHudPanel";

        [SerializeField] private Button btnExit;
        [SerializeField] private Button btnBuild;

        public static SecretBaseHudPanel Instance { get; private set; }

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.HUD;
            Instance = this;

            if (btnExit != null)
            {
                btnExit.onClick.AddListener(OnExitClicked);
            }

            if (btnBuild != null)
            {
                btnBuild.onClick.AddListener(OnBuildClicked);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
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

        void OnExitClicked()
        {
            MainGameManager.Instance?.gameLogicManager?.ExitSecretBase();
        }

        void OnBuildClicked()
        {
            UIManager.Instance.ShowPanel(SecretBaseBuildPanel.PanelIdConst);
        }
    }
}
