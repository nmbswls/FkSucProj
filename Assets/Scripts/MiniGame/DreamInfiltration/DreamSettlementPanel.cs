using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    public class DreamSettlementPanel : PanelWithInput
    {
        [Header("Prefab（可空则按路径解析：Bg、Box、Box/Body、Box/CloseBtn）")]
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button closeButton;

        private RectTransform _rootRt;
        private TextMeshProUGUI _bodyTmp;
        private Button _closeBtn;
        private DreamSettlementPayload _payload;
        private bool _closeWired;

        public override int FocusPriority => 840;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _rootRt = GetComponent<RectTransform>();
            layer = UILayer.Overlay;
            ResolveRefs();
            EnsureSprites();
            WireCloseOnce();
        }

        private void ResolveRefs()
        {
            _bodyTmp = bodyText != null ? bodyText : _rootRt.Find("Box/Body")?.GetComponent<TextMeshProUGUI>();
            _closeBtn = closeButton != null ? closeButton : _rootRt.Find("Box/CloseBtn")?.GetComponent<Button>();
        }

        private void EnsureSprites()
        {
            DreamUISpriteUtil.EnsureWhiteSprite(_rootRt.Find("Bg")?.GetComponent<Image>());
            var boxRt = _rootRt.Find("Box");
            if (boxRt != null) DreamUISpriteUtil.EnsureWhiteSprite(boxRt.GetComponent<Image>());
            var closeTr = _rootRt.Find("Box/CloseBtn");
            if (closeTr != null) DreamUISpriteUtil.EnsureWhiteSprite(closeTr.GetComponent<Image>());
        }

        private void WireCloseOnce()
        {
            if (_closeWired || _closeBtn == null) return;
            _closeBtn.onClick.AddListener(OnCloseClicked);
            _closeWired = true;
        }

        private void OnCloseClicked()
        {
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.SettlementPanel);
            DreamInfiltrationBootstrap.ExitMiniGame();
        }

        public override void Setup(object data = null)
        {
            _payload = data as DreamSettlementPayload ?? new DreamSettlementPayload();
            if (_bodyTmp == null) ResolveRefs();
            if (_bodyTmp == null)
            {
                Debug.LogWarning("[DreamInfiltration] Settlement body text missing. Check prefab Box/Body.");
                return;
            }

            var r = _payload;
            var total = r.ForceScore + r.SoothingScore + r.TrickScore;
            _bodyTmp.text =
                $"主题：{r.ThemeDisplayName}\n结果：{(r.Won ? "核心摧毁成功" : "梦境侵蚀失败")}\n\n" +
                $"对核心伤害总计：{total}\n" +
                $"  暴力炮弹：{r.ForceScore}\n" +
                $"  温柔炮弹：{r.SoothingScore}\n" +
                $"  计谋炮弹：{r.TrickScore}";
        }

        public override bool OnCancel()
        {
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.SettlementPanel);
            DreamInfiltrationBootstrap.ExitMiniGame();
            return true;
        }
    }
}
