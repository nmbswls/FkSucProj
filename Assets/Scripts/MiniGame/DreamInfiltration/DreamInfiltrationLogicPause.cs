using My;
using My.Map;
using UnityEngine;

namespace My.MiniGame.Dream
{
    // 梦境潜入流程期间暂停 LogicTime；与 PauseController 约定来源名
    public static class DreamInfiltrationLogicPause
    {
        public const string PauseSource = "Dream";

        private static bool _held;

        public static void EnterMiniGame()
        {
            if (_held) return;
            if (LogicTimeManager.Instance == null)
            {
                Debug.LogWarning("[DreamInfiltration] LogicTimeManager missing; cannot pause LogicTime.");
                return;
            }

            LogicTime.RequestPause(PauseSource);
            _held = true;
            ClearPlayerMoveInput();
        }

        public static void ExitMiniGame()
        {
            if (!_held) return;
            _held = false;
            if (LogicTimeManager.Instance != null)
                LogicTime.ReleasePause(PauseSource);
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
