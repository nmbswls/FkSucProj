using System.Collections.Generic;
using My.Map.Entity;
using My.Player;
using System;

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

        // 从施法 ctx 的 RunningVariables 解析整型；供 AddBuff 层数等按技能等级差异化
        public static int ResolveIntFromRunningVar(
            GameLogicManager.LogicFightEffectContext ctx,
            string attrKey,
            int fallback)
        {
            fallback = Math.Max(1, fallback);
            if (ctx?.RunningVariables == null || string.IsNullOrEmpty(attrKey))
            {
                return fallback;
            }

            if (ctx.RunningVariables.TryGetValue(attrKey, out var raw)
                && !string.IsNullOrEmpty(raw)
                && int.TryParse(raw, out var parsed))
            {
                return Math.Max(1, parsed);
            }

            return fallback;
        }
    }
}
