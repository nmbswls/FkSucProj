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

        private static void ClearPlayerMoveInput()
        {
            var mg = MainGameManager.Instance;
            if (mg == null || mg.inputBinder == null) return;
            mg.inputBinder.DoPlayerMove(Vector2.zero);
        }
    }
}
