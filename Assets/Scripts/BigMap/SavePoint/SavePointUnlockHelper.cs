using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Logic;
using My.Saving;

namespace My.Map
{
    public static class SavePointUnlockHelper
    {
        public static SavePoint GetCfg(string savePointId) =>
            string.IsNullOrEmpty(savePointId) ? null : CfgMgr.Cfgs?.TbSavePoint?.GetOrDefault(savePointId);

        public static SavePointUnlockPersist GetPersist(GameLogicManager glm, string savePointId) =>
            glm?.worldPersistState?.GetOrCreateSavePointUnlockState(savePointId);

        public static bool ShouldShowOnMap(GameLogicManager glm, string savePointId)
        {
            var cfg = GetCfg(savePointId);
            if (cfg == null || glm == null)
            {
                return false;
            }

            return glm.CheckCommonCondsAll(cfg.ShowUnlockConds);
        }

        public static bool IsActivated(GameLogicManager glm, string savePointId)
        {
            var cfg = CfgMgr.Cfgs.TbSavePoint.GetOrDefault(savePointId);
            if(cfg != null && cfg.DefaultActivated)
            {
                return true;
            }
            var st = glm?.worldPersistState?.GetSavePointUnlockStateOrNull(savePointId);
            return st != null && st.Unlocked;
        }

        public static bool IsTributeSubmitted(GameLogicManager glm, string savePointId)
        {
            var cfg = GetCfg(savePointId);
            if (cfg == null || !cfg.RequireTribute)
            {
                return true;
            }

            var persist = glm?.worldPersistState?.GetSavePointUnlockStateOrNull(savePointId);
            return persist != null && persist.TributeSubmitted;
        }

        public static bool ShouldBeVisible(GameLogicManager glm, string savePointId) =>
            ShouldShowOnMap(glm, savePointId);

        public static bool IsTributeComplete(SavePoint cfg, SavePointUnlockPersist persist)
        {
            if (cfg == null || !cfg.RequireTribute)
            {
                return true;
            }

            if (persist != null && persist.TributeSubmitted)
            {
                return true;
            }

            if (cfg.TributeCosts == null)
            {
                return false;
            }

            persist.TributePut ??= new Dictionary<string, long>();
            foreach (var c in cfg.TributeCosts)
            {
                if (c == null || string.IsNullOrEmpty(c.ItemId) || c.Count <= 0)
                {
                    continue;
                }

                persist.TributePut.TryGetValue(c.ItemId, out var put);
                if (put < c.Count)
                {
                    return false;
                }
            }

            return true;
        }

        public static void MarkActivated(GameLogicManager glm, string savePointId)
        {
            var persist = GetPersist(glm, savePointId);
            if (persist != null)
            {
                persist.Unlocked = true;
            }
        }

        public static bool TryActivate(GameLogicManager glm, string savePointId, out string failReason)
        {
            failReason = null;
            var cfg = GetCfg(savePointId);
            if (cfg == null)
            {
                failReason = "no_cfg";
                return false;
            }

            if (!ShouldShowOnMap(glm, savePointId))
            {
                failReason = "show_conds";
                return false;
            }

            if (IsActivated(glm, savePointId))
            {
                return true;
            }

            if (cfg.RequireTribute && !IsTributeSubmitted(glm, savePointId))
            {
                failReason = "need_tribute";
                return false;
            }

            MarkActivated(glm, savePointId);
            return true;
        }

        public static bool TrySubmitTribute(GameLogicManager glm, string savePointId, out string failReason)
        {
            failReason = null;
            var cfg = GetCfg(savePointId);
            if (cfg == null || !cfg.RequireTribute)
            {
                failReason = "no_tribute_cfg";
                return false;
            }

            if (!ShouldShowOnMap(glm, savePointId))
            {
                failReason = "show_conds";
                return false;
            }

            if (IsActivated(glm, savePointId) || IsTributeSubmitted(glm, savePointId))
            {
                return true;
            }

            var persist = GetPersist(glm, savePointId);
            if (persist == null)
            {
                failReason = "no_persist";
                return false;
            }

            var pdm = glm.playerDataManager;
            if (pdm == null)
            {
                failReason = "no_player";
                return false;
            }

            if (cfg.TributeCosts == null || cfg.TributeCosts.Count == 0)
            {
                failReason = "empty_tribute_costs";
                return false;
            }

            foreach (var c in cfg.TributeCosts)
            {
                if (c == null || string.IsNullOrEmpty(c.ItemId) || c.Count <= 0)
                {
                    continue;
                }

                if (!pdm.CheckHaveItem(c.ItemId, c.Count))
                {
                    failReason = "cost_" + c.ItemId;
                    return false;
                }
            }

            persist.TributePut ??= new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var c in cfg.TributeCosts)
            {
                if (c == null || string.IsNullOrEmpty(c.ItemId) || c.Count <= 0)
                {
                    continue;
                }

                pdm.CostItem(c.ItemId, c.Count);
                persist.TributePut[c.ItemId] = c.Count;
            }

            persist.TributeSubmitted = true;
            return true;
        }

        public static string BuildTributeRequirementText(SavePoint cfg)
        {
            if (cfg == null || !cfg.RequireTribute || cfg.TributeCosts == null || cfg.TributeCosts.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            foreach (var c in cfg.TributeCosts)
            {
                if (c == null || string.IsNullOrEmpty(c.ItemId))
                {
                    continue;
                }

                parts.Add($"{c.ItemId} x{c.Count}");
            }

            return string.Join(", ", parts);
        }

        public static List<SavePoint> GetActivatedSavePointConfigs(GameLogicManager glm)
        {
            var result = new List<SavePoint>();
            var table = CfgMgr.Cfgs?.TbSavePoint;
            if (table == null || glm?.worldPersistState == null)
            {
                return result;
            }

            foreach (var row in table.DataList)
            {
                if (row != null && IsActivated(glm, row.SavePointId))
                {
                    result.Add(row);
                }
            }

            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }

        public static bool IsAvailableForTeleport(GameLogicManager glm, string savePointId) =>
            ShouldShowOnMap(glm, savePointId) && IsActivated(glm, savePointId);

        // 旧档加载后补全 TributeSubmitted
        public static void NormalizePersistAfterLoad(SavePointUnlockPersist persist)
        {
            if (persist == null || persist.TributeSubmitted)
            {
                return;
            }

            var cfg = GetCfg(persist.SavePointId);
            if (cfg == null)
            {
                return;
            }

            if (!cfg.RequireTribute)
            {
                return;
            }

            if (persist.Unlocked || IsTributeComplete(cfg, persist))
            {
                persist.TributeSubmitted = true;
            }
        }
    }
}
