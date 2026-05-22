using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Entity;

namespace My.Player
{
    // 人类武器表查询与施放参数构建
    public static class HumanWeaponCatalog
    {
        public const string ViewKey = "HumanWeaponView";
        public const string DefaultViewPrefab = "Prefab/Presentations/HumanWeaponView";

        public const string CastKeyWeaponAnimName = "WeaponAnimName";
        public const string CastKeyWeaponLevel = "WeaponLevel";
        public const string CastKeyStunValue = "StunValue";

        public static HumanWeapon GetOrDefault(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            return CfgMgr.Cfgs?.TbHumanWeapon?.GetOrDefault(itemId);
        }

        public static bool IsHumanWeapon(string itemId)
        {
            return GetOrDefault(itemId) != null;
        }

        public static string GetSkillId(string itemId)
        {
            var def = GetOrDefault(itemId);
            return string.IsNullOrEmpty(def?.SkillId) ? null : def.SkillId;
        }

        public static Dictionary<string, string> BuildCastParams(string itemId)
        {
            var cache = BuildCastCacheAttrs(itemId);
            if (cache == null)
            {
                return null;
            }

            return new Dictionary<string, string>
            {
                [CastKeyWeaponLevel] = cache[AttrIdConsts.CastWeaponLevel].ToString(),
                [CastKeyStunValue] = cache[AttrIdConsts.CastStunValue].ToString(),
            };
        }

        public static Dictionary<string, long> BuildCastCacheAttrs(string itemId)
        {
            var def = GetOrDefault(itemId);
            if (def == null)
            {
                return null;
            }

            return new Dictionary<string, long>
            {
                [AttrIdConsts.CastWeaponLevel] = def.WeaponLevel,
                [AttrIdConsts.CastStunValue] = def.StunValue,
            };
        }
    }
}
