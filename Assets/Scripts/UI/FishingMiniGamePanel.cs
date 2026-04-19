using System.Collections;
using My;
using My.Map;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class FishingMiniGamePanel : PanelBase
    {
        public static FishingMiniGamePanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("FishingMiniGamePanel");
                if (panel != null && panel is FishingMiniGamePanel retPanel)
                {
                    return retPanel;
                }
                return null;
            }
        }


        public class Ctx
        {
            public long FishingEntityId;
        }

        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button cancelButton;

        private Ctx _ctx;
        private Coroutine _co;

        public override void Setup(object data = null)
        {
            _ctx = data as Ctx;
            if (hintText != null)
            {
                hintText.text = "Fishing... (prototype auto-finish in 3s)";
            }
        }

        public override void Show()
        {
            base.Show();
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(CoRun());
        }

        private IEnumerator CoRun()
        {
            yield return new WaitForSecondsRealtime(3f);

            bool ok = false;
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            if (glm != null && _ctx != null)
            {
                var ent = glm.AreaManager.GetLogicEntiy(_ctx.FishingEntityId) as FishingSpotLogicEntity;
                if (ent != null)
                {
                    ok = ent.TryCompleteOneCatchAfterMiniGame();
                    ent.SetMiniGameOpen(false);
                    ent.ReloadRemainingFromPlayerSave();
                }
            }

            if (hintText != null)
            {
                hintText.text = ok ? "Got a catch." : "No catch.";
            }

            yield return new WaitForSecondsRealtime(0.35f);
            UIManager.Instance.HidePanel("FishingMiniGamePanel");
            LogicTime.ReleasePause("FishingMiniGame");
            _co = null;
        }

        private void Awake()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(() =>
                {
                    if (_co != null) StopCoroutine(_co);
                    var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
                    if (glm != null && _ctx != null)
                    {
                        var ent = glm.AreaManager.GetLogicEntiy(_ctx.FishingEntityId) as FishingSpotLogicEntity;
                        ent?.SetMiniGameOpen(false);
                    }
                    UIManager.Instance.HidePanel("FishingMiniGamePanel");
                    LogicTime.ReleasePause("FishingMiniGame");
                });
            }
        }
    }
}
