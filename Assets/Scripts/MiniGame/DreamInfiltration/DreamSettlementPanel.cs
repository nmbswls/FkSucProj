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
        private bool _outcomeApplied;
        private bool _dayAdvanceRequested;

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
            CloseAndAdvance();
        }

        private void ApplyOutcomeOnce()
        {
            if (_outcomeApplied) return;
            _outcomeApplied = true;
            DreamInfiltrationOutcomeApplier.Apply(_payload);
            RefreshBodyText();
        }

        private void CloseAndAdvance()
        {
            ApplyOutcomeOnce();
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.SettlementPanel);
            DreamInfiltrationBootstrap.ExitMiniGame();
            if (!_dayAdvanceRequested)
            {
                _dayAdvanceRequested = true;
                DreamInfiltrationBootstrap.AdvanceDayAfterDreamIfNeeded(_payload);
            }
        }

        public override void Setup(object data = null)
        {
            _payload = data as DreamSettlementPayload ?? new DreamSettlementPayload();
            _outcomeApplied = false;
            _dayAdvanceRequested = false;
            if (_bodyTmp == null) ResolveRefs();
            // 先落盘再刷新文案，便于展示团体奖励/秘会说明
            ApplyOutcomeOnce();
        }

        private void RefreshBodyText()
        {
            if (_bodyTmp == null)
            {
                Debug.LogWarning("[DreamInfiltration] Settlement body text missing. Check prefab Box/Body.");
                return;
            }

            var r = _payload;
            var total = r.ForceScore + r.SoothingScore + r.TrickScore;
            var tendencyLine = "";
            if (r.Won && r.VictoryTendency.HasValue)
            {
                var tendencyName = r.VictoryTendency.Value switch
                {
                    DreamTendencyKind.Force => "暴力",
                    DreamTendencyKind.Soothing => "安抚",
                    _ => "计谋",
                };
                tendencyLine = $"\n本次胜利方式：{tendencyName}";
            }

            var groupLine = "";
            if (r.EntrySource == DreamEntrySourceKind.AbstractGroupEntry && !string.IsNullOrEmpty(r.AbstractGroupId))
            {
                groupLine = $"\n小团体：{r.AbstractGroupId} 阶段 {r.AbstractGroupStage}";
            }

            var extra = string.IsNullOrEmpty(r.ExtraSettlementNote)
                ? ""
                : $"\n\n{r.ExtraSettlementNote}";

            _bodyTmp.text =
                $"主题：{r.ThemeDisplayName}\n结果：{(r.Won ? "核心摧毁成功" : "梦境侵蚀失败")}{tendencyLine}{groupLine}\n\n" +
                $"对核心伤害总计：{total}\n" +
                $"  暴力炮弹：{r.ForceScore}\n" +
                $"  温柔炮弹：{r.SoothingScore}\n" +
                $"  计谋炮弹：{r.TrickScore}" +
                extra +
                "\n\n关闭后将推进至下一天。";
        }

        public override bool OnCancel()
        {
            CloseAndAdvance();
            return true;
        }
    }
}
