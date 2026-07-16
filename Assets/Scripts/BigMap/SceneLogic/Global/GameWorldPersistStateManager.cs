// 跨图「地图实体」交互持久状态（垂钓点、废墟等），非 RPG 全局开关。
// 当前随 SaveData 整体进内存；若未来拆分 per-map 存档或联机分 Region，再评估按图分桶或按需从盘加载。

using System.Collections.Generic;
using System.Linq;
using My.Config;
using My.Home;
using My.Saving;
using System;
using UnityEngine;
using My.Map;

namespace My
{
    public class GameWorldPersistStateManager
    {
        private readonly Dictionary<string, FishingSpotRuntimeSave> _fishingRuntime = new();
        private readonly Dictionary<string, RepairPointRuntimeSave> _ruinRuntime = new();
        private readonly Dictionary<string, SavePointUnlockPersist> _savePointUnlockById = new(StringComparer.Ordinal);
        private readonly HashSet<string> _secretBaseUnlockedFacilities = new(StringComparer.Ordinal);
        private int _secretBaseBuildLevel = 1;

        public readonly WorldNpcCharacterPersistRegistry NpcCharacters = new();

        public readonly MapInteractPointPersistRegistry MapInteractPoints = new();

        // mapId|triggerId -> consumed
        private readonly Dictionary<string, bool> _microPlotConsumed = new();

        // logic_area map_id -> homestead runtime
        private readonly Dictionary<string, TownDevelopmentPersist> _logicAreaHomesteadByMapId = new(StringComparer.Ordinal);

        public event Action<string, TownDevelopmentPersist> EvOnLogicAreaHomesteadChanged;

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
            if (savingData?.TownDevelopmentById != null)
            {
                foreach (var kv in savingData.TownDevelopmentById)
                {
                    if (string.IsNullOrEmpty(kv.Key) || kv.Value == null || !HasMeaningfulHomesteadState(kv.Value)) continue;
                    _logicAreaHomesteadByMapId[kv.Key] = CloneTownDevelopment(kv.Value);
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

            data.TownDevelopmentById ??= new Dictionary<string, TownDevelopmentPersist>();
            data.TownDevelopmentById.Clear();
            foreach (var kv in _logicAreaHomesteadByMapId)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null || !HasMeaningfulHomesteadState(kv.Value)) continue;
                data.TownDevelopmentById[kv.Key] = CloneTownDevelopment(kv.Value);
            }
        }        public int GetSecretBaseBuildLevel()
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

        public TownDevelopmentPersist GetLogicAreaHomesteadState(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return null;
            }

