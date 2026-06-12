using My.Map;
using UnityEngine;

namespace My.UI
{
    public static class SkillUseDenyFeedback
    {
        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var mgr = MainGameManager.Instance;
            var player = mgr?.gameLogicManager?.playerLogicEntity;
            if (mgr != null && player != null)
            {
                mgr.ShowFakeFxEffect(message, player.Pos);
                return;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                FakeHintTextManager.ShowWorld(message, cam.transform.position + cam.transform.forward * 2f, cam);
                return;
            }

            Debug.LogWarning($"[SkillUseDenyFeedback] {message}");
        }
    }
}
