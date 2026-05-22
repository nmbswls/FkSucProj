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
        private readonly Dictionary<string, RepairPointRuntimeSave> _ruinRuntime = new();
        private readonly Dictionary<string, SavePointUnlockPersist> _savePointUnlockById = new(StringComparer.Ordinal);
        private readonly HashSet<string> _secretBaseUnlockedFacilities = new(StringComparer.Ordinal);
        private int _secretBaseBuildLevel = 1;

        public readonly WorldNpcCharacterPersistRegistry NpcCharacters = new();

        // mapId|triggerId -> consumed
        private readonly Dictionary<string, bool> _microPlotConsumed = new();

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
    }
}
