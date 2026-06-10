using My.Map;
using My.Map.Entity;

namespace My
{
    public readonly struct PlayerBuffCastRemapRule
    {
        public readonly string RequiredStateAttrId;
        public readonly string FromBuffId;
        public readonly string ToBuffId;

        public PlayerBuffCastRemapRule(string requiredStateAttrId, string fromBuffId, string toBuffId)
        {
            RequiredStateAttrId = requiredStateAttrId;
            FromBuffId = fromBuffId;
            ToBuffId = toBuffId;
        }
    }

    public static class BuffCastRemap
    {
        public static readonly PlayerBuffCastRemapRule[] Rules =
        {
            new PlayerBuffCastRemapRule(AttrIdConsts.PlayerUnlockYuhuo, "status_burn", "status_yuhuo"),
            new PlayerBuffCastRemapRule(AttrIdConsts.PlayerUnlockJiang, "status_freeze", "status_stiff"),
            new PlayerBuffCastRemapRule(AttrIdConsts.PlayerUnlockYindu, "status_poison", "status_yindu"),
            new PlayerBuffCastRemapRule(AttrIdConsts.PlayerUnlockYijin, "status_bleed", "status_yijin"),
        };

        public static string ResolveBuffId(GameLogicManager mgr, long? casterId, string buffId)
        {
            if (mgr == null || casterId == null || string.IsNullOrEmpty(buffId))
            {
                return buffId;
            }

            var caster = mgr.GetLogicEntity(casterId.Value, false) as PlayerLogicEntity;
            if (caster == null)
            {
                return buffId;
            }

            foreach (var rule in Rules)
            {
                if (buffId != rule.FromBuffId)
                {
                    continue;
                }

                if (caster.CheckHasState(rule.RequiredStateAttrId))
                {
                    return rule.ToBuffId;
                }
            }

            return buffId;
        }
    }
}
