using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    // 绉橀椈鎯呮姤锛氬瓨妗ｅ湪 PlayerData.MapRumorByMapId锛涗笘鐣屾棩涓?GameLogicManager.SettlementDayIndex 瀵归綈
    public sealed class RumorIntelSystem : IPlayerSystem
    {
        GameLogicManager _glm;

        readonly Dictionary<string, MapRumorPersist> _byMap = new();

        int Day => _glm != null ? _glm.SettlementDayIndex : 0;

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _glm = ctx;
            _byMap.Clear();
            if (savingData?.PlayerData?.MapRumorByMapId == null)
            {
                return;
            }

            foreach (var kv in savingData.PlayerData.MapRumorByMapId)
            {
                _byMap[kv.Key] = CloneBlock(kv.Value);
            }
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void Tick(float dt)
        {
        }

        public void SaveTo(PlayerData playerData)
        {
            if (playerData == null)
            {
                return;
            }

            playerData.MapRumorByMapId = new Dictionary<string, MapRumorPersist>();
            foreach (var kv in _byMap)
            {
                playerData.MapRumorByMapId[kv.Key] = CloneBlock(kv.Value);
            }
        }

        static MapRumorPersist CloneBlock(MapRumorPersist src)
        {
            var d = new MapRumorPersist
            {
                RandomRollSettlementDay = src.RandomRollSettlementDay,
            };
            if (src.RandomOfferRumorIds != null)
            {
                d.RandomOfferRumorIds = new List<string>(src.RandomOfferRumorIds);
            }

            if (src.ActiveIntel != null)
            {
                foreach (var e in src.ActiveIntel)
                {
                    d.ActiveIntel.Add(new RumorActiveEntry
                    {
                        RumorId = e.RumorId,
                        PurchasedSettlementDay = e.PurchasedSettlementDay,
                        ExpireSettlementDay = e.ExpireSettlementDay,
                        IsRandomKind = e.IsRandomKind,
                        Revealed = e.Revealed,
                        Spawned = e.Spawned,
                        EventExpireSettlementDay = e.EventExpireSettlementDay,
                    });
                }
            }

            return d;
        }

        MapRumorPersist GetOrCreateBlock(string mapId)
        {
            if (!_byMap.TryGetValue(mapId, out var b))
            {
                b = new MapRumorPersist();
                _byMap[mapId] = b;
            }

            return b;
        }

        public void PruneExpiredRumors(int worldDay)
        {
            foreach (var b in _byMap.Values)
            {
                b.ActiveIntel.RemoveAll(a => !IsEntryActive(a, worldDay));
            }
        }

        static bool IsEntryActive(RumorActiveEntry entry, int worldDay)
        {
            if (entry == null)
            {
                return false;
            }

            var expireDay = entry.Spawned
                ? entry.EventExpireSettlementDay
                : entry.ExpireSettlementDay;
            return expireDay > worldDay;
        }

        static string ResolveAreaVariantId(string mapId)
        {
            var overlay = string.IsNullOrEmpty(mapId)
                ? null
                : CfgMgr.Cfgs?.TbAreaOverlayStateInfo?.GetOrDefault(mapId);
            return !string.IsNullOrEmpty(overlay?.VarId) ? overlay.VarId : mapId;
        }

        public static bool MatchesTargetMap(RumorIntel def, string mapId)
        {
            return def != null
                && (string.IsNullOrEmpty(def.TargetOverlayId)
                    || def.TargetOverlayId == mapId
                    || def.TargetOverlayId == ResolveAreaVariantId(mapId));
        }

        public bool HasActiveRandomIntel(string mapId)
        {
            var b = GetOrCreateBlock(mapId);
            foreach (var a in b.ActiveIntel)
            {
                if (a.IsRandomKind && IsEntryActive(a, Day))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsRumorActive(string mapId, string rumorId, int worldDay)
        {
            var b = GetOrCreateBlock(mapId);
            foreach (var a in b.ActiveIntel)
            {
                if (a.RumorId == rumorId && IsEntryActive(a, worldDay))
                {
                    return true;
                }
            }

            return false;
        }

        void RollRandomOffers(string mapId)
        {
            if (_glm == null || CfgMgr.Cfgs == null)
            {
                return;
            }

            var cfgGlobal = CfgMgr.Cfgs.TbRumorGlobal?.GetOrDefault("default");
            if (cfgGlobal == null)
            {
                return;
            }

            var poolId = string.IsNullOrEmpty(cfgGlobal.DefaultPoolId) ? "default" : cfgGlobal.DefaultPoolId;
            var k = Mathf.Max(1, cfgGlobal.RandomOfferCount);

            var poolIds = new List<string>();
            var weights = new List<int>();
            foreach (var row in CfgMgr.Cfgs.TbRumorRandomPool.DataList)
            {
                if (row.PoolId != poolId)
                {
                    continue;
                }

                var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(row.RumorId);
                if (def == null
                    || def.IntelKind != ERumorIntelKind.RandomPoolEntry
                    || !MatchesTargetMap(def, mapId)
                    || !_glm.CheckCommonCondsAll(def.AppearConds))
                {
                    continue;
                }

                poolIds.Add(row.RumorId);
                weights.Add(row.Weight);
            }

            var block = GetOrCreateBlock(mapId);
            block.RandomOfferRumorIds.Clear();

            for (var i = 0; i < k && poolIds.Count > 0; i++)
            {
                var sum = 0;
                for (var j = 0; j < weights.Count; j++)
                {
                    sum += Mathf.Max(0, weights[j]);
                }

                if (sum <= 0)
                {
                    break;
                }

                var r = Random.Range(0, sum);
                var acc = 0;
                var pick = 0;
                for (var j = 0; j < weights.Count; j++)
                {
                    acc += Mathf.Max(0, weights[j]);
                    if (r < acc)
                    {
                        pick = j;
                        break;
                    }
                }

                block.RandomOfferRumorIds.Add(poolIds[pick]);
                poolIds.RemoveAt(pick);
                weights.RemoveAt(pick);
            }

            block.RandomRollSettlementDay = Day;
        }

        public void EnsureRandomOffersForShop(string mapId)
        {
            if (_glm == null || CfgMgr.Cfgs == null)
            {
                return;
            }

            PruneExpiredRumors(Day);
            if (HasActiveRandomIntel(mapId))
            {
                return;
            }

            var block = GetOrCreateBlock(mapId);
            if (block.RandomRollSettlementDay == Day && block.RandomOfferRumorIds.Count > 0)
            {
                return;
            }

            RollRandomOffers(mapId);
        }

        public IReadOnlyList<string> GetRandomOfferIds(string mapId)
        {
            return GetOrCreateBlock(mapId).RandomOfferRumorIds;
        }

        public List<RumorIntel> ListPurchasableFixed(string mapId)
        {
            var result = new List<RumorIntel>();
            if (CfgMgr.Cfgs == null || _glm == null || string.IsNullOrEmpty(mapId))
            {
                return result;
            }

            var day = Day;
            foreach (var row in CfgMgr.Cfgs.TbRumorIntel.DataList)
            {
                if (row.IntelKind != ERumorIntelKind.Fixed)
                {
                    continue;
                }

                if (!MatchesTargetMap(row, mapId))
                {
                    continue;
                }

                if (IsRumorActive(mapId, row.RumorId, day))
                {
                    continue;
                }

                if (!_glm.CheckCommonCondsAll(row.AppearConds))
                {
                    continue;
                }

                result.Add(row);
            }

            return result;
        }

        public bool TryPurchase(string mapId, string rumorId, out string error)
        {
            error = null;
            if (_glm?.playerDataManager == null || CfgMgr.Cfgs == null)
            {
                error = "rumor_no_context";
                return false;
            }

            if (string.IsNullOrEmpty(mapId))
            {
                error = "rumor_no_map";
                return false;
            }

            var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(rumorId);
            if (def == null)
            {
                error = "rumor_unknown";
                return false;
            }

            if (!MatchesTargetMap(def, mapId))
            {
                error = "rumor_wrong_map";
                return false;
            }

            var day = Day;
            PruneExpiredRumors(day);
            if (IsRumorActive(mapId, rumorId, day))
            {
                error = "rumor_already_active";
                return false;
            }

            var block = GetOrCreateBlock(mapId);
            var isRandom = def.IntelKind == ERumorIntelKind.RandomPoolEntry;
            if (isRandom)
            {
                if (HasActiveRandomIntel(mapId))
                {
                    error = "rumor_random_slot_occupied";
                    return false;
                }

                EnsureRandomOffersForShop(mapId);
                if (!block.RandomOfferRumorIds.Contains(rumorId))
                {
                    error = "rumor_not_in_offer";
                    return false;
                }
            }
            else
            {
                if (!_glm.CheckCommonCondsAll(def.AppearConds))
                {
                    error = "rumor_cond_fail";
                    return false;
                }
            }

            var tradeCost = _glm.playerDataManager.ProgressionSystem?.HumanCivilization?.GetTradeCost(def.CostCount) ?? def.CostCount;
            if (!_glm.playerDataManager.CheckHaveItem(def.CostItemId, tradeCost))
            {
                error = "rumor_cost";
                return false;
            }

            var left = _glm.playerDataManager.CostItem(def.CostItemId, tradeCost);
            if (left != 0)
            {
                error = "rumor_cost";
                return false;
            }

            var entry = new RumorActiveEntry
            {
                RumorId = rumorId,
                PurchasedSettlementDay = day,
                ExpireSettlementDay = day + Mathf.Max(1, def.DurationDays),
                IsRandomKind = isRandom,
                Revealed = true,
            };
            block.ActiveIntel.Add(entry);
            if (isRandom)
            {
                block.RandomOfferRumorIds.Clear();
                block.RandomRollSettlementDay = -1;
            }

            return true;
        }

        public List<RumorActiveEntry> GetActiveSnapshot(string mapId)
        {
            var day = Day;
            PruneExpiredRumors(day);
            var b = GetOrCreateBlock(mapId);
            return b.ActiveIntel.FindAll(a => IsEntryActive(a, day));
        }

        public int MarkSpawned(string mapId, string rumorId, int eventExpireDays)
        {
            var b = GetOrCreateBlock(mapId);
            foreach (var entry in b.ActiveIntel)
            {
                if (entry.RumorId == rumorId)
                {
                    if (!entry.Spawned || entry.EventExpireSettlementDay <= 0)
                    {
                        entry.Spawned = true;
                        entry.EventExpireSettlementDay = Day + Mathf.Max(1, eventExpireDays);
                    }

                    return entry.EventExpireSettlementDay;
                }
            }

            return 0;
        }

        public void ConsumeActiveForMap(string mapId, IReadOnlyCollection<string> rumorIds)
        {
            if (rumorIds == null || rumorIds.Count == 0)
            {
                return;
            }

            var consumed = rumorIds as HashSet<string> ?? new HashSet<string>(rumorIds);
            var b = GetOrCreateBlock(mapId);
            b.ActiveIntel.RemoveAll(a => consumed.Contains(a.RumorId));
        }
    }
}

