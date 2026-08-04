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
        // 受控城镇日结信仰
        TownDailyFaith = 1,
        // 各地区日结自然皈依教徒数（多节点累加）
        RegionDailyLinker = 2,
        // 秘会席位上限（多节点累加）
        SecretUnitSlot = 3,
        // 教徒日产信仰系数：faith += totalLinkers * rate / 100
        LinkerFaithRate = 4,
        // 锚点周期额外信仰：extra = rarity * bonus
        AnchorRarityFaithBonus = 5,
        // 远征出征点基础产出百分比加成（特殊产出不吃）
        ScoutLootBonusPercent = 6,
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
        public string AssignedScoutSiteId { get; internal set; } = string.Empty;
        // 0 = 永久；>0 表示该结算日完成
        public int MissionEndsSettlementDay { get; internal set; }
        // 唯一能力值 0~100
        public int Capability { get; internal set; }
    }

    public sealed class CultSecretMissionSettlementResult
    {
        public int CompletedCount;
        public long FaithAdded;
        public long LinkersAdded;
        public long PressureReduced;
        public int WatchClearedCount;
        public int DisableClearedCount;
        public Dictionary<string, long> ItemOutputs { get; } = new(StringComparer.Ordinal);
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

    // 教团：信仰账户 + 布道科技属性结算 + 锚点/秘会
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
                            AssignedScoutSiteId = entry.AssignedScoutSiteId ?? string.Empty,
                            MissionEndsSettlementDay = Math.Max(0, entry.MissionEndsSettlementDay),
                            Capability = NormalizeCapabilityFromPersist(entry),
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

            if (isFresh)
            {
                _faith = 100;
            }

            RebuildCultAttributes();
            SyncSecretUnitSlots();
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
                    AssignedScoutSiteId = unit.AssignedScoutSiteId,
                    MissionEndsSettlementDay = unit.MissionEndsSettlementDay,
                    Capability = Math.Clamp(unit.Capability, 0, 100),
                    Aptitude = 0,
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

                var coverInProgress = IsCoverTracesMissionOnAnchor(state.LogicAreaId, state.AnchorId);
                if (state.IsWatched)
                {
                    var watchedCfg = GetPressureLevelConfig(state.WatchedPressureLevel) ?? pressureCfg;
                    var warningDays = Math.Max(1, watchedCfg?.WarningDays ?? 1);
                    // 灭迹进行中：暂缓由盯梢转入封锁
                    if (!coverInProgress
                        && settlementDay - state.WatchedSinceSettlementDay >= warningDays)
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
                else if (!coverInProgress
                    && pressureCfg != null
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
                    var rarityBonus = Math.Max(0, cfg.Rarity)
                        * GetCultAttributeValue(ECultAttribute.AnchorRarityFaithBonus);
                    if (rarityBonus > 0)
                    {
                        faith += rarityBonus;
                    }

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
                || result.FaithAdded > 0
                || result.WatchedAnchorCount > 0
                || result.DisabledAnchorCount > 0
                || result.ReenabledAnchorCount > 0)
            {
                OnCultChanged?.Invoke();
            }
            return result;
        }

        // 灵感皈依：每个已知 region 日结增加 RegionDailyLinker 教徒
        public long ApplyRegionDailyLinkerSettlement()
        {
            var perRegion = GetCultAttributeValue(ECultAttribute.RegionDailyLinker);
            if (perRegion <= 0)
            {
                return 0;
            }

            long added = 0;
            var regions = GetKnownRegionKeys();
            for (var i = 0; i < regions.Count; i++)
            {
                AddLinkers(regions[i], perRegion);
                added += perRegion;
            }

            return added;
        }

        // 低语什一：按总教徒产微量日结信仰
        public long ApplyLinkerFaithSettlement()
        {
            var rate = GetCultAttributeValue(ECultAttribute.LinkerFaithRate);
            if (rate <= 0)
            {
                return 0;
            }

            var faith = GetTotalLinkerCount() * rate / 100;
            if (faith <= 0)
            {
                return 0;
            }

            AddFaith(faith);
            return faith;
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

        // 按 SecretUnitSlot 属性补齐秘会席位（只增不减）
        void SyncSecretUnitSlots()
        {
            var target = (int)Math.Max(0, GetCultAttributeValue(ECultAttribute.SecretUnitSlot));
            while (_secretUnits.Count < target)
            {
                if (!TryAcquireSecretUnit($"cult_slot_{_secretUnits.Count + 1}", out _))
                {
                    break;
                }
            }
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
                    Capability = RollSecretUnitCapability(),
                };
                unitId = candidate;
                OnCultChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryAssignAmplifyAnchorMission(
            string unitId,
            string logicAreaId,
            string anchorId,
            out string failReason)
        {
            failReason = null;
            if (!TryGetAvailableUnit(unitId, out var unit, out failReason)) return false;

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
            BeginMission(unit, mission.MissionId, anchorCfg?.RegionKey, logicAreaId, anchorId, null, 0);
            return true;
        }

        public bool TryAssignScoutGatherMission(string unitId, string siteId, out string failReason)
        {
            failReason = null;
            if (!TryGetAvailableUnit(unitId, out var unit, out failReason)) return false;

            var mission = GetMissionByType(ECultMissionType.ScoutGather);
            if (mission == null)
            {
                failReason = "mission_not_configured";
                return false;
            }

            var site = CfgMgr.Cfgs?.TbCultScoutSite?.GetOrDefault(siteId);
            if (site == null)
            {
                failReason = "site_not_found";
                return false;
            }
            if (!IsScoutSiteUnlocked(site))
            {
                failReason = "site_locked";
                return false;
            }
            if (IsScoutSiteOccupied(site.SiteId))
            {
                failReason = "site_occupied";
                return false;
            }

            var duration = site.DurationDays > 0 ? site.DurationDays : Math.Max(1, mission.DurationDays);
            BeginMission(unit, mission.MissionId, site.RegionKey, site.LogicAreaId, null, site.SiteId, CalcMissionEndDay(duration));
            return true;
        }

        public bool TryAssignPreachMission(string unitId, string regionKey, out string failReason)
        {
            failReason = null;
            if (!TryGetAvailableUnit(unitId, out var unit, out failReason)) return false;

            var mission = GetMissionByType(ECultMissionType.Preach);
            if (mission == null)
            {
                failReason = "mission_not_configured";
                return false;
            }
            if (string.IsNullOrEmpty(regionKey))
            {
                failReason = "region_required";
                return false;
            }

            var duration = Math.Max(1, mission.DurationDays);
            BeginMission(unit, mission.MissionId, regionKey, null, null, null, CalcMissionEndDay(duration));
            return true;
        }

        public bool TryAssignOfferEssenceMission(string unitId, string regionKey, out string failReason)
        {
            failReason = null;
            if (!TryGetAvailableUnit(unitId, out var unit, out failReason)) return false;

            var mission = GetMissionByType(ECultMissionType.OfferEssence);
            if (mission == null)
            {
                failReason = "mission_not_configured";
                return false;
            }
            if (string.IsNullOrEmpty(regionKey))
            {
                failReason = "region_required";
                return false;
            }

            var duration = Math.Max(1, mission.DurationDays);
            BeginMission(unit, mission.MissionId, regionKey, null, null, null, CalcMissionEndDay(duration));
            return true;
        }

        public CultOfferEssenceRule GetOfferEssenceRule(int capability)
        {
            capability = Math.Clamp(capability, 0, 100);
            var table = CfgMgr.Cfgs?.TbCultOfferEssenceRule;
            if (table == null) return null;

            CultOfferEssenceRule best = null;
            foreach (var row in table.DataList)
            {
                if (row == null || row.CapabilityMin > capability) continue;
                if (best == null || row.CapabilityMin > best.CapabilityMin) best = row;
            }
            return best;
        }

        static int NormalizeCapabilityFromPersist(CultSecretUnitPersist entry)
        {
            if (entry == null) return 0;
            if (entry.Capability > 0) return Math.Clamp(entry.Capability, 0, 100);
            // 旧适性 1~5 → 能力值 10/20/35/50/65
            if (entry.Aptitude > 0)
            {
                return entry.Aptitude switch
                {
                    1 => 10,
                    2 => 20,
                    3 => 35,
                    4 => 50,
                    _ => 65,
                };
            }
            return 0;
        }

        static int RollSecretUnitCapability()
        {
            // 初值约 8~18，贴近升级表第一档前后
            return UnityEngine.Random.Range(8, 19);
        }

        public CultSecretCapabilityLevel GetNextCapabilityLevel(CultSecretUnitInfo unit)
        {
            if (unit == null) return null;
            var table = CfgMgr.Cfgs?.TbCultSecretCapabilityLevel?.DataList;
            if (table == null) return null;
            CultSecretCapabilityLevel best = null;
            for (var i = 0; i < table.Count; i++)
            {
                var row = table[i];
                if (row == null || row.Capability <= unit.Capability) continue;
                if (best == null || row.Capability < best.Capability) best = row;
            }
            return best;
        }

        public bool TryUpgradeSecretUnitCapability(string unitId, out string failReason)
        {
            failReason = null;
            if (!TryGetAvailableUnit(unitId, out var unit, out failReason)) return false;

            var next = GetNextCapabilityLevel(unit);
            if (next == null)
            {
                failReason = "max_capability";
                return false;
            }

            if (_logic?.worldPersistState == null || _logic.playerDataManager == null)
            {
                failReason = "invalid";
                return false;
            }

            if (next.CostJingyuan > 0
                && My.SecretBase.JingYuanPoolService.GetStored(_logic) < next.CostJingyuan)
            {
                failReason = "jingyuan_not_enough";
                return false;
            }

            if (next.CostFaith > 0 && Faith < next.CostFaith)
            {
                failReason = "faith_not_enough";
                return false;
            }

            if (!string.IsNullOrEmpty(next.CostItemId) && next.CostItemCount > 0
                && !_logic.playerDataManager.CheckHaveItem(next.CostItemId, next.CostItemCount))
            {
                failReason = "item_not_enough";
                return false;
            }

            if (next.CostJingyuan > 0)
                _logic.worldPersistState.AddJingYuanPoolStored(-next.CostJingyuan);
            if (next.CostFaith > 0)
                AddFaith(-next.CostFaith);
            if (!string.IsNullOrEmpty(next.CostItemId) && next.CostItemCount > 0)
                _logic.playerDataManager.CostItem(next.CostItemId, next.CostItemCount);

            unit.Capability = Math.Clamp(next.Capability, 0, 100);
            OnCultChanged?.Invoke();
            return true;
        }

        public bool TryAssignCoverTracesMission(
            string unitId,
            string logicAreaId,
            string anchorId,
            out string failReason)
        {
            failReason = null;
            if (!TryGetAvailableUnit(unitId, out var unit, out failReason)) return false;

            var mission = GetMissionByType(ECultMissionType.CoverTraces);
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

            var settlementDay = GetUpcomingSettlementDay();
            var needsCover = anchor.IsWatched || anchor.IsDisabled(settlementDay);
            if (!needsCover)
            {
                failReason = "anchor_not_threatened";
                return false;
            }
            if (IsCoverTracesMissionOnAnchor(logicAreaId, anchorId))
            {
                failReason = "cover_already_assigned";
                return false;
            }

            var anchorCfg = FindAnchorConfig(logicAreaId, anchorId);
            var duration = Math.Max(1, mission.DurationDays);
            BeginMission(unit, mission.MissionId, anchorCfg?.RegionKey, logicAreaId, anchorId, null, CalcMissionEndDay(duration));
            return true;
        }

        public CultSecretMissionSettlementResult ApplySecretMissionSettlement(int settlementDay)
        {
            var result = new CultSecretMissionSettlementResult();
            var completed = new List<string>();
            foreach (var unit in _secretUnits.Values)
            {
                if (unit == null || unit.State != ECultSecretUnitState.OnMission) continue;
                if (unit.MissionEndsSettlementDay <= 0 || unit.MissionEndsSettlementDay > settlementDay) continue;
                completed.Add(unit.UnitId);
            }

            for (var i = 0; i < completed.Count; i++)
            {
                if (!_secretUnits.TryGetValue(completed[i], out var unit) || unit == null) continue;
                CompleteTimedMission(unit, settlementDay, result);
                result.CompletedCount++;
            }

            if (result.CompletedCount > 0) OnCultChanged?.Invoke();
            return result;
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

        public IReadOnlyList<CultMission> GetDispatchableMissions()
        {
            var result = new List<CultMission>();
            var table = CfgMgr.Cfgs?.TbCultMission?.DataList;
            if (table == null) return result;
            for (var i = 0; i < table.Count; i++)
            {
                var mission = table[i];
                if (mission != null && mission.MissionType != ECultMissionType.None)
                    result.Add(mission);
            }
            return result;
        }

        public IReadOnlyList<CultScoutSite> GetUnlockedScoutSites()
        {
            var result = new List<CultScoutSite>();
            var table = CfgMgr.Cfgs?.TbCultScoutSite?.DataList;
            if (table == null) return result;
            for (var i = 0; i < table.Count; i++)
            {
                var site = table[i];
                if (site != null && IsScoutSiteUnlocked(site))
                    result.Add(site);
            }
            return result;
        }

        public bool IsScoutSiteUnlocked(CultScoutSite site)
            => site != null && (_logic == null || _logic.CheckCommonCondsAll(site.UnlockConds));

        public bool IsAnchorAmplified(string logicAreaId, string anchorId)
            => GetAnchorOutputBonusPercent(logicAreaId, anchorId) > 0;

        public bool IsCoverTracesMissionOnAnchor(string logicAreaId, string anchorId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(anchorId)) return false;
            foreach (var unit in _secretUnits.Values)
            {
                if (unit == null || unit.State != ECultSecretUnitState.OnMission) continue;
                if (unit.AssignedLogicAreaId != logicAreaId || unit.AssignedAnchorId != anchorId) continue;
                var mission = CfgMgr.Cfgs?.TbCultMission?.GetOrDefault(unit.MissionId);
                if (mission?.MissionType == ECultMissionType.CoverTraces) return true;
            }
            return false;
        }

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

        public int GetMissionRemainingDays(CultSecretUnitInfo unit)
        {
            if (unit == null || unit.MissionEndsSettlementDay <= 0) return 0;
            var upcoming = GetUpcomingSettlementDay();
            return Math.Max(0, unit.MissionEndsSettlementDay - upcoming + 1);
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
                ClearMissionAssignment(unit);
            }
            OnCultChanged?.Invoke();
            return true;
        }

        bool TryGetAvailableUnit(string unitId, out CultSecretUnitInfo unit, out string failReason)
        {
            unit = null;
            failReason = null;
            if (!_secretUnits.TryGetValue(unitId ?? string.Empty, out unit))
            {
                failReason = "unit_not_found";
                return false;
            }
            if (unit.State != ECultSecretUnitState.Available)
            {
                failReason = "unit_unavailable";
                return false;
            }
            return true;
        }

        void BeginMission(
            CultSecretUnitInfo unit,
            string missionId,
            string regionKey,
            string logicAreaId,
            string anchorId,
            string scoutSiteId,
            int endsSettlementDay)
        {
            unit.State = ECultSecretUnitState.OnMission;
            unit.MissionId = missionId ?? string.Empty;
            unit.AssignedRegionKey = regionKey ?? string.Empty;
            unit.AssignedLogicAreaId = logicAreaId ?? string.Empty;
            unit.AssignedAnchorId = anchorId ?? string.Empty;
            unit.AssignedScoutSiteId = scoutSiteId ?? string.Empty;
            unit.MissionEndsSettlementDay = Math.Max(0, endsSettlementDay);
            OnCultChanged?.Invoke();
        }

        void ClearMissionAssignment(CultSecretUnitInfo unit)
        {
            if (unit == null) return;
            unit.MissionId = string.Empty;
            unit.AssignedRegionKey = string.Empty;
            unit.AssignedLogicAreaId = string.Empty;
            unit.AssignedAnchorId = string.Empty;
            unit.AssignedScoutSiteId = string.Empty;
            unit.MissionEndsSettlementDay = 0;
        }

        int GetUpcomingSettlementDay() => (_logic?.SettlementDayIndex ?? 0) + 1;

        int CalcMissionEndDay(int durationDays)
            => (_logic?.SettlementDayIndex ?? 0) + Math.Max(1, durationDays);

        bool IsScoutSiteOccupied(string siteId)
        {
            if (string.IsNullOrEmpty(siteId)) return false;
            foreach (var unit in _secretUnits.Values)
            {
                if (unit != null
                    && unit.State == ECultSecretUnitState.OnMission
                    && unit.AssignedScoutSiteId == siteId)
                {
                    return true;
                }
            }
            return false;
        }

        void CompleteTimedMission(CultSecretUnitInfo unit, int settlementDay, CultSecretMissionSettlementResult result)
        {
            var mission = CfgMgr.Cfgs?.TbCultMission?.GetOrDefault(unit.MissionId);
            if (mission == null)
            {
                unit.State = ECultSecretUnitState.Available;
                ClearMissionAssignment(unit);
                return;
            }

            switch (mission.MissionType)
            {
                case ECultMissionType.ScoutGather:
                    CompleteScoutGather(unit, mission, result);
                    break;
                case ECultMissionType.Preach:
                    CompletePreach(unit, mission, result);
                    break;
                case ECultMissionType.CoverTraces:
                    CompleteCoverTraces(unit, mission, settlementDay, result);
                    break;
                case ECultMissionType.OfferEssence:
                    CompleteOfferEssence(unit, result);
                    break;
            }

            unit.State = ECultSecretUnitState.Available;
            ClearMissionAssignment(unit);
        }

        void CompleteScoutGather(CultSecretUnitInfo unit, CultMission mission, CultSecretMissionSettlementResult result)
        {
            var site = CfgMgr.Cfgs?.TbCultScoutSite?.GetOrDefault(unit.AssignedScoutSiteId);
            var bonusPercent = GetCultAttributeValue(ECultAttribute.ScoutLootBonusPercent);
            long ScaleBase(long value)
            {
                if (value <= 0) return 0;
                if (bonusPercent <= 0) return value;
                return value * (100 + bonusPercent) / 100;
            }

            long faith;
            long linkers;
            string baseItemId;
            long baseItemCount;
            string specialItemId;
            long specialItemCount;
            int specialChance;
            string region;

            if (site != null)
            {
                faith = ScaleBase(site.BaseFaith);
                linkers = ScaleBase(site.BaseLinker);
                baseItemId = site.BaseItemId;
                baseItemCount = ScaleBase(site.BaseItemCount);
                specialItemId = site.SpecialItemId;
                specialItemCount = site.SpecialItemCount;
                specialChance = Math.Max(0, site.SpecialChancePercent);
                region = site.RegionKey;
            }
            else
            {
                // 无站点时回退任务表微量奖励（仍吃基础加成）
                faith = ScaleBase(mission.FaithReward);
                linkers = ScaleBase(mission.LinkerReward);
                baseItemId = mission.ItemId;
                baseItemCount = ScaleBase(mission.ItemCount);
                specialItemId = string.Empty;
                specialItemCount = 0;
                specialChance = 0;
                region = unit.AssignedRegionKey;
            }

            if (faith > 0)
            {
                AddFaith(faith);
                result.FaithAdded += faith;
            }
            if (linkers > 0 && !string.IsNullOrEmpty(region))
            {
                AddLinkers(region, linkers);
                result.LinkersAdded += linkers;
            }
            GrantMissionItem(baseItemId, baseItemCount, result);

            // 特殊产出：独立概率，不吃 ScoutLootBonusPercent
            if (!string.IsNullOrEmpty(specialItemId)
                && specialItemCount > 0
                && specialChance > 0
                && UnityEngine.Random.Range(0, 100) < Math.Min(100, specialChance))
            {
                GrantMissionItem(specialItemId, specialItemCount, result);
            }
        }

        void CompletePreach(CultSecretUnitInfo unit, CultMission mission, CultSecretMissionSettlementResult result)
        {
            if (mission.FaithReward > 0)
            {
                AddFaith(mission.FaithReward);
                result.FaithAdded += mission.FaithReward;
            }
            if (mission.LinkerReward > 0 && !string.IsNullOrEmpty(unit.AssignedRegionKey))
            {
                AddLinkers(unit.AssignedRegionKey, mission.LinkerReward);
                result.LinkersAdded += mission.LinkerReward;
            }
            GrantMissionItem(mission.ItemId, mission.ItemCount, result);
        }

        void CompleteOfferEssence(CultSecretUnitInfo unit, CultSecretMissionSettlementResult result)
        {
            var rule = GetOfferEssenceRule(unit.Capability);
            if (rule == null) return;

            if (rule.JingyuanReward > 0 && _logic != null)
            {
                // 入池允许短暂超额，下一日结开始时再丢弃仍未消化的部分
                My.SecretBase.JingYuanPoolService.TryAddToPool(_logic, rule.JingyuanReward, out var accepted, out _);
                if (accepted > 0)
                {
                    result.ItemOutputs["jingyuan_pool"] = result.ItemOutputs.TryGetValue("jingyuan_pool", out var cur)
                        ? cur + accepted : accepted;
                }
            }

            if (rule.PremiumChancePercent > 0
                && rule.PremiumType != EJingYuanType.None
                && UnityEngine.Random.Range(0, 100) < Math.Min(100, rule.PremiumChancePercent))
            {
                var essenceSys = _logic?.playerDataManager?.JingYuanEssenceSystem;
                essenceSys?.CreateAndAddAbstractDrop(
                    rule.PremiumType,
                    Math.Max(1, rule.PremiumDropLevel),
                    Math.Max(1, rule.PremiumQualityTier),
                    "cult_offer_essence",
                    PremiumEssenceStorageState.Warehouse);
            }
        }

        void CompleteCoverTraces(
            CultSecretUnitInfo unit,
            CultMission mission,
            int settlementDay,
            CultSecretMissionSettlementResult result)
        {
            if (!TryGetAnchor(unit.AssignedLogicAreaId, unit.AssignedAnchorId, out var anchor) || anchor == null)
            {
                return;
            }

            if (mission.ClearWatch && anchor.IsWatched)
            {
                anchor.IsWatched = false;
                anchor.WatchedSinceSettlementDay = 0;
                anchor.WatchedPressureLevel = 0;
                result.WatchClearedCount++;
            }

            if (mission.ClearDisable && anchor.DisabledUntilSettlementDay > 0)
            {
                anchor.DisabledUntilSettlementDay = 0;
                result.DisableClearedCount++;
            }

            if (mission.PressureReduction > 0 && !string.IsNullOrEmpty(unit.AssignedRegionKey))
            {
                AddChurchPressure(unit.AssignedRegionKey, -mission.PressureReduction);
                result.PressureReduced += mission.PressureReduction;
            }

            if (mission.FaithReward > 0)
            {
                AddFaith(mission.FaithReward);
                result.FaithAdded += mission.FaithReward;
            }
            GrantMissionItem(mission.ItemId, mission.ItemCount, result);
        }

        void GrantMissionItem(string itemId, long itemCount, CultSecretMissionSettlementResult result)
        {
            if (string.IsNullOrEmpty(itemId) || itemCount <= 0 || _logic?.playerDataManager == null) return;
            var gained = _logic.playerDataManager.GiveItemToPlayer(itemId, itemCount);
            if (gained <= 0) return;
            result.ItemOutputs[itemId] = result.ItemOutputs.TryGetValue(itemId, out var cur) ? cur + gained : gained;
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
            SyncSecretUnitSlots();
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
