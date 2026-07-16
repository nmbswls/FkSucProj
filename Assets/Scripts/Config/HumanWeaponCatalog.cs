using System.Collections.Generic;
using System.Linq;
using System;
using cfg.demo;
using My.Config;
using My.Map.Entity;
using Newtonsoft.Json;
using UnityEngine;

namespace My.Player
{
    // 人类武器表查询与施放参数构建
    public static class HumanWeaponCatalog
    {
        public const string ViewKey = "HumanWeaponView";
        public const string DefaultViewPrefab = "Prefab/HumanWeapon/HumanWeaponView";

        public const string CastKeyWeaponAnimName = "WeaponAnimName";
        public const string CastKeyWeaponLevel = "WeaponLevel";
        public const string CastKeyStunValue = "StunValue";

        static List<HumanWeaponAffixDef> _affixes;
        static List<HumanWeaponAffixTier> _tiers;
        static List<HumanWeaponAffixLinkPrice> _linkPrices;

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

        static void EnsureAffixData()
        {
            if (_affixes != null) return;
            _affixes = LoadJson<List<HumanWeaponAffixDef>>("Config/Json/demo_tbweaponaffixdef") ?? new();
            _tiers = LoadJson<List<HumanWeaponAffixTier>>("Config/Json/demo_tbweaponaffixtier") ?? new();
            _linkPrices = LoadJson<List<HumanWeaponAffixLinkPrice>>("Config/Json/demo_tbweaponaffixlinkprice") ?? new();
        }

        static T LoadJson<T>(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            return asset == null ? default : JsonConvert.DeserializeObject<T>(asset.text);
        }

        public static ItemInstance4HumanWeapon GetInstance(ItemStack stack)
            => stack?.InstanceInfo?.Get<ItemInstance4HumanWeapon>();

        public static bool IsIdentified(ItemStack stack)
            => GetInstance(stack)?.IsIdentified == true;

        public static bool TryEnsureSeed(ItemStack stack)
        {
            var weapon = GetInstance(stack);
            if (weapon == null) return false;
            if (weapon.IdentificationSeed == 0)
            {
                unchecked
                {
                    weapon.IdentificationSeed = (stack.ItemInstanceId * 1103515245L) ^ 0x5DEECE66DL;
                    if (weapon.IdentificationSeed == 0) weapon.IdentificationSeed = 1;
                }
            }
            return true;
        }

        public static bool TryIdentify(ItemStack stack)
        {
            var weapon = GetInstance(stack);
            var def = GetOrDefault(stack?.ItemID);
            if (weapon == null || def == null) return false;
            if (weapon.IsIdentified) return true;
            EnsureAffixData();
            TryEnsureSeed(stack);

            var rng = new System.Random(unchecked((int)weapon.IdentificationSeed));
            int min = Math.Max(0, def.AffixMinCount);
            int max = Math.Max(min, def.AffixMaxCount);
            int count = min == max ? min : rng.Next(min, max + 1);
            var candidates = _affixes.Where(a => a != null && IsSubtypeAllowed(a, def.WeaponSubtype)).ToList();
            weapon.AffixIds ??= new List<string>();
            weapon.AffixIds.Clear();
            weapon.AffixTiers ??= new List<int>();
            weapon.AffixTiers.Clear();
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int total = candidates.Sum(a => Math.Max(1, a.Weight));
                int roll = rng.Next(total);
                HumanWeaponAffixDef chosen = candidates[0];
                foreach (var candidate in candidates)
                {
                    roll -= Math.Max(1, candidate.Weight);
                    if (roll < 0) { chosen = candidate; break; }
                }
                weapon.AffixIds.Add(chosen.AffixId);
                var tierRows = _tiers.Where(t => t.AffixId == chosen.AffixId).ToList();
                weapon.AffixTiers.Add(RollTier(tierRows, rng));
                candidates.RemoveAll(a => a.ExclusiveGroup != 0 && a.ExclusiveGroup == chosen.ExclusiveGroup);
            }
            weapon.IsIdentified = true;
            return true;
        }

