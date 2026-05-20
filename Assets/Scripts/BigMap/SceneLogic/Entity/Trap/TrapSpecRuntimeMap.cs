using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Fight;
using UnityEngine;

namespace My.Map.Entity
{
    // TbTrapSpec -> 运行时 TrapSpecConfig（内存 SO 实例），供 TrapLogicEntity 与现有 TriggerEffects 管线使用。
    public static class TrapSpecRuntimeMap
    {
        static readonly Dictionary<string, TrapSpecConfig> Cache = new();

        public static TrapSpecConfig Get(string cfgId)
        {
            if (string.IsNullOrEmpty(cfgId))
            {
                return null;
            }

            if (Cache.TryGetValue(cfgId, out var cached))
            {
                return cached;
            }

            if (CfgMgr.Cfgs == null)
            {
                Debug.LogError("TrapSpecRuntimeMap: CfgMgr.Cfgs is null.");
                return null;
            }

            var row = CfgMgr.Cfgs.TbTrapSpec.GetOrDefault(cfgId);
            if (row == null)
            {
                Debug.LogError($"TrapSpecRuntimeMap: trap spec not found in Luban for id '{cfgId}'.");
                return null;
            }

            var spec = MapRowToSpec(row);
            Cache[cfgId] = spec;
            return spec;
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }

        static TrapSpecConfig MapRowToSpec(TrapSpec row)
        {
            var s = ScriptableObject.CreateInstance<TrapSpecConfig>();
            s.TriggerRadius = row.TriggerRadius;
            s.CampFilter = (ECampFilterType)row.CampFilter;
            s.OnlyPlayer = row.OnlyPlayer;
            s.PostTrigger = (ETrapPostTrigger)row.PostTrigger;
            s.SleepDuration = row.SleepDuration;

            if (row.TriggerEffects != null)
            {
                foreach (var te in row.TriggerEffects)
                {
                    if (te == null)
                    {
                        continue;
                    }

                    // Luban trap_spec：kind 0 = AddBuff
                    if (te.Kind == 0)
                    {
                        s.TriggerEffects.Add(new MapAbilityEffectAddBuffCfg
                        {
                            BuffId = te.BuffId,
                            Layer = te.Layer,
                            Duration = te.Duration,
                            //TargetType = te.TargetType,
                        });
                    }
                }
            }

            return s;
        }
    }
}
