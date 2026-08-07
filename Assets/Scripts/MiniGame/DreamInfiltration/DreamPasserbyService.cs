using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using My.Saving;
using UnityEngine;

namespace My.MiniGame.Dream
{
    // 抽象路人浅梦：区域仅作刷人条件，日刷落到具体 dream_passerby
    public static class DreamPasserbyService
    {
        // 初版固定每晚显示数量；可成长见 design，后续可挂属性
        public const int DefaultNightlyDisplayCount = 6;

        static DreamPasserbyDailyRefreshPersist Daily = new();

        public static void LoadFromSave(PlayerData pd)
        {
            Daily = CloneDaily(pd?.DreamPasserbyDailyRefresh);
        }

        public static void ApplyToSave(PlayerData pd)
        {
            if (pd == null) return;
            pd.DreamPasserbyDailyRefresh = CloneDaily(Daily);
        }

        public static void OnSettlementDayBalance(GameLogicManager glm)
        {
            RollDaily(glm, force: true);
        }

        public static void EnsureDailySynced(GameLogicManager glm)
        {
            if (glm == null) return;
            if (Daily.SettlementDayIndex != glm.SettlementDayIndex)
            {
                RollDaily(glm, force: true);
            }
        }

        public static IReadOnlyList<DreamPasserbyDailyEntryPersist> GetTodayEntries(GameLogicManager glm)
        {
            EnsureDailySynced(glm);
            return Daily.Entries ?? (IReadOnlyList<DreamPasserbyDailyEntryPersist>)Array.Empty<DreamPasserbyDailyEntryPersist>();
        }

        public static int GetNightlyDisplayCount(GameLogicManager glm)
        {
            // 预留：日后可读 EYCAttribute / 设施等级；初版固定
            return DefaultNightlyDisplayCount;
        }

        public static bool TryCreateGameplayContext(
            GameLogicManager glm,
            string passerbyId,
            out DreamGameplayContext ctx,
            out string failReason)
        {
            ctx = null;
            failReason = null;
            if (!AbstractGroupDreamService.IsDreamAllowedTonight(glm, out failReason))
            {
                return false;
            }

            EnsureDailySynced(glm);
            DreamPasserbyDailyEntryPersist entry = null;
            if (Daily.Entries != null)
            {
                foreach (var e in Daily.Entries)
                {
                    if (e != null && string.Equals(e.PasserbyId, passerbyId, StringComparison.Ordinal))
                    {
                        entry = e;
                        break;
                    }
                }
            }

            if (entry == null)
            {
                failReason = "not_today";
                return false;
            }

            var cfg = CfgMgr.Cfgs?.TbDreamPasserby?.GetOrDefault(passerbyId);
            if (cfg == null)
            {
                failReason = "missing_cfg";
                return false;
            }

            ctx = new DreamGameplayContext
            {
                ThemeId = cfg.DreamThemeId ?? string.Empty,
                ThemeDisplayName = string.IsNullOrEmpty(cfg.DreamThemeDisplayName)
                    ? "浅梦"
                    : cfg.DreamThemeDisplayName,
                CoreMaxHp = Math.Max(1, cfg.CoreMaxHp),
                MaxHp = Math.Max(1, cfg.PlayerMaxHp),
                BulletDamage = Math.Max(1, cfg.BulletDamage),
                AoeDamage = Math.Max(1, cfg.AoeDamage),
                ProjectileDamage = Math.Max(1, cfg.ProjectileDamage),
                EntrySource = DreamEntrySourceKind.PasserbyEntry,
                PasserbyId = cfg.PasserbyId,
                PasserbyRegionId = entry.RegionId ?? string.Empty,
            };
            return true;
        }

        public static string ApplySettlement(GameLogicManager glm, DreamSettlementPayload payload)
        {
            if (glm == null || payload == null || !payload.Won) return string.Empty;
            if (payload.EntrySource != DreamEntrySourceKind.PasserbyEntry) return string.Empty;
            if (string.IsNullOrEmpty(payload.PasserbyId)) return string.Empty;

            var cfg = CfgMgr.Cfgs?.TbDreamPasserby?.GetOrDefault(payload.PasserbyId);
            if (cfg == null) return string.Empty;

            var parts = new List<string>(3);
            if (cfg.RewardDesireShard > 0)
            {
                glm.playerDataManager?.GiveItemToPlayer("desire_shard", cfg.RewardDesireShard);
                parts.Add($"欲望碎片×{cfg.RewardDesireShard}");
            }

            if (cfg.RewardJingyuan > 0)
            {
                glm.worldPersistState?.AddJingYuanPoolStored(cfg.RewardJingyuan);
                parts.Add($"精元池+{cfg.RewardJingyuan}");
            }

            if (cfg.RewardFallenBaseAmount > 0)
            {
                FallenPopulationService.AddBaseAmount(glm.playerDataManager, cfg.RewardFallenBaseAmount);
                parts.Add($"基础沉沦+{cfg.RewardFallenBaseAmount}");
            }

            return parts.Count > 0 ? "浅梦收获：" + string.Join(" / ", parts) : string.Empty;
        }

        static void RollDaily(GameLogicManager glm, bool force)
        {
            if (glm == null) return;
            var day = glm.SettlementDayIndex;
            if (!force && Daily.SettlementDayIndex == day) return;

            Daily.SettlementDayIndex = day;
            Daily.Entries ??= new List<DreamPasserbyDailyEntryPersist>();
            Daily.Entries.Clear();

            var unlockedRegions = CollectUnlockedRegions(glm);
            if (unlockedRegions.Count == 0)
            {
                Debug.Log("[DreamPasserby] No unlocked regions; empty nightly pool.");
                return;
            }

            var unlockedRegionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in unlockedRegions) unlockedRegionIds.Add(r.RegionId);

