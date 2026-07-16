using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Home;
using My.Saving;
using My.Player.Bag;
using UnityEngine;

namespace My.Player
{
    public enum HumanTechNodeVisualState
    {
        Locked,
        Unlockable,
        InsufficientCost,
        Unlocked,
    }

    public sealed class HumanCivilizationSystem
    {
        readonly Dictionary<int, int> _techLevels = new();
        readonly Dictionary<EHumanCivilizationAttribute, long> _talentBonuses = new();
        readonly List<MarketListing> _marketListings = new();
        GameLogicManager _logic;

        public event Action<int> OnCivilizationLevelChanged;
        public event Action<int> OnTechNodeChanged;
        public event Action OnMarketChanged;

        public void Initialize(GameLogicManager logic, SaveData savingData)
        {
            _logic = logic;
            _techLevels.Clear();
            _talentBonuses.Clear();
            _marketListings.Clear();
            _lastMarketSettlementDay = savingData?.HumanCivilization?.LastMarketSettlementDay ?? -1;
            _lastMarketGoldEarned = savingData?.HumanCivilization?.LastMarketGoldEarned ?? 0;
            _lastMarketSoldItemCount = savingData?.HumanCivilization?.LastMarketSoldItemCount ?? 0;
            var entries = savingData?.HumanCivilization?.TechNodes;

            foreach (var entry in entries ?? new List<HumanTechNodeLevelPersist>())
            {
                if (entry != null && entry.NodeId > 0 && entry.Level > 0)
                {
                    _techLevels[entry.NodeId] = entry.Level;
                }
            }

            foreach (var entry in savingData?.HumanCivilization?.MarketListings ?? new List<MarketListingPersist>())
            {
                if (entry == null || string.IsNullOrEmpty(entry.BuildingId)
                    || string.IsNullOrEmpty(entry.ItemId) || entry.Count <= 0)
                {
                    continue;
                }

                _marketListings.Add(new MarketListing
                {
                    BuildingId = entry.BuildingId,
                    LogicAreaId = entry.LogicAreaId ?? string.Empty,
                    ItemId = entry.ItemId,
                    Count = entry.Count,
                    ItemInstanceId = entry.ItemInstanceId,
                    InstanceInfo = entry.InstanceInfo,
                    ListedSettlementDay = entry.ListedSettlementDay,
                });
            }

            _transportPendingItems.Clear();
            foreach (var entry in savingData?.HumanCivilization?.TransportPendingItems
                     ?? new List<TransportPendingItemPersist>())
            {
                if (entry == null || string.IsNullOrEmpty(entry.ItemId) || entry.Count <= 0)
                {
                    continue;
                }

                _transportPendingItems.Add(CloneTransportPending(entry));
            }

            _lastTransportSettlementDay = savingData?.HumanCivilization?.LastTransportSettlementDay ?? -1;
            _lastTransportRecoveredStackCount = savingData?.HumanCivilization?.LastTransportRecoveredStackCount ?? 0;
            _lastTransportLostStackCount = savingData?.HumanCivilization?.LastTransportLostStackCount ?? 0;
        }

        public void SaveTo(SaveData savingData)
        {
            if (savingData == null)
            {
                return;
            }

            savingData.HumanCivilization ??= new HumanCivilizationPersist();
            savingData.HumanCivilization.TechNodes ??= new List<HumanTechNodeLevelPersist>();
            savingData.HumanCivilization.TechNodes.Clear();
            foreach (var pair in _techLevels)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                savingData.HumanCivilization.TechNodes.Add(new HumanTechNodeLevelPersist
                {
                    NodeId = pair.Key,
                    Level = pair.Value,
                });
            }

            savingData.HumanCivilization.MarketListings ??= new List<MarketListingPersist>();
            savingData.HumanCivilization.MarketListings.Clear();
            foreach (var listing in _marketListings)
            {
                savingData.HumanCivilization.MarketListings.Add(new MarketListingPersist
                {
                    BuildingId = listing.BuildingId,
                    LogicAreaId = listing.LogicAreaId,
                    ItemId = listing.ItemId,
                    Count = listing.Count,
                    ItemInstanceId = listing.ItemInstanceId,
                    InstanceInfo = listing.InstanceInfo?.Clone(),
                    ListedSettlementDay = listing.ListedSettlementDay,
                });
            }
            savingData.HumanCivilization.LastMarketSettlementDay = _lastMarketSettlementDay;
            savingData.HumanCivilization.LastMarketGoldEarned = _lastMarketGoldEarned;
            savingData.HumanCivilization.LastMarketSoldItemCount = _lastMarketSoldItemCount;

