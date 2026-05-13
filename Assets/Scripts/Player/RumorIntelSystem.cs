using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    // 大世界秘闻情报：购买、随机池展示、过期与进图消耗由 RumorIntelMapSpawn 处理
    public sealed class RumorIntelSystem
    {
        readonly Dictionary<string, MapRumorPersist> _byMap = new();

        public void LoadFrom(PlayerData playerData)
        {
            _byMap.Clear();
            if (playerData?.MapRumorByMapId == null)
            {
                return;
            }

            foreach (var kv in playerData.MapRumorByMapId)
            {
                _byMap[kv.Key] = CloneBlock(kv.Value);
            }
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

        public void PruneExpiredRumors(int settlementDayIndex)
        {
            foreach (var b in _byMap.Values)
            {
                b.ActiveIntel.RemoveAll(a => settlementDayIndex >= a.ExpireSettlementDay);
            }
        }

        public bool HasActiveRandomIntel(string mapId)
        {
            var b = GetOrCreateBlock(mapId);
            foreach (var a in b.ActiveIntel)
            {
                if (a.IsRandomKind)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsRumorActive(string mapId, string rumorId, int settlementDay)
        {
            var b = GetOrCreateBlock(mapId);
            foreach (var a in b.ActiveIntel)
            {
                if (a.RumorId == rumorId && settlementDay < a.ExpireSettlementDay)
                {
                    return true;
                }
            }

            return false;
        }

        void RollRandomOffers(string mapId, GameLogicManager glm)
        {
            var cfgGlobal = CfgMgr.Cfgs?.TbRumorGlobal?.GetOrDefault("default");
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

            block.RandomRollSettlementDay = glm.SettlementDayIndex;
        }

        public void EnsureRandomOffersForShop(string mapId, GameLogicManager glm)
        {
            if (glm == null || CfgMgr.Cfgs == null)
            {
                return;
            }

            PruneExpiredRumors(glm.SettlementDayIndex);
            if (HasActiveRandomIntel(mapId))
            {
                return;
            }

            var block = GetOrCreateBlock(mapId);
            if (block.RandomRollSettlementDay == glm.SettlementDayIndex && block.RandomOfferRumorIds.Count > 0)
            {
                return;
            }

            RollRandomOffers(mapId, glm);
        }

        public IReadOnlyList<string> GetRandomOfferIds(string mapId)
        {
            return GetOrCreateBlock(mapId).RandomOfferRumorIds;
        }

        public List<RumorIntel> ListPurchasableFixed(string mapId, GameLogicManager glm)
        {
            var result = new List<RumorIntel>();
            if (CfgMgr.Cfgs == null || glm == null)
            {
                return result;
            }

            var day = glm.SettlementDayIndex;
            foreach (var row in CfgMgr.Cfgs.TbRumorIntel.DataList)
            {
                if (row.IntelKind != ERumorIntelKind.Fixed)
                {
                    continue;
                }

                if (IsRumorActive(mapId, row.RumorId, day))
                {
                    continue;
                }

                if (!glm.CheckCommonCondsAll(row.AppearConds))
                {
                    continue;
                }

                result.Add(row);
            }

            return result;
        }

        public bool TryPurchase(string mapId, string rumorId, GameLogicManager glm, out string error)
        {
            error = null;
            if (glm?.playerDataManager == null || CfgMgr.Cfgs == null)
            {
                error = "rumor_no_context";
                return false;
            }

            var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(rumorId);
            if (def == null)
            {
                error = "rumor_unknown";
                return false;
            }

            var day = glm.SettlementDayIndex;
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

                EnsureRandomOffersForShop(mapId, glm);
                if (!block.RandomOfferRumorIds.Contains(rumorId))
                {
                    error = "rumor_not_in_offer";
                    return false;
                }
            }
            else
            {
                if (!glm.CheckCommonCondsAll(def.AppearConds))
                {
                    error = "rumor_cond_fail";
                    return false;
                }
            }

            if (!glm.playerDataManager.CheckHaveItem(def.CostItemId, def.CostCount))
            {
                error = "rumor_cost";
                return false;
            }

            var left = glm.playerDataManager.CostItem(def.CostItemId, def.CostCount);
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

        public List<RumorActiveEntry> GetActiveSnapshot(string mapId, int settlementDay)
        {
            var b = GetOrCreateBlock(mapId);
            return b.ActiveIntel.FindAll(a => settlementDay < a.ExpireSettlementDay);
        }

        public void ConsumeAllActiveForMap(string mapId, int settlementDay)
        {
            var b = GetOrCreateBlock(mapId);
            b.ActiveIntel.RemoveAll(a => settlementDay < a.ExpireSettlementDay);
        }
    }
}
