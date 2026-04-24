// 跨图「地图实体」交互持久状态（垂钓点、废墟等），非 RPG 全局开关。
// 当前随 SaveData 整体进内存；若未来拆分 per-map 存档或联机分 Region，再评估按图分桶或按需从盘加载。

using System.Collections.Generic;
using System.Linq;
using My.Config;
using My.Saving;
using UnityEngine;

namespace My
{
    public class GameWorldPersistStateManager
    {
        private readonly Dictionary<string, FishingSpotRuntimeSave> _fishingRuntime = new();
        private readonly Dictionary<string, RepairPointRuntimeSave> _ruinRuntime = new();

        public GameWorldPersistStateManager()
        {
        }

        public void InitFromSave(SaveData savingData)
        {
            _fishingRuntime.Clear();
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
        }

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
