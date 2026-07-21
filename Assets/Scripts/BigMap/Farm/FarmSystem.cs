using System;
using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Home;
using My.Map;
using My.Player;
using My.Player.Bag;
using My.Saving;
using My.UI;
using UnityEngine;

namespace My.Farm
{
    // 种植系统：农田格状态、种子篮、农业小站日结算、播种模式
    public sealed class FarmSystem
    {
        readonly GameLogicManager _logic;
        readonly Dictionary<string, TownFarmPersist> _townFarms = new();
        readonly Dictionary<string, PlayerBag> _seedBags = new();
        readonly Dictionary<string, PlayerBag> _produceBags = new();

        public bool IsPlantingMode { get; private set; }
        public string PlantingLogicAreaId { get; private set; }
        public string SelectedSeedItemId { get; private set; }
        public event Action EvOnFarmChanged;
        public event Action EvOnPlantingModeChanged;

        public FarmSystem(GameLogicManager logic)
        {
            _logic = logic;
        }

        public void LoadFromSave(SaveData saveData)
        {
            _townFarms.Clear();
            _seedBags.Clear();
            _produceBags.Clear();
            ExitPlantingMode();

            if (saveData?.TownFarmByLogicAreaId == null)
            {
                return;
            }

            foreach (var kv in saveData.TownFarmByLogicAreaId)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                {
                    continue;
                }

                var persist = kv.Value;
                persist.LogicAreaId = kv.Key;
                EnsurePlotShapes(persist);
                EnsureDefaultPlan(persist);
                _townFarms[kv.Key] = persist;
                RebuildBags(kv.Key, persist);
            }
        }