        static bool IsSubtypeAllowed(HumanWeaponAffixDef affix, string subtype)
            => affix.AllowedSubtypes == null || affix.AllowedSubtypes.Count == 0 || affix.AllowedSubtypes.Contains(subtype);

        static int RollTier(List<HumanWeaponAffixTier> rows, System.Random rng)
        {
            if (rows == null || rows.Count == 0) return 1;
            var total = rows.Sum(x => Math.Max(1, x.Weight));
            var roll = rng.Next(total);
            foreach (var row in rows)
            {
                roll -= Math.Max(1, row.Weight);
                if (roll < 0) return row.Tier;
            }
            return rows[0].Tier;
        }

        public static IReadOnlyList<string> GetAffixDisplayLines(ItemStack stack)
        {
            var weapon = GetInstance(stack);
            if (weapon == null || !weapon.IsIdentified) return Array.Empty<string>();
            EnsureAffixData();
            var lines = new List<string>();
            for (var i = 0; i < (weapon.AffixIds?.Count ?? 0); i++)
            {
                var id = weapon.AffixIds[i];
                var def = _affixes.FirstOrDefault(a => a.AffixId == id);
                var tierValue = weapon.AffixTiers != null && i < weapon.AffixTiers.Count ? weapon.AffixTiers[i] : 1;
                var tier = _tiers.FirstOrDefault(t => t.AffixId == id && t.Tier == tierValue);
                if (def != null) lines.Add($"{def.DisplayName}{(tier == null ? string.Empty : $" [{tier.Tier}级]")}：{def.Description}");
            }
            return lines;
        }

        public static long GetAffixMarketValue(ItemStack stack)
        {
            var weapon = GetInstance(stack);
            if (weapon == null || !weapon.IsIdentified) return 0;
            EnsureAffixData();
            var value = (weapon.AffixIds ?? new List<string>())
                .Select((id, i) => GetAffixBasePrice(id)
                    * Math.Max(1, weapon.AffixTiers != null && i < weapon.AffixTiers.Count ? weapon.AffixTiers[i] : 1))
                .Sum();
            return value + GetLinkPrice(weapon.AffixIds);
        }

        public static long GetAffixMarketBaseValue(ItemStack stack)
        {
            var weapon = GetInstance(stack);
            if (weapon == null || !weapon.IsIdentified) return 0;
            EnsureAffixData();
            return (weapon.AffixIds ?? new List<string>())
                .Select((id, i) => GetAffixBasePrice(id)
                    * Math.Max(1, weapon.AffixTiers != null && i < weapon.AffixTiers.Count ? weapon.AffixTiers[i] : 1))
                .Sum();
        }

        public static IReadOnlyList<string> GetAffixMarketBreakdown(ItemStack stack)
        {
            var weapon = GetInstance(stack);
            if (weapon == null || !weapon.IsIdentified)
            {
                return Array.Empty<string>();
            }

            EnsureAffixData();
            var lines = new List<string>();
            var ids = weapon.AffixIds ?? new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                var affix = _affixes.FirstOrDefault(a => a.AffixId == ids[i]);
                if (affix == null) continue;
                int tier = weapon.AffixTiers != null && i < weapon.AffixTiers.Count
                    ? Math.Max(1, weapon.AffixTiers[i]) : 1;
                lines.Add($"{affix.DisplayName}：基础 {GetAffixBasePrice(affix.AffixId)} x 品阶 {tier} = {GetAffixBasePrice(affix.AffixId) * tier}");
            }

