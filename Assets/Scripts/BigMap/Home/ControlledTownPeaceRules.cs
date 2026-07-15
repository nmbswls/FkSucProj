using My;

namespace My.Home
{
    // 玩家掌控城镇的判定规则（buff 挂卸由 PeaceCombatBuffRefresh 统一入口处理）
    public static class ControlledTownPeaceRules
    {
        public const string BuffId = "homestead_peace";

        public static bool ShouldApply(GameLogicManager glm)
        {
            if (glm?.townFacilityDevelopmentSystem == null)
            {
                return false;
            }

            var logicAreaId = glm.townFacilityDevelopmentSystem.ResolveCurrentLogicAreaId();
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return false;
            }

            return glm.townFacilityDevelopmentSystem.IsAreaUnderPlayerControl(logicAreaId);
        }
    }
}