            return _logicAreaHomesteadByMapId.TryGetValue(logicAreaId, out var state) ? state : null;
        }

        public int GetControlledTownCount()
        {
            var towns = CfgMgr.Cfgs?.TbLogicAreaInfo?.DataList;
            if (towns == null) return 0;
            var count = 0;
            foreach (var town in towns)
            {
                if (town != null && town.CanAnnexHomestead && IsLogicAreaControlRequirementMet(town.MapId))
                {
                    count++;
                }
            }
            return count;
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

            var req = TownFacilityUtil.GetHomesteadReq(logicAreaId);
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

            var req = TownFacilityUtil.GetHomesteadReq(logicAreaId);
            if (req == null || req.RequiredControl <= 0)
            {
                return false;
            }

            return GetLogicAreaControl(logicAreaId) >= req.RequiredControl;
        }

        TownDevelopmentPersist GetOrCreateHomesteadState(string logicAreaId)
        {
            if (!_logicAreaHomesteadByMapId.TryGetValue(logicAreaId, out var state) || state == null)
            {
                state = new TownDevelopmentPersist();
                _logicAreaHomesteadByMapId[logicAreaId] = state;
            }

            return state;
        }

        static bool HasMeaningfulHomesteadState(TownDevelopmentPersist state)
        {
            if (state == null)
            {
                return false;
            }

            if (state.IsAnnexed || state.ControlDegree > 0)
            {
                return true;
            }

            if (state.Facilities == null)
            {
                return false;
            }

            foreach (var b in state.Facilities)
            {
                if (b != null && (b.SiteId > 0 || !string.IsNullOrEmpty(b.FacilityId)))
                {
                    return true;
                }
            }

            return false;
        }

        static TownDevelopmentPersist CloneTownDevelopment(TownDevelopmentPersist source)
        {
            var clone = new TownDevelopmentPersist
            {
                Prosperity = source.Prosperity,
                Population = source.Population,
                Influence = source.Influence,
                Stability = source.Stability,
                ControlDegree = source.ControlDegree,
                IsAnnexed = source.IsAnnexed,
                Facilities = new List<TownFacilityPersist>(),
            };
            if (source.Facilities != null)
            {
                foreach (var b in source.Facilities)
                {
                    if (b == null || (b.SiteId <= 0 && string.IsNullOrEmpty(b.FacilityId))) continue;
                    clone.Facilities.Add(new TownFacilityPersist
                    {
                        SiteId = b.SiteId,
                        InstanceId = b.InstanceId,
                        FacilityId = b.FacilityId,
                        IsConstructed = b.IsConstructed,
                        DevelopmentLevel = b.DevelopmentLevel,
                        RenovationId = b.RenovationId,
                        AssignedWorkforce = b.AssignedWorkforce,
                        LastOutputSettlementDay = b.LastOutputSettlementDay,
                        SupervisorSlots = CloneSupervisorSlots(b.SupervisorSlots),
                    });
                }
            }
            return clone;
        }

        static List<FacilitySupervisorSlotPersist> CloneSupervisorSlots(List<FacilitySupervisorSlotPersist> source)
        {
            var clone = new List<FacilitySupervisorSlotPersist>();
            if (source == null)
            {
                return clone;
            }

            foreach (var slot in source)
            {
                if (slot == null)
                {
                    continue;
                }

                clone.Add(new FacilitySupervisorSlotPersist
                {
                    SlotIndex = slot.SlotIndex,
                    CharacterKey = slot.CharacterKey,
                });
            }

            return clone;
        }

        public string GetFacilitySupervisor(string townId, int siteId, long instanceId, string facilityId, int slotIndex)
        {
            var facility = ResolveFacilityPersist(townId, siteId, instanceId, facilityId, false);
            return facility == null ? null : GetSupervisorFromFacility(facility, slotIndex);
        }

        public void SetFacilitySupervisor(string townId, int siteId, long instanceId, string facilityId, int slotIndex, string characterKey)
        {
            var facility = ResolveFacilityPersist(townId, siteId, instanceId, facilityId, true);
            if (facility == null)
            {
                return;
            }

            facility.SupervisorSlots ??= new List<FacilitySupervisorSlotPersist>();
            for (int i = facility.SupervisorSlots.Count - 1; i >= 0; i--)
            {
                var slot = facility.SupervisorSlots[i];
                if (slot != null && slot.SlotIndex == slotIndex)
                {
                    facility.SupervisorSlots.RemoveAt(i);
                }
            }

            if (!string.IsNullOrEmpty(characterKey))
            {
                facility.SupervisorSlots.Add(new FacilitySupervisorSlotPersist
                {
                    SlotIndex = slotIndex,
                    CharacterKey = characterKey,
                });
            }

            NormalizeHomesteadEntry(townId, GetLogicAreaHomesteadState(townId));
            EvOnLogicAreaHomesteadChanged?.Invoke(townId, GetLogicAreaHomesteadState(townId));
        }

        public void ClearFacilitySupervisors(TownFacilityPersist facility)
        {
            if (facility == null)
            {
                return;
            }

            facility.SupervisorSlots?.Clear();
        }

        static string GetSupervisorFromFacility(TownFacilityPersist facility, int slotIndex)
        {
            if (facility?.SupervisorSlots == null)
            {
                return null;
            }

            foreach (var slot in facility.SupervisorSlots)
            {
                if (slot != null && slot.SlotIndex == slotIndex && !string.IsNullOrEmpty(slot.CharacterKey))
                {
                    return slot.CharacterKey;
                }
            }

            return null;
        }

        TownFacilityPersist ResolveFacilityPersist(string townId, int siteId, long instanceId, string facilityId, bool create)
        {
            if (siteId > 0)
            {
                return GetTownFacilityBySite(townId, siteId, create);
            }

            return GetTownFacility(townId, instanceId, facilityId, create);
        }
        public IReadOnlyList<TownFacilityPersist> GetTownFacilities(string townId)
        {
            var facilitys = GetLogicAreaHomesteadState(townId)?.Facilities;
            return facilitys ?? (IReadOnlyList<TownFacilityPersist>)Array.Empty<TownFacilityPersist>();
        }

        public TownFacilityPersist GetTownFacilityBySite(string townId, int siteId, bool create = false)
        {
            if (string.IsNullOrEmpty(townId) || siteId <= 0)
            {
                return null;
            }

            var site = TownFacilitySiteCatalog.Get(siteId);
            if (site == null)
            {
                return null;
            }

            var state = create ? GetOrCreateHomesteadState(townId) : GetLogicAreaHomesteadState(townId);
            if (state == null)
            {
                return null;
            }

            state.Facilities ??= new List<TownFacilityPersist>();
            foreach (var facility in state.Facilities)
            {
                if (facility != null && facility.SiteId == siteId)
                {
                    EnsureFacilityPersistIdentity(townId, facility);
                    return facility;
                }
            }

            if (!create)
            {
                return null;
            }

            var created = new TownFacilityPersist
            {
                SiteId = siteId,
                FacilityId = site.FacilityCfgId,
            };
            state.Facilities.Add(created);
            return created;
        }

        public TownFacilityPersist GetTownFacility(string townId, long instanceId, string facilityId, bool create = false)
        {
            if (string.IsNullOrEmpty(townId) || string.IsNullOrEmpty(facilityId))
            {
                return null;
            }

            if (instanceId == 0)
            {
                var site = TownFacilitySiteCatalog.FindByMapAndFacility(townId, facilityId);
                if (site != null)
                {
                    return GetTownFacilityBySite(townId, site.Id, create);
                }
            }

            var state = create ? GetOrCreateHomesteadState(townId) : GetLogicAreaHomesteadState(townId);
            if (state == null)
            {
                return null;
            }

            state.Facilities ??= new List<TownFacilityPersist>();
            foreach (var facility in state.Facilities)
            {
                if (facility != null && facility.FacilityId == facilityId && facility.InstanceId == instanceId)
                {
                    EnsureFacilityPersistIdentity(townId, facility);
                    return facility;
                }
            }

            if (!create)
            {
                return null;
            }

            var created = new TownFacilityPersist { InstanceId = instanceId, FacilityId = facilityId };
            EnsureFacilityPersistIdentity(townId, created);
            state.Facilities.Add(created);
            return created;
        }

        static void EnsureFacilityPersistIdentity(string townId, TownFacilityPersist facility)
        {
            if (facility == null || string.IsNullOrEmpty(townId))
            {
                return;
            }

            if (facility.SiteId > 0)
            {
                var site = TownFacilitySiteCatalog.Get(facility.SiteId);
                if (site != null && string.IsNullOrEmpty(facility.FacilityId))
                {
                    facility.FacilityId = site.FacilityCfgId;
                }

                return;
            }

            if (facility.InstanceId == 0 && !string.IsNullOrEmpty(facility.FacilityId))
            {
                var site = TownFacilitySiteCatalog.FindByMapAndFacility(townId, facility.FacilityId);
                if (site != null)
                {
                    facility.SiteId = site.Id;
                }
            }
        }

        public int GetSiteDevelopmentLevel(string townId, int siteId)
        {
            var facility = GetTownFacilityBySite(townId, siteId, false);
            return facility != null ? Mathf.Max(0, facility.DevelopmentLevel) : 0;
        }

        public int GetFacilityDevelopmentLevel(string logicAreaId, string facilityId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(facilityId))
            {
                return 0;
            }

            var site = TownFacilitySiteCatalog.FindByMapAndFacility(logicAreaId, facilityId);
            if (site != null)
            {
                return GetSiteDevelopmentLevel(logicAreaId, site.Id);
            }

            var state = GetLogicAreaHomesteadState(logicAreaId);
            if (state?.Facilities == null)
            {
                return 0;
            }

            foreach (var b in state.Facilities)
            {
                if (b != null && b.FacilityId == facilityId && b.InstanceId == 0)
                {
                    return Mathf.Max(0, b.DevelopmentLevel);
                }
            }

            return 0;
        }

        public int GetInstanceFacilityDevelopmentLevel(string townId, long instanceId, string facilityId)
        {
            if (instanceId == 0)
            {
                var site = TownFacilitySiteCatalog.FindByMapAndFacility(townId, facilityId);
                if (site != null)
                {
                    return GetSiteDevelopmentLevel(townId, site.Id);
                }
            }

            var facility = GetTownFacility(townId, instanceId, facilityId, false);
            if (facility != null)
            {
                return Mathf.Max(0, facility.DevelopmentLevel);
            }

            return GetFacilityDevelopmentLevel(townId, facilityId);
        }

        public void SetSiteDevelopmentLevel(string townId, int siteId, int level, string renovationId, int assignedWorkforce)
        {
            var site = TownFacilitySiteCatalog.Get(siteId);
            if (site == null || string.IsNullOrEmpty(townId))
            {
                return;
            }

            SetTownFacilityDevelopment(townId, 0, site.FacilityCfgId, level, renovationId, assignedWorkforce, level > 0, siteId);
        }

        public void SetInstanceFacilityDevelopmentLevel(string townId, long instanceId, string facilityId, int level)
        {
            var existing = GetTownFacility(townId, instanceId, facilityId, false);
            var renovationId = existing?.RenovationId;
            var assignedWorkforce = existing?.AssignedWorkforce ?? 0;
            var siteId = existing?.SiteId ?? 0;
            if (siteId <= 0 && instanceId == 0)
            {
                siteId = TownFacilitySiteCatalog.FindByMapAndFacility(townId, facilityId)?.Id ?? 0;
            }

            SetTownFacilityDevelopment(townId, instanceId, facilityId, level, renovationId, assignedWorkforce, level > 0, siteId);
        }

        public void SetTownFacilityDevelopment(string townId, long instanceId, string facilityId,
            int level, string renovationId, int assignedWorkforce, bool isBuilt, int siteId = 0)
        {
            TownFacilityPersist facility;
            if (siteId > 0)
            {
                facility = GetTownFacilityBySite(townId, siteId, true);
            }
            else
            {
                facility = GetTownFacility(townId, instanceId, facilityId, true);
            }

            if (facility == null)
            {
                return;
            }

            if (siteId > 0)
            {
                facility.SiteId = siteId;
            }

            if (!string.IsNullOrEmpty(facilityId))
            {
                facility.FacilityId = facilityId;
            }

            facility.DevelopmentLevel = Mathf.Max(0, level);
            facility.RenovationId = renovationId;
            facility.AssignedWorkforce = Mathf.Max(0, assignedWorkforce);
            facility.IsConstructed = isBuilt && facility.DevelopmentLevel > 0;
            NormalizeHomesteadEntry(townId, GetLogicAreaHomesteadState(townId));
            EvOnLogicAreaHomesteadChanged?.Invoke(townId, GetLogicAreaHomesteadState(townId));
        }

        public void SetFacilityDevelopmentLevel(string logicAreaId, string facilityId, int level)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(facilityId))
            {
                return;
            }

            var site = TownFacilitySiteCatalog.FindByMapAndFacility(logicAreaId, facilityId);
            if (site != null)
            {
                SetSiteDevelopmentLevel(logicAreaId, site.Id, level, level > 0 ? GetTownFacilityBySite(logicAreaId, site.Id, false)?.RenovationId : null, 0);
                return;
            }

            var facility = GetTownFacility(logicAreaId, 0, facilityId, true);
            facility.DevelopmentLevel = Mathf.Max(0, level);
            facility.IsConstructed = facility.DevelopmentLevel > 0;
            if (facility.DevelopmentLevel <= 0)
            {
                facility.RenovationId = null;
                facility.AssignedWorkforce = 0;
                ClearFacilitySupervisors(facility);
            }
            NormalizeHomesteadEntry(logicAreaId, GetLogicAreaHomesteadState(logicAreaId));
            EvOnLogicAreaHomesteadChanged?.Invoke(logicAreaId, GetLogicAreaHomesteadState(logicAreaId));
        }
        void NormalizeHomesteadEntry(string logicAreaId, TownDevelopmentPersist state)
        {
            if (state == null || HasMeaningfulHomesteadState(state))
            {
                return;
            }

            _logicAreaHomesteadByMapId.Remove(logicAreaId);
        }
    }
}
