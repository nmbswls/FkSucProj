using My;
using My.Map;
using My.UI;
using UnityEngine;

namespace My.MiniGame.Dream
{
    public static class DreamInfiltrationIds
    {
        public const string EntryPanel = "DreamInfiltrationEntry";
        public const string GameplayPanel = "DreamInfiltrationGameplay";
        public const string SettlementPanel = "DreamInfiltrationSettlement";
    }

    public static class DreamInfiltrationBootstrap
    {
        public const string DreamPauseSource = "Dream";

        private static bool _dreamPauseHeld;

        public static void OpenEntry()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[DreamInfiltration] UIManager not ready.");
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (!AbstractGroupDreamService.IsDreamAllowedTonight(glm, out var reason))
            {
                if (reason == "not_night")
                {
                    Debug.Log("[DreamInfiltration] Dream facility only available at night.");
                    UIEventGrantToastPanel.ShowToast("入梦", "仅夜间可入梦", "白天无法开启梦境通道。");
                }
                else if (reason == "used_today")
                {
                    Debug.Log("[DreamInfiltration] Dream already used today.");
                    UIEventGrantToastPanel.ShowToast("入梦", "今日已入梦", "请等待下一天夜间再试。");
                }
                else
                {
                    Debug.LogWarning($"[DreamInfiltration] Cannot open entry: {reason}");
                }

                return;
            }

            EnterMiniGame();
            UIManager.Instance.ShowPanel(DreamInfiltrationIds.EntryPanel, null, UILayer.Overlay);
        }

        // LogicTime 暂停与清输入；与 PauseController 约定来源名 DreamPauseSource
        public static void EnterMiniGame()
        {
            if (_dreamPauseHeld) return;
            if (LogicTimeManager.Instance == null)
            {
                Debug.LogWarning("[DreamInfiltration] LogicTimeManager missing; cannot pause LogicTime.");
                return;
            }

            LogicTime.RequestPause(DreamPauseSource);
            _dreamPauseHeld = true;
            ClearPlayerMoveInput();
        }

        public static void ExitMiniGame()
        {
            if (!_dreamPauseHeld) return;
            _dreamPauseHeld = false;
            if (LogicTimeManager.Instance != null)
                LogicTime.ReleasePause(DreamPauseSource);
            ClearPlayerMoveInput();
        }

        public static void BeginGameplay(DreamGameplayContext ctx)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            AbstractGroupDreamService.MarkDreamUsedToday(glm);
            UIManager.Instance?.HidePanel(DreamInfiltrationIds.EntryPanel);
            UIManager.Instance?.ShowPanel(DreamInfiltrationIds.GameplayPanel, ctx, UILayer.Overlay);
        }

        // 结算关闭后：夜间完成一局则推进至白天并日结
        public static void AdvanceDayAfterDreamIfNeeded(DreamSettlementPayload payload)
        {
            if (payload == null || !payload.AdvanceDayAfterClose) return;
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null) return;
            if (glm.DayPeriod != GameLogicManager.EDayPeriod.Night) return;
            glm.RequestAdvanceDayPeriod();
        }

        private static void ClearPlayerMoveInput()
        {
            var mg = MainGameManager.Instance;
            if (mg == null || mg.inputBinder == null) return;
            mg.inputBinder.DoPlayerMove(Vector2.zero);
        }
    }
}
