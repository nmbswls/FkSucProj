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
        [SerializeField] Button btnSwitch;
        [SerializeField] Button btnNextPeriod;
        [SerializeField] TextMeshProUGUI txtFakeState;
        [SerializeField] TextMeshProUGUI txtDayPeriod;

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.HUD;

            EnsureDayPeriodUI();
            EnsureDayPeriodRefs();

            if (btnExit != null)
            {
                btnExit.onClick.AddListener(() => MainGameManager.Instance?.gameLogicManager?.ExitSecretBase());
            }

            if (btnBuild != null)
            {
                btnBuild.onClick.AddListener(() => UIManager.Instance.ShowPanel(SecretBaseBuildPanel.PanelIdConst));
            }

            if (btnSwitch != null)
            {
                btnSwitch.onClick.AddListener(SwitchFakeState);
            }

            if (btnNextPeriod != null)
            {
                btnNextPeriod.onClick.AddListener(OnClickNextPeriod);
            }
        }

        void EnsureDayPeriodRefs()
        {
            if (btnNextPeriod == null)
            {
                btnNextPeriod = transform.Find("TopLeftBar/BtnNextPeriod")?.GetComponent<Button>();
            }

            if (txtDayPeriod == null)
            {
                txtDayPeriod = transform.Find("TopLeftBar/TxtDayPeriod")?.GetComponent<TextMeshProUGUI>();
            }
        }

        void EnsureDayPeriodUI()
        {
            if (transform.Find("TopLeftBar") != null)
            {
                return;
            }

            var barGo = new GameObject("TopLeftBar", typeof(RectTransform));
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.SetParent(transform, false);
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 1f);
            barRt.anchoredPosition = new Vector2(16f, -16f);
            barRt.sizeDelta = new Vector2(220f, 36f);

            var labelGo = new GameObject("TxtDayPeriod", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(barRt, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(0f, 0.5f);
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(72f, 32f);
            txtDayPeriod = labelGo.GetComponent<TextMeshProUGUI>();
            txtDayPeriod.fontSize = 20f;
            txtDayPeriod.alignment = TextAlignmentOptions.MidlineLeft;
            txtDayPeriod.text = "??";

            var btnGo = new GameObject("BtnNextPeriod", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(barRt, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = Vector2.zero;
            btnRt.sizeDelta = new Vector2(132f, 36f);
            btnGo.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.3f, 1f);
            btnNextPeriod = btnGo.GetComponent<Button>();

            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnRt, false);
            var btnTextRt = btnTextGo.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = Vector2.zero;
            btnTextRt.offsetMax = Vector2.zero;
            var btnText = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnText.fontSize = 18f;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.text = "????";
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
                title = "????";
                message = "???????";
            }
            else
            {
                title = "????";
                message = "????????????????????";
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
            EnsureDayPeriodRefs();
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            if (txtFakeState != null)
            {
                txtFakeState.text = glm.GameSession.PlayerHumanMode ? "??" : "??";
            }

            if (txtDayPeriod != null)
            {
                txtDayPeriod.text = glm.DayPeriod == GameLogicManager.EDayPeriod.Day ? "??" : "??";
            }
        }
    }
}
