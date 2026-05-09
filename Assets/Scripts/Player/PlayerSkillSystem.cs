using System;
using System.Collections;
using System.Collections.Generic;
using My.Config;
using My.Map.Logic;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    // 零分配：对已学列表仅暴露 SkillId 序列（与 learnedSkills 同源）
    public sealed class LearnedSkillIdView : IReadOnlyList<string>
    {
        readonly List<LearnedSkillEntry> _entries;

        public LearnedSkillIdView(List<LearnedSkillEntry> entries)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public int Count => _entries.Count;

        public string this[int index] => _entries[index].SkillId;

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                yield return _entries[i].SkillId;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class PlayerSkillSystem : IPlayerSystem
    {
        public GameLogicManager LogicManager { get; private set; }

        public readonly List<LearnedSkillEntry> learnedSkills = new();

        readonly LearnedSkillIdView _learnedSkillIdsView;

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

            "player_burst_h_voice",

            "force_dash_push_down",
        };

        public PlayerSkillSystem()
        {
            NormalSkillSlots[0] = "queen_attack";
            NormalSkillSlots[1] = "queen_attack_heavy";
            NormalSkillSlots[2] = "queen_dash";
            NormalSkillSlots[3] = "queen_dash_down";
            NormalSkillSlots[4] = "queen_pull_all";
            NormalSkillSlots[5] = "player_mortar_acquire_01";

            _learnedSkillIdsView = new LearnedSkillIdView(learnedSkills);
            SeedDefaultLearnedList();
        }

        public IReadOnlyList<string> LearnedSkillIdsView => _learnedSkillIdsView;

        private void SeedDefaultLearnedList()
        {
            learnedSkills.Clear();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in DefaultLearnedSeed)
            {
                if (string.IsNullOrEmpty(id) || seen.Contains(id))
                {
                    continue;
                }

                seen.Add(id);
                learnedSkills.Add(new LearnedSkillEntry { SkillId = id, Level = 1 });
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

            if (pd.LearnedSkills is { Count: > 0 })
            {
                learnedSkills.Clear();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var row in pd.LearnedSkills)
                {
                    if (row == null || string.IsNullOrEmpty(row.SkillId) || seen.Contains(row.SkillId))
                    {
                        continue;
                    }

                    seen.Add(row.SkillId);
                    learnedSkills.Add(new LearnedSkillEntry
                    {
                        SkillId = row.SkillId,
                        Level = Math.Max(1, row.Level),
                    });
                }

                if (learnedSkills.Count == 0)
                {
                    SeedDefaultLearnedList();
                }
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
            {
                data.PlayerData = new PlayerData();
            }

            data.PlayerData.LearnedSkills = new List<LearnedSkillEntry>();
            foreach (var e in learnedSkills)
            {
                if (e == null || string.IsNullOrEmpty(e.SkillId))
                {
                    continue;
                }

                data.PlayerData.LearnedSkills.Add(new LearnedSkillEntry()
                {
                    SkillId = e.SkillId,
                    Level = Math.Max(1, e.Level),
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
            if (saved == null || saved.Count == 0)
            {
                return;
            }

            for (int i = 0; i < 5 && i < saved.Count; i++)
            {
                int idx = 3 + i;
                string v = saved[i];
                if (!string.IsNullOrEmpty(v))
                {
                    NormalSkillSlots[idx] = v;
                }
            }
        }

        public bool IsLearned(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            foreach (var e in learnedSkills)
            {
                if (e != null && string.Equals(e.SkillId, skillId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetSkillLevel(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || !IsLearned(skillId))
            {
                return 1;
            }

            foreach (var e in learnedSkills)
            {
                if (e != null && string.Equals(e.SkillId, skillId, StringComparison.Ordinal))
                {
                    return Math.Max(1, e.Level);
                }
            }

            return 1;
        }

        public bool TrySetSkillLevel(string skillId, int level)
        {
            if (string.IsNullOrEmpty(skillId) || !IsLearned(skillId) || level < 1)
            {
                return false;
            }

            foreach (var e in learnedSkills)
            {
                if (e != null && string.Equals(e.SkillId, skillId, StringComparison.Ordinal))
                {
                    e.Level = level;
                    return true;
                }
            }

            return false;
        }

        public bool TryAddLearned(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || IsLearned(skillId))
            {
                return false;
            }

            learnedSkills.Add(new LearnedSkillEntry { SkillId = skillId, Level = 1 });
            return true;
        }

        public bool TryRemoveLearned(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            int removed = learnedSkills.RemoveAll(e =>
                e != null && string.Equals(e.SkillId, skillId, StringComparison.Ordinal));
            return removed > 0;
        }

        public bool TryReplaceLearnedSkill(string oldSkillId, string newSkillId)
        {
            if (string.IsNullOrEmpty(oldSkillId) || string.IsNullOrEmpty(newSkillId))
            {
                return false;
            }

            int carry = 1;
            int removed = 0;
            for (int i = learnedSkills.Count - 1; i >= 0; i--)
            {
                var e = learnedSkills[i];
                if (e == null || !string.Equals(e.SkillId, oldSkillId, StringComparison.Ordinal))
                {
                    continue;
                }

                carry = Math.Max(carry, Math.Max(1, e.Level));
                learnedSkills.RemoveAt(i);
                removed++;
            }

            if (removed == 0)
            {
                return false;
            }

            if (!IsLearned(newSkillId))
            {
                learnedSkills.Add(new LearnedSkillEntry { SkillId = newSkillId, Level = carry });
            }

            ReplaceSkillIdInNormalSlots(oldSkillId, newSkillId);
            return true;
        }

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
