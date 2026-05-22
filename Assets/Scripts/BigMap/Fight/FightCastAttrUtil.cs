using System.Collections.Generic;
using My.Map.Entity;
using My.Player;

namespace My.Map.Fight
{
    public static class FightCastAttrUtil
    {
        public static void MergeCastRunningVars(
            Dictionary<string, string> running,
            Dictionary<string, long> cache)
        {
            if (running == null || cache == null)
            {
                return;
            }

            if (running.TryGetValue(HumanWeaponCatalog.CastKeyWeaponLevel, out var levelRaw)
                && long.TryParse(levelRaw, out var level))
            {
                cache[AttrIdConsts.CastWeaponLevel] = level;
            }

            if (running.TryGetValue(HumanWeaponCatalog.CastKeyStunValue, out var stunRaw)
                && long.TryParse(stunRaw, out var stun))
            {
                cache[AttrIdConsts.CastStunValue] = stun;
            }
        }

        public static Dictionary<string, long> CopyCacheAttrs(Dictionary<string, long> src)
        {
            if (src == null || src.Count == 0)
            {
                return null;
            }

            return new Dictionary<string, long>(src);
        }

        public static void CopyInto(Dictionary<string, long> src, Dictionary<string, long> dst)
        {
            if (src == null || dst == null)
            {
                return;
            }

            foreach (var kv in src)
            {
                dst[kv.Key] = kv.Value;
            }
        }
    }
}
