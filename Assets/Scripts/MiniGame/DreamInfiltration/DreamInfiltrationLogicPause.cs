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
            _held = true;
            LogicTime.RequestPause(PauseSource);
        }

        public static void ExitMiniGame()
        {
            if (!_held) return;
            _held = false;
            LogicTime.ReleasePause(PauseSource);
        }
    }
}
