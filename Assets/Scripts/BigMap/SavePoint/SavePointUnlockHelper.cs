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

        public static bool IsFormallyUnlocked(GameLogicManager glm, string savePointId)
        {
            var st = glm?.worldPersistState?.GetSavePointUnlockStateOrNull(savePointId);
            return st != null && st.Unlocked;
        }

        public static bool CanShowAndInteract(GameLogicManager glm, string savePointId)
        {
            if (IsFormallyUnlocked(glm, savePointId))
            {
                return true;
            }

            var cfg = GetCfg(savePointId);
            if (cfg == null || glm == null)
            {
                return false;
            }

            return glm.CheckCommonCondsAll(cfg.ShowUnlockConds);
        }

        public static bool ShouldBeVisible(GameLogicManager glm, string savePointId) =>
            CanShowAndInteract(glm, savePointId);

        public static bool IsTributeComplete(SavePoint cfg, SavePointUnlockPersist persist)
        {
            if (cfg == null || !cfg.RequireTribute || cfg.TributeCosts == null)
            {
                return true;
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

        public static void MarkFormallyUnlocked(GameLogicManager glm, string savePointId)
        {
            var persist = GetPersist(glm, savePointId);
            if (persist != null)
            {
                persist.Unlocked = true;
            }
        }

        public static bool TryUnlockOnInteract(GameLogicManager glm, string savePointId, out string failReason)
        {
            failReason = null;
            var cfg = GetCfg(savePointId);
            if (cfg == null)
            {
                failReason = "no_cfg";
                return false;
            }

            if (!CanShowAndInteract(glm, savePointId))
            {
                failReason = "show_conds";
                return false;
            }

            if (IsFormallyUnlocked(glm, savePointId))
            {
                return true;
            }

            if (cfg.RequireTribute)
            {
                failReason = "need_tribute";
                return false;
            }

            MarkFormallyUnlocked(glm, savePointId);
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

            if (!CanShowAndInteract(glm, savePointId))
            {
                failReason = "show_conds";
                return false;
            }

            if (IsFormallyUnlocked(glm, savePointId))
            {
                return true;
            }

            var persist = GetPersist(glm, savePointId);
            if (persist == null)
            {
                failReason = "no_persist";
                return false;
            }

            persist.TributePut ??= new Dictionary<string, long>();
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

                persist.TributePut.TryGetValue(c.ItemId, out var already);
                long need = c.Count - already;
                if (need <= 0)
                {
                    continue;
                }

                if (!pdm.CheckHaveItem(c.ItemId, need))
                {
                    failReason = "cost_" + c.ItemId;
                    return false;
                }
            }

            foreach (var c in cfg.TributeCosts)
            {
                if (c == null || string.IsNullOrEmpty(c.ItemId) || c.Count <= 0)
                {
                    continue;
                }

                persist.TributePut.TryGetValue(c.ItemId, out var already);
                long need = c.Count - already;
                if (need <= 0)
                {
                    continue;
                }

                pdm.CostItem(c.ItemId, need);
                persist.TributePut[c.ItemId] = already + need;
            }

            if (IsTributeComplete(cfg, persist))
            {
                persist.Unlocked = true;
            }

            return true;
        }

        public static string BuildTributeProgressText(SavePoint cfg, SavePointUnlockPersist persist)
        {
            if (cfg == null || !cfg.RequireTribute || cfg.TributeCosts == null || cfg.TributeCosts.Count == 0)
            {
                return string.Empty;
            }

            persist.TributePut ??= new Dictionary<string, long>();
            var parts = new List<string>();
            foreach (var c in cfg.TributeCosts)
            {
                if (c == null || string.IsNullOrEmpty(c.ItemId))
                {
                    continue;
                }

                persist.TributePut.TryGetValue(c.ItemId, out var put);
                parts.Add($"{c.ItemId} {put}/{c.Count}");
            }

            return string.Join(", ", parts);
        }

        public static List<SavePoint> GetFormallyUnlockedConfigs(GameLogicManager glm)
        {
            var result = new List<SavePoint>();
            var table = CfgMgr.Cfgs?.TbSavePoint;
            if (table == null || glm?.worldPersistState == null)
            {
                return result;
            }

            foreach (var row in table.DataList)
            {
                if (row != null && IsFormallyUnlocked(glm, row.SavePointId))
                {
                    result.Add(row);
                }
            }

            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }

        public static List<SavePoint> GetUnlockedForMap(GameLogicManager glm, string mapId)
        {
            var result = new List<SavePoint>();
            if (string.IsNullOrEmpty(mapId) || glm == null)
            {
                return result;
            }

            foreach (var cfg in GetFormallyUnlockedConfigs(glm))
            {
                if (cfg?.ShowMapId == null)
                {
                    continue;
                }

                foreach (var mid in cfg.ShowMapId)
                {
                    if (mid == mapId)
                    {
                        result.Add(cfg);
                        break;
                    }
                }
            }

            return result;
        }

        //public static bool TryTeleportToSavePoint(GameLogicManager glm, string savePointId, out string failReason)
        //{
        //    failReason = null;
        //    if (glm == null)
        //    {
        //        failReason = "no_glm";
        //        return false;
        //    }

        //    var cfg = GetCfg(savePointId);
        //    if (cfg == null)
        //    {
        //        failReason = "no_cfg";
        //        return false;
        //    }

        //    if (!IsFormallyUnlocked(glm, savePointId))
        //    {
        //        failReason = "locked";
        //        return false;
        //    }

        //    var mapId = cfg.TeleportMapId;
        //    var named = cfg.TeleportNamedPoint;
        //    if (string.IsNullOrEmpty(mapId))
        //    {
        //        failReason = "no_teleport_map";
        //        return false;
        //    }

        //    if (string.IsNullOrEmpty(named))
        //    {
        //        named = "default";
        //    }

        //    glm.PreparePlayerSwitchArea(mapId, true, targetPoint: named);
        //    return true;
        //}
    }
}
