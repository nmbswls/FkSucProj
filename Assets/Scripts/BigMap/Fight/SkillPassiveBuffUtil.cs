using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;

namespace My.Map.Entity
{
    public static class SkillPassiveBuffUtil
    {
        static readonly List<string> Empty = new();
        static readonly Dictionary<string, List<string>> CacheBySkillId = new(StringComparer.Ordinal);

        public static IReadOnlyList<string> GetPassiveBuffIds(EntitySkillData cfg)
        {
            if (cfg == null || string.IsNullOrEmpty(cfg.SkillId))
            {
                return Empty;
            }

            if (CacheBySkillId.TryGetValue(cfg.SkillId, out var cached))
            {
                return cached;
            }

            var list = new List<string>();
            if (cfg.PassiveBuffIds != null)
            {
                foreach (var id in cfg.PassiveBuffIds)
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    if (!list.Contains(id))
                    {
                        list.Add(id);
                    }
                }
            }

            CacheBySkillId[cfg.SkillId] = list;
            return list;
        }

        public static void ClearCache()
        {
            CacheBySkillId.Clear();
        }

        public static bool HasPassiveBuffs(EntitySkillData cfg)
        {
            return cfg != null && cfg.IsPassive && GetPassiveBuffIds(cfg).Count > 0;
        }

        public static int ClampLayerForAllPassiveBuffs(EntitySkillData cfg, int layer)
        {
            layer = Math.Max(1, layer);
            if (cfg == null)
            {
                return layer;
            }

            foreach (var buffId in GetPassiveBuffIds(cfg))
            {
                BuffDefinition def = BuffLibrary.GetBuffDefinition(buffId);
                if (def != null && def.MaxStackLayer > 0)
                {
                    layer = Math.Min(layer, def.MaxStackLayer);
                }
            }

            return layer;
        }

        public static int ClampLayerForPassiveSkill(string skillId, int layer)
        {
            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg == null || !HasPassiveBuffs(cfg))
            {
                return Math.Max(1, layer);
            }

            return ClampLayerForAllPassiveBuffs(cfg, layer);
        }
    }
}
