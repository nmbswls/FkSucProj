using My;
using My.Config;
using My.Map;

namespace My.Map.Logic
{
    // 生成前为 NPC 掷出精型；同一实例/具名角色持久化
    public static class JingYuanTypeSpawnLogic
    {
        public static void ApplyOnNpcBeforeSpawn(GameLogicManager gm, GameLogicAreaManager area, LogicEntityRecord4Npc rec)
        {
            if (rec == null || gm == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(rec.RolledJingyuanTypeId))
            {
                return;
            }

            if (!string.IsNullOrEmpty(rec.CharacterKey))
            {
                ApplyNamed(gm, rec);
                if (!string.IsNullOrEmpty(rec.RolledJingyuanTypeId))
                {
                    return;
                }
            }

            var npcCfg = CfgMgr.Cfgs?.TbUnitNpc.GetOrDefault(rec.CfgId);
            var poolId = npcCfg?.JingyuanPoolId;
            if (string.IsNullOrEmpty(poolId))
            {
                return;
            }

            rec.RolledJingyuanTypeId = JingYuanTypeCatalog.RollTypeIdFromPool(poolId);
        }

        static void ApplyNamed(GameLogicManager gm, LogicEntityRecord4Npc rec)
        {
            var registry = gm.worldPersistState?.NpcCharacters;
            if (registry == null)
            {
                return;
            }

            if (registry.TryGetRolledJingyuanTypeId(rec.CharacterKey, out var typeId))
            {
                rec.RolledJingyuanTypeId = typeId;
                return;
            }

            var npcCfg = CfgMgr.Cfgs?.TbUnitNpc.GetOrDefault(rec.CfgId);
            var poolId = npcCfg?.JingyuanPoolId;
            if (string.IsNullOrEmpty(poolId))
            {
                return;
            }

            typeId = JingYuanTypeCatalog.RollTypeIdFromPool(poolId);
            if (string.IsNullOrEmpty(typeId))
            {
                return;
            }

            rec.RolledJingyuanTypeId = typeId;
            registry.SetRolledJingyuanTypeId(rec.CharacterKey, typeId);
        }
    }
}
