using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public enum ECultAttribute
    {
        None = 0,
        TownDailyFaith = 1,
    }

    public enum CultTechNodeVisualState
    {
        Locked,
        Unlockable,
        InsufficientFaith,
        Unlocked,
    }

    public sealed class CultSecretUnitInfo
    {
        public string UnitId { get; internal set; } = string.Empty;
        public string SourceId { get; internal set; } = string.Empty;
        public ECultSecretUnitState State { get; internal set; }
        public string AssignedRegionKey { get; internal set; } = string.Empty;
        public string MissionId { get; internal set; } = string.Empty;
        public string AssignedLogicAreaId { get; internal set; } = string.Empty;
        public string AssignedAnchorId { get; internal set; } = string.Empty;
    }

    public sealed class CultAnchorInfo
    {
        public string LogicAreaId { get; internal set; } = string.Empty;
        public string AnchorId { get; internal set; } = string.Empty;
        public int Level { get; internal set; } = 1;
        public long Progress { get; internal set; }
        public bool Established { get; internal set; }
        public int NextOutputSettlementDay { get; internal set; }
        public bool IsWatched { get; internal set; }
        public int WatchedSinceSettlementDay { get; internal set; }
        public int WatchedPressureLevel { get; internal set; }
        public int DisabledUntilSettlementDay { get; internal set; }
        public int CurrentPressureLevel { get; internal set; }

        public bool IsDisabled(int settlementDay)
            => DisabledUntilSettlementDay > settlementDay;
    }

    public sealed class CultAnchorSettlementResult
    {
        public int EstablishedAnchorCount;
        public int ProcessedOutputCount;
        public int WatchedAnchorCount;
        public int DisabledAnchorCount;
        public int ReenabledAnchorCount;
        public long FaithAdded;
        public long LinkersAdded;
        public Dictionary<string, long> ItemOutputs { get; } = new(StringComparer.Ordinal);
    }

    // 教团：信仰账户 + 布道/教义节点（首期仅解锁与占位效果描述）
    public sealed class DemonCultSystem
    {
        readonly Dictionary<int, int> _techLevels = new();
        readonly HashSet<int> _unlockedSeats = new();
        readonly Dictionary<(int seatId, int nodeId), int> _seatTechLevels = new();
        readonly Dictionary<ECultAttribute, long> _cultAttributes = new();
        readonly Dictionary<string, long> _linkerCountByRegionKey = new(StringComparer.Ordinal);
        readonly Dictionary<string, CultSecretUnitInfo> _secretUnits = new(StringComparer.Ordinal);
        readonly Dictionary<string, List<CultInfluenceApplicationPersist>> _influenceByLogicAreaId = new(StringComparer.Ordinal);
        readonly Dictionary<string, CultAnchorInfo> _anchors = new(StringComparer.Ordinal);
        readonly Dictionary<string, long> _churchPressureByRegionKey = new(StringComparer.Ordinal);
        long _faith;
        GameLogicManager _logic;

        const int BasicCultTechNodeId = 1;
        public const int AmplifyAnchorOutputBonusPercent = 25;

        public event Action OnCultChanged;

        public long Faith => _faith;
        public int UnlockedSeatCount => _unlockedSeats.Count;
        public int SecretUnitCount => _secretUnits.Count;
        public int AvailableSecretUnitCount
        {
            get
            {
                var count = 0;
                foreach (var unit in _secretUnits.Values)
                {
                    if (unit.State == ECultSecretUnitState.Available) count++;
                }

                return count;
            }
        }

        public IReadOnlyCollection<CultSecretUnitInfo> SecretUnits => _secretUnits.Values;

        public long GetLinkerCount(string regionKey)
        {
            return !string.IsNullOrEmpty(regionKey)
                && _linkerCountByRegionKey.TryGetValue(regionKey, out var count)
                ? count : 0;
        }

        public long GetTotalLinkerCount()
        {
            long total = 0;
            foreach (var count in _linkerCountByRegionKey.Values)
            {
                total += count;
            }

            return total;
        }

        // 汇总教徒 / 压力 / 锚点配置中出现过的 region，供教团 Overview 列表使用
        public IReadOnlyList<string> GetKnownRegionKeys()
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            void Add(string key)
            {
                if (string.IsNullOrEmpty(key) || !seen.Add(key)) return;
                result.Add(key);
            }

            foreach (var key in _linkerCountByRegionKey.Keys)
                Add(key);
            foreach (var key in _churchPressureByRegionKey.Keys)
                Add(key);

            var anchors = CfgMgr.Cfgs?.TbCultAnchor?.DataList;
            if (anchors != null)
            {
                foreach (var cfg in anchors)
                {
                    if (cfg != null && CheckAnchorShowCondition(cfg)) Add(cfg.RegionKey);
                }
            }

            if (result.Count == 0)
                Add("default");
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public long AddLinkers(string regionKey, long amount)
        {
            if (string.IsNullOrEmpty(regionKey) || amount == 0)
            {
                return GetLinkerCount(regionKey);
            }

            var next = Math.Max(0, GetLinkerCount(regionKey) + amount);
            if (next == 0)
            {
                _linkerCountByRegionKey.Remove(regionKey);
            }
            else
            {
                _linkerCountByRegionKey[regionKey] = next;
            }

            OnCultChanged?.Invoke();
            return next;
        }

        public long SetLinkerCount(string regionKey, long amount)
        {
            var current = GetLinkerCount(regionKey);
            return AddLinkers(regionKey, Math.Max(0, amount) - current);
        }

        public long GetChurchPressure(string regionKey)
        {
            return !string.IsNullOrEmpty(regionKey)
                && _churchPressureByRegionKey.TryGetValue(regionKey, out var pressure)
                ? pressure : 0;
        }

        public int GetChurchPressureLevel(string regionKey)
        {
            var pressure = GetChurchPressure(regionKey);
            var level = 0;
            var configs = CfgMgr.Cfgs?.TbCultChurchPressureLevel?.DataList;
            if (configs == null) return level;

            foreach (var cfg in configs)
            {
                if (cfg != null && cfg.MinPressure <= pressure && cfg.Level > level)
                {
                    level = cfg.Level;
                }
            }

            return level;
        }

        public long AddChurchPressure(string regionKey, long amount)
        {
            if (string.IsNullOrEmpty(regionKey) || amount == 0)
            {
                return GetChurchPressure(regionKey);
            }

            var next = Math.Max(0, GetChurchPressure(regionKey) + amount);
            if (next == 0)
            {
                _churchPressureByRegionKey.Remove(regionKey);
            }
            else
            {
                _churchPressureByRegionKey[regionKey] = next;
            }

            OnCultChanged?.Invoke();
            return next;
        }

        public long SetChurchPressure(string regionKey, long amount)
        {
            var current = GetChurchPressure(regionKey);
            return AddChurchPressure(regionKey, Math.Max(0, amount) - current);
        }

        public long GetCultAttributeValue(ECultAttribute attribute)
            => _cultAttributes.TryGetValue(attribute, out var value) ? value : 0;

        public int UnlockedSeatTechNodeCount
        {
            get
            {
                var count = 0;
                foreach (var pair in _seatTechLevels)
                {
                    if (pair.Value > 0) count++;
                }
                return count;
            }
        }

        public void Initialize(GameLogicManager logic, SaveData savingData)
        {
            _logic = logic;
            _techLevels.Clear();
            _unlockedSeats.Clear();
            _seatTechLevels.Clear();
            _linkerCountByRegionKey.Clear();
            _secretUnits.Clear();
            _influenceByLogicAreaId.Clear();
            _anchors.Clear();
            _churchPressureByRegionKey.Clear();
            _faith = 0;

            var persist = savingData?.DemonCult;
            bool isFresh = persist == null
                || (persist.Faith <= 0
                    && (persist.TechNodes == null || persist.TechNodes.Count == 0));

            if (persist != null)
            {
                _faith = Math.Max(0, persist.Faith);
                if (persist.TechNodes != null)
                {
                    foreach (var entry in persist.TechNodes)
                    {
                        if (entry != null && entry.NodeId > 0 && entry.Level > 0)
                        {
                            _techLevels[entry.NodeId] = entry.Level;
                        }
                    }
                }
                if (persist.AncientSeats != null)
                {
                    foreach (var entry in persist.AncientSeats)
                    {
                        if (entry != null && entry.SeatId > 0 && entry.Unlocked)
                        {
                            _unlockedSeats.Add(entry.SeatId);
                        }
                    }
                }
                if (persist.SeatTechNodes != null)
                {
                    foreach (var entry in persist.SeatTechNodes)
                    {
                        if (entry != null && entry.SeatId > 0 && entry.NodeId > 0 && entry.Level > 0)
                            _seatTechLevels[(entry.SeatId, entry.NodeId)] = entry.Level;
                    }
                }
                if (persist.LinkerCountByRegionKey != null)
                {
                    foreach (var pair in persist.LinkerCountByRegionKey)
                    {
                        if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
                        {
                            _linkerCountByRegionKey[pair.Key] = pair.Value;
                        }
                    }
                }

                if (persist.ChurchPressureByRegionKey != null)
                {
                    foreach (var pair in persist.ChurchPressureByRegionKey)
                    {
                        if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
                        {
                            _churchPressureByRegionKey[pair.Key] = pair.Value;
                        }
                    }
                }
                if (persist.SecretUnits != null)
                {
                    foreach (var entry in persist.SecretUnits)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.UnitId)) continue;
                        _secretUnits[entry.UnitId] = new CultSecretUnitInfo
                        {
                            UnitId = entry.UnitId,
                            SourceId = entry.SourceId ?? string.Empty,
                            State = NormalizeSecretUnitState(entry.State),
                            AssignedRegionKey = entry.AssignedRegionKey ?? string.Empty,
                            MissionId = entry.MissionId ?? string.Empty,
                            AssignedLogicAreaId = entry.AssignedLogicAreaId ?? string.Empty,
                            AssignedAnchorId = entry.AssignedAnchorId ?? string.Empty,
                        };
                    }
                }
                if (persist.InfluenceByLogicAreaId != null)
                {
                    foreach (var pair in persist.InfluenceByLogicAreaId)
                    {
                        if (string.IsNullOrEmpty(pair.Key) || pair.Value?.Applications == null)
                        {
                            continue;
                        }

                        var applications = new List<CultInfluenceApplicationPersist>();
                        foreach (var entry in pair.Value.Applications)
                        {
                            if (entry == null || string.IsNullOrEmpty(entry.InfluenceId))
                            {
                                continue;
                            }

                            applications.Add(new CultInfluenceApplicationPersist
                            {
                                InfluenceId = entry.InfluenceId,
                                SourceRumorId = entry.SourceRumorId ?? string.Empty,
                                AppliedSettlementDay = entry.AppliedSettlementDay,
                            });
                        }

                        if (applications.Count > 0)
                        {
                            _influenceByLogicAreaId[pair.Key] = applications;
                        }
                    }
                }

                if (persist.Anchors != null)
                {
                    foreach (var entry in persist.Anchors)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.LogicAreaId) || string.IsNullOrEmpty(entry.AnchorId))
                        {
                            continue;
                        }

                        _anchors[MakeAnchorKey(entry.LogicAreaId, entry.AnchorId)] = new CultAnchorInfo
                        {
                            LogicAreaId = entry.LogicAreaId,
                            AnchorId = entry.AnchorId,
                            Level = Math.Max(1, entry.Level),
                            Progress = Math.Max(0, entry.Progress),
                            Established = entry.Established,
                            NextOutputSettlementDay = Math.Max(0, entry.NextOutputSettlementDay),
                            IsWatched = entry.IsWatched,
                            WatchedSinceSettlementDay = Math.Max(0, entry.WatchedSinceSettlementDay),
                            WatchedPressureLevel = Math.Max(0, entry.WatchedPressureLevel),
                            DisabledUntilSettlementDay = Math.Max(0, entry.DisabledUntilSettlementDay),
                        };
                    }
                }
            }

            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            if (seats != null)
            {
                foreach (var seat in seats)
                {
                    if (seat != null && seat.DefaultUnlocked)
                    {
                        _unlockedSeats.Add(seat.SeatId);
                    }
                }
            }

            // 教座之心默认点亮；新档给少量信仰便于调试布道树
            if (GetTechNodeLevel(BasicCultTechNodeId) <= 0)
            {
                _techLevels[BasicCultTechNodeId] = 1;
            }

            EnsureInitialSecretUnitFromBasicTech();

            if (isFresh)
            {
                _faith = 100;
            }

            RebuildCultAttributes();
            RefreshAutoUnlockedSeats();
        }

        public void SaveTo(SaveData savingData)
        {
            if (savingData == null)
            {
                return;
            }

            savingData.DemonCult ??= new DemonCultPersist();
            savingData.DemonCult.Faith = _faith;
            savingData.DemonCult.TechNodes ??= new List<CultTechNodeLevelPersist>();
            savingData.DemonCult.TechNodes.Clear();
            foreach (var pair in _techLevels)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                savingData.DemonCult.TechNodes.Add(new CultTechNodeLevelPersist
                {
                    NodeId = pair.Key,
                    Level = pair.Value,
                });
            }
            savingData.DemonCult.AncientSeats ??= new List<CultAncientSeatPersist>();
            savingData.DemonCult.AncientSeats.Clear();
            foreach (var seatId in _unlockedSeats)
            {
                savingData.DemonCult.AncientSeats.Add(new CultAncientSeatPersist { SeatId = seatId, Unlocked = true });
            }
            savingData.DemonCult.SeatTechNodes ??= new List<CultSeatTechNodeLevelPersist>();
            savingData.DemonCult.SeatTechNodes.Clear();
            foreach (var pair in _seatTechLevels)
            {
                if (pair.Value <= 0) continue;
                savingData.DemonCult.SeatTechNodes.Add(new CultSeatTechNodeLevelPersist
                {
                    SeatId = pair.Key.seatId,
                    NodeId = pair.Key.nodeId,
                    Level = pair.Value,
                });
            }

            savingData.DemonCult.LinkerCountByRegionKey ??= new Dictionary<string, long>();
            savingData.DemonCult.LinkerCountByRegionKey.Clear();
            foreach (var pair in _linkerCountByRegionKey)
            {
                if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
                {
                    savingData.DemonCult.LinkerCountByRegionKey[pair.Key] = pair.Value;
                }
            }

            savingData.DemonCult.ChurchPressureByRegionKey ??= new Dictionary<string, long>();
            savingData.DemonCult.ChurchPressureByRegionKey.Clear();
            foreach (var pair in _churchPressureByRegionKey)
            {
                if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
                {
                    savingData.DemonCult.ChurchPressureByRegionKey[pair.Key] = pair.Value;
                }
            }

            savingData.DemonCult.SecretUnits ??= new List<CultSecretUnitPersist>();
            savingData.DemonCult.SecretUnits.Clear();
            foreach (var unit in _secretUnits.Values)
            {
                savingData.DemonCult.SecretUnits.Add(new CultSecretUnitPersist
                {
                    UnitId = unit.UnitId,
                    SourceId = unit.SourceId,
                    State = unit.State,
                    AssignedRegionKey = unit.AssignedRegionKey,
                    MissionId = unit.MissionId,
                    AssignedLogicAreaId = unit.AssignedLogicAreaId,
                    AssignedAnchorId = unit.AssignedAnchorId,
                });
            }

            savingData.DemonCult.InfluenceByLogicAreaId ??= new Dictionary<string, CultInfluenceAreaPersist>();
            savingData.DemonCult.InfluenceByLogicAreaId.Clear();
            foreach (var pair in _influenceByLogicAreaId)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null || pair.Value.Count == 0)
                {
                    continue;
                }

                var area = new CultInfluenceAreaPersist();
                foreach (var entry in pair.Value)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.InfluenceId))
                    {
                        continue;
                    }

                    area.Applications.Add(new CultInfluenceApplicationPersist
                    {
                        InfluenceId = entry.InfluenceId,
                        SourceRumorId = entry.SourceRumorId ?? string.Empty,
                        AppliedSettlementDay = entry.AppliedSettlementDay,
                    });
                }

                if (area.Applications.Count > 0)
                {
                    savingData.DemonCult.InfluenceByLogicAreaId[pair.Key] = area;
                }
            }

            savingData.DemonCult.Anchors ??= new List<CultAnchorPersist>();
            savingData.DemonCult.Anchors.Clear();
            foreach (var anchor in _anchors.Values)
            {
                if (anchor == null || string.IsNullOrEmpty(anchor.LogicAreaId) || string.IsNullOrEmpty(anchor.AnchorId))
                {
                    continue;
                }

                savingData.DemonCult.Anchors.Add(new CultAnchorPersist
                {
                    LogicAreaId = anchor.LogicAreaId,
                    AnchorId = anchor.AnchorId,
                    Level = Math.Max(1, anchor.Level),
                    Progress = Math.Max(0, anchor.Progress),
                    Established = anchor.Established,
                    NextOutputSettlementDay = Math.Max(0, anchor.NextOutputSettlementDay),
                    IsWatched = anchor.IsWatched,
                    WatchedSinceSettlementDay = Math.Max(0, anchor.WatchedSinceSettlementDay),
                    WatchedPressureLevel = Math.Max(0, anchor.WatchedPressureLevel),
                    DisabledUntilSettlementDay = Math.Max(0, anchor.DisabledUntilSettlementDay),
                });
            }
        }

        public IReadOnlyList<CultAnchorInfo> GetAnchors(string logicAreaId = null, bool includeHidden = false)
        {
            var result = new List<CultAnchorInfo>();
            var table = CfgMgr.Cfgs?.TbCultAnchor?.DataList;
            if (table == null)
            {
                return result;
            }

            foreach (var cfg in table)
            {
                if (cfg == null || (logicAreaId != null && cfg.LogicAreaId != logicAreaId))
                {
                    continue;
                }

                if (!includeHidden && !CheckAnchorShowCondition(cfg))
                {
                    continue;
                }

                var state = GetOrCreateAnchorState(cfg);
                state.CurrentPressureLevel = GetChurchPressureLevel(cfg.RegionKey);
                result.Add(state);
            }

            return result;
        }

        public bool TryGetAnchor(string logicAreaId, string anchorId, out CultAnchorInfo anchor)
        {
            anchor = null;
            var cfg = FindAnchorConfig(logicAreaId, anchorId);
            if (cfg == null || !CheckAnchorShowCondition(cfg))
            {
                return false;
            }

            anchor = GetOrCreateAnchorState(cfg);
            return true;
        }

        public bool TryApplyAnchorAction(string logicAreaId, string actionId, int settlementDay, out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(actionId))
            {
                failReason = "invalid_action";
                return false;
            }

            var actions = CfgMgr.Cfgs?.TbCultAnchorAction?.DataList;
            if (actions == null)
            {
                failReason = "no_action_config";
                return false;
            }

            foreach (var action in actions)
            {
                if (action == null || action.LogicAreaId != logicAreaId || action.ActionId != actionId)
                {
                    continue;
                }

                var cfg = FindAnchorConfig(action.LogicAreaId, action.AnchorId);
                if (cfg == null || !CheckAnchorShowCondition(cfg))
                {
                    continue;
                }

                var state = GetOrCreateAnchorState(cfg);
                if (state.Established)
                {
                    failReason = "anchor_established";
                    return false;
                }
                if (state.Level != Math.Max(1, action.AnchorLevel))
                {
                    continue;
                }

                state.Progress = Math.Max(0, state.Progress + Math.Max(0, action.Progress) + GetAnchorProgressAcceleration());
                if (state.Progress >= Math.Max(1, cfg.EstablishProgress))
                {
                    state.Established = true;
                    state.NextOutputSettlementDay = Math.Max(1, settlementDay + Math.Max(1, GetOutputConfig(cfg)?.CycleDays ?? 1));
                }

                OnCultChanged?.Invoke();
                return true;
            }

            failReason = "action_not_mapped";
            return false;
        }

        public bool TryApplyAnchorCountermeasure(
            string logicAreaId,
            string anchorId,
            string actionId,
            int settlementDay,
            out string failReason)
        {
            failReason = null;
            var cfg = FindAnchorConfig(logicAreaId, anchorId);
            var action = CfgMgr.Cfgs?.TbCultAnchorPressureAction?.GetOrDefault(actionId);
            if (cfg == null || action == null)
            {
                failReason = "countermeasure_not_configured";
                return false;
            }

            var state = GetOrCreateAnchorState(cfg);
            if (!state.Established)
            {
                failReason = "anchor_not_established";
                return false;
            }
            if (state.IsDisabled(settlementDay))
            {
                failReason = "anchor_disabled";
                return false;
            }

            if (action.ClearWatch)
            {
                state.IsWatched = false;
                state.WatchedSinceSettlementDay = 0;
                state.WatchedPressureLevel = 0;
            }

            if (action.PressureReduction > 0)
            {
                AddChurchPressure(cfg.RegionKey, -action.PressureReduction);
            }

            OnCultChanged?.Invoke();
            return true;
        }

        public CultAnchorSettlementResult ApplyAnchorSettlement(int settlementDay)
        {
            var result = new CultAnchorSettlementResult();
            foreach (var state in _anchors.Values)
            {
                if (state == null || !state.Established)
                {
                    continue;
                }

                var cfg = FindAnchorConfig(state.LogicAreaId, state.AnchorId);
                var output = GetOutputConfig(cfg);
                if (cfg == null || output == null)
                {
                    continue;
                }

                state.CurrentPressureLevel = GetChurchPressureLevel(cfg.RegionKey);
                var pressureCfg = GetPressureLevelConfig(state.CurrentPressureLevel);
                var outputBonusPercent = GetAnchorOutputBonusPercent(state.LogicAreaId, state.AnchorId);
                var dailyLinkerAdd = ScaleByPercent(
                    Math.Max(0, output.DailyLinkerAdd),
                    100L + outputBonusPercent);
                var cycleDays = Math.Max(1, output.CycleDays);

                if (state.IsDisabled(settlementDay))
                {
                    state.NextOutputSettlementDay = Math.Max(
                        state.NextOutputSettlementDay,
                        state.DisabledUntilSettlementDay + cycleDays);
                    result.DisabledAnchorCount++;
                    continue;
                }

                if (state.DisabledUntilSettlementDay > 0)
                {
                    state.DisabledUntilSettlementDay = 0;
                    result.ReenabledAnchorCount++;
                }

                if (state.IsWatched)
                {
                    var watchedCfg = GetPressureLevelConfig(state.WatchedPressureLevel) ?? pressureCfg;
                    var warningDays = Math.Max(1, watchedCfg?.WarningDays ?? 1);
                    if (settlementDay - state.WatchedSinceSettlementDay >= warningDays)
                    {
                        state.IsWatched = false;
                        state.WatchedSinceSettlementDay = 0;
                        state.WatchedPressureLevel = 0;
                        state.DisabledUntilSettlementDay = settlementDay + Math.Max(1, watchedCfg?.DisableDays ?? 1);
                        state.NextOutputSettlementDay = state.DisabledUntilSettlementDay + cycleDays;
                        result.DisabledAnchorCount++;
                        continue;
                    }

                    result.WatchedAnchorCount++;
                }
                else if (pressureCfg != null
                    && pressureCfg.WatchChancePercent > 0
                    && UnityEngine.Random.Range(0, 100) < Math.Min(100, pressureCfg.WatchChancePercent))
                {
                    state.IsWatched = true;
                    state.WatchedSinceSettlementDay = settlementDay;
                    state.WatchedPressureLevel = pressureCfg.Level;
                    result.WatchedAnchorCount++;
                }

                if (dailyLinkerAdd > 0)
                {
                    AddLinkers(cfg.RegionKey, dailyLinkerAdd);
                    result.LinkersAdded += dailyLinkerAdd;
                }

                if (state.NextOutputSettlementDay <= 0)
                {
                    state.NextOutputSettlementDay = settlementDay + cycleDays;
                }

                while (state.NextOutputSettlementDay <= settlementDay)
                {
                    var efficiency = CalculateAnchorEfficiency(output, GetLinkerCount(cfg.RegionKey));
                    var amplifiedEfficiency = ScaleByPercent(efficiency, 100L + outputBonusPercent);
                    var faith = ScaleByPercent(output.Faith, amplifiedEfficiency);
                    if (faith > 0)
                    {
                        AddFaith(faith);
                        result.FaithAdded += faith;
                    }

                    var itemCount = ScaleByPercent(output.ItemCount, amplifiedEfficiency);
                    if (!string.IsNullOrEmpty(output.ItemId) && itemCount > 0 && _logic?.playerDataManager != null)
                    {
                        var gained = _logic.playerDataManager.GiveItemToPlayer(output.ItemId, itemCount);
                        if (gained > 0)
                        {
                            result.ItemOutputs[output.ItemId] = result.ItemOutputs.TryGetValue(output.ItemId, out var current)
                                ? current + gained : gained;
                        }
                    }

                    // 入梦资源仅保留配置入口，当前不实际发放。
                    result.ProcessedOutputCount++;
                    state.NextOutputSettlementDay += cycleDays;
                }
            }

            if (result.EstablishedAnchorCount > 0
                || result.ProcessedOutputCount > 0
                || result.LinkersAdded > 0
                || result.WatchedAnchorCount > 0
                || result.DisabledAnchorCount > 0
                || result.ReenabledAnchorCount > 0)
            {
                OnCultChanged?.Invoke();
            }
            return result;
        }

        static string MakeAnchorKey(string logicAreaId, string anchorId)
            => $"{logicAreaId}\u001f{anchorId}";

        CultAnchorInfo GetOrCreateAnchorState(CultAnchor cfg)
        {
            var key = MakeAnchorKey(cfg.LogicAreaId, cfg.AnchorId);
            if (!_anchors.TryGetValue(key, out var state))
            {
                state = new CultAnchorInfo { LogicAreaId = cfg.LogicAreaId, AnchorId = cfg.AnchorId };
                _anchors[key] = state;
            }
            return state;
        }

        CultAnchor FindAnchorConfig(string logicAreaId, string anchorId)
        {
            var table = CfgMgr.Cfgs?.TbCultAnchor?.DataList;
            if (table == null) return null;
            foreach (var cfg in table)
            {
                if (cfg != null && cfg.LogicAreaId == logicAreaId && cfg.AnchorId == anchorId)
                {
                    return cfg;
                }
            }
            return null;
        }

        CultAnchorOutput GetOutputConfig(CultAnchor cfg)
            => cfg == null ? null : CfgMgr.Cfgs?.TbCultAnchorOutput?.GetOrDefault(cfg.OutputId);

        CultChurchPressureLevel GetPressureLevelConfig(int level)
            => level <= 0 ? null : CfgMgr.Cfgs?.TbCultChurchPressureLevel?.GetOrDefault(level);

        bool CheckAnchorShowCondition(CultAnchor cfg)
            => cfg != null && (_logic == null || _logic.CheckCommonCondsAll(cfg.ShowConds));

        // 眷恋者 = 全局 FallenAmount / TotalFallPeopleAmount；当前预留但固定不加速。
        long GetAnchorProgressAcceleration() => 0;

        static long CalculateAnchorEfficiency(CultAnchorOutput output, long linkerCount)
        {
            var raw = output.BaseEfficiencyPercent + linkerCount * (long)output.EfficiencyPerLinkerPercent;
            return Math.Max(0, Math.Min(Math.Max(0, output.MaxEfficiencyPercent), raw));
        }

        static long ScaleByPercent(long value, long percent)
            => value <= 0 || percent <= 0 ? 0 : value * percent / 100;

        public IReadOnlyList<CultInfluenceApplicationPersist> GetInfluenceApplications(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId)
                || !_influenceByLogicAreaId.TryGetValue(logicAreaId, out var applications))
            {
                return Array.Empty<CultInfluenceApplicationPersist>();
            }

            return applications;
        }

        public bool HasAppliedInfluence(string logicAreaId, string influenceId, string sourceRumorId = null)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(influenceId)
                || !_influenceByLogicAreaId.TryGetValue(logicAreaId, out var applications))
            {
                return false;
            }

            foreach (var entry in applications)
            {
                if (entry != null && entry.InfluenceId == influenceId
                    && (string.IsNullOrEmpty(sourceRumorId) || entry.SourceRumorId == sourceRumorId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryApplyInfluence(
            string logicAreaId,
            string influenceId,
            string sourceRumorId,
            int appliedSettlementDay)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(influenceId))
            {
                return false;
            }

            if (!_influenceByLogicAreaId.TryGetValue(logicAreaId, out var applications))
            {
                applications = new List<CultInfluenceApplicationPersist>();
                _influenceByLogicAreaId[logicAreaId] = applications;
            }

            applications.Add(new CultInfluenceApplicationPersist
            {
                InfluenceId = influenceId,
                SourceRumorId = sourceRumorId ?? string.Empty,
                AppliedSettlementDay = appliedSettlementDay,
            });
            OnCultChanged?.Invoke();
            return true;
        }

        static ECultSecretUnitState NormalizeSecretUnitState(ECultSecretUnitState state)
        {
            return Enum.IsDefined(typeof(ECultSecretUnitState), state)
                ? state : ECultSecretUnitState.Available;
        }

        void EnsureInitialSecretUnitFromBasicTech()
        {
            if (GetTechNodeLevel(BasicCultTechNodeId) <= 0 || _secretUnits.Count > 0)
            {
                return;
            }

            TryAcquireSecretUnit("basic_cult_tech", out _);
        }

        public bool TryAcquireSecretUnit(string sourceId, out string unitId)
        {
            unitId = string.Empty;
            for (var index = 1; index < int.MaxValue; index++)
            {
                var candidate = $"secret_unit_{index}";
                if (_secretUnits.ContainsKey(candidate)) continue;

                _secretUnits[candidate] = new CultSecretUnitInfo
                {
                    UnitId = candidate,
                    SourceId = sourceId ?? string.Empty,
                    State = ECultSecretUnitState.Available,
                };
                unitId = candidate;
                OnCultChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryAssignSecretUnit(string unitId, string missionId, string regionKey, out string failReason)
        {
            failReason = null;
            if (!_secretUnits.TryGetValue(unitId ?? string.Empty, out var unit))
            {
                failReason = "unit_not_found";
                return false;
            }
            if (unit.State != ECultSecretUnitState.Available)
            {
                failReason = "unit_unavailable";
                return false;
            }
            if (string.IsNullOrEmpty(missionId))
            {
                failReason = "mission_required";
                return false;
            }

            unit.State = ECultSecretUnitState.OnMission;
            unit.MissionId = missionId;
            unit.AssignedRegionKey = regionKey ?? string.Empty;
            unit.AssignedLogicAreaId = string.Empty;
            unit.AssignedAnchorId = string.Empty;
            OnCultChanged?.Invoke();
            return true;
        }

        public bool TryAssignAmplifyAnchorMission(
            string unitId,
            string logicAreaId,
            string anchorId,
            out string failReason)
        {
            failReason = null;
            if (!_secretUnits.TryGetValue(unitId ?? string.Empty, out var unit))
            {
                failReason = "unit_not_found";
                return false;
            }
            if (unit.State != ECultSecretUnitState.Available)
            {
                failReason = "unit_unavailable";
                return false;
            }

            var mission = GetMissionByType(ECultMissionType.AmplifyAnchor);
            if (mission == null)
            {
                failReason = "mission_not_configured";
                return false;
            }
            if (!TryGetAnchor(logicAreaId, anchorId, out var anchor) || !anchor.Established)
            {
                failReason = "anchor_unavailable";
                return false;
            }
            if (IsAnchorAmplified(logicAreaId, anchorId))
            {
                failReason = "anchor_already_amplified";
                return false;
            }

            var anchorCfg = FindAnchorConfig(logicAreaId, anchorId);
            unit.State = ECultSecretUnitState.OnMission;
            unit.MissionId = mission.MissionId;
            unit.AssignedRegionKey = anchorCfg?.RegionKey ?? string.Empty;
            unit.AssignedLogicAreaId = logicAreaId ?? string.Empty;
            unit.AssignedAnchorId = anchorId ?? string.Empty;
            OnCultChanged?.Invoke();
            return true;
        }

        public CultMission GetMissionByType(ECultMissionType missionType)
        {
            var table = CfgMgr.Cfgs?.TbCultMission?.DataList;
            if (table == null) return null;
            for (var i = 0; i < table.Count; i++)
            {
                var mission = table[i];
                if (mission != null && mission.MissionType == missionType)
                    return mission;
            }
            return null;
        }

        public bool IsAnchorAmplified(string logicAreaId, string anchorId)
            => GetAnchorOutputBonusPercent(logicAreaId, anchorId) > 0;

        public int GetAnchorOutputBonusPercent(string logicAreaId, string anchorId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(anchorId)) return 0;
            foreach (var unit in _secretUnits.Values)
            {
                if (unit == null || unit.State != ECultSecretUnitState.OnMission
                    || unit.AssignedLogicAreaId != logicAreaId || unit.AssignedAnchorId != anchorId)
                {
                    continue;
                }

                var mission = CfgMgr.Cfgs?.TbCultMission?.GetOrDefault(unit.MissionId);
                if (mission?.MissionType == ECultMissionType.AmplifyAnchor)
                    return AmplifyAnchorOutputBonusPercent;
            }
            return 0;
        }

        public bool TryReleaseSecretUnit(string unitId, ECultSecretUnitState nextState = ECultSecretUnitState.Available)
        {
            if (!_secretUnits.TryGetValue(unitId ?? string.Empty, out var unit)
                || nextState == ECultSecretUnitState.OnMission
                || nextState == ECultSecretUnitState.Assigned)
            {
                return false;
            }

            unit.State = NormalizeSecretUnitState(nextState);
            if (unit.State == ECultSecretUnitState.Available || unit.State == ECultSecretUnitState.Recovering)
            {
                unit.MissionId = string.Empty;
                unit.AssignedRegionKey = string.Empty;
                unit.AssignedLogicAreaId = string.Empty;
                unit.AssignedAnchorId = string.Empty;
            }
            OnCultChanged?.Invoke();
            return true;
        }

        public bool IsSeatUnlocked(int seatId) => seatId <= 0 || _unlockedSeats.Contains(seatId);

        public int GetTechNodeCount(int seatId)
        {
            int count = 0;
            var table = CfgMgr.Cfgs?.TbCultSeatTechNode?.DataList;
            if (table == null) return count;
            foreach (var node in table)
            {
                if (node != null && node.SeatId == seatId && GetSeatTechNodeLevel(seatId, node.NodeId) > 0) count++;
            }
            return count;
        }

        public void RefreshAutoUnlockedSeats()
        {
            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            if (seats == null) return;
            var changed = false;
            foreach (var seat in seats)
            {
                if (seat == null || _unlockedSeats.Contains(seat.SeatId)) continue;
                if (seat.DefaultUnlocked || _logic == null || _logic.CheckCommonCondsAll(seat.UnlockConds))
                {
                    _unlockedSeats.Add(seat.SeatId);
                    changed = true;
                }
            }
            if (changed) OnCultChanged?.Invoke();
        }

        public int GetTechNodeLevel(int nodeId)
        {
            return _techLevels.TryGetValue(nodeId, out var level) ? level : 0;
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

        public int GetSeatTechNodeLevel(int seatId, int nodeId)
            => _seatTechLevels.TryGetValue((seatId, nodeId), out var level) ? level : 0;

        public CultTechNodeVisualState GetSeatTechNodeVisualState(int seatId, int nodeId)
        {
            var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(nodeId);
            if (node == null || node.SeatId != seatId || !IsSeatUnlocked(seatId)) return CultTechNodeVisualState.Locked;
            int current = GetSeatTechNodeLevel(seatId, nodeId);
            if (current >= Math.Max(1, node.MaxLevel)) return CultTechNodeVisualState.Unlocked;
            var level = CfgMgr.Cfgs?.TbCultSeatTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null || !CheckSeatPrerequisites(seatId, level)) return CultTechNodeVisualState.Locked;
            return _faith >= level.FaithCost ? CultTechNodeVisualState.Unlockable : CultTechNodeVisualState.InsufficientFaith;
        }

        public bool TryUnlockSeatTechNode(int seatId, int nodeId, out string failReason)
        {
            failReason = null;
            var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(nodeId);
            if (node == null || node.SeatId != seatId) { failReason = "no_cfg"; return false; }
            int current = GetSeatTechNodeLevel(seatId, nodeId);
            if (current >= Math.Max(1, node.MaxLevel)) { failReason = "max_level"; return false; }
            var level = CfgMgr.Cfgs?.TbCultSeatTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null || !CheckSeatPrerequisites(seatId, level)) { failReason = "prereq"; return false; }
            if (_faith < level.FaithCost) { failReason = "faith"; return false; }
            _faith -= level.FaithCost;
            _seatTechLevels[(seatId, nodeId)] = current + 1;
            OnCultChanged?.Invoke();
            return true;
        }

        bool CheckSeatPrerequisites(int seatId, CultSeatTechNodeLevel level)
        {
            if (level?.PrereqNodeIds == null) return true;
            foreach (var id in level.PrereqNodeIds)
            {
                if (GetSeatTechNodeLevel(seatId, id) <= 0) return false;
            }
            return true;
        }

        public void AddFaith(long amount)
        {
            if (amount == 0)
            {
                return;
            }

            _faith = Math.Max(0, _faith + amount);
            OnCultChanged?.Invoke();
        }

        public CultTechNodeVisualState GetTechNodeVisualState(int nodeId)
        {
            var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                return CultTechNodeVisualState.Locked;
            }
            int current = GetTechNodeLevel(nodeId);
            if (current >= Math.Max(1, node.MaxLevel))
            {
                return CultTechNodeVisualState.Unlocked;
            }

            var level = CfgMgr.Cfgs?.TbCultTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null)
            {
                return CultTechNodeVisualState.Locked;
            }

            if (!CheckPrerequisites(level))
            {
                return CultTechNodeVisualState.Locked;
            }

            return _faith >= level.FaithCost
                ? CultTechNodeVisualState.Unlockable
                : CultTechNodeVisualState.InsufficientFaith;
        }

        void RebuildCultAttributes()
        {
            _cultAttributes.Clear();
            foreach (var pair in _techLevels)
            {
                if (pair.Value <= 0) continue;
                for (var level = 1; level <= pair.Value; level++)
                {
                    var cfg = CfgMgr.Cfgs?.TbCultTechNodeLevel?.Get(pair.Key, level);
                    if (cfg == null || cfg.CultAttrValue == 0 || string.IsNullOrEmpty(cfg.CultAttr)) continue;
                    if (!Enum.TryParse(cfg.CultAttr, true, out ECultAttribute attribute)
                        || attribute == ECultAttribute.None)
                    {
                        continue;
                    }
                    _cultAttributes[attribute] = GetCultAttributeValue(attribute) + cfg.CultAttrValue;
                }
            }
        }

        public bool TryUnlockNode(int nodeId, out string failReason)
        {
            failReason = null;
            var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                failReason = "no_cfg";
                return false;
            }
            int current = GetTechNodeLevel(nodeId);
            if (current >= Math.Max(1, node.MaxLevel))
            {
                failReason = "max_level";
                return false;
            }

            var level = CfgMgr.Cfgs?.TbCultTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null)
            {
                failReason = "no_level_cfg";
                return false;
            }

            if (!CheckPrerequisites(level))
            {
                failReason = "prereq";
                return false;
            }

            if (_faith < level.FaithCost)
            {
                failReason = "faith";
                return false;
            }

            _faith -= level.FaithCost;
            _techLevels[nodeId] = current + 1;
            RebuildCultAttributes();
            OnCultChanged?.Invoke();
            return true;
        }

        bool CheckPrerequisites(CultTechNodeLevel level)
        {
            if (level?.PrereqNodeIds == null || level.PrereqNodeIds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < level.PrereqNodeIds.Count; i++)
            {
                if (GetTechNodeLevel(level.PrereqNodeIds[i]) <= 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
