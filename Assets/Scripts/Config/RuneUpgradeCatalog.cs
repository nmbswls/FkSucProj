using System;
using System.Collections.Generic;
using System.Linq;
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
        public string LockReason;
    }

    // 符文升级项配置索引（demo_tbruneupgradeinfo.json）
    public static class RuneUpgradeCatalog
    {
        static Dictionary<string, RuneUpgradeInfo> _byId;
        static Dictionary<string, List<RuneUpgradeInfo>> _byBaseRuneId;
        static bool _built;

        static void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            _byId = new Dictionary<string, RuneUpgradeInfo>(StringComparer.Ordinal);
            _byBaseRuneId = new Dictionary<string, List<RuneUpgradeInfo>>(StringComparer.Ordinal);

            var table = CfgMgr.Cfgs?.TbRuneUpgradeInfo;
            if (table?.DataList == null)
            {
                _built = true;
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

            _built = true;
        }

        public static void Rebuild()
        {
            _built = false;
            _byId = null;
            _byBaseRuneId = null;
            EnsureBuilt();
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

            EnsureBuilt();
            return _byId.TryGetValue(upgradeId, out var row) ? row : null;
        }

        public static IReadOnlyList<RuneUpgradeInfo> GetUpgradesForRune(string baseRuneId)
        {
            if (string.IsNullOrEmpty(baseRuneId))
            {
                return Array.Empty<RuneUpgradeInfo>();
            }

            EnsureBuilt();
            if (_byBaseRuneId.TryGetValue(baseRuneId, out var list))
            {
                return list;
            }

            return Array.Empty<RuneUpgradeInfo>();
        }

        public static IReadOnlyList<string> GetBranchIdsForRune(string baseRuneId)
        {
            var upgrades = GetUpgradesForRune(baseRuneId);
            if (upgrades.Count == 0)
            {
                return Array.Empty<string>();
            }

            var branches = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var u in upgrades)
            {
                if (string.IsNullOrEmpty(u.BranchId) || !seen.Add(u.BranchId))
                {
                    continue;
                }

                branches.Add(u.BranchId);
            }

            return branches;
        }

        public static IReadOnlyList<RuneUpgradeInfo> GetUpgradesInBranch(string baseRuneId, string branchId)
        {
            if (string.IsNullOrEmpty(branchId))
            {
                return GetUpgradesForRune(baseRuneId)
                    .Where(x => string.IsNullOrEmpty(x.BranchId))
                    .ToList();
            }

            return GetUpgradesForRune(baseRuneId)
                .Where(x => x.BranchId == branchId)
                .ToList();
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
