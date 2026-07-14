using SimpleJSON;

namespace cfg.demo
{
    public partial class TbUnitNpc
    {
        public void AddRuntime(UnitNpc row)
        {
            if (row == null || _dataMap.ContainsKey(row.Id)) return;
            _dataMap.Add(row.Id, row);
            _dataList.Add(row);
        }
    }

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
            tables.TbUnitNpc.AddRuntime(new cfg.demo.UnitNpc(JSON.Parse(BossNpcJson)));
            tables.TbUnitNpc.AddRuntime(new cfg.demo.UnitNpc(JSON.Parse(SummonNpcJson)));
        }

        const string AreaEffectJson = "{\"id\":\"treant_corrosive_pool\",\"shape_type\":2,\"shape_length\":0,\"shape_width\":0,\"shape_radius\":0.95,\"shape_angle\":0,\"area_buff_id\":\"b_treant_corrosion\",\"camp_filter\":1,\"default_lifetime\":8,\"refresh_buff_duration\":true}";

        const string SummonSkillJson = "{\"skill_id\":\"treant_summon_root_nodes\",\"main_ability_id\":\"treant_summon_root_nodes\",\"desc\":\"Summon fixed root nodes\",\"is_passive\":false,\"passive_buff_ids\":[],\"passive_buff_level_variable_key\":\"\",\"is_combo\":false,\"need_h_mode\":false,\"interrupt_combo\":true,\"is_derived\":false,\"stack_count\":0,\"icon_path\":\"\",\"priority\":35,\"desired_use_angle\":180,\"desired_use_distance\":8,\"buffer_cache_time\":0,\"cast_conditions\":[]}";

        const string BarrageSkillJson = "{\"skill_id\":\"treant_corrosive_barrage\",\"main_ability_id\":\"treant_corrosive_barrage\",\"desc\":\"Launch corrosive fruit around the target\",\"is_passive\":false,\"passive_buff_ids\":[],\"passive_buff_level_variable_key\":\"\",\"is_combo\":false,\"need_h_mode\":false,\"interrupt_combo\":true,\"is_derived\":false,\"stack_count\":0,\"icon_path\":\"\",\"priority\":55,\"desired_use_angle\":55,\"desired_use_distance\":7,\"buffer_cache_time\":0,\"cast_conditions\":[]}";

        const string SummonLevelJson = "{\"skill_id\":\"treant_summon_root_nodes\",\"level\":1,\"cool_down\":14,\"ability_extra\":[],\"main_ability_id\":\"\",\"passive_buff_ids\":[]}";

        const string BarrageLevelJson = "{\"skill_id\":\"treant_corrosive_barrage\",\"level\":1,\"cool_down\":7,\"ability_extra\":[],\"main_ability_id\":\"\",\"passive_buff_ids\":[]}";

        const string BossNpcJson = "{\"id\":\"forest_treant_boss\",\"name\":\"Deep Forest Treant\",\"desc\":\"Ancient treant in the deep forest\",\"prefab_name\":\"forest_treant_boss\",\"attr_template_id\":10001,\"emnity_cfg_id\":\"default_monster\",\"faction_id\":4,\"move_style\":1,\"idle_move_behave\":3,\"ai_brain_id\":\"default\",\"skill_list\":[\"default_normal_attack\",\"treant_corrosive_barrage\",\"treant_summon_root_nodes\"],\"h_behave_list\":[],\"is_peace\":false,\"no_aggro\":false,\"peace_dialog_id\":\"\",\"defeat_drop_id\":1,\"desire_density_type\":1,\"not_target\":false,\"always_h_mode\":false,\"ignore_attract_level\":0,\"unit_bottom_size\":0.8,\"no_unsensored\":false,\"desire_crystal_random_attachable\":false,\"no_real_jing\":false,\"vision_cone_kind\":0,\"vision_range\":12,\"vision_fov_deg\":180,\"mind_tag\":\"forest_treant\",\"jingyuan_pool_id\":\"orc\",\"fallback_drop_id\":0,\"lock_initial_face\":false,\"race_id\":\"monster\",\"npc_tags\":[\"boss\",\"forest\"]}";

        const string SummonNpcJson = "{\"id\":\"forest_treant_root_node\",\"name\":\"Root Node\",\"desc\":\"Fixed root node summoned by the treant\",\"prefab_name\":\"forest_treant_root_node\",\"attr_template_id\":10000,\"emnity_cfg_id\":\"default_monster\",\"faction_id\":4,\"move_style\":0,\"idle_move_behave\":0,\"ai_brain_id\":\"fixed_turret\",\"skill_list\":[\"cannon_shoot_01\"],\"h_behave_list\":[],\"is_peace\":false,\"no_aggro\":false,\"peace_dialog_id\":\"\",\"defeat_drop_id\":0,\"desire_density_type\":1,\"not_target\":false,\"always_h_mode\":false,\"ignore_attract_level\":0,\"unit_bottom_size\":0.5,\"no_unsensored\":false,\"desire_crystal_random_attachable\":false,\"no_real_jing\":false,\"vision_cone_kind\":0,\"vision_range\":10,\"vision_fov_deg\":360,\"mind_tag\":\"forest_treant_summon\",\"jingyuan_pool_id\":\"\",\"fallback_drop_id\":0,\"lock_initial_face\":true,\"race_id\":\"monster\",\"npc_tags\":[\"summon\",\"forest\"]}";
    }
}
