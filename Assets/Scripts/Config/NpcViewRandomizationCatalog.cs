using System;
using System.Collections.Generic;
using Config;
using cfg.demo;

namespace My.Config
{
    /// <summary>
    /// Resolves town NPC presentation variants. The entity id is used as the
    /// stable seed so pooling and AOI refreshes do not change an NPC's look.
    /// </summary>
    public static class NpcViewRandomizationCatalog
    {
        public static string ResolvePrefabName(string npcId, long entityId)
        {
            var npc = CfgMgr.Cfgs?.TbUnitNpc?.GetOrDefault(npcId);
            var fallback = npc?.PrefabName;
            if (string.IsNullOrEmpty(fallback))
                fallback = npcId;

            var table = CfgMgr.Cfgs?.TbNpcViewRandomization;
            if (table == null)
                return fallback;

            var variants = new List<NpcViewRandomization>();
            foreach (var row in table.DataList)
            {
                if (row.NpcId == npcId && row.Weight > 0 && !string.IsNullOrEmpty(row.ViewPrefabName))
                    variants.Add(row);
            }

            if (variants.Count == 0)
                return fallback;

            long seed = entityId == 0 ? StableHash(npcId) : entityId;
            var random = new System.Random(unchecked((int)(seed ^ (seed >> 32))));
            int totalWeight = 0;
            foreach (var row in variants)
                totalWeight += row.Weight;
            int roll = random.Next(totalWeight);
            foreach (var row in variants)
            {
                if (roll < row.Weight)
                    return row.ViewPrefabName;
                roll -= row.Weight;
            }
            return fallback;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash;
            }
        }
    }
}
