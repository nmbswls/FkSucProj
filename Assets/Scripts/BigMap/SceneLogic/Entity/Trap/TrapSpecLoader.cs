using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    public static class TrapSpecLoader
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

            var loaded = Resources.Load<TrapSpecConfig>($"Config/TrapSpecs/{cfgId}");
            if (loaded != null)
            {
                Cache[cfgId] = loaded;
            }

            return loaded;
        }
    }
}
