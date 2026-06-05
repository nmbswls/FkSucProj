using My;
using My.Map.Entity;
using My.Map.Hunting;
using My.Map.Scene;
using My.UI;

namespace My.Input
{
    // 场景内「取消操作」（Input Action: SceneCancel / X 键），与 Esc UI Cancel 分离
    public static class SceneOperationCancelRouter
    {
        public static bool TryCancelOperation()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return false;
            }

            if (glm.NpcDirectControl.Active)
            {
                glm.NpcDirectControl.Exit();
                return true;
            }

            if (PlayerNpcCarryService.IsCarrying)
            {
                PlayerNpcCarryService.TryPutDownCarriedBody();
                return true;
            }

            var huntingRadial = HuntingHudPanel.Instance?.ActionRadial;
            if (huntingRadial != null && huntingRadial.IsOpen)
            {
                huntingRadial.Close();
                return true;
            }

            var hud = OverworldHUDPanel.Instance;
            if (hud != null && hud.HudMode == OverworldHUDPanel.EHudMode.PreviewSkill)
            {
                hud.CancelSkillCast();
                return true;
            }

            return false;
        }
    }
}
