using System;
using System.Collections.Generic;
using My.Config;
using My.Map.Logic;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public class PlayerSkillSystem : IPlayerSystem
    {
        public GameLogicManager LogicManager { get; private set; }

        // 已学习技能（当前需求：默认全部视为已学，存档可覆盖）
        public List<string> learnedSkillIds = new();

        // 技能等级（仅已学技能有意义；缺省在 GetSkillLevel 中为 1）
        private readonly Dictionary<string, int> learnedSkillLevelById = new(StringComparer.Ordinal);

        // 暴露形态 / 特定 HUD 使用的 8 槽技能栏
        public readonly string[] NormalSkillSlots = new string[8];

        private static readonly string[] DefaultLearnedSeed =
        {
            "queen_attack",
            "queen_attack_heavy",
            "queen_dash",
            "queen_shoot",
            "fix_clothes",
            "spawn_attract",
            "queen_pull_all",
            "player_mortar_acquire_01",
            "player_enter_queen",
            "player_quit_queen",
            "queen_dash_down",
            "h_mode_execute",
            "queen_counter",
            "queen_desire_charm_01",
            "player_small_staggering",
            "default_push",
            "player_normal_defend",
            "crazy_fire",
            "player_dark_dance",
            "player_ziwei",
            "player_push_surround",
            "player_trace_bullet_01",

            "player_naishou_to_jianshang",
        };

        public PlayerSkillSystem()
        {
            NormalSkillSlots[0] = "queen_attack";
            NormalSkillSlots[1] = "queen_attack_heavy";
            NormalSkillSlots[2] = "queen_dash";
            NormalSkillSlots[3] = "queen_dash_down";
            NormalSkillSlots[4] = "queen_pull_all";
            NormalSkillSlots[5] = "player_mortar_acquire_01";

            SeedDefaultLearnedList();
        }

        private void SeedDefaultLearnedList()
        {
            learnedSkillIds.Clear();
            learnedSkillLevelById.Clear();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in DefaultLearnedSeed)
            {
                if (string.IsNullOrEmpty(id) || seen.Contains(id)) continue;
                seen.Add(id);
                learnedSkillIds.Add(id);
                learnedSkillLevelById[id] = 1;
            }
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            LogicManager = ctx;

            if (savingData == null)
            {
                SeedDefaultLearnedList();
                return;
            }

            SaveData.EnsureHydrated(savingData);
            var pd = savingData.PlayerData;

            if (pd.LearnedSkillIds is { Count: > 0 })
            {
                learnedSkillIds.Clear();
                learnedSkillLevelById.Clear();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var id in pd.LearnedSkillIds)
                {
                    if (string.IsNullOrEmpty(id) || seen.Contains(id)) continue;
                    seen.Add(id);
                    learnedSkillIds.Add(id);
                }

                if (pd.LearnedSkillLevels is { Count: > 0 })
                {
                    foreach (var row in pd.LearnedSkillLevels)
                    {
                        if (row == null || string.IsNullOrEmpty(row.SkillId) || !seen.Contains(row.SkillId))
                        {
                            continue;
                        }

                        learnedSkillLevelById[row.SkillId] = Math.Max(1, row.Level);
                    }
                }

                EnsureLevelsForAllLearnedDefault1();
            }
            else
            {
                SeedDefaultLearnedList();
            }

            ApplyNormalSlotOverridesFromSave(pd.NormalSkillSlotOverrides);
        }

        public void Tick(float dt)
        {
        }

        public void WriteToSave(SaveData data)
        {
            if (data.PlayerData == null)
                data.PlayerData = new PlayerData();

            data.PlayerData.LearnedSkillIds = new List<string>(learnedSkillIds);

            data.PlayerData.LearnedSkillLevels = new List<LearnedSkillLevelEntry>();
            foreach (var id in learnedSkillIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                data.PlayerData.LearnedSkillLevels.Add(new LearnedSkillLevelEntry()
                {
                    SkillId = id,
                    Level = GetSkillLevel(id),
                });
            }

            data.PlayerData.NormalSkillSlotOverrides = new List<string>(5);
            for (int i = 0; i < 5; i++)
            {
                int idx = 3 + i;
                data.PlayerData.NormalSkillSlotOverrides.Add(NormalSkillSlots[idx] ?? string.Empty);
            }
        }

        private void ApplyNormalSlotOverridesFromSave(List<string> saved)
        {
            if (saved == null || saved.Count == 0) return;

            for (int i = 0; i < 5 && i < saved.Count; i++)
            {
                int idx = 3 + i;
                string v = saved[i];
                if (!string.IsNullOrEmpty(v))
                    NormalSkillSlots[idx] = v;
            }
        }

        public bool IsLearned(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return false;
            foreach (var id in learnedSkillIds)
            {
                if (string.Equals(id, skillId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        void EnsureLevelsForAllLearnedDefault1()
        {
            foreach (var id in learnedSkillIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!learnedSkillLevelById.ContainsKey(id))
                {
                    learnedSkillLevelById[id] = 1;
                }
            }
        }

        public int GetSkillLevel(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || !IsLearned(skillId))
            {
                return 1;
            }

            return learnedSkillLevelById.TryGetValue(skillId, out var lv) ? Math.Max(1, lv) : 1;
        }

        public bool TrySetSkillLevel(string skillId, int level)
        {
            if (string.IsNullOrEmpty(skillId) || !IsLearned(skillId) || level < 1)
            {
                return false;
            }

            learnedSkillLevelById[skillId] = level;
            return true;
        }

        internal void OnSkillLearned(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            learnedSkillLevelById.TryAdd(skillId, 1);
        }

        internal void OnSkillForgotten(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            learnedSkillLevelById.Remove(skillId);
        }

        internal void OnReplaceSkillId(string oldSkillId, string newSkillId)
        {
            if (string.IsNullOrEmpty(oldSkillId) || string.IsNullOrEmpty(newSkillId))
            {
                return;
            }

            int carry = learnedSkillLevelById.TryGetValue(oldSkillId, out var lv) ? lv : 1;
            learnedSkillLevelById.Remove(oldSkillId);
            if (!learnedSkillLevelById.ContainsKey(newSkillId))
            {
                learnedSkillLevelById[newSkillId] = Math.Max(1, carry);
            }
        }

        // 将技能装配到 Normal 槽位；0~2 为固定槽，不可通过此处修改
        public bool TryAssignNormalSlot(int slotIndex, string skillId, bool allowDuplicateSwap, out string failReason)
        {
            failReason = null;
            if (slotIndex < 3 || slotIndex > 7)
            {
                failReason = "invalid_slot";
                return false;
            }
            if (string.IsNullOrEmpty(skillId))
            {
                failReason = "empty_skill";
                return false;
            }
            if (!IsLearned(skillId))
            {
                failReason = "not_learned";
                return false;
            }

            if (!allowDuplicateSwap)
            {
                for (int i = 3; i <= 7; i++)
                {
                    if (i != slotIndex && NormalSkillSlots[i] == skillId)
                    {
                        failReason = "duplicate";
                        return false;
                    }
                }
            }
            else
            {
                for (int i = 3; i <= 7; i++)
                {
                    if (i != slotIndex && NormalSkillSlots[i] == skillId)
                    {
                        NormalSkillSlots[i] = NormalSkillSlots[slotIndex];
                        break;
                    }
                }
            }

            NormalSkillSlots[slotIndex] = skillId;
            return true;
        }

        public bool BelongsToSchoolPool(string schoolId, string skillId) =>
            SkillSchoolTable.Instance.SkillDefinedInSchool(schoolId, skillId);

        // 技能 id 替换时同步 Normal 槽位引用（3~7 可装配区及固定展示槽）
        public void ReplaceSkillIdInNormalSlots(string oldSkillId, string newSkillId)
        {
            if (string.IsNullOrEmpty(oldSkillId))
            {
                return;
            }

            for (int i = 0; i < NormalSkillSlots.Length; i++)
            {
                var s = NormalSkillSlots[i];
                if (s != null && string.Equals(s, oldSkillId, StringComparison.Ordinal))
                {
                    NormalSkillSlots[i] = newSkillId;
                }
            }
        }
    }
}
