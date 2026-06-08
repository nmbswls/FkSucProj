using System;
using System.Collections.Generic;
using cfg.demo;

namespace My.Config
{
    // UI / 逻辑共用的升级节点状态
    public enum ERuneUpgradeNodeState
    {
        Locked = 0,
        Available = 1,
        Unlocked = 2,
    }

    public sealed class RuneUpgradeNodeView
    {
        public RuneUpgradeInfo Def;
        public ERuneUpgradeNodeState State;
    }

    // 符文升级项索引；RebuildCaches 由 CfgMgr.InitializeCfgs 调用
    public static class RuneUpgradeCatalog
    {
        static Dictionary<string, RuneUpgradeInfo> _byId = new(StringComparer.Ordinal);
        static Dictionary<string, List<RuneUpgradeInfo>> _byBaseRuneId = new(StringComparer.Ordinal);

        public static void RebuildCaches()
        {
            _byId = new Dictionary<string, RuneUpgradeInfo>(StringComparer.Ordinal);
            _byBaseRuneId = new Dictionary<string, List<RuneUpgradeInfo>>(StringComparer.Ordinal);

            var table = CfgMgr.Cfgs?.TbRuneUpgradeInfo;
            if (table?.DataList == null)
            {
                return;
            }

            foreach (var row in table.DataList)
            {
                if (row == null || string.IsNullOrEmpty(row.UpgradeId))
                {
                    continue;
                }

                _byId[row.UpgradeId] = row;

                if (string.IsNullOrEmpty(row.BaseRuneId))
                {
                    continue;
                }

                if (!_byBaseRuneId.TryGetValue(row.BaseRuneId, out var list))
                {
                    list = new List<RuneUpgradeInfo>();
                    _byBaseRuneId[row.BaseRuneId] = list;
                }

                list.Add(row);
            }

            foreach (var list in _byBaseRuneId.Values)
            {
                list.Sort(CompareUpgradeOrder);
            }
        }

        static int CompareUpgradeOrder(RuneUpgradeInfo a, RuneUpgradeInfo b)
        {
            int c = a.SortOrder.CompareTo(b.SortOrder);
            if (c != 0)
            {
                return c;
            }

            return string.Compare(a.UpgradeId, b.UpgradeId, StringComparison.Ordinal);
        }

        public static RuneUpgradeInfo GetOrDefault(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return null;
            }

            return _byId.TryGetValue(upgradeId, out var row) ? row : null;
        }

        public static IReadOnlyList<RuneUpgradeInfo> GetUpgradesForRune(string baseRuneId)
        {
            if (string.IsNullOrEmpty(baseRuneId))
            {
                return Array.Empty<RuneUpgradeInfo>();
            }

            if (_byBaseRuneId.TryGetValue(baseRuneId, out var list))
            {
                return list;
            }

            return Array.Empty<RuneUpgradeInfo>();
        }

        public static bool IsInitialUpgrade(RuneUpgradeInfo def)
        {
            if (def == null || string.IsNullOrEmpty(def.BaseRuneId))
            {
                return false;
            }

            var rune = RuneCatalog.GetOrDefault(def.BaseRuneId);
            return rune != null && rune.InitialUpgradeId == def.UpgradeId;
        }

        public static RuneUpgradeInfo GetUpgradeByLayoutSlot(string baseRuneId, int layoutSlot)
        {
            if (string.IsNullOrEmpty(baseRuneId) || layoutSlot <= 0)
            {
                return null;
            }

            foreach (var def in GetUpgradesForRune(baseRuneId))
            {
                if (def != null && def.LayoutSlot == layoutSlot)
                {
                    return def;
                }
            }

            return null;
        }

        public static bool ArePrerequisitesMet(
            RuneUpgradeInfo def,
            Func<string, bool> isUpgradeUnlocked)
        {
            if (def == null || def.PrerequisiteUpgradeIds == null || def.PrerequisiteUpgradeIds.Count == 0)
            {
                return true;
            }

            foreach (var pre in def.PrerequisiteUpgradeIds)
            {
                if (string.IsNullOrEmpty(pre))
                {
                    continue;
                }

                if (isUpgradeUnlocked == null || !isUpgradeUnlocked(pre))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
