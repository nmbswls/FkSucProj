// 跨图「地图实体」交互持久状态（垂钓点、废墟等），非 RPG 全局开关。
// 当前随 SaveData 整体进内存；若未来拆分 per-map 存档或联机分 Region，再评估按图分桶或按需从盘加载。

using System.Collections.Generic;
using System.Linq;
using My.Config;
using My.Saving;
using System;
using UnityEngine;
using My.Map;

namespace My
{
    public class GameWorldPersistStateManager
    {
        private readonly Dictionary<string, FishingSpotRuntimeSave> _fishingRuntime = new();
        private readonly Dictionary<string, RenewableResourceNodeRuntimeSave> _renewableResourceRuntime = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RepairPointRuntimeSave> _ruinRuntime = new();
        private readonly Dictionary<string, SavePointUnlockPersist> _savePointUnlockById = new(StringComparer.Ordinal);
        private readonly HashSet<string> _secretBaseUnlockedFacilities = new(StringComparer.Ordinal);
        private int _secretBaseBuildLevel = 1;

        public readonly WorldNpcCharacterPersistRegistry NpcCharacters = new();

        public readonly MapInteractPointPersistRegistry MapInteractPoints = new();

        // mapId|triggerId -> consumed
        private readonly Dictionary<string, bool> _microPlotConsumed = new();

        // logic_area map_id -> homestead runtime
        private readonly Dictionary<string, LogicAreaHomesteadPersist> _logicAreaHomesteadByMapId = new(StringComparer.Ordinal);

        public event Action<string, LogicAreaHomesteadPersist> EvOnLogicAreaHomesteadChanged;

        public GameWorldPersistStateManager()
        {
        }

        // 小剧情触发器是否已消耗（成功或按配置在中断后消耗）。
        public bool IsMicroPlotTriggerConsumed(string mapId, string triggerId)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(triggerId))
            {
                return false;
            }

