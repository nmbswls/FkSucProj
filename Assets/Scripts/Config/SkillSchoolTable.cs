using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace My.Config
{
    // 流派技能配置表（数据来自 Resources/Config/skill_school_table.json，便于以表形式维护）
    [Serializable]
    public class SkillSchoolTable
    {
        [Serializable]
        public class SchoolEntry
        {
            public string Id;
            public string DisplayName;
            public List<string> SkillIds = new();
        }

        public List<SchoolEntry> Schools = new();

        private static SkillSchoolTable _instance;
        private readonly Dictionary<string, SchoolEntry> _byId = new(StringComparer.Ordinal);

        public static SkillSchoolTable Instance => _instance ??= LoadOrFallback();

        public IReadOnlyList<SchoolEntry> AllSchools => Schools;

        private static SkillSchoolTable LoadOrFallback()
        {
            var ta = Resources.Load<TextAsset>("Config/skill_school_table");
            if (ta != null && !string.IsNullOrWhiteSpace(ta.text))
            {
                try
                {
                    var t = JsonConvert.DeserializeObject<SkillSchoolTable>(ta.text);
                    if (t != null)
                    {
                        t.RebuildIndex();
                        return t;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("SkillSchoolTable: failed to parse JSON. " + e.Message);
                }
            }

            var fallback = BuildEmbeddedFallback();
            fallback.RebuildIndex();
            return fallback;
        }

        private void RebuildIndex()
        {
            _byId.Clear();
            if (Schools == null) return;
            foreach (var s in Schools)
            {
                if (s == null || string.IsNullOrEmpty(s.Id)) continue;
                _byId[s.Id] = s;
            }
        }

        public bool TryGetSchool(string schoolId, out SchoolEntry entry) =>
            _byId.TryGetValue(schoolId, out entry);

        public bool SkillDefinedInSchool(string schoolId, string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return false;
            if (!_byId.TryGetValue(schoolId, out var school) || school.SkillIds == null)
                return false;
            foreach (var id in school.SkillIds)
            {
                if (string.Equals(id, skillId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static SkillSchoolTable BuildEmbeddedFallback()
        {
            return new SkillSchoolTable
            {
                Schools = new List<SchoolEntry>
                {
                    new()
                    {
                        Id = "queen_style",
                        DisplayName = "Queen",
                        SkillIds = new List<string>
                        {
                            "queen_attack", "queen_attack_heavy", "queen_dash", "queen_shoot",
                            "queen_pull_all", "queen_dash_down", "queen_counter",
                            "player_enter_queen", "player_quit_queen", "h_mode_execute",
                        },
                    },
                    new()
                    {
                        Id = "field_ops",
                        DisplayName = "Field",
                        SkillIds = new List<string>
                        {
                            "fix_clothes", "spawn_attract", "player_mortar_acquire_01",
                            "player_normal_defend", "default_push", "player_small_staggering",
                            "crazy_fire", "player_dark_dance", "player_push_surround",
                            "player_trace_bullet_01",
                        },
                    },
                    new()
                    {
                        Id = "misc",
                        DisplayName = "Misc",
                        SkillIds = new List<string>
                        {
                            "player_ziwei", "player_mortar_acquire_01",
                        },
                    },
                },
            };
        }
    }
}
