using My;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SecretBaseHudPanel : PanelBase
    {
        public const string PanelIdConst = "SecretBaseHudPanel";

        public static SecretBaseHudPanel Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as SecretBaseHudPanel;
            }
        }

        [SerializeField] Button btnExit;
        [SerializeField] Button btnBuild;
        [SerializeField] Button btnJingYuanWarehouse;
        [SerializeField] Button btnRumorIntel;
        [SerializeField] Button btnSwitch;
        [SerializeField] Button btnNextPeriod;
        [SerializeField] TextMeshProUGUI txtFakeState;
        [SerializeField] TextMeshProUGUI txtDayPeriod;

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

            if (btnJingYuanWarehouse != null)
            {
                btnJingYuanWarehouse.onClick.AddListener(() => UIManager.Instance.ShowPanel(JingYuanWarehousePanel.PanelIdConst));
            }

            if (btnRumorIntel == null)
                btnRumorIntel = transform.Find("TopRightBar/BtnRumorIntel")?.GetComponent<Button>();
            if (btnRumorIntel != null)
                btnRumorIntel.onClick.AddListener(RumorIntelShopPanel.Open);

            if (btnSwitch != null)
            {
                btnSwitch.onClick.AddListener(SwitchFakeState);
            }

            if (btnNextPeriod != null)
            {
                btnNextPeriod.onClick.AddListener(OnClickNextPeriod);
            }
        }

        void OnClickNextPeriod()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            string title;
            string message;
            if (glm.DayPeriod == GameLogicManager.EDayPeriod.Day)
            {
                title = "进入夜晚";
                message = "确定要进入夜晚吗？";
            }
            else
            {
                title = "进入白天";
                message = "确定要进入白天吗？部分事件仅在白天推进。";
            }

            YesNoMsgBox.Show(title, message, () => glm.RequestAdvanceDayPeriod());
        }

        void SwitchFakeState()
        {
            MainGameManager.Instance.gameLogicManager.ForcePlayerHumanMode(
                !MainGameManager.Instance.gameLogicManager.GameSession.PlayerHumanMode);
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
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            if (txtFakeState != null)
            {
                txtFakeState.text = glm.GameSession.PlayerHumanMode ? "人类" : "伪装";
            }

            if (txtDayPeriod != null)
            {
                txtDayPeriod.text = glm.DayPeriod == GameLogicManager.EDayPeriod.Day ? "白天" : "夜晚";
            }
        }
    }
}
