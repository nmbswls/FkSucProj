using System.Collections.Generic;
using cfg.demo;
using My.Player;
using UnityEngine;

namespace My.Config
{
    public static class JingYuanCodexCatalog
    {
        static Dictionary<string, List<JingYuanCodexDef>> _defsByTag;
        public static void RebuildTagIndex()
        {
            _defsByTag = new Dictionary<string, List<JingYuanCodexDef>>();
            var table = CfgMgr.Cfgs?.TbJingYuanCodexDef;
            if (table?.DataList == null)
            {
                return;
            }

            foreach (var def in table.DataList)
            {
                if (def == null || string.IsNullOrEmpty(def.MatchTag))
                {
                    continue;
                }

                if (!_defsByTag.TryGetValue(def.MatchTag, out var list))
                {
                    list = new List<JingYuanCodexDef>();
                    _defsByTag[def.MatchTag] = list;
                }

                list.Add(def);
            }

            foreach (var list in _defsByTag.Values)
            {
                list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            }
        }

        public static JingYuanCodexDef GetDef(string codexId)
        {
            if (string.IsNullOrEmpty(codexId) || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbJingYuanCodexDef.GetOrDefault(codexId);
        }

        public static IReadOnlyList<JingYuanCodexDef> GetDefsByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return System.Array.Empty<JingYuanCodexDef>();
            }

            if (_defsByTag == null)
            {
                RebuildTagIndex();
            }

            if (_defsByTag != null && _defsByTag.TryGetValue(tag, out var list))
            {
                return list;
            }

            return System.Array.Empty<JingYuanCodexDef>();
        }

        public static int ResolveLevel(string codexId, int extractCount, long totalAmount)
        {
            if (CfgMgr.Cfgs == null || string.IsNullOrEmpty(codexId))
            {
                return 0;
            }

            int resolved = 0;
            foreach (var row in CfgMgr.Cfgs.TbJingYuanCodexLevel.DataList)
            {
                if (row == null || row.CodexId != codexId)
                {
                    continue;
                }

                if (extractCount >= row.NeedExtractCount && totalAmount >= row.NeedTotalAmount)
                {
                    if (row.Level > resolved)
                    {
                        resolved = row.Level;
                    }
                }
            }

            return resolved;
        }

        public static void SumStatBonusesUpToLevel(string codexId, int level, StatMap target)
        {
            if (target == null || CfgMgr.Cfgs == null || string.IsNullOrEmpty(codexId) || level <= 0)
            {
                return;
            }

            for (int lv = 1; lv <= level; lv++)
            {
                var row = CfgMgr.Cfgs.TbJingYuanCodexLevel.Get(codexId, lv);
                if (row?.StatBonuses == null)
                {
                    continue;
                }

                foreach (var bonus in row.StatBonuses)
                {
                    target.Add(bonus.AttrId, bonus.Val);
                }
            }
        }

        public static string GetPickupItemIdByMatchTag(string matchTag)
        {
            var defs = GetDefsByTag(matchTag);
            if (defs.Count == 0)
            {
                return null;
            }

            var def = defs[0];
            if (!string.IsNullOrEmpty(def.PickupItemId))
            {
                return def.PickupItemId;
            }

            return string.IsNullOrEmpty(def.CodexId) ? null : $"j_drop_{def.CodexId}";
        }
    }
}