            foreach (var link in GetMatchingLinks(ids))
            {
                lines.Add($"联动·{link.DisplayName}：+{link.ExtraPrice}");
            }
            return lines;
        }

        static int GetAffixBasePrice(string affixId)
        {
            EnsureAffixData();
            var affix = _affixes.FirstOrDefault(a => a.AffixId == affixId);
            return Math.Max(0, affix?.BasePrice > 0 ? affix.BasePrice : affix?.MarketValue ?? 0);
        }

        static int GetLinkPrice(List<string> affixIds)
            => GetMatchingLinks(affixIds).Sum(x => Math.Max(0, x.ExtraPrice));

        static IEnumerable<HumanWeaponAffixLinkPrice> GetMatchingLinks(List<string> affixIds)
        {
            EnsureAffixData();
            var set = new HashSet<string>(affixIds ?? new List<string>());
            return _linkPrices.Where(x => x != null && x.AffixIds != null
                && x.AffixIds.Count > 0 && x.AffixIds.All(set.Contains));
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

    public static class HumanArmarCatalog
    {
        static bool _appraisalSession;
        static List<HumanWeaponAffixDef> _affixes;
        static List<HumanWeaponAffixTier> _tiers;

        public static HumanArmar GetOrDefault(string itemId)
            => string.IsNullOrEmpty(itemId) ? null : CfgMgr.Cfgs?.TbHumanArmar?.GetOrDefault(itemId);

        public static bool IsHumanArmar(string itemId) => GetOrDefault(itemId) != null;

        public static bool CanAppraise => _appraisalSession;
        public static void BeginAppraisalSession() => _appraisalSession = true;
        public static void EndAppraisalSession() => _appraisalSession = false;

        public static ItemInstance4HumanArmar GetInstance(ItemStack stack)
            => stack?.InstanceInfo?.Get<ItemInstance4HumanArmar>();

        public static bool TryEnsureSeed(ItemStack stack)
        {
            var instance = GetInstance(stack);
            if (instance == null) return false;
            if (instance.IdentificationSeed == 0)
            {
                unchecked
                {
                    instance.IdentificationSeed = (stack.ItemInstanceId * 1103515245L) ^ 0x2A9F31D5L;
                    if (instance.IdentificationSeed == 0) instance.IdentificationSeed = 1;
                }
            }
            return true;
        }

        // Armor affixes are rolled when the instance is created. Appraisal only reveals them.
        public static bool TryGenerateAffixes(ItemStack stack)
        {
            var instance = GetInstance(stack);
            var def = GetOrDefault(stack?.ItemID);
            if (instance == null || def == null) return false;
            if (instance.AffixesGenerated) return true;
            if (instance.AffixIds != null && instance.AffixIds.Count > 0)
            {
                instance.AffixesGenerated = true;
                return true;
            }

            EnsureAffixData();
            TryEnsureSeed(stack);
            var rng = new System.Random(unchecked((int)instance.IdentificationSeed));
            int min = System.Math.Max(0, def.AffixMinCount);
            int max = System.Math.Max(min, def.AffixMaxCount);
            int count = min == max ? min : rng.Next(min, max + 1);
            var candidates = new List<HumanWeaponAffixDef>(_affixes);
            instance.AffixIds ??= new List<string>();
            instance.AffixTiers ??= new List<int>();
            instance.AffixIds.Clear();
            instance.AffixTiers.Clear();
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int total = 0;
                foreach (var candidate in candidates) total += System.Math.Max(1, candidate.Weight);
                int roll = rng.Next(total);
                var chosen = candidates[0];
                foreach (var candidate in candidates)
                {
                    roll -= System.Math.Max(1, candidate.Weight);
                    if (roll < 0) { chosen = candidate; break; }
                }

                instance.AffixIds.Add(chosen.AffixId);
                instance.AffixTiers.Add(RollTier(_tiers.FindAll(x => x.AffixId == chosen.AffixId), rng));
                candidates.RemoveAll(x => x.ExclusiveGroup != 0 && x.ExclusiveGroup == chosen.ExclusiveGroup);
            }
            instance.AffixesGenerated = true;
            return true;
        }

        static void EnsureAffixData()
        {
            if (_affixes != null) return;
            var defs = Resources.Load<TextAsset>("Config/Json/demo_tbweaponaffixdef");
            var tiers = Resources.Load<TextAsset>("Config/Json/demo_tbweaponaffixtier");
            _affixes = defs == null
                ? new List<HumanWeaponAffixDef>()
                : Newtonsoft.Json.JsonConvert.DeserializeObject<List<HumanWeaponAffixDef>>(defs.text) ?? new();
            _tiers = tiers == null
                ? new List<HumanWeaponAffixTier>()
                : Newtonsoft.Json.JsonConvert.DeserializeObject<List<HumanWeaponAffixTier>>(tiers.text) ?? new();
        }

        public static bool IsIdentified(ItemStack stack) => GetInstance(stack)?.IsIdentified == true;

        public static bool TryIdentify(ItemStack stack)
        {
            if (!CanAppraise) return false;
            var instance = GetInstance(stack);
            var def = GetOrDefault(stack?.ItemID);
            if (instance == null || def == null || instance.IsIdentified) return instance?.IsIdentified == true;

            TryGenerateAffixes(stack);

            instance.IsIdentified = true;
            return true;
        }

        static int RollTier(List<HumanWeaponAffixTier> rows, System.Random rng)
        {
            if (rows == null || rows.Count == 0) return 1;
            int total = 0;
            foreach (var row in rows) total += System.Math.Max(1, row.Weight);
            int roll = rng.Next(total);
            foreach (var row in rows)
            {
                roll -= System.Math.Max(1, row.Weight);
                if (roll < 0) return row.Tier;
            }
            return rows[0].Tier;
        }

        public static IReadOnlyList<string> GetAffixDisplayLines(ItemStack stack)
        {
            var instance = GetInstance(stack);
            if (instance == null || !instance.IsIdentified) return System.Array.Empty<string>();
            EnsureAffixData();
            var lines = new List<string>();
            for (int i = 0; i < (instance.AffixIds?.Count ?? 0); i++)
            {
                var affix = _affixes.Find(x => x.AffixId == instance.AffixIds[i]);
                if (affix == null) continue;
                int tier = instance.AffixTiers != null && i < instance.AffixTiers.Count ? instance.AffixTiers[i] : 1;
                lines.Add($"{affix.DisplayName} [品阶 {tier}]：{affix.Description}");
            }
            return lines;
        }

        public static long GetAffixMarketValue(ItemStack stack)
        {
            var instance = GetInstance(stack);
            if (instance == null || !instance.IsIdentified) return 0;
            EnsureAffixData();
            long value = 0;
            for (int i = 0; i < (instance.AffixIds?.Count ?? 0); i++)
            {
                var affix = _affixes.Find(x => x.AffixId == instance.AffixIds[i]);
                int tier = instance.AffixTiers != null && i < instance.AffixTiers.Count ? instance.AffixTiers[i] : 1;
                value += (affix?.BasePrice > 0 ? affix.BasePrice : affix?.MarketValue ?? 0) * System.Math.Max(1, tier);
            }
            return value;
        }

        public static long GetPotentialMarketValue(ItemStack stack)
        {
            var instance = GetInstance(stack);
            if (instance == null || instance.AffixIds == null) return 0;
            EnsureAffixData();
            long value = 0;
            for (int i = 0; i < instance.AffixIds.Count; i++)
            {
                var affix = _affixes.Find(x => x.AffixId == instance.AffixIds[i]);
                int tier = instance.AffixTiers != null && i < instance.AffixTiers.Count ? instance.AffixTiers[i] : 1;
                value += (affix?.BasePrice > 0 ? affix.BasePrice : affix?.MarketValue ?? 0) * System.Math.Max(1, tier);
            }
            return value;
        }

        public static IReadOnlyList<string> GetAffixMarketBreakdown(ItemStack stack)
        {
            var instance = GetInstance(stack);
            if (instance == null || !instance.IsIdentified) return System.Array.Empty<string>();
            EnsureAffixData();
            var lines = new List<string>();
            for (int i = 0; i < (instance.AffixIds?.Count ?? 0); i++)
            {
                var affix = _affixes.Find(x => x.AffixId == instance.AffixIds[i]);
                if (affix == null) continue;
                int tier = instance.AffixTiers != null && i < instance.AffixTiers.Count ? instance.AffixTiers[i] : 1;
                int price = (affix.BasePrice > 0 ? affix.BasePrice : affix.MarketValue) * System.Math.Max(1, tier);
                lines.Add($"{affix.DisplayName}：基础 {affix.BasePrice} x 品阶 {tier} = {price}");
            }
            return lines;
        }

        public static bool CanBeEquippedByPlayer(ItemStack stack) => false;
    }
}