        public void ApplyToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.TownFarmByLogicAreaId ??= new Dictionary<string, TownFarmPersist>();
            data.TownFarmByLogicAreaId.Clear();
            foreach (var kv in _townFarms)
            {
                FlushBagsToPersist(kv.Key, kv.Value);
                data.TownFarmByLogicAreaId[kv.Key] = kv.Value;
            }
        }

        public TownFarmPersist GetOrCreateTownFarm(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId))
            {
                logicAreaId = FarmCatalog.DefaultLogicAreaId;
            }

            if (_townFarms.TryGetValue(logicAreaId, out var exist) && exist != null)
            {
                EnsurePlotShapes(exist);
                EnsureDefaultPlan(exist);
                EnsureBags(logicAreaId, exist);
                return exist;
            }

            var created = new TownFarmPersist { LogicAreaId = logicAreaId };
            EnsurePlotShapes(created);
            EnsureDefaultPlan(created);
            _townFarms[logicAreaId] = created;
            RebuildBags(logicAreaId, created);
            return created;
        }

        public PlayerBag GetSeedBasket(string logicAreaId)
        {
            var farm = GetOrCreateTownFarm(logicAreaId);
            EnsureBags(logicAreaId, farm);
            return _seedBags[logicAreaId];
        }

        public PlayerBag GetProduceWarehouse(string logicAreaId)
        {
            var farm = GetOrCreateTownFarm(logicAreaId);
            EnsureBags(logicAreaId, farm);
            return _produceBags[logicAreaId];
        }

        public bool IsFarmStationBuilt(string logicAreaId)
        {
            if (_logic?.worldPersistState == null)
            {
                return false;
            }

            return _logic.worldPersistState.GetFacilityDevelopmentLevel(
                logicAreaId, FarmCatalog.FarmStationFacilityId) > 0;
        }

        public bool IsPlotVisible(FarmPlot plotCfg)
        {
            if (plotCfg == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(plotCfg.ShowVar))
            {
                return true;
            }

            var player = _logic?.playerDataManager;
            return player != null && player.CheckHasParam(plotCfg.ShowVar);
        }

        public bool EnterPlantingMode(string logicAreaId)
        {
            var bag = GetSeedBasket(logicAreaId);
            IsPlantingMode = true;
            PlantingLogicAreaId = logicAreaId;
            if (string.IsNullOrEmpty(SelectedSeedItemId) || bag.GetItemCount(SelectedSeedItemId) <= 0)
            {
                SelectedSeedItemId = FindFirstSeedInBasket(bag);
            }

            PlayerHumanItemBarPanel.TryHide();
            FarmSeedBarPanel.TryShow();
            EvOnPlantingModeChanged?.Invoke();
            EvOnFarmChanged?.Invoke();
            return true;
        }

        public void ExitPlantingMode()
        {
            if (!IsPlantingMode)
            {
                return;
            }

            IsPlantingMode = false;
            PlantingLogicAreaId = null;
            FarmSeedBarPanel.TryHide();
            PlayerHumanItemBarPanel.TryShow();
            EvOnPlantingModeChanged?.Invoke();
        }

        public void SelectSeed(string seedItemId)
        {
            SelectedSeedItemId = seedItemId;
            EvOnFarmChanged?.Invoke();
        }

        public void CycleSelectedSeed(int delta)
        {
            if (!IsPlantingMode)
            {
                return;
            }

            var bag = GetSeedBasket(PlantingLogicAreaId);
            var seeds = CollectSeedItemIds(bag);
            if (seeds.Count == 0)
            {
                SelectedSeedItemId = null;
                EvOnFarmChanged?.Invoke();
                return;
            }

            int idx = Mathf.Max(0, seeds.IndexOf(SelectedSeedItemId));
            idx = (idx + delta) % seeds.Count;
            if (idx < 0)
            {
                idx += seeds.Count;
            }

            SelectedSeedItemId = seeds[idx];
            EvOnFarmChanged?.Invoke();
        }

        public bool TryPlantFacingCell()
        {
            if (!IsPlantingMode || string.IsNullOrEmpty(SelectedSeedItemId))
            {
                return false;
            }

            var player = _logic?.playerLogicEntity;
            if (player == null)
            {
                return false;
            }

            if (!TryResolveFacingPlantCell(player, out var logicAreaId, out var plotId, out var cell))
            {
                return false;
            }

            return TryPlantAt(logicAreaId, plotId, cell.Cx, cell.Cy, SelectedSeedItemId, fromPlayer: true);
        }

        public bool TryPlantAt(string logicAreaId, string plotId, int cx, int cy, string seedItemId, bool fromPlayer)
        {
            var crop = FarmCatalog.FindCropBySeedItem(seedItemId);
            if (crop == null)
            {
                return false;
            }

            var farm = GetOrCreateTownFarm(logicAreaId);
            var cell = FindCell(farm, plotId, cx, cy);
            if (cell == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(cell.CropId))
            {
                var oldCrop = FarmCatalog.GetCrop(cell.CropId);
                if (oldCrop != null && FarmCatalog.IsSprouted(oldCrop, cell.GrowProgress))
                {
                    return false;
                }

                if (oldCrop != null && !string.IsNullOrEmpty(oldCrop.SeedItemId))
                {
                    GetSeedBasket(logicAreaId).TryGiveItem(oldCrop.SeedItemId, 1);
                }
            }

            var bag = GetSeedBasket(logicAreaId);
            if (!TryConsumeSeed(bag, seedItemId, 1))
            {
                return false;
            }

            cell.CropId = crop.CropId;
            cell.GrowProgress = 0;
            cell.Watered = false;
            // 施肥状态保留在地格上直到收获
            EvOnFarmChanged?.Invoke();
            return true;
        }

        public bool TryWaterFacingCell()
        {
            var player = _logic?.playerLogicEntity;
            if (player == null || !TryResolveFacingPlantCell(player, out var area, out var plotId, out var cell))
            {
                return false;
            }

            return TryWaterCell(area, plotId, cell.Cx, cell.Cy);
        }

        public bool TryFertilizeFacingCell()
        {
            var player = _logic?.playerLogicEntity;
            if (player == null || !TryResolveFacingPlantCell(player, out var area, out var plotId, out var cell))
            {
                return false;
            }

            return TryFertilizeCell(area, plotId, cell.Cx, cell.Cy);
        }

        public bool TryWaterCell(string logicAreaId, string plotId, int cx, int cy)
        {
            var cell = FindCell(GetOrCreateTownFarm(logicAreaId), plotId, cx, cy);
            if (cell == null || string.IsNullOrEmpty(cell.CropId) || cell.Watered)
            {
                return false;
            }

            cell.Watered = true;
            EvOnFarmChanged?.Invoke();
            return true;
        }

        public bool TryFertilizeCell(string logicAreaId, string plotId, int cx, int cy)
        {
            var cell = FindCell(GetOrCreateTownFarm(logicAreaId), plotId, cx, cy);
            if (cell == null || string.IsNullOrEmpty(cell.CropId) || cell.Fertilized)
            {
                return false;
            }

            var inv = _logic.playerDataManager;
            if (inv == null || !inv.CheckHaveItem("item_fertilizer_basic", 1))
            {
                return false;
            }

            if (inv.CostItem("item_fertilizer_basic", 1) < 1)
            {
                return false;
            }

            cell.Fertilized = true;
            EvOnFarmChanged?.Invoke();
            return true;
        }

        public bool TryHarvestCell(string logicAreaId, string plotId, int cx, int cy, bool fromPlayerInteract)
        {
            var farm = GetOrCreateTownFarm(logicAreaId);
            var cell = FindCell(farm, plotId, cx, cy);
            if (cell == null || string.IsNullOrEmpty(cell.CropId))
            {
                return false;
            }

            var crop = FarmCatalog.GetCrop(cell.CropId);
            if (crop == null || !FarmCatalog.IsMature(crop, cell.GrowProgress))
            {
                return false;
            }

            long count = Mathf.Max(1, crop.HarvestCount);
            bool toStation = IsFarmStationBuilt(logicAreaId);
            if (toStation)
            {
                GetProduceWarehouse(logicAreaId).TryGiveItem(crop.HarvestItemId, count);
            }
            else if (fromPlayerInteract)
            {
                _logic.playerDataManager?.GiveItemToPlayer(crop.HarvestItemId, count);
            }
            else
            {
                return false;
            }

            cell.CropId = null;
            cell.GrowProgress = 0;
            cell.Watered = false;
            cell.Fertilized = false;
            EvOnFarmChanged?.Invoke();
            return true;
        }

        public void ApplyDailySettlement(GameLogicManager.OneDayBalanceInfo balanceInfo)
        {
            int day = _logic.SettlementDayIndex;
            foreach (var kv in _townFarms)
            {
                ApplyTownDaily(kv.Key, kv.Value, day, balanceInfo);
            }

            // 尚未进存档但配表有田的地图，在可见后也会建档
            var plots = CfgMgr.Cfgs?.TbFarmPlot?.DataList;
            if (plots != null)
            {
                var seen = new HashSet<string>(_townFarms.Keys);
                for (int i = 0; i < plots.Count; i++)
                {
                    var area = plots[i].LogicAreaId;
                    if (seen.Contains(area))
                    {
                        continue;
                    }

                    if (!IsPlotVisible(plots[i]))
                    {
                        continue;
                    }

                    var farm = GetOrCreateTownFarm(area);
                    ApplyTownDaily(area, farm, day, balanceInfo);
                    seen.Add(area);
                }
            }
        }

        void ApplyTownDaily(string logicAreaId, TownFarmPersist farm, int day, GameLogicManager.OneDayBalanceInfo balanceInfo)
        {
            if (farm.LastSettledDay >= day)
            {
                return;
            }

            bool station = IsFarmStationBuilt(logicAreaId);

            if (station)
            {
                // 镇民假表现：直接浇水施肥
                foreach (var plot in farm.Plots)
                {
                    foreach (var cell in plot.Cells)
                    {
                        if (!string.IsNullOrEmpty(cell.CropId))
                        {
                            cell.Watered = true;
                            cell.Fertilized = true;
                        }
                    }
                }
            }

            // 生长
            foreach (var plot in farm.Plots)
            {
                foreach (var cell in plot.Cells)
                {
                    if (string.IsNullOrEmpty(cell.CropId))
                    {
                        continue;
                    }

                    var crop = FarmCatalog.GetCrop(cell.CropId);
                    if (crop == null || FarmCatalog.IsMature(crop, cell.GrowProgress))
                    {
                        cell.Watered = false;
                        continue;
                    }

                    if (!cell.Watered)
                    {
                        // 未浇水当天不生长
                        continue;
                    }

                    cell.GrowProgress += 1;
                    if (cell.Fertilized)
                    {
                        cell.GrowProgress += 1;
                    }

                    cell.Watered = false;
                }
            }

            if (station)
            {
                int harvested = ApplyDispatchHarvest(logicAreaId, farm);
                int planted = ApplyAutoPlant(logicAreaId, farm);
                if (balanceInfo != null)
                {
                    if (harvested > 0)
                    {
                        balanceInfo.FarmHarvestCount += harvested;
                    }

                    if (planted > 0)
                    {
                        balanceInfo.FarmAutoPlantCount += planted;
                    }
                }
            }

            farm.LastSettledDay = day;
            EvOnFarmChanged?.Invoke();
        }

        int ApplyDispatchHarvest(string logicAreaId, TownFarmPersist farm)
        {
            int quota = Mathf.Max(0, farm.HarvestWorkforce);
            if (quota <= 0)
            {
                return 0;
            }

            int done = 0;
            foreach (var plot in farm.Plots)
            {
                foreach (var cell in plot.Cells)
                {
                    if (done >= quota)
                    {
                        return done;
                    }

                    if (TryHarvestCell(logicAreaId, plot.PlotId, cell.Cx, cell.Cy, fromPlayerInteract: false))
                    {
                        done++;
                    }
                }
            }

            return done;
        }

        int ApplyAutoPlant(string logicAreaId, TownFarmPersist farm)
        {
            EnsureDefaultPlan(farm);
            var bag = GetSeedBasket(logicAreaId);
            int planted = 0;

            // 统计当前数量
            var counts = new Dictionary<string, int>();
            foreach (var plot in farm.Plots)
            {
                foreach (var cell in plot.Cells)
                {
                    if (string.IsNullOrEmpty(cell.CropId))
                    {
                        continue;
                    }

                    counts.TryGetValue(cell.CropId, out int c);
                    counts[cell.CropId] = c + 1;
                }
            }

            var plan = new List<FarmPlanEntryPersist>(farm.AutoPlantPlan);
            plan.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var entry in plan)
            {
                counts.TryGetValue(entry.CropId, out int have);
                int need = entry.TargetCount - have;
                if (need <= 0)
                {
                    continue;
                }

                var crop = FarmCatalog.GetCrop(entry.CropId);
                if (crop == null || bag.GetItemCount(crop.SeedItemId) <= 0)
                {
                    continue;
                }

                foreach (var plot in farm.Plots)
                {
                    foreach (var cell in plot.Cells)
                    {
                        if (need <= 0)
                        {
                            break;
                        }

                        if (!string.IsNullOrEmpty(cell.CropId))
                        {
                            continue;
                        }

                        if (TryPlantAt(logicAreaId, plot.PlotId, cell.Cx, cell.Cy, crop.SeedItemId, fromPlayer: false))
                        {
                            planted++;
                            need--;
                            have++;
                            counts[entry.CropId] = have;
                        }
                    }
                }
            }

            return planted;
        }

        public void SetHarvestWorkforce(string logicAreaId, int value)
        {
            var farm = GetOrCreateTownFarm(logicAreaId);
            farm.HarvestWorkforce = Mathf.Clamp(value, 0, 20);
            EvOnFarmChanged?.Invoke();
        }

        public void NotifyChanged()
        {
            EvOnFarmChanged?.Invoke();
        }

        public void SetPlanTarget(string logicAreaId, string cropId, int target)
        {
            var farm = GetOrCreateTownFarm(logicAreaId);
            EnsureDefaultPlan(farm);
            for (int i = 0; i < farm.AutoPlantPlan.Count; i++)
            {
                if (farm.AutoPlantPlan[i].CropId == cropId)
                {
                    farm.AutoPlantPlan[i].TargetCount = Mathf.Max(0, target);
                    EvOnFarmChanged?.Invoke();
                    return;
                }
            }

            farm.AutoPlantPlan.Add(new FarmPlanEntryPersist
            {
                CropId = cropId,
                TargetCount = Mathf.Max(0, target),
                Priority = 100 + farm.AutoPlantPlan.Count,
            });
            EvOnFarmChanged?.Invoke();
        }

        bool TryResolveFacingPlantCell(PlayerLogicEntity player, out string logicAreaId, out string plotId, out FarmCellPersist cell)
        {
            logicAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(_logic.AreaManager);
            plotId = null;
            cell = null;
            if (string.IsNullOrEmpty(logicAreaId))
            {
                logicAreaId = FarmCatalog.DefaultLogicAreaId;
            }

            var farm = GetOrCreateTownFarm(logicAreaId);
            var look = player.CurrentLook;
            if (look.sqrMagnitude < 0.0001f)
            {
                look = Vector2.down;
            }

            look.Normalize();
            // 主轴量化，取前方一格
            Vector2 axis = Mathf.Abs(look.x) >= Mathf.Abs(look.y)
                ? new Vector2(Mathf.Sign(look.x), 0f)
                : new Vector2(0f, Mathf.Sign(look.y));

            var plots = FarmCatalog.GetPlotsForArea(logicAreaId);
            Vector2 targetWorld = player.Pos + axis;

            float best = float.MaxValue;
            FarmCellPersist bestCell = null;
            string bestPlot = null;
            FarmPlotAreaRegistry.TryGetOrigin(logicAreaId, out var origins);

            for (int i = 0; i < plots.Count; i++)
            {
                var cfg = plots[i];
                if (!IsPlotVisible(cfg))
                {
                    continue;
                }

                if (!origins.TryGetValue(cfg.PlotId, out var origin))
                {
                    continue;
                }

                float cellSize = cfg.CellSize > 0.01f ? cfg.CellSize : 1f;
                var plotPersist = FindPlot(farm, cfg.PlotId);
                if (plotPersist == null)
                {
                    continue;
                }

                foreach (var c in plotPersist.Cells)
                {
                    Vector2 world = origin + new Vector2((c.Cx + 0.5f) * cellSize, (c.Cy + 0.5f) * cellSize);
                    float d = (world - targetWorld).sqrMagnitude;
                    if (d < best && d <= cellSize * cellSize * 0.85f)
                    {
                        best = d;
                        bestCell = c;
                        bestPlot = cfg.PlotId;
                    }
                }
            }

            if (bestCell == null)
            {
                return false;
            }

            plotId = bestPlot;
            cell = bestCell;
            return true;
        }

        static FarmPlotPersist FindPlot(TownFarmPersist farm, string plotId)
        {
            for (int i = 0; i < farm.Plots.Count; i++)
            {
                if (farm.Plots[i].PlotId == plotId)
                {
                    return farm.Plots[i];
                }
            }

            return null;
        }

        static FarmCellPersist FindCell(TownFarmPersist farm, string plotId, int cx, int cy)
        {
            var plot = FindPlot(farm, plotId);
            if (plot == null)
            {
                return null;
            }

            for (int i = 0; i < plot.Cells.Count; i++)
            {
                var c = plot.Cells[i];
                if (c.Cx == cx && c.Cy == cy)
                {
                    return c;
                }
            }

            return null;
        }

        void EnsurePlotShapes(TownFarmPersist farm)
        {
            var cfgs = FarmCatalog.GetPlotsForArea(farm.LogicAreaId);
            for (int i = 0; i < cfgs.Count; i++)
            {
                var cfg = cfgs[i];
                var plot = FindPlot(farm, cfg.PlotId);
                if (plot == null)
                {
                    plot = new FarmPlotPersist { PlotId = cfg.PlotId };
                    farm.Plots.Add(plot);
                }

                var wanted = FarmCatalog.ParseCells(cfg.Cells);
                var map = new Dictionary<long, FarmCellPersist>();
                foreach (var c in plot.Cells)
                {
                    map[PackCell(c.Cx, c.Cy)] = c;
                }

                plot.Cells.Clear();
                for (int k = 0; k < wanted.Count; k++)
                {
                    var key = PackCell(wanted[k].x, wanted[k].y);
                    if (!map.TryGetValue(key, out var cell))
                    {
                        cell = new FarmCellPersist { Cx = wanted[k].x, Cy = wanted[k].y };
                    }

                    plot.Cells.Add(cell);
                }
            }
        }

        void EnsureDefaultPlan(TownFarmPersist farm)
        {
            if (farm.AutoPlantPlan != null && farm.AutoPlantPlan.Count > 0)
            {
                return;
            }

            farm.AutoPlantPlan = new List<FarmPlanEntryPersist>();
            var defaults = CfgMgr.Cfgs?.TbFarmStationPlanDefault?.DataList;
            if (defaults == null)
            {
                return;
            }

            for (int i = 0; i < defaults.Count; i++)
            {
                var d = defaults[i];
                if (d.LogicAreaId != farm.LogicAreaId)
                {
                    continue;
                }

                farm.AutoPlantPlan.Add(new FarmPlanEntryPersist
                {
                    CropId = d.CropId,
                    TargetCount = d.TargetCount,
                    Priority = d.Priority,
                });
            }
        }

        void EnsureBags(string logicAreaId, TownFarmPersist farm)
        {
            if (!_seedBags.ContainsKey(logicAreaId))
            {
                RebuildBags(logicAreaId, farm);
            }
        }

        void RebuildBags(string logicAreaId, TownFarmPersist farm)
        {
            var seed = new PlayerBag();
            seed.InitBag(EPlayerBagId.TownSeedBasket, FarmCatalog.SeedBasketCapacity, 0);
            seed.SetAcceptedAnyTags(new[] { EItemTag.Seed });
            LoadBagPersist(seed, farm.SeedBasket);
            _seedBags[logicAreaId] = seed;

            var produce = new PlayerBag();
            produce.InitBag(EPlayerBagId.FarmProduceWarehouse, FarmCatalog.ProduceWarehouseCapacity, 0);
            LoadBagPersist(produce, farm.ProduceWarehouse);
            _produceBags[logicAreaId] = produce;
        }

        void FlushBagsToPersist(string logicAreaId, TownFarmPersist farm)
        {
            if (_seedBags.TryGetValue(logicAreaId, out var seed))
            {
                farm.SeedBasket = SaveBagPersist(seed);
            }

            if (_produceBags.TryGetValue(logicAreaId, out var produce))
            {
                farm.ProduceWarehouse = SaveBagPersist(produce);
            }
        }

        static PlayerBagPersist SaveBagPersist(PlayerBag bag)
        {
            var p = new PlayerBagPersist { BagId = (int)bag.BagId };
            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                var s = bag.NormalSlots[i];
                if (s == null || s.IsEmpty)
                {
                    continue;
                }

                p.Slots.Add(new MainBagSlotPersist
                {
                    SlotIndex = i,
                    ItemId = s.ItemID,
                    Count = s.Count,
                    ItemInstanceId = s.ItemInstanceId,
                    InstanceInfo = s.InstanceInfo,
                });
            }

            return p;
        }

        static void LoadBagPersist(PlayerBag bag, PlayerBagPersist persist)
        {
            if (persist?.Slots == null)
            {
                return;
            }

            foreach (var slot in persist.Slots)
            {
                if (slot == null || string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0)
                {
                    continue;
                }

                if (slot.SlotIndex < 0 || slot.SlotIndex >= bag.NormalSlots.Count)
                {
                    bag.TryGiveItem(slot.ItemId, slot.Count);
                    continue;
                }

                bag.NormalSlots[slot.SlotIndex] = new ItemStack(slot.ItemId, slot.Count)
                {
                    ItemInstanceId = slot.ItemInstanceId,
                    InstanceInfo = slot.InstanceInfo,
                };
            }
        }

        static bool TryConsumeSeed(PlayerBag bag, string itemId, long count)
        {
            long left = count;
            for (int i = 0; i < bag.NormalSlots.Count && left > 0; i++)
            {
                var s = bag.NormalSlots[i];
                if (s == null || s.IsEmpty || s.ItemID != itemId)
                {
                    continue;
                }

                long take = Math.Min(left, s.Count);
                s.Count -= take;
                left -= take;
                if (s.Count <= 0)
                {
                    bag.NormalSlots[i] = null;
                }
            }

            if (left < count)
            {
                bag.CompactPackPrimary();
            }

            return left == 0;
        }

        static string FindFirstSeedInBasket(PlayerBag bag)
        {
            var list = CollectSeedItemIds(bag);
            return list.Count > 0 ? list[0] : null;
        }

        static List<string> CollectSeedItemIds(PlayerBag bag)
        {
            var result = new List<string>();
            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                var s = bag.NormalSlots[i];
                if (s == null || s.IsEmpty)
                {
                    continue;
                }

                if (!result.Contains(s.ItemID) && FarmCatalog.FindCropBySeedItem(s.ItemID) != null)
                {
                    result.Add(s.ItemID);
                }
            }

            return result;
        }

        static long PackCell(int x, int y) => ((long)x << 32) ^ (uint)y;
    }
}
