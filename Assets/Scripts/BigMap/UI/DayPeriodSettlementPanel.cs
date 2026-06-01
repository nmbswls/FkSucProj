using My;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class DayPeriodSettlementPanel : PanelWithInput
    {
        public const string PanelId = "DayPeriodSettlementPanel";

        [SerializeField] TMP_Text summaryText;
        [SerializeField] Button btnConfirm;

        public static void Show(GameLogicManager.OneDayBalanceInfo info)
        {
            var panel = UIManager.Instance.ShowPanel(PanelId, info) as DayPeriodSettlementPanel;
            if (panel == null)
            {
                Debug.LogError("DayPeriodSettlementPanel: panel not found");
            }
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelId;
            }

            EnsureUiBuilt();
            EnsureRefs();
        }

        void EnsureUiBuilt()
        {
            if (transform.Find("Summary") != null)
            {
                return;
            }

            var rootRt = transform as RectTransform;
            if (rootRt == null)
            {
                return;
            }

            rootRt.sizeDelta = new Vector2(420f, 220f);
            var bg = gameObject.GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.12f, 0.12f, 0.16f, 0.96f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            titleRt.sizeDelta = new Vector2(-32f, 32f);
            var titleText = titleGo.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 22f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.text = "日结算";

            var summaryGo = new GameObject("Summary", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            summaryGo.transform.SetParent(transform, false);
            var summaryRt = summaryGo.GetComponent<RectTransform>();
            summaryRt.anchorMin = new Vector2(0f, 0f);
            summaryRt.anchorMax = new Vector2(1f, 1f);
            summaryRt.offsetMin = new Vector2(20f, 56f);
            summaryRt.offsetMax = new Vector2(-20f, -52f);
            summaryText = summaryGo.GetComponent<TextMeshProUGUI>();
            summaryText.fontSize = 18f;
            summaryText.alignment = TextAlignmentOptions.TopLeft;

            var btnGo = new GameObject("BtnConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 12f);
            btnRt.sizeDelta = new Vector2(120f, 36f);
            btnGo.GetComponent<Image>().color = new Color(0.28f, 0.3f, 0.38f, 1f);
            btnConfirm = btnGo.GetComponent<Button>();

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
            btnText.text = "确认";
        }

        void EnsureRefs()
        {
            if (summaryText == null)
            {
                summaryText = transform.Find("Summary")?.GetComponent<TMP_Text>();
            }

            if (btnConfirm == null)
            {
                btnConfirm = transform.Find("BtnConfirm")?.GetComponent<Button>();
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            EnsureRefs();

            if (data is GameLogicManager.OneDayBalanceInfo info)
            {
                RefreshSummary(info);
            }
        }

        public override void Show()
        {
            base.Show();
            EnsureRefs();
        }

        void RefreshSummary(GameLogicManager.OneDayBalanceInfo info)
        {
            if (summaryText == null || info == null)
            {
                return;
            }

            long afterFallen = info.FromFallenAmount + info.AddFallenAmount;
            summaryText.text =
                $"沉沦人数：{info.FromFallenAmount} → {afterFallen}（+{info.AddFallenAmount}）\n" +
                $"获得欲望碎片：{info.DesireShardAdded}";
        }

        void OnEnable()
        {
            EnsureRefs();
            if (btnConfirm != null)
            {
                btnConfirm.onClick.RemoveAllListeners();
                btnConfirm.onClick.AddListener(Close);
            }
        }

        public void Close()
        {
            UIManager.Instance.HidePanel(PanelId);
        }

        public override bool OnCancel()
        {
            Close();
            return true;
        }

        public override bool OnConfirm()
        {
            Close();
            return true;
        }
    }
}
