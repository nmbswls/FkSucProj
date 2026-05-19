using My.Map;
using My.Map.Entity;
using static My.Map.Fight.FightStruct;

namespace My
{
    public static class PlayerOutgoingFightRule
    {
        public static EDmgFlag AugmentOutgoingDamageFlags(GameLogicManager mgr, long? srcEntityId, EDmgFlag flags)
        {
            if (mgr == null || srcEntityId == null || srcEntityId.Value == 0)
            {
                return flags;
            }

            var src = mgr.GetLogicEntity(srcEntityId.Value, false) as PlayerLogicEntity;
            if (src == null)
            {
                return flags;
            }

            if (src.CheckHasState(AttrIdConsts.PlayerZhaZhiMode))
            {
                flags |= EDmgFlag.Nonlethal;
            }

            return flags;
        }
    }
}
