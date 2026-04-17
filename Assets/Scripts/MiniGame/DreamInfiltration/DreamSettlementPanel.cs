using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    public class DreamSettlementPanel : PanelWithInput
    {
        [SerializeField] private string panelId = DreamInfiltrationIds.SettlementPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform _rootRt;
        private TextMeshProUGUI _bodyTmp;
        private DreamSettlementPayload _payload;
        private bool _layoutBuilt;

        public override int FocusPriority => 840;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _rootRt = GetComponent<RectTransform>();
            layer = UILayer.Overlay;
            BuildIfNeeded();
        }

        private void BuildIfNeeded()
        {
            if (_layoutBuilt) return;
            _layoutBuilt = true;

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_rootRt, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = DreamUISpriteUtil.WhiteSprite();
            bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(_rootRt, false);
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(420f, 280f);
            boxRt.anchoredPosition = Vector2.zero;
            var boxImg = box.GetComponent<Image>();
            boxImg.sprite = DreamUISpriteUtil.WhiteSprite();
            boxImg.color = new Color(0.15f, 0.14f, 0.2f, 1f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(box.transform, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            titleRt.sizeDelta = new Vector2(-24f, 40f);
            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "梦境结算";
            titleTmp.fontSize = 26;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(box.transform, false);
            var bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = new Vector2(0f, 0.35f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(16f, 8f);
            bodyRt.offsetMax = new Vector2(-16f, -52f);
            _bodyTmp = bodyGo.GetComponent<TextMeshProUGUI>();
            _bodyTmp.fontSize = 18;
            _bodyTmp.alignment = TextAlignmentOptions.Left;
            _bodyTmp.color = new Color(0.9f, 0.9f, 0.95f);

            var btnGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(box.transform, false);
            var btnRt = (RectTransform)btnGo.transform;
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 16f);
            btnRt.sizeDelta = new Vector2(160f, 40f);
            btnGo.GetComponent<Image>().sprite = DreamUISpriteUtil.WhiteSprite();
            btnGo.GetComponent<Image>().color = new Color(0.35f, 0.32f, 0.5f, 1f);
            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                UIManager.Instance?.HidePanel(DreamInfiltrationIds.SettlementPanel);
            });
            var btnLabel = new GameObject("t", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnLabel.transform.SetParent(btnGo.transform, false);
            var blRt = (RectTransform)btnLabel.transform;
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.offsetMin = Vector2.zero;
            blRt.offsetMax = Vector2.zero;
            var blTmp = btnLabel.GetComponent<TextMeshProUGUI>();
            blTmp.text = "关闭";
            blTmp.fontSize = 18;
            blTmp.alignment = TextAlignmentOptions.Center;
            blTmp.color = Color.white;
        }

        public override void Setup(object data = null)
        {
            _payload = data as DreamSettlementPayload ?? new DreamSettlementPayload();
            if (_bodyTmp == null) BuildIfNeeded();
            var r = _payload;
            _bodyTmp.text =
                $"主题：{r.ThemeDisplayName}\n结果：{(r.Won ? "成功脱离梦境" : "梦境侵蚀失败")}\n\n" +
                $"暴力破解：{r.ForceScore}\n温柔安抚：{r.SoothingScore}\n计谋突破：{r.TrickScore}";
        }

        public override bool OnCancel()
        {
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.SettlementPanel);
            return true;
        }
    }
}