            savingData.HumanCivilization.TransportPendingItems ??= new List<TransportPendingItemPersist>();
            savingData.HumanCivilization.TransportPendingItems.Clear();
            foreach (var pending in _transportPendingItems)
            {
                savingData.HumanCivilization.TransportPendingItems.Add(new TransportPendingItemPersist
                {
                    RegionKey = pending.RegionKey,
                    HomeLogicAreaId = pending.HomeLogicAreaId,
                    SourceOverlayId = pending.SourceOverlayId,
                    DepositedSettlementDay = pending.DepositedSettlementDay,
                    ItemId = pending.ItemId,
                    Count = pending.Count,
                    ItemInstanceId = pending.ItemInstanceId,
                    InstanceInfo = pending.InstanceInfo?.Clone(),
                });
            }

            savingData.HumanCivilization.LastTransportSettlementDay = _lastTransportSettlementDay;
            savingData.HumanCivilization.LastTransportRecoveredStackCount = _lastTransportRecoveredStackCount;
            savingData.HumanCivilization.LastTransportLostStackCount = _lastTransportLostStackCount;
        }

        public sealed class MarketListing
        {
            public string BuildingId;
            public string LogicAreaId;
            public string ItemId;
            public long Count;
            public long ItemInstanceId;
            public ItemInstanceInfo InstanceInfo;
            public int ListedSettlementDay;
        }

        public sealed class MarketSettlementResult
        {
            public int SoldItemCount;
            public long GoldEarned;
            public int UnsoldItemCount;
        }

        public sealed class TransportPendingItem
        {
            public string RegionKey;
            public string HomeLogicAreaId;
            public string SourceOverlayId;
            public int DepositedSettlementDay = -1;
            public string ItemId;
            public long Count;
            public long ItemInstanceId;
            public ItemInstanceInfo InstanceInfo;
        }

        public sealed class TransportSettlementResult
        {
            public Dictionary<string, long> RecoveredOutputs = new();
            public int RecoveredStackCount;
            public int LostStackCount;
        }

        readonly List<MarketListing> _marketListingScratch = new();
        readonly List<TransportPendingItem> _transportPendingItems = new();
        readonly List<TransportPendingItem> _transportPendingScratch = new();
        int _lastMarketSettlementDay = -1;
        long _lastMarketGoldEarned;
        int _lastMarketSoldItemCount;
        int _lastTransportSettlementDay = -1;
        int _lastTransportRecoveredStackCount;
        int _lastTransportLostStackCount;