            var pool = new List<DreamPasserby>();
            var table = CfgMgr.Cfgs?.TbDreamPasserby;
            if (table?.DataList != null)
            {
                foreach (var p in table.DataList)
                {
                    if (p == null || p.RefreshWeight <= 0) continue;
                    if (!PasserbyTouchesUnlockedRegion(p, unlockedRegionIds)) continue;
                    pool.Add(p);
                }
            }

            if (pool.Count == 0)
            {
                Debug.Log("[DreamPasserby] Passerby pool empty after region filter.");
                return;
            }

            var count = Math.Min(GetNightlyDisplayCount(glm), pool.Count);
            var slots = BuildMapSlots(count + 1); // +1 预留给团体位，路人先占前 count 个
            var picked = WeightedPickUnique(pool, count);
            for (var i = 0; i < picked.Count; i++)
            {
                var pb = picked[i];
                var regionId = PickRegionForPasserby(pb, unlockedRegions);
                var slot = slots[i % slots.Count];
                Daily.Entries.Add(new DreamPasserbyDailyEntryPersist
                {
                    PasserbyId = pb.PasserbyId,
                    RegionId = regionId,
                    AnchorX = slot.x,
                    AnchorY = slot.y,
                });
            }

            Debug.Log($"[DreamPasserby] Daily rolled {Daily.Entries.Count} passerby.");
        }

        static List<DreamPasserbyRegion> CollectUnlockedRegions(GameLogicManager glm)
        {
            var list = new List<DreamPasserbyRegion>();
            var table = CfgMgr.Cfgs?.TbDreamPasserbyRegion;
            if (table?.DataList == null) return list;
            foreach (var r in table.DataList)
            {
                if (r == null || !r.Enabled || r.RegionWeight <= 0) continue;
                if (!glm.CheckCommonCondsAll(r.UnlockConds)) continue;
                list.Add(r);
            }

            return list;
        }

        static bool PasserbyTouchesUnlockedRegion(DreamPasserby p, HashSet<string> unlocked)
        {
            if (p.RegionIds == null || p.RegionIds.Count == 0) return true;
            foreach (var id in p.RegionIds)
            {
                if (!string.IsNullOrEmpty(id) && unlocked.Contains(id)) return true;
            }

            return false;
        }

        static string PickRegionForPasserby(DreamPasserby p, List<DreamPasserbyRegion> unlockedRegions)
        {
            var candidates = new List<DreamPasserbyRegion>();
            foreach (var r in unlockedRegions)
            {
                if (p.RegionIds == null || p.RegionIds.Count == 0 || p.RegionIds.Contains(r.RegionId))
                    candidates.Add(r);
            }

            if (candidates.Count == 0) return unlockedRegions[0].RegionId;
            var sum = 0;
            foreach (var c in candidates) sum += Math.Max(1, c.RegionWeight);
            var roll = UnityEngine.Random.Range(0, sum);
            var acc = 0;
            foreach (var c in candidates)
            {
                acc += Math.Max(1, c.RegionWeight);
                if (roll < acc) return c.RegionId;
            }

            return candidates[candidates.Count - 1].RegionId;
        }

        static List<DreamPasserby> WeightedPickUnique(List<DreamPasserby> pool, int count)
        {
            var result = new List<DreamPasserby>(count);
            var remain = new List<DreamPasserby>(pool);
            for (var n = 0; n < count && remain.Count > 0; n++)
            {
                var sum = 0;
                foreach (var p in remain) sum += Math.Max(1, p.RefreshWeight);
                var roll = UnityEngine.Random.Range(0, sum);
                var acc = 0;
                var pickIndex = remain.Count - 1;
                for (var i = 0; i < remain.Count; i++)
                {
                    acc += Math.Max(1, remain[i].RefreshWeight);
                    if (roll < acc)
                    {
                        pickIndex = i;
                        break;
                    }
                }

                result.Add(remain[pickIndex]);
                remain.RemoveAt(pickIndex);
            }

            return result;
        }

        // 地图区归一化锚点（避开右上角色区、左下过低）
        static List<Vector2> BuildMapSlots(int count)
        {
            var slots = new List<Vector2>();
            var cols = 3;
            var rows = 3;
            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < cols; x++)
                {
                    var ax = 0.22f + x * 0.22f;
                    var ay = 0.28f + y * 0.18f;
                    // 避开右上角色区
                    if (ax > 0.72f && ay > 0.62f) continue;
                    slots.Add(new Vector2(ax, ay));
                }
            }

            // 打乱
            for (var i = 0; i < slots.Count; i++)
            {
                var j = UnityEngine.Random.Range(i, slots.Count);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            while (slots.Count < Math.Max(1, count))
            {
                slots.Add(new Vector2(
                    UnityEngine.Random.Range(0.2f, 0.7f),
                    UnityEngine.Random.Range(0.25f, 0.7f)));
            }

            return slots;
        }

        static DreamPasserbyDailyRefreshPersist CloneDaily(DreamPasserbyDailyRefreshPersist src)
        {
            src ??= new DreamPasserbyDailyRefreshPersist();
            var d = new DreamPasserbyDailyRefreshPersist
            {
                SettlementDayIndex = src.SettlementDayIndex,
                Entries = new List<DreamPasserbyDailyEntryPersist>(),
            };
            if (src.Entries != null)
            {
                foreach (var e in src.Entries)
                {
                    if (e == null) continue;
                    d.Entries.Add(new DreamPasserbyDailyEntryPersist
                    {
                        PasserbyId = e.PasserbyId ?? string.Empty,
                        RegionId = e.RegionId ?? string.Empty,
                        AnchorX = e.AnchorX,
                        AnchorY = e.AnchorY,
                    });
                }
            }

            return d;
        }
    }
}
