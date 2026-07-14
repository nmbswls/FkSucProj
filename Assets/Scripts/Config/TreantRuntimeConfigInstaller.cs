using SimpleJSON;

namespace cfg.demo
{
    public partial class TbMapAreaEffect
    {
        public void AddRuntime(MapAreaEffect row)
        {
            if (row == null || _dataMap.ContainsKey(row.Id)) return;
            _dataMap.Add(row.Id, row);
            _dataList.Add(row);
        }
    }

    public partial class TbEntitySkillData
    {
        public void AddRuntime(EntitySkillData row)
        {
            if (row == null || _dataMap.ContainsKey(row.SkillId)) return;
            _dataMap.Add(row.SkillId, row);
            _dataList.Add(row);
        }
    }

    public partial class TbEntitySkillLevel
    {
        public void AddRuntime(EntitySkillLevel row)
        {
            if (row == null || _dataMapUnion.ContainsKey((row.SkillId, row.Level))) return;
            _dataMapUnion.Add((row.SkillId, row.Level), row);
            _dataList.Add(row);
        }
    }
}

namespace My.Config
{
    public static class TreantRuntimeConfigInstaller
    {
        public static void Install(cfg.Tables tables)
        {
            tables.TbMapAreaEffect.AddRuntime(new cfg.demo.MapAreaEffect(JSON.Parse(AreaEffectJson)));
            tables.TbEntitySkillData.AddRuntime(new cfg.demo.EntitySkillData(JSON.Parse(SummonSkillJson)));
            tables.TbEntitySkillData.AddRuntime(new cfg.demo.EntitySkillData(JSON.Parse(BarrageSkillJson)));
            tables.TbEntitySkillLevel.AddRuntime(new cfg.demo.EntitySkillLevel(JSON.Parse(SummonLevelJson)));
            tables.TbEntitySkillLevel.AddRuntime(new cfg.demo.EntitySkillLevel(JSON.Parse(BarrageLevelJson)));
        }

        const string AreaEffectJson = "{\"id\":\"treant_corrosive_pool\",\"shape_type\":2,\"shape_length\":0,\"shape_width\":0,\"shape_radius\":0.95,\"shape_angle\":0,\"area_buff_id\":\"b_treant_corrosion\",\"camp_filter\":1,\"default_lifetime\":8,\"refresh_buff_duration\":true}";

        const string SummonSkillJson = "{\"skill_id\":\"treant_summon_root_nodes\",\"main_ability_id\":\"treant_summon_root_nodes\",\"desc\":\"Summon fixed root nodes\",\"is_passive\":false,\"passive_buff_ids\":[],\"passive_buff_level_variable_key\":\"\",\"is_combo\":false,\"need_h_mode\":false,\"interrupt_combo\":true,\"is_derived\":false,\"stack_count\":0,\"icon_path\":\"\",\"priority\":35,\"desired_use_angle\":180,\"desired_use_distance\":8,\"buffer_cache_time\":0,\"cast_conditions\":[]}";

        const string BarrageSkillJson = "{\"skill_id\":\"treant_corrosive_barrage\",\"main_ability_id\":\"treant_corrosive_barrage\",\"desc\":\"Launch corrosive fruit around the target\",\"is_passive\":false,\"passive_buff_ids\":[],\"passive_buff_level_variable_key\":\"\",\"is_combo\":false,\"need_h_mode\":false,\"interrupt_combo\":true,\"is_derived\":false,\"stack_count\":0,\"icon_path\":\"\",\"priority\":55,\"desired_use_angle\":55,\"desired_use_distance\":7,\"buffer_cache_time\":0,\"cast_conditions\":[]}";

        const string SummonLevelJson = "{\"skill_id\":\"treant_summon_root_nodes\",\"level\":1,\"cool_down\":14,\"ability_extra\":[],\"main_ability_id\":\"\",\"passive_buff_ids\":[]}";

        const string BarrageLevelJson = "{\"skill_id\":\"treant_corrosive_barrage\",\"level\":1,\"cool_down\":7,\"ability_extra\":[],\"main_ability_id\":\"\",\"passive_buff_ids\":[]}";

    }
}
