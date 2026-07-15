using System;
using System.Collections.Generic;
using cfg.demo;
using My.Player;
using UnityEngine;

namespace My.Config
{
    public static class JingYuanEssenceCatalog
    {
        static long _nextInstanceId;

        public static JingYuanTypeInfo GetTypeInfo(EJingYuanType typeId)
            => CfgMgr.Cfgs?.TbJingYuanTypeInfo?.GetOrDefault(typeId);

        public static EJingYuanType GetNamedNpcType(string characterKey)
            => CfgMgr.Cfgs?.TbNamedNpcJingYuanType?.GetOrDefault(characterKey)?.TypeId ?? EJingYuanType.None;

        public static EJingYuanType RollTypeFromPool(string poolId)
        {
            var rows = CfgMgr.Cfgs?.TbJingYuanTypePoolEnum?.DataList;
            if (rows == null || string.IsNullOrEmpty(poolId)) return EJingYuanType.None;
            var candidates = new List<JingYuanTypePoolEnum>();
            int total = 0;
            foreach (var row in rows) if (row != null && row.PoolId == poolId && row.Weight > 0) { candidates.Add(row); total += row.Weight; }
            if (total <= 0 || candidates.Count == 0) return EJingYuanType.None;
            int roll = UnityEngine.Random.Range(0, total);
            foreach (var row in candidates) { if (roll < row.Weight) return row.TypeId; roll -= row.Weight; }
            return candidates[candidates.Count - 1].TypeId;
        }

        public static int ResolveDropLevel(int npcLevel)
        {
            var rows = CfgMgr.Cfgs?.TbJingYuanEssenceLevelMap?.DataList;
            if (rows == null) return 1;
            foreach (var row in rows) if (row != null && npcLevel >= row.NpcLevelMin && npcLevel <= row.NpcLevelMax) return row.EssenceLevel;
            return npcLevel < 1 ? 1 : rows[rows.Count - 1].EssenceLevel;
        }

        public static JingYuanPremiumEffect ResolveEffect(EJingYuanType typeId, int dropLevel, int concentration)
        {
            var rows = CfgMgr.Cfgs?.TbJingYuanPremiumEffect?.DataList;
            JingYuanPremiumEffect best = null;
            if (rows == null) return null;
            foreach (var row in rows)
            {
                if (row == null || row.TypeId != typeId || row.DropLevel != dropLevel || row.ConcentrationFloor > concentration) continue;
                if (best == null || row.ConcentrationFloor > best.ConcentrationFloor) best = row;
            }
            return best;
        }

        public static PremiumEssenceInstance CreateInstance(EJingYuanType typeId, int npcLevel, string sourceType)
            => CreateInstanceAtLevel(typeId, ResolveDropLevel(npcLevel), 1, sourceType, null);

        public static PremiumEssenceInstance CreateInstanceAtLevel(EJingYuanType typeId, int dropLevel, int qualityTier, string sourceType, string sourceItemId)
        {
            var def = GetTypeInfo(typeId);
            var essence = CfgMgr.Cfgs?.TbJingYuanPremiumEssence?.DataList;
            JingYuanPremiumEssence essenceDef = null;
            if (essence != null) foreach (var row in essence) if (row != null && row.TypeId == typeId) { essenceDef = row; break; }
            return new PremiumEssenceInstance
            {
                InstanceId = System.Threading.Interlocked.Increment(ref _nextInstanceId),
                TypeId = typeId,
                Concentration = UnityEngine.Random.Range(0, 101),
                DropLevel = Math.Max(1, dropLevel),
                QualityTier = Math.Max(1, qualityTier),
                SourceItemId = sourceItemId,
                RemainingShelfLifeDays = essenceDef?.BaseShelfLifeDays ?? 3,
                SourceType = sourceType,
                StorageState = PremiumEssenceStorageState.Temporary,
            };
        }

        public static string ToLegacyTypeId(EJingYuanType typeId) => typeId switch
        {
            EJingYuanType.OrcI => "orc_i",
            EJingYuanType.OrcII => "orc_ii",
            EJingYuanType.HumanI => "human_i",
            EJingYuanType.HumanII => "human_ii",
            EJingYuanType.HumanIII => "human_iii",
            EJingYuanType.HumanIV => "human_iv",
            EJingYuanType.HumanV => "human_v",
            EJingYuanType.HumanVI => "human_vi",
            EJingYuanType.HumanVII => "human_vii",
            EJingYuanType.MonsterI => "monster_i",
            EJingYuanType.MonsterII => "monster_ii",
            EJingYuanType.MonsterIII => "monster_iii",
            _ => string.Empty,
        };
    }
}
