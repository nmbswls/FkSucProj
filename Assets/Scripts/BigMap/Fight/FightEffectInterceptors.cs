using My.Map;
using My.Map.Entity;

namespace My.Map.Fight
{
    public struct FightEffectInterceptContext
    {
        public GameLogicManager Env;
        public long SrcEntityId;
        public long TargetId;

        public FightEffectInterceptContext(GameLogicManager env, long srcEntityId, long targetId)
        {
            Env = env;
            SrcEntityId = srcEntityId;
            TargetId = targetId;
        }
    }

    public interface IFightEffectInterceptor
    {
        bool ShouldBlockEffect(in FightEffectInterceptContext ctx);
        bool ShouldBlockHit(BaseUnitLogicEntity src, BaseUnitLogicEntity target);
        bool TryGetSkillCastDeny(BaseUnitLogicEntity caster, out string denyMessage);
    }

    // 战斗效果 / 受击 / 施法前的统一拦截链
    public static class FightEffectInterceptors
    {
        static readonly IFightEffectInterceptor[] Chain =
        {
            new PeaceZoneCombatInterceptor(),
        };

        public static bool ShouldBlockEffect(GameLogicManager env, long srcEntityId, long targetId)
        {
            if (env == null || srcEntityId == 0 || targetId == 0)
            {
                return false;
            }

            var ctx = new FightEffectInterceptContext(env, srcEntityId, targetId);
            for (var i = 0; i < Chain.Length; i++)
            {
                if (Chain[i].ShouldBlockEffect(in ctx))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ShouldBlockHit(BaseUnitLogicEntity src, BaseUnitLogicEntity target)
        {
            if (src == null || target == null)
            {
                return false;
            }

            for (var i = 0; i < Chain.Length; i++)
            {
                if (Chain[i].ShouldBlockHit(src, target))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetSkillCastDeny(BaseUnitLogicEntity caster, out string denyMessage)
        {
            denyMessage = null;
            if (caster == null)
            {
                return false;
            }

            for (var i = 0; i < Chain.Length; i++)
            {
                if (Chain[i].TryGetSkillCastDeny(caster, out denyMessage))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