            var key = MicroPlotPersistKey(mapId, triggerId);
            return _microPlotConsumed.TryGetValue(key, out var v) && v;
        }

        // 将小剧情触发器标记为已消耗（写入内存并在下次存档时进入 SaveData）。
        public void MarkMicroPlotConsumed(string mapId, string triggerId)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(triggerId))
            {
                return;
            }

            _microPlotConsumed[MicroPlotPersistKey(mapId, triggerId)] = true;
        }

        public static string MicroPlotPersistKey(string mapId, string triggerId) => $"{mapId}|{triggerId}";

        public void InitFromSave(SaveData savingData)
        {
            _fishingRuntime.Clear();
            _renewableResourceRuntime.Clear();
            _microPlotConsumed.Clear();
            if (savingData?.PlayerData?.FishingSpotByUniqName != null)
            {
                foreach (var kv in savingData.PlayerData.FishingSpotByUniqName)
                {
                    _fishingRuntime[kv.Key] = new FishingSpotRuntimeSave
                    {
                        CfgId = kv.Value.CfgId,
                        Remaining = kv.Value.Remaining,
                        LastRestockSettlementDayIndex = kv.Value.LastRestockSettlementDayIndex,
                    };
                }
            }

            if (savingData?.PlayerData?.RenewableResourceNodeByUniqName != null)
            {
                foreach (var kv in savingData.PlayerData.RenewableResourceNodeByUniqName)
                {
                    if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                    {
                        continue;
                    }

                    _renewableResourceRuntime[kv.Key] = new RenewableResourceNodeRuntimeSave
                    {
                        CfgId = kv.Value.CfgId,
                        PermanentlyUnlocked = kv.Value.PermanentlyUnlocked,
                        UnlockProgress = kv.Value.UnlockProgress,
                        StoredResources = kv.Value.StoredResources,
                        NextReadySettlementDay = kv.Value.NextReadySettlementDay,
                    };
                }
            }

            _ruinRuntime.Clear();
            if (savingData?.PlayerData?.HomeRuinByUniqName != null)
            {
                foreach (var kv in savingData.PlayerData.HomeRuinByUniqName)
                {
                    var v = kv.Value;
                    var putCopy = new Dictionary<string, long>();
                    if (v.PutMaterial != null)
                    {
                        foreach (var p in v.PutMaterial)
                        {
                            putCopy[p.Key] = p.Value;
                        }
                    }

                    _ruinRuntime[kv.Key] = new RepairPointRuntimeSave
                    {
                        UniqName = string.IsNullOrEmpty(v.UniqName) ? kv.Key : v.UniqName,
                        IsRepaired = v.IsRepaired,
                        RepairProgress = v.RepairProgress,
                        PutMaterial = putCopy,
                    };
                }
            }

            NpcCharacters.LoadFromSave(savingData?.PlayerData?.NpcCharacterPersistByKey);

            MapInteractPoints.LoadFromSave(savingData?.PlayerData?.InteractPointByUniqName);

            if (savingData?.PlayerData?.MicroPlotConsumedByKey != null)
            {
                foreach (var kv in savingData.PlayerData.MicroPlotConsumedByKey)
                {
                    if (kv.Value)
                    {
                        _microPlotConsumed[kv.Key] = true;
                    }
                }
            }

            _savePointUnlockById.Clear();
            if (savingData?.PlayerData?.SavePointUnlocks != null)
            {
                foreach (var row in savingData.PlayerData.SavePointUnlocks)
                {
                    if (row == null || string.IsNullOrEmpty(row.SavePointId))
                    {
                        continue;
                    }

                    var put = new Dictionary<string, long>(StringComparer.Ordinal);
                    if (row.TributePut != null)
                    {
                        foreach (var p in row.TributePut)
                        {
                            put[p.Key] = p.Value;
                        }
                    }

                    var loaded = new SavePointUnlockPersist
                    {
                        SavePointId = row.SavePointId,
                        Unlocked = row.Unlocked,
                        TributeSubmitted = row.TributeSubmitted,
                        TributePut = put,
                    };
                    SavePointUnlockHelper.NormalizePersistAfterLoad(loaded);
                    _savePointUnlockById[row.SavePointId] = loaded;
                }
            }

            _secretBaseUnlockedFacilities.Clear();
            if (savingData?.PlayerData?.SecretBaseUnlockedFacilityIds != null)
            {
                foreach (var id in savingData.PlayerData.SecretBaseUnlockedFacilityIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _secretBaseUnlockedFacilities.Add(id);
                    }
                }
            }

            _secretBaseBuildLevel = savingData?.PlayerData?.SecretBaseBuildLevel ?? 1;
            if (_secretBaseBuildLevel < 1)
            {
                _secretBaseBuildLevel = 1;
            }

            _logicAreaHomesteadByMapId.Clear();
            if (savingData?.PlayerData?.LogicAreaHomesteadByMapId != null)
            {
                foreach (var kv in savingData.PlayerData.LogicAreaHomesteadByMapId)
                {
                    if (string.IsNullOrEmpty(kv.Key) || kv.Value == null || !HasMeaningfulHomesteadState(kv.Value))
                    {
                        continue;
                    }

                    _logicAreaHomesteadByMapId[kv.Key] = CloneHomesteadPersist(kv.Value);
                }
            }
        }

        public void ApplyRuntimeToSaveData(SaveData data)
        {
            if (data.PlayerData == null)
            {
                data.PlayerData = new PlayerData();
            }

            data.PlayerData.FishingSpotByUniqName.Clear();
            foreach (var kv in _fishingRuntime)
            {
                data.PlayerData.FishingSpotByUniqName[kv.Key] = new FishingSpotRuntimeSave
                {
                    CfgId = kv.Value.CfgId,
                    Remaining = kv.Value.Remaining,
                    LastRestockSettlementDayIndex = kv.Value.LastRestockSettlementDayIndex,
                };
            }

            data.PlayerData.RenewableResourceNodeByUniqName ??= new Dictionary<string, RenewableResourceNodeRuntimeSave>(StringComparer.Ordinal);
            data.PlayerData.RenewableResourceNodeByUniqName.Clear();
            foreach (var kv in _renewableResourceRuntime)
            {
                data.PlayerData.RenewableResourceNodeByUniqName[kv.Key] = new RenewableResourceNodeRuntimeSave
                {
                    CfgId = kv.Value.CfgId,
                    PermanentlyUnlocked = kv.Value.PermanentlyUnlocked,
                    UnlockProgress = kv.Value.UnlockProgress,
                    StoredResources = kv.Value.StoredResources,
                    NextReadySettlementDay = kv.Value.NextReadySettlementDay,
                };
            }

            data.PlayerData.HomeRuinByUniqName.Clear();
            foreach (var kv in _ruinRuntime)
            {
                var v = kv.Value;
                var put = new Dictionary<string, long>();
                if (v.PutMaterial != null)
                {
                    foreach (var p in v.PutMaterial)
                    {
                        put[p.Key] = p.Value;
                    }
                }

                data.PlayerData.HomeRuinByUniqName[kv.Key] = new RepairPointRuntimeSave
                {
                    UniqName = string.IsNullOrEmpty(v.UniqName) ? kv.Key : v.UniqName,
                    IsRepaired = v.IsRepaired,
                    RepairProgress = v.RepairProgress,
                    PutMaterial = put,
                };
            }

            NpcCharacters.SaveTo(data.PlayerData);

            MapInteractPoints.SaveTo(data.PlayerData);

            data.PlayerData.MicroPlotConsumedByKey ??= new Dictionary<string, bool>();
            data.PlayerData.MicroPlotConsumedByKey.Clear();
            foreach (var kv in _microPlotConsumed)
            {
                if (kv.Value)
                {
                    data.PlayerData.MicroPlotConsumedByKey[kv.Key] = true;
                }
            }

            data.PlayerData.SavePointUnlocks ??= new List<SavePointUnlockPersist>();
            data.PlayerData.SavePointUnlocks.Clear();
            foreach (var kv in _savePointUnlockById)
            {
                var v = kv.Value;
                if (v == null || string.IsNullOrEmpty(v.SavePointId))
                {
                    continue;
                }

                var put = new Dictionary<string, long>();
                if (v.TributePut != null)
                {
                    foreach (var p in v.TributePut)
                    {
                        put[p.Key] = p.Value;
                    }
                }

                data.PlayerData.SavePointUnlocks.Add(new SavePointUnlockPersist
                {
                    SavePointId = v.SavePointId,
                    Unlocked = v.Unlocked,
                    TributeSubmitted = v.TributeSubmitted,
                    TributePut = put,
                });
            }

            data.PlayerData.SecretBaseUnlockedFacilityIds ??= new List<string>();
            data.PlayerData.SecretBaseUnlockedFacilityIds.Clear();
            foreach (var id in _secretBaseUnlockedFacilities)
            {
                data.PlayerData.SecretBaseUnlockedFacilityIds.Add(id);
            }

            data.PlayerData.SecretBaseBuildLevel = _secretBaseBuildLevel < 1 ? 1 : _secretBaseBuildLevel;

            data.PlayerData.LogicAreaHomesteadByMapId ??= new Dictionary<string, LogicAreaHomesteadPersist>();
            data.PlayerData.LogicAreaHomesteadByMapId.Clear();
            foreach (var kv in _logicAreaHomesteadByMapId)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null || !HasMeaningfulHomesteadState(kv.Value))
                {
                    continue;
                }

                data.PlayerData.LogicAreaHomesteadByMapId[kv.Key] = CloneHomesteadPersist(kv.Value);
            }
        }

        public int GetSecretBaseBuildLevel()
        {
            return _secretBaseBuildLevel < 1 ? 1 : _secretBaseBuildLevel;
        }

        public void SetSecretBaseBuildLevel(int level)
        {
            _secretBaseBuildLevel = level < 1 ? 1 : level;
        }

        public bool IsSecretBaseFacilityUnlocked(string facilityId)
        {
            return !string.IsNullOrEmpty(facilityId) && _secretBaseUnlockedFacilities.Contains(facilityId);
        }

        public void MarkSecretBaseFacilityUnlocked(string facilityId)
        {
            if (string.IsNullOrEmpty(facilityId))
            {
                return;
            }

            _secretBaseUnlockedFacilities.Add(facilityId);
        }

        public SavePointUnlockPersist GetOrCreateSavePointUnlockState(string savePointId)
        {
            if (string.IsNullOrEmpty(savePointId))
            {
                return null;
            }

            if (_savePointUnlockById.TryGetValue(savePointId, out var existing))
            {
                return existing;
            }

            var created = new SavePointUnlockPersist
            {
                SavePointId = savePointId,
                Unlocked = false,
                TributePut = new Dictionary<string, long>(StringComparer.Ordinal),
            };
            _savePointUnlockById[savePointId] = created;
            return created;
        }

        public SavePointUnlockPersist GetSavePointUnlockStateOrNull(string savePointId)
        {
            if (string.IsNullOrEmpty(savePointId))
            {
                return null;
            }

            _savePointUnlockById.TryGetValue(savePointId, out var s);
            return s;
        }

        public IEnumerable<SavePointUnlockPersist> EnumerateSavePointUnlockStates() => _savePointUnlockById.Values;

        public FishingSpotRuntimeSave GetOrCreateFishingSpotState(string uniqName, string cfgId, int settlementDayIndex)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return null;
            }

            if (_fishingRuntime.TryGetValue(uniqName, out var existing))
            {
                return existing;
            }

            var cfg = CfgMgr.Cfgs.TbFishingSpot.GetOrDefault(cfgId);
            int cap = cfg != null ? cfg.Capacity : 0;
            var created = new FishingSpotRuntimeSave
            {
                CfgId = cfgId,
                Remaining = cap,
                LastRestockSettlementDayIndex = settlementDayIndex,
            };
            _fishingRuntime[uniqName] = created;
            return created;
        }

        public RepairPointRuntimeSave GetOrCreateRuineRepairState(string uniqName)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return null;
            }

            if (_ruinRuntime.TryGetValue(uniqName, out var existing))
            {
                return existing;
            }

            var created = new RepairPointRuntimeSave
            {
                UniqName = uniqName,
                PutMaterial = new Dictionary<string, long>(),
            };
            _ruinRuntime[uniqName] = created;
            return created;
        }

        public FishingSpotRuntimeSave GetFishingSpotStateOrNull(string uniqName)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return null;
            }

            _fishingRuntime.TryGetValue(uniqName, out var s);
            return s;
        }

        public void TryConsumeOneFishingUse(string uniqName)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return;
            }

            if (_fishingRuntime.TryGetValue(uniqName, out var st))
            {
                st.Remaining = Mathf.Max(0, st.Remaining - 1);
            }
        }

        public void ApplyFishingRestockForSettlement(int newSettlementDayIndex)
        {
            foreach (var kv in _fishingRuntime.ToList())
            {
                var cfg = CfgMgr.Cfgs.TbFishingSpot.GetOrDefault(kv.Value.CfgId);
                if (cfg == null)
                {
                    continue;
                }

                int n = Mathf.Max(1, cfg.RestockEveryNDays);
                if (newSettlementDayIndex - kv.Value.LastRestockSettlementDayIndex >= n)
                {
                    kv.Value.Remaining = cfg.Capacity;
                    kv.Value.LastRestockSettlementDayIndex = newSettlementDayIndex;
                }
            }
        }

        public RenewableResourceNodeRuntimeSave GetOrCreateRenewableNodeState(string uniqName, string cfgId, int settlementDayIndex)
        {
            if (string.IsNullOrEmpty(uniqName) || string.IsNullOrEmpty(cfgId))
            {
                return null;
            }

            if (!_renewableResourceRuntime.TryGetValue(uniqName, out var state))
            {
                state = new RenewableResourceNodeRuntimeSave
                {
                    CfgId = cfgId,
                    NextReadySettlementDay = -1,
                };
                _renewableResourceRuntime[uniqName] = state;
            }

            RefreshRenewableNode(state, settlementDayIndex);
            return state;
        }

        public bool IsRenewableNodeUnlocked(string uniqName, string cfgId, int settlementDayIndex)
        {
            return GetOrCreateRenewableNodeState(uniqName, cfgId, settlementDayIndex)?.PermanentlyUnlocked == true;
        }

        public bool IsRenewableNodeReady(string uniqName, string cfgId, int settlementDayIndex)
        {
            return GetOrCreateRenewableNodeState(uniqName, cfgId, settlementDayIndex)?.StoredResources > 0;
        }

        public void AddRenewableNodeUnlockProgress(string uniqName, string cfgId, int amount, int settlementDayIndex)
        {
            if (amount <= 0 || string.IsNullOrEmpty(uniqName))
            {
                return;
            }

            var state = GetOrCreateRenewableNodeState(uniqName, cfgId, settlementDayIndex);
            var cfg = CfgMgr.Cfgs?.TbRenewableResourceNode?.GetOrDefault(state?.CfgId);
            if (state == null || cfg == null || state.PermanentlyUnlocked)
            {
                return;
            }

            state.UnlockProgress = Mathf.Min(cfg.UnlockRequiredProgress, state.UnlockProgress + amount);
            if (state.UnlockProgress < cfg.UnlockRequiredProgress)
            {
                return;
            }

            state.PermanentlyUnlocked = true;
            if (cfg.ImmediateReadyOnUnlock)
            {
                state.StoredResources = Mathf.Min(cfg.Capacity, state.StoredResources + 1);
                state.NextReadySettlementDay = -1;
            }
            else
            {
                state.NextReadySettlementDay = settlementDayIndex + Mathf.Max(1, cfg.RestockEveryNDays);
            }
        }

        public bool TryHarvestRenewableNode(string uniqName, string cfgId, int settlementDayIndex, out string itemId, out int count)
        {
            itemId = string.Empty;
            count = 0;
            var state = GetOrCreateRenewableNodeState(uniqName, cfgId, settlementDayIndex);
            var cfg = CfgMgr.Cfgs?.TbRenewableResourceNode?.GetOrDefault(state?.CfgId);
            if (state == null || cfg == null || !state.PermanentlyUnlocked || state.StoredResources <= 0)
            {
                return false;
            }

            state.StoredResources--;
            itemId = cfg.RewardItemId;
            count = cfg.RewardCount;
            if (state.StoredResources < cfg.Capacity)
            {
                state.NextReadySettlementDay = settlementDayIndex + Mathf.Max(1, cfg.RestockEveryNDays);
            }
            return !string.IsNullOrEmpty(itemId) && count > 0;
        }

        public void ApplyRenewableNodeRestockForSettlement(int settlementDayIndex)
        {
            foreach (var state in _renewableResourceRuntime.Values)
            {
                RefreshRenewableNode(state, settlementDayIndex);
            }
        }

        void RefreshRenewableNode(RenewableResourceNodeRuntimeSave state, int settlementDayIndex)
        {
            if (state == null || !state.PermanentlyUnlocked || state.NextReadySettlementDay < 0
                || settlementDayIndex < state.NextReadySettlementDay)
            {
                return;
            }

            var cfg = CfgMgr.Cfgs?.TbRenewableResourceNode?.GetOrDefault(state.CfgId);
            if (cfg == null)
            {
                return;
            }

            state.StoredResources = Mathf.Min(cfg.Capacity, state.StoredResources + 1);
            state.NextReadySettlementDay = state.StoredResources >= cfg.Capacity
                ? -1
                : settlementDayIndex + Mathf.Max(1, cfg.RestockEveryNDays);
        }

        public LogicAreaHomesteadPersist GetLogicAreaHomesteadState(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return null;
            }

            return _logicAreaHomesteadByMapId.TryGetValue(logicAreaId, out var state) ? state : null;
        }

        public int GetLogicAreaControl(string logicAreaId)
        {
            return GetLogicAreaHomesteadState(logicAreaId)?.ControlDegree ?? 0;
        }

        public bool IsLogicAreaAnnexed(string logicAreaId)
        {
            return GetLogicAreaHomesteadState(logicAreaId)?.IsAnnexed ?? false;
        }

        public int AddLogicAreaControl(string logicAreaId, int delta)
        {
            if (string.IsNullOrEmpty(logicAreaId) || delta <= 0)
            {
                return GetLogicAreaControl(logicAreaId);
            }

            var req = LogicAreaHomesteadUtil.GetHomesteadReq(logicAreaId);
            if (req == null)
            {
                Debug.LogWarning($"AddLogicAreaControl: logic area has no homestead req config: {logicAreaId}");
                return GetLogicAreaControl(logicAreaId);
            }

            var state = GetOrCreateHomesteadState(logicAreaId);
            var next = state.ControlDegree + delta;
            if (req.RequiredControl > 0)
            {
                next = Mathf.Min(next, req.RequiredControl);
            }

            state.ControlDegree = next;
            NormalizeHomesteadEntry(logicAreaId, state);
            EvOnLogicAreaHomesteadChanged?.Invoke(logicAreaId, state);
            return state.ControlDegree;
        }

        public bool IsLogicAreaControlRequirementMet(string logicAreaId)
        {
            if (IsLogicAreaAnnexed(logicAreaId))
            {
                return true;
            }

            var req = LogicAreaHomesteadUtil.GetHomesteadReq(logicAreaId);
            if (req == null || req.RequiredControl <= 0)
            {
                return false;
            }

            return GetLogicAreaControl(logicAreaId) >= req.RequiredControl;
        }

        LogicAreaHomesteadPersist GetOrCreateHomesteadState(string logicAreaId)
        {
            if (!_logicAreaHomesteadByMapId.TryGetValue(logicAreaId, out var state) || state == null)
            {
                state = new LogicAreaHomesteadPersist();
                _logicAreaHomesteadByMapId[logicAreaId] = state;
            }

            return state;
        }

        static bool HasMeaningfulHomesteadState(LogicAreaHomesteadPersist state)
        {
            if (state == null)
            {
                return false;
            }

            if (state.IsAnnexed || state.ControlDegree > 0)
            {
                return true;
            }

            if (state.Buildings == null)
            {
                return false;
            }

            foreach (var b in state.Buildings)
            {
                if (b != null && !string.IsNullOrEmpty(b.BuildingId) && b.Level > 0)
                {
                    return true;
                }
            }

            return false;
        }

        static LogicAreaHomesteadPersist CloneHomesteadPersist(LogicAreaHomesteadPersist source)
        {
            var clone = new LogicAreaHomesteadPersist
            {
                ControlDegree = source.ControlDegree,
                IsAnnexed = source.IsAnnexed,
            };

            if (source.Buildings != null)
            {
                foreach (var b in source.Buildings)
                {
                    if (b == null || string.IsNullOrEmpty(b.BuildingId) || b.Level <= 0)
                    {
                        continue;
                    }

                    clone.Buildings.Add(new HomesteadBuildingPersist
                    {
                        BuildingId = b.BuildingId,
                        Level = b.Level,
                    });
                }
            }

            return clone;
        }

        public int GetHomesteadBuildingLevel(string logicAreaId, string buildingId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(buildingId))
            {
                return 0;
            }

            var state = GetLogicAreaHomesteadState(logicAreaId);
            if (state?.Buildings == null)
            {
                return 0;
            }

            foreach (var b in state.Buildings)
            {
                if (b != null && b.BuildingId == buildingId)
                {
                    return Mathf.Max(0, b.Level);
                }
            }

            return 0;
        }

        public void SetHomesteadBuildingLevel(string logicAreaId, string buildingId, int level)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(buildingId))
            {
                return;
            }

            level = Mathf.Max(0, level);
            var state = GetOrCreateHomesteadState(logicAreaId);
            state.Buildings ??= new List<HomesteadBuildingPersist>();

            for (int i = 0; i < state.Buildings.Count; i++)
            {
                var b = state.Buildings[i];
                if (b != null && b.BuildingId == buildingId)
                {
                    if (level <= 0)
                    {
                        state.Buildings.RemoveAt(i);
                    }
                    else
                    {
                        b.Level = level;
                    }

                    NormalizeHomesteadEntry(logicAreaId, state);
                    EvOnLogicAreaHomesteadChanged?.Invoke(logicAreaId, state);
                    return;
                }
            }

            if (level <= 0)
            {
                NormalizeHomesteadEntry(logicAreaId, state);
                return;
            }

            state.Buildings.Add(new HomesteadBuildingPersist
            {
                BuildingId = buildingId,
                Level = level,
            });
            EvOnLogicAreaHomesteadChanged?.Invoke(logicAreaId, state);
        }

        void NormalizeHomesteadEntry(string logicAreaId, LogicAreaHomesteadPersist state)
        {
            if (state == null || HasMeaningfulHomesteadState(state))
            {
                return;
            }

            _logicAreaHomesteadByMapId.Remove(logicAreaId);
        }
    }
}
