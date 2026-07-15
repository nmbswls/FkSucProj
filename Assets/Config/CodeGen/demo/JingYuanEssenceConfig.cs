using Luban;
using SimpleJSON;
using System.Collections.Generic;

namespace cfg.demo
{
    [System.Serializable]
    public sealed class JingYuanTuneRule
    {
        public JingYuanTuneRule(JSONNode b) { RuleId = b["rule_id"]; BaseSuccessRate = b["base_success_rate"].AsInt; ConcentrationDiffRate = b["concentration_diff_rate"].AsInt; TargetConcentrationPenalty = b["target_concentration_penalty"].AsInt; LowerLevelPenaltyPerLevel = b["lower_level_penalty_per_level"].AsInt; MinSuccessRate = b["min_success_rate"].AsInt; MaxSuccessRate = b["max_success_rate"].AsInt; MinGain = b["min_gain"].AsInt; MaxGain = b["max_gain"].AsInt; ResidueOnFailure = b["residue_on_failure"].AsInt; ResidueOnSuccess = b["residue_on_success"].AsInt; AffixInheritRate = b["affix_inherit_rate"].AsInt; ResidueBoostCost = b["residue_boost_cost"].AsInt; ResidueBoostSuccessRate = b["residue_boost_success_rate"].AsInt; }
        public string RuleId; public int BaseSuccessRate; public int ConcentrationDiffRate; public int TargetConcentrationPenalty; public int LowerLevelPenaltyPerLevel; public int MinSuccessRate; public int MaxSuccessRate; public int MinGain; public int MaxGain; public int ResidueOnFailure; public int ResidueOnSuccess; public int AffixInheritRate; public int ResidueBoostCost; public int ResidueBoostSuccessRate;
        public void ResolveRef(Tables tables) { }
    }
    public sealed class TbJingYuanTuneRule
    {
        readonly Dictionary<string, JingYuanTuneRule> _map = new(); public readonly List<JingYuanTuneRule> DataList = new();
        public TbJingYuanTuneRule(JSONNode b) { foreach (var e in b.Children) { var v = new JingYuanTuneRule(e); DataList.Add(v); _map.Add(v.RuleId, v); } }
        public JingYuanTuneRule GetOrDefault(string key) => _map.TryGetValue(key, out var v) ? v : null;
        public void ResolveRef(Tables tables) { foreach (var v in DataList) v.ResolveRef(tables); }
    }

    [System.Serializable]
    public sealed class JingYuanRenewalRule
    {
        public JingYuanRenewalRule(JSONNode b) { RuleId = b["rule_id"]; DesireCrystalItemId = b["desire_crystal_item_id"]; DesireCrystalCost = b["desire_crystal_cost"].AsInt; ResidueCost = b["residue_cost"].AsInt; AddedShelfLifeDays = b["added_shelf_life_days"].AsInt; MaxRenewalCount = b["max_renewal_count"].AsInt; }
        public string RuleId; public string DesireCrystalItemId; public int DesireCrystalCost; public int ResidueCost; public int AddedShelfLifeDays; public int MaxRenewalCount;
        public void ResolveRef(Tables tables) { }
    }
    public sealed class TbJingYuanRenewalRule
    {
        readonly Dictionary<string, JingYuanRenewalRule> _map = new(); public readonly List<JingYuanRenewalRule> DataList = new();
        public TbJingYuanRenewalRule(JSONNode b) { foreach (var e in b.Children) { var v = new JingYuanRenewalRule(e); DataList.Add(v); _map.Add(v.RuleId, v); } }
        public JingYuanRenewalRule GetOrDefault(string key) => _map.TryGetValue(key, out var v) ? v : null;
        public void ResolveRef(Tables tables) { foreach (var v in DataList) v.ResolveRef(tables); }
    }

    [System.Serializable]
    public sealed class JingYuanTypeInfo
    {
        public JingYuanTypeInfo(JSONNode b) { TypeId = (EJingYuanType)b["type_id"].AsInt; DisplayName = b["display_name"]; IconPath = b["icon_path"]; RaceId = b["race_id"]; MatchTag = b["match_tag"]; SortOrder = b["sort_order"].AsInt; }
        public EJingYuanType TypeId; public string DisplayName; public string IconPath; public string RaceId; public string MatchTag; public int SortOrder;
        public void ResolveRef(Tables tables) { }
    }

    public sealed class TbJingYuanTypeInfo
    {
        readonly Dictionary<EJingYuanType, JingYuanTypeInfo> _map = new(); public readonly List<JingYuanTypeInfo> DataList = new();
        public TbJingYuanTypeInfo(JSONNode b) { foreach (var e in b.Children) { var v = new JingYuanTypeInfo(e); DataList.Add(v); _map.Add(v.TypeId, v); } }
        public JingYuanTypeInfo GetOrDefault(EJingYuanType key) => _map.TryGetValue(key, out var v) ? v : null;
        public void ResolveRef(Tables tables) { foreach (var v in DataList) v.ResolveRef(tables); }
    }

