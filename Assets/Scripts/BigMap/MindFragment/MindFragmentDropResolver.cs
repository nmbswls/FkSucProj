using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Logic;
using UnityEngine;

namespace My.Map.MindFragment
{
    public static class MindFragmentDropResolver
    {
        public static List<(string itemId, int count)> Roll(NpcUnitLogicEntity npc, long finalDensity)
        {
            var result = new List<(string, int)>();
            if (npc?.NpcConfig == null || npc.NpcRecord == null)
            {
                return result;
            }

            var mindTag = npc.NpcConfig.MindTag;
            if (string.IsNullOrEmpty(mindTag))
            {
                return result;
            }

            var desireType = npc.NpcRecord.DesireDensityType;
            if (desireType == EDesireDensityType.None)
            {
                return result;
            }

            int tier = DesireDensityUtil.GetDensityTier(finalDensity);
            if (tier <= 0)
            {
                return result;
            }

            var table = CfgMgr.Cfgs?.TbMindFragmentPoolEntry;
            if (table?.DataList == null)
            {
                return result;
            }

            var candidates = new List<MindFragmentPoolEntry>();
            var weights = new List<float>();
            float total = 0f;

            foreach (var entry in table.DataList)
            {
                if (entry == null || entry.MindTag != mindTag || entry.DesireType != desireType)
                {
                    continue;
                }

                if (tier < entry.DensityTierMin || tier > entry.DensityTierMax)
                {
                    continue;
                }

                float w = entry.BaseWeight + entry.WeightPerDensity * finalDensity;
                if (w <= 0f)
                {
                    continue;
                }

                candidates.Add(entry);
                weights.Add(w);
                total += w;
            }

            if (candidates.Count == 0 || total <= 0f)
            {
                return result;
            }

            float roll = Random.Range(0f, total);
            float acc = 0f;
            MindFragmentPoolEntry chosen = candidates[candidates.Count - 1];
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += weights[i];
                if (roll < acc)
                {
                    chosen = candidates[i];
                    break;
                }
            }

            int count = chosen.CountMin;
            if (chosen.CountMax > chosen.CountMin)
            {
                count = Random.Range(chosen.CountMin, chosen.CountMax + 1);
            }

            if (!string.IsNullOrEmpty(chosen.ItemId) && count > 0)
            {
                result.Add((chosen.ItemId, count));
            }

            return result;
        }
    }
}
