using My.Map;
using My.Map.Entity;

namespace My.Map.Fight
{
    // 攻击者带 PeaceCombatRestricted 时，禁止对 NPC 产生战斗效果 / 受击
    class PeaceZoneCombatInterceptor : IFightEffectInterceptor
    {
        public bool ShouldBlockEffect(in FightEffectInterceptContext ctx)
        {
            if (ctx.Env == null || ctx.SrcEntityId == 0 || ctx.TargetId == 0)
            {
                return false;
            }

            var target = ctx.Env.GetLogicEntity(ctx.TargetId, false) as BaseUnitLogicEntity;
            var src = ctx.Env.GetLogicEntity(ctx.SrcEntityId, false) as BaseUnitLogicEntity;
            return ShouldBlockHit(src, target);
        }

        public bool ShouldBlockHit(BaseUnitLogicEntity src, BaseUnitLogicEntity target)
        {
            if (src == null || target == null || target is not NpcUnitLogicEntity)
            {
                return false;
            }

            return src.CheckHasState(AttrIdConsts.PeaceCombatRestricted);
        }

        public bool TryGetSkillCastDeny(BaseUnitLogicEntity caster, out string denyMessage)
        {
            denyMessage = null;
            if (caster == null || !caster.CheckHasState(AttrIdConsts.PeaceCombatRestricted))
            {
                return false;
            }

            denyMessage = PeaceZoneCombatInterceptorMessages.SkillCastDenied;
            return true;
        }
    }

    static class PeaceZoneCombatInterceptorMessages
    {
        public const string SkillCastDenied = "安全区内禁止战斗";
    }
}