    [System.Serializable]
    public sealed class JingYuanPremiumEssence
    {
        public JingYuanPremiumEssence(JSONNode b) { EssenceId = b["essence_id"]; TypeId = (EJingYuanType)b["type_id"].AsInt; DisplayName = b["display_name"]; BaseShelfLifeDays = b["base_shelf_life_days"].AsInt; ExtraAffixPoolId = b["extra_affix_pool_id"]; SourceTag = b["source_tag"]; }
        public string EssenceId; public EJingYuanType TypeId; public string DisplayName; public int BaseShelfLifeDays; public string ExtraAffixPoolId; public string SourceTag;
        public void ResolveRef(Tables tables) { }
    }

    public sealed class TbJingYuanPremiumEssence
    {
        readonly Dictionary<string, JingYuanPremiumEssence> _map = new(); public readonly List<JingYuanPremiumEssence> DataList = new();
        public TbJingYuanPremiumEssence(JSONNode b) { foreach (var e in b.Children) { var v = new JingYuanPremiumEssence(e); DataList.Add(v); _map.Add(v.EssenceId, v); } }
        public JingYuanPremiumEssence GetOrDefault(string key) => _map.TryGetValue(key, out var v) ? v : null;
        public void ResolveRef(Tables tables) { foreach (var v in DataList) v.ResolveRef(tables); }
    }

    [System.Serializable]
    public sealed class JingYuanPremiumEffect
    {
        public JingYuanPremiumEffect(JSONNode b) { TypeId = (EJingYuanType)b["type_id"].AsInt; DropLevel = b["drop_level"].AsInt; ConcentrationFloor = b["concentration_floor"].AsInt; AttrId = b["attr_id"].AsInt; AttrValue = b["attr_value"].AsLong; }
        public EJingYuanType TypeId; public int DropLevel; public int ConcentrationFloor; public int AttrId; public long AttrValue;
        public void ResolveRef(Tables tables) { }
    }

    public sealed class TbJingYuanPremiumEffect
    {
        readonly Dictionary<(EJingYuanType, int, int), JingYuanPremiumEffect> _map = new(); public readonly List<JingYuanPremiumEffect> DataList = new();
        public TbJingYuanPremiumEffect(JSONNode b) { foreach (var e in b.Children) { var v = new JingYuanPremiumEffect(e); DataList.Add(v); _map.Add((v.TypeId, v.DropLevel, v.ConcentrationFloor), v); } }
        public JingYuanPremiumEffect Get(EJingYuanType typeId, int dropLevel, int concentrationFloor) => _map.TryGetValue((typeId, dropLevel, concentrationFloor), out var v) ? v : null;
        public void ResolveRef(Tables tables) { foreach (var v in DataList) v.ResolveRef(tables); }
    }

    [System.Serializable]
    public sealed class JingYuanEssenceLevelMap
    {
        public JingYuanEssenceLevelMap(JSONNode b) { MapId = b["map_id"]; NpcLevelMin = b["npc_level_min"].AsInt; NpcLevelMax = b["npc_level_max"].AsInt; EssenceLevel = b["essence_level"].AsInt; }
        public string MapId; public int NpcLevelMin; public int NpcLevelMax; public int EssenceLevel; public void ResolveRef(Tables tables) { }
    }
    public sealed class TbJingYuanEssenceLevelMap
    {
        public readonly List<JingYuanEssenceLevelMap> DataList = new();
        public TbJingYuanEssenceLevelMap(JSONNode b) { foreach (var e in b.Children) DataList.Add(new JingYuanEssenceLevelMap(e)); }
        public void ResolveRef(Tables tables) { foreach (var v in DataList) v.ResolveRef(tables); }
    }

    [System.Serializable]
    public sealed class JingYuanTypePoolEnum
    {
        public JingYuanTypePoolEnum(JSONNode b) { EntryId = b["entry_id"].AsInt; PoolId = b["pool_id"]; TypeId = (EJingYuanType)b["type_id"].AsInt; Weight = b["weight"].AsInt; }
        public int EntryId; public string PoolId; public EJingYuanType TypeId; public int Weight; public void ResolveRef(Tables tables) { }
    }
    public sealed class TbJingYuanTypePoolEnum
    {
        public readonly List<JingYuanTypePoolEnum> DataList = new();
        public TbJingYuanTypePoolEnum(JSONNode b) { foreach (var e in b.Children) DataList.Add(new JingYuanTypePoolEnum(e)); }
        public void ResolveRef(Tables tables) { foreach (var v in DataList) v.ResolveRef(tables); }
    }

    [System.Serializable]
    public sealed class NamedNpcJingYuanType
    {
        public NamedNpcJingYuanType(JSONNode b) { CharacterKey = b["character_key"]; TypeId = (EJingYuanType)b["type_id"].AsInt; }
        public string CharacterKey; public EJingYuanType TypeId; public void ResolveRef(Tables tables) { }
    }
    public sealed class TbNamedNpcJingYuanType
    {
        readonly Dictionary<string, NamedNpcJingYuanType> _map = new(); public readonly List<NamedNpcJingYuanType> DataList = new();
        public TbNamedNpcJingYuanType(JSONNode b) { foreach (var e in b.Children) { var v = new NamedNpcJingYuanType(e); DataList.Add(v); _map.Add(v.CharacterKey, v); } }
        public NamedNpcJingYuanType GetOrDefault(string key) => _map.TryGetValue(key, out var v) ? v : null;
        public void ResolveRef(Tables tables) { foreach (var v in DataList) v.ResolveRef(tables); }
    }
}
