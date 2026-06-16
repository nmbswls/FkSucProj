using System.Collections.Generic;
using cfg.demo;
using UnityEngine;

namespace My.Config
{
    public static class JingYuanTypeCatalog
    {
        static Dictionary<string, List<JingYuanTypePoolEntry>> _poolEntries;

        public static void RebuildPoolIndex()
        {
            _poolEntries = new Dictionary<string, List<JingYuanTypePoolEntry>>();
            var table = CfgMgr.Cfgs?.TbJingYuanTypePoolEntry;
            if (table?.DataList == null)
            {
                return;
            }

            foreach (var entry in table.DataList)
            {
                if (entry == null || string.IsNullOrEmpty(entry.PoolId) || string.IsNullOrEmpty(entry.TypeId))
                {
                    continue;
                }

                if (!_poolEntries.TryGetValue(entry.PoolId, out var list))
                {
                    list = new List<JingYuanTypePoolEntry>();
                    _poolEntries[entry.PoolId] = list;
                }

                list.Add(entry);
            }
        }

        public static JingYuanTypeDef GetTypeDef(string typeId)
        {
            if (string.IsNullOrEmpty(typeId) || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbJingYuanTypeDef.GetOrDefault(typeId);
        }

        public static string GetMatchTag(string typeId)
        {
            var def = GetTypeDef(typeId);
            return string.IsNullOrEmpty(def?.MatchTag) ? null : def.MatchTag;
        }

        public static string RollTypeIdFromPool(string poolId)
        {
            if (string.IsNullOrEmpty(poolId))
            {
                return null;
            }

            if (_poolEntries == null)
            {
                RebuildPoolIndex();
            }

            if (_poolEntries == null || !_poolEntries.TryGetValue(poolId, out var entries) || entries.Count == 0)
            {
                return null;
            }

            int sum = 0;
            foreach (var entry in entries)
            {
                sum += Mathf.Max(0, entry.Weight);
            }

            if (sum <= 0)
            {
                return entries[0].TypeId;
            }

            int roll = Random.Range(0, sum);
            foreach (var entry in entries)
            {
                int w = Mathf.Max(0, entry.Weight);
                if (roll < w)
                {
                    return entry.TypeId;
                }

                roll -= w;
            }

            return entries[entries.Count - 1].TypeId;
        }
    }
}