        public IReadOnlyList<MarketListing> MarketListings => _marketListings;
        public int GetMarketSlotCapacity(string buildingId, string logicAreaId)
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                return 0;
            }

            var bonus = _logic?.townFacilityDevelopmentSystem?.GetBuildingAttribute(
                logicAreaId, buildingId, EBuildingAttribute.MarketSlotCount) ?? 0;
            return Math.Max(1, 3 + (int)Math.Max(0, bonus));
        }
        public IReadOnlyList<TransportPendingItem> TransportPendingItems => _transportPendingItems;
        public int LastMarketSettlementDay => _lastMarketSettlementDay;
        public long LastMarketGoldEarned => _lastMarketGoldEarned;
        public int LastMarketSoldItemCount => _lastMarketSoldItemCount;
        public int LastTransportSettlementDay => _lastTransportSettlementDay;
        public int LastTransportRecoveredStackCount => _lastTransportRecoveredStackCount;
        public int LastTransportLostStackCount => _lastTransportLostStackCount;

        public sealed class MarketPricePreview
        {
            public long BasePrice;
            public long AffixPrice;
            public long LinkPrice;
            public int SaleChancePercent;
            public int FacilityPriceBonusPercent;
            public int CivilizationPriceBonusPercent;
            public long UnitPrice;
            public long ExpectedUnitRevenue;
            public IReadOnlyList<string> PriceBreakdown = Array.Empty<string>();
        }

        public MarketPricePreview GetMarketPricePreview(MarketListing listing)
        {
            var item = My.Config.ItemCatalog.GetItemDef(listing?.ItemId);
            if (item == null)
            {
                return new MarketPricePreview();
            }

            var priceBonus = _logic?.townFacilityDevelopmentSystem?.GetBuildingAttribute(
                listing.LogicAreaId, listing.BuildingId, EBuildingAttribute.MarketPriceBonus) ?? 0;
            var civilizationPriceBonus = GetTechEffectValue(EHumanCivilizationAttribute.MarketSalePriceBonus);
            var chanceBonus = _logic?.townFacilityDevelopmentSystem?.GetBuildingAttribute(
                listing.LogicAreaId, listing.BuildingId, EBuildingAttribute.MarketSaleChanceBonus) ?? 0;
            var chance = Mathf.Clamp01(0.5f + chanceBonus * 0.05f);
            var stack = new My.ItemStack(listing.ItemId, listing.Count)
            {
                ItemInstanceId = listing.ItemInstanceId,
                InstanceInfo = listing.InstanceInfo?.Clone(),
            };
            var affixPrice = HumanWeaponCatalog.GetAffixMarketValue(stack)
                + HumanArmarCatalog.GetAffixMarketValue(stack);
            var rawUnitPrice = item.MarketBasePrice + affixPrice;
            var unitPrice = Math.Max(1L, (long)Math.Floor(rawUnitPrice
                * (1d + Math.Max(0, priceBonus) * 0.1d + Math.Max(0, civilizationPriceBonus) * 0.01d)));
            var breakdown = new List<string> { $"物品基础价：{item.MarketBasePrice}" };
            breakdown.AddRange(HumanWeaponCatalog.GetAffixMarketBreakdown(stack));
            breakdown.AddRange(HumanArmarCatalog.GetAffixMarketBreakdown(stack));
            if (civilizationPriceBonus > 0)
                breakdown.Add($"文明商贸加成：+{civilizationPriceBonus}%");
            if (priceBonus > 0)
            {
                breakdown.Add($"建筑售价倍率：+{priceBonus * 10}%");
            }

            return new MarketPricePreview
            {
                BasePrice = item.MarketBasePrice,
                AffixPrice = affixPrice,
                LinkPrice = Math.Max(0, affixPrice - HumanWeaponCatalog.GetAffixMarketBaseValue(stack)),
                SaleChancePercent = Mathf.RoundToInt(chance * 100f),
                FacilityPriceBonusPercent = (int)Math.Max(0, priceBonus * 10),
                CivilizationPriceBonusPercent = (int)Math.Max(0, civilizationPriceBonus),
                UnitPrice = unitPrice,
                ExpectedUnitRevenue = (long)Math.Floor(unitPrice * chance),
                PriceBreakdown = breakdown,
            };
        }

        public bool TryListItem(string buildingId, string logicAreaId, IItemContainer source, int sourceIndex, long count, out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(buildingId) || source == null || !source.IsSlotIdxValid(sourceIndex))
            {
                failReason = "invalid_market_listing_source";
                return false;
            }

            var sourceStack = source.GetItemByIdx(sourceIndex);
            if (sourceStack == null || sourceStack.IsEmpty || count <= 0 || count > sourceStack.Count)
            {
                failReason = "invalid_market_listing_count";
                return false;
            }

            var item = My.Config.ItemCatalog.GetItemDef(sourceStack.ItemID);
            if (item == null || !item.MarketSellable || item.MarketBasePrice <= 0)
            {
                failReason = "item_not_market_sellable";
                return false;
            }

            var capacity = GetMarketSlotCapacity(buildingId, logicAreaId);
            var usedSlots = 0;
            foreach (var listing in _marketListings)
            {
                if (listing.BuildingId == buildingId && listing.LogicAreaId == (logicAreaId ?? string.Empty))
                {
                    usedSlots++;
                }
            }
            if (usedSlots >= capacity)
            {
                failReason = "market_slots_full";
                return false;
            }

            if (sourceStack.ItemInstanceId != 0 && count != sourceStack.Count)
            {
                failReason = "instance_item_must_be_listed_whole";
                return false;
            }

            _marketListings.Add(new MarketListing
            {
                BuildingId = buildingId,
                LogicAreaId = logicAreaId ?? string.Empty,
                ItemId = sourceStack.ItemID,
                Count = count,
                ItemInstanceId = sourceStack.ItemInstanceId,
                InstanceInfo = sourceStack.InstanceInfo?.Clone(),
                ListedSettlementDay = _logic?.SettlementDayIndex ?? 0,
            });

            if (count == sourceStack.Count)
            {
                source.SetItemData(sourceIndex, null);
            }
            else
            {
                source.SetItemCount(sourceIndex, sourceStack.Count - count);
            }

            OnMarketChanged?.Invoke();
            return true;
        }

        public bool TryCancelListing(int index, out MarketListing listing)
        {
            listing = null;
            if (index < 0 || index >= _marketListings.Count)
            {
                return false;
            }

            var candidate = _marketListings[index];
            var returnStack = new My.ItemStack(candidate.ItemId, candidate.Count)
            {
                ItemInstanceId = candidate.ItemInstanceId,
                InstanceInfo = candidate.InstanceInfo?.Clone(),
            };
            var mainBag = _logic?.playerDataManager?.InventorySystem?.MainBag;
            if (mainBag == null || !mainBag.TryPlaceStackWithoutMerge(returnStack))
            {
                return false;
            }

            listing = candidate;
            _marketListings.RemoveAt(index);
            OnMarketChanged?.Invoke();
            return true;
        }

        public bool TryCancelListing(MarketListing target, out MarketListing listing)
        {
            listing = null;
            if (target == null) return false;
            int index = _marketListings.IndexOf(target);
            return index >= 0 && TryCancelListing(index, out listing);
        }

        public MarketSettlementResult SettleMarket()
        {
            var result = new MarketSettlementResult();
            if (_marketListings.Count == 0)
            {
                _lastMarketSettlementDay = _logic?.SettlementDayIndex ?? 0;
                _lastMarketGoldEarned = 0;
                _lastMarketSoldItemCount = 0;
                return result;
            }

            _marketListingScratch.Clear();
            var gold = 0L;
            foreach (var listing in _marketListings)
            {
                var item = My.Config.ItemCatalog.GetItemDef(listing.ItemId);
                if (item == null || !item.MarketSellable || item.MarketBasePrice <= 0)
                {
                    result.UnsoldItemCount += (int)Math.Min(int.MaxValue, listing.Count);
                    _marketListingScratch.Add(listing);
                    continue;
                }

                var chanceBonus = _logic?.townFacilityDevelopmentSystem?.GetBuildingAttribute(
                    listing.LogicAreaId, listing.BuildingId, EBuildingAttribute.MarketSaleChanceBonus) ?? 0;
                var priceBonus = _logic?.townFacilityDevelopmentSystem?.GetBuildingAttribute(
                    listing.LogicAreaId, listing.BuildingId, EBuildingAttribute.MarketPriceBonus) ?? 0;
                var civilizationPriceBonus = GetTechEffectValue(EHumanCivilizationAttribute.MarketSalePriceBonus);
                var chance = Mathf.Clamp01(0.5f + chanceBonus * 0.05f);
                if (UnityEngine.Random.value > chance)
                {
                    result.UnsoldItemCount += (int)Math.Min(int.MaxValue, listing.Count);
                    _marketListingScratch.Add(listing);
                    continue;
                }

                var weaponStack = new My.ItemStack(listing.ItemId, listing.Count)
                {
                    ItemInstanceId = listing.ItemInstanceId,
                    InstanceInfo = listing.InstanceInfo?.Clone(),
                };
                var affixValue = HumanWeaponCatalog.GetAffixMarketValue(weaponStack);
                affixValue += HumanArmarCatalog.GetAffixMarketValue(weaponStack);
                var unitPrice = Math.Max(1L, (long)Math.Floor(
                    (item.MarketBasePrice + affixValue) * (1d + Math.Max(0, priceBonus) * 0.1d
                        + Math.Max(0, civilizationPriceBonus) * 0.01d)));
                gold += unitPrice * listing.Count;
                result.SoldItemCount += (int)Math.Min(int.MaxValue, listing.Count);
            }

            _marketListings.Clear();
            _marketListings.AddRange(_marketListingScratch);
            result.GoldEarned = gold;
            if (gold > 0)
            {
                _logic?.playerDataManager?.GiveItemToPlayer("gold", gold);
            }

            _lastMarketSettlementDay = _logic?.SettlementDayIndex ?? 0;
            _lastMarketGoldEarned = gold;
            _lastMarketSoldItemCount = result.SoldItemCount;
            OnMarketChanged?.Invoke();
            return result;
        }

        public void AddTransportPendingItems(
            string regionKey,
            string homeLogicAreaId,
            string sourceOverlayId,
            List<ItemStack> stacks)
        {
            if (stacks == null)
            {
                return;
            }

            var settlementDay = _logic?.SettlementDayIndex ?? 0;
            foreach (var stack in stacks)
            {
                if (stack == null || stack.Count <= 0 || string.IsNullOrEmpty(stack.ItemID))
                {
                    continue;
                }

                _transportPendingItems.Add(new TransportPendingItem
                {
                    RegionKey = regionKey ?? string.Empty,
                    HomeLogicAreaId = homeLogicAreaId ?? string.Empty,
                    SourceOverlayId = sourceOverlayId ?? string.Empty,
                    DepositedSettlementDay = settlementDay,
                    ItemId = stack.ItemID,
                    Count = stack.Count,
                    ItemInstanceId = stack.ItemInstanceId,
                    InstanceInfo = stack.InstanceInfo?.Clone(),
                });
            }
        }

        public TransportSettlementResult SettleTransportRecovery()
        {
            var result = new TransportSettlementResult();
            if (_transportPendingItems.Count == 0)
            {
                _lastTransportSettlementDay = _logic?.SettlementDayIndex ?? 0;
                _lastTransportRecoveredStackCount = 0;
                _lastTransportLostStackCount = 0;
                return result;
            }

            var transportSystem = _logic?.transportLootSystem;
            var grouped = new Dictionary<string, List<TransportPendingItem>>(StringComparer.Ordinal);
            foreach (var pending in _transportPendingItems)
            {
                var homeId = string.IsNullOrEmpty(pending.HomeLogicAreaId)
                    ? GameRegionUtil.ResolveHomeLogicAreaId(pending.RegionKey)
                    : pending.HomeLogicAreaId;
                if (!grouped.TryGetValue(homeId, out var list))
                {
                    list = new List<TransportPendingItem>();
                    grouped[homeId] = list;
                }

                list.Add(pending);
            }

            _transportPendingItems.Clear();
            foreach (var pair in grouped)
            {
                var minPickup = 0;
                var maxPickup = 0;
                transportSystem?.GetTransportPickupBounds(pair.Key, out minPickup, out maxPickup);
                CalculateTransportRecovery(pair.Value, minPickup, maxPickup, out var recovered, out var lost);
                GrantRecoveredTransportItems(recovered, result.RecoveredOutputs);
                result.RecoveredStackCount += recovered.Count;
                result.LostStackCount += lost.Count;
            }

            _lastTransportSettlementDay = _logic?.SettlementDayIndex ?? 0;
            _lastTransportRecoveredStackCount = result.RecoveredStackCount;
            _lastTransportLostStackCount = result.LostStackCount;
            return result;
        }

        // 占位算法：后续可替换为更复杂的成功率/筛选规则
        static void CalculateTransportRecovery(
            List<TransportPendingItem> pending,
            int minPickup,
            int maxPickup,
            out List<TransportPendingItem> recovered,
            out List<TransportPendingItem> lost)
        {
            recovered = new List<TransportPendingItem>();
            lost = new List<TransportPendingItem>();
            if (pending == null || pending.Count == 0)
            {
                return;
            }

            if (maxPickup <= 0)
            {
                lost.AddRange(pending);
                return;
            }

            var recoverCount = Math.Min(maxPickup, pending.Count);
            if (minPickup > 0)
            {
                recoverCount = Math.Max(recoverCount, Math.Min(minPickup, pending.Count));
            }

            for (int i = 0; i < pending.Count; i++)
            {
                if (i < recoverCount)
                {
                    recovered.Add(pending[i]);
                }
                else
                {
                    lost.Add(pending[i]);
                }
            }
        }

        void GrantRecoveredTransportItems(
            List<TransportPendingItem> recovered,
            Dictionary<string, long> mergedOutputs)
        {
            var inv = _logic?.playerDataManager?.InventorySystem;
            if (inv == null)
            {
                return;
            }

            foreach (var item in recovered)
            {
                if (item == null || item.Count <= 0 || string.IsNullOrEmpty(item.ItemId))
                {
                    continue;
                }

                inv.GiveItemToWarehouse(item.ItemId, item.Count);

                if (!mergedOutputs.ContainsKey(item.ItemId))
                {
                    mergedOutputs[item.ItemId] = 0;
                }

                mergedOutputs[item.ItemId] += item.Count;
            }
        }

        static TransportPendingItem CloneTransportPending(TransportPendingItemPersist entry)
        {
            return new TransportPendingItem
            {
                RegionKey = entry.RegionKey ?? string.Empty,
                HomeLogicAreaId = entry.HomeLogicAreaId ?? string.Empty,
                SourceOverlayId = entry.SourceOverlayId ?? string.Empty,
                DepositedSettlementDay = entry.DepositedSettlementDay,
                ItemId = entry.ItemId,
                Count = entry.Count,
                ItemInstanceId = entry.ItemInstanceId,
                InstanceInfo = entry.InstanceInfo,
            };
        }

        public int GetUnlockedTechCount()
        {
            int count = 0;
            foreach (var pair in _techLevels)
            {
                if (pair.Value > 0)
                {
                    count++;
                }
            }

            return count;
        }

        public int GetTechNodeLevel(int nodeId)
        {
            return _techLevels.TryGetValue(nodeId, out var level) ? level : 0;
        }

        public int GetCivilizationLevel()
        {
            int result = 0;
            var table = My.Config.CfgMgr.Cfgs?.TbHumanCivilizationLevel;
            if (table == null)
            {
                return result;
            }

            foreach (var row in table.DataList)
            {
                if (row == null || row.Level <= result)
                {
                    continue;
                }

                if (CheckLevelConditions(row))
                {
                    result = row.Level;
                }
            }

            return result;
        }

        public HumanTechNodeVisualState GetTechNodeVisualState(int nodeId)
        {
            var node = My.Config.CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                return HumanTechNodeVisualState.Locked;
            }

            int current = GetTechNodeLevel(nodeId);
            if (current >= node.MaxLevel)
            {
                return HumanTechNodeVisualState.Unlocked;
            }

            if (node.RequiredCivilizationLevel > GetCivilizationLevel())
            {
                return HumanTechNodeVisualState.Locked;
            }

            var level = My.Config.CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null || !CheckPrerequisites(level) || !CheckCommonConditions(level.UnlockConds))
            {
                return HumanTechNodeVisualState.Locked;
            }

            return CanPayUnlockCosts(level) ? HumanTechNodeVisualState.Unlockable : HumanTechNodeVisualState.InsufficientCost;
        }

        public bool TryUnlockTechNode(int nodeId, out string failReason)
        {
            failReason = null;
            var node = My.Config.CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                failReason = "unknown tech node";
                return false;
            }

            int next = GetTechNodeLevel(nodeId) + 1;
            if (next > node.MaxLevel)
            {
                failReason = "tech node is already max level";
                return false;
            }

            if (node.RequiredCivilizationLevel > GetCivilizationLevel())
            {
                failReason = "civilization level is too low";
                return false;
            }

            var level = My.Config.CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(nodeId, next);
            if (level == null || !CheckPrerequisites(level))
            {
                failReason = "prerequisite tech is missing";
                return false;
            }

            if (!CheckCommonConditions(level.UnlockConds))
            {
                failReason = "unlock conditions are not met";
                return false;
            }

            if (!TryPayUnlockCosts(level, out failReason))
            {
                return false;
            }

            _techLevels[nodeId] = next;
            OnTechNodeChanged?.Invoke(nodeId);
            OnCivilizationLevelChanged?.Invoke(GetCivilizationLevel());
            return true;
        }

        bool CheckLevelConditions(HumanCivilizationLevel row)
        {
            if (_logic == null || !CheckCommonConditions(row.UnlockConds))
            {
                return false;
            }

            if (row.CustomUnlockConds == null)
            {
                return true;
            }

            foreach (var cond in row.CustomUnlockConds)
            {
                if (!CheckCustomCondition(cond))
                {
                    return false;
                }
            }

            return true;
        }

        bool CheckCommonConditions(IReadOnlyList<CommonCheckCond> conditions)
        {
            return _logic != null && _logic.CheckCommonCondsAll(conditions);
        }

        bool CheckCustomCondition(HumanCivilizationCustomCond cond)
        {
            if (cond == null || string.IsNullOrEmpty(cond.Type))
            {
                return true;
            }

            if (cond.Type == "homestead_building_level")
            {
                var parts = cond.Param1?.Split('|');
                if (parts == null || parts.Length != 2 || _logic?.worldPersistState == null)
                {
                    return false;
                }

                return _logic.worldPersistState.GetFacilityDevelopmentLevel(parts[0], parts[1]) >= cond.Param2;
            }

            return false;
        }

        bool CheckPrerequisites(HumanTechNodeLevel level)
        {
            if (level.PrereqNodeIds == null)
            {
                return true;
            }

            foreach (var prerequisite in level.PrereqNodeIds)
            {
                if (GetTechNodeLevel(prerequisite) <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        bool HasUnlockCosts(HumanTechNodeLevel level)
        {
            return level?.UnlockCosts != null && level.UnlockCosts.Count > 0;
        }

        bool CanPayUnlockCosts(HumanTechNodeLevel level)
        {
            var player = _logic?.playerDataManager;
            if (player == null)
            {
                return false;
            }

            if (!HasUnlockCosts(level))
            {
                return true;
            }

            foreach (var cost in level.UnlockCosts)
            {
                if (cost != null && !string.IsNullOrEmpty(cost.ItemId) && cost.Count > 0
                    && !player.CheckHaveItem(cost.ItemId, cost.Count))
                {
                    return false;
                }
            }

            return true;
        }

        public long GetTechEffectValue(EHumanCivilizationAttribute effectKey)
        {
            if (effectKey == EHumanCivilizationAttribute.None)
            {
                return 0;
            }

            long value = 0;
            foreach (var pair in _techLevels)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                for (int level = 1; level <= pair.Value; level++)
                {
                    var row = My.Config.CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(pair.Key, level);
                    if (row != null && row.EffectKey == effectKey)
                    {
                        value += row.EffectValue;
                    }
                }
            }

            _talentBonuses.TryGetValue(effectKey, out var talentValue);
            return value + talentValue
                + (_logic?.townFacilityDevelopmentSystem?.GetHumanCivilizationBonusFromFacilities(effectKey) ?? 0);
        }

        public void SetTalentBonuses(IReadOnlyDictionary<EHumanCivilizationAttribute, long> bonuses)
        {
            _talentBonuses.Clear();
            if (bonuses == null) return;
            foreach (var pair in bonuses)
            {
                if (pair.Key != EHumanCivilizationAttribute.None && pair.Value != 0)
                    _talentBonuses[pair.Key] = pair.Value;
            }
        }

        public long GetTradeCost(long baseCost)
        {
            if (baseCost <= 0) return 0;
            var tier = GetTechEffectValue(EHumanCivilizationAttribute.ScavengeExchangeAccess);
            return Math.Max(1, (long)Math.Ceiling(baseCost * 100d / (100d + Math.Max(0, tier) * 10d)));
        }

        public long ModifyExplorationLoot(long baseAmount)
        {
            if (baseAmount <= 0) return 0;
            var tier = GetTechEffectValue(EHumanCivilizationAttribute.ExplorationLootValueBonus);
            return Math.Max(0, (long)Math.Floor(baseAmount * (1d + Math.Max(0, tier) * 0.1d)));
        }

        public bool HasTechEffect(EHumanCivilizationAttribute effectKey, long minValue = 1)
        {
            return GetTechEffectValue(effectKey) >= minValue;
        }
        bool TryPayUnlockCosts(HumanTechNodeLevel level, out string failReason)
        {
            failReason = null;
            var player = _logic?.playerDataManager;
            if (player == null)
            {
                failReason = "player data manager unavailable";
                return false;
            }

            if (!HasUnlockCosts(level))
            {
                return true;
            }

            foreach (var cost in level.UnlockCosts)
            {
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                if (!player.CheckHaveItem(cost.ItemId, cost.Count))
                {
                    failReason = "missing cost item: " + cost.ItemId;
                    return false;
                }
            }

            foreach (var cost in level.UnlockCosts)
            {
                if (cost != null && !string.IsNullOrEmpty(cost.ItemId) && cost.Count > 0)
                {
                    player.CostItem(cost.ItemId, cost.Count);
                }
            }

            return true;
        }
    }
}
