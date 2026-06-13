using System;
using System.Collections;
using System.Collections.Generic;
using My.Config;
using My.Map.Entity;
using My.Map.Logic;
using My.Quest;
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
        public readonly List<string> innateSkillIds = new();
        public readonly List<GrantedSkillEntry> grantedPassives = new();
        public readonly List<GrantedSkillEntry> grantedActives = new();

        readonly LearnedSkillIdView _learnedSkillIdsView;

        public const int PassiveSlotCount = 8;

        // 暴露形态 / 特定 HUD 使用的 8 槽技能栏
        public readonly string[] NormalSkillSlots = new string[8];

        // 成长面板被动搭配栏
        public readonly string[] PassiveSkillSlots = new string[PassiveSlotCount];

        string _tempSkillId;
        float _tempSkillRemainingSec;
        int _tempSkillLastCeilSec = -1;

        public bool HasTempSkill => !string.IsNullOrEmpty(_tempSkillId);

        public string TempSkillId => _tempSkillId;

        public float TempSkillRemainingSec => _tempSkillRemainingSec;

        public string GetTempSkillId() => _tempSkillId;

        public bool IsTempSkill(string skillId)
        {
            return HasTempSkill
                && !string.IsNullOrEmpty(skillId)
                && skillId == _tempSkillId;
        }

        private static readonly string[] InnateSkillIds =
        {
            "queen_attack",
            "queen_attack_heavy",
            "queen_dash",
            "queen_shoot",
            "fix_clothes",
            "spawn_attract",
            "fight_effect_place_stun_trap",
            "queen_pull_all",
            "player_mortar_acquire_01",
            "player_enter_queen",
            "player_enter_expose",
            "player_return_disguise",
            "player_quit_queen",
            "queen_dash_down",
            "h_mode_execute",
            "h_mode_control",
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
            "player_summon_ally_turret",
            "grapple_hook",

            "player_naishou_to_jianshang",

            "player_burst_h_voice",
            "player_burst_milk",

            "force_dash_push_down",

            // 变身/解除变身
            "player_enter_expose",
            "player_return_disguise",
        };

        // 默认槽位技能，卸下后回滚到此值（null 表示该槽位无默认）
        static readonly string[] DefaultNormalSlotSkills =
        {
            "queen_attack",          // 0
            "queen_attack_heavy",    // 1
            "queen_dash",            // 2
            null,        // 3
            null,        // 4
            null,        // 5
            null,        // 6
            null,        // 7
        };

        // 返回指定普通槽的默认技能 id，不存在则返回 null
        public static string GetDefaultNormalSlotSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= DefaultNormalSlotSkills.Length)
            {
                return null;
            }

            return DefaultNormalSlotSkills[slotIndex];
        }

        // 被动槽没有预设默认，始终返回 null
        public static string GetDefaultPassiveSlotSkill(int slotIndex) => null;

        public PlayerSkillSystem()
        {
            NormalSkillSlots[0] = "queen_attack";
            NormalSkillSlots[1] = "queen_attack_heavy";
            NormalSkillSlots[2] = "queen_dash";

            _learnedSkillIdsView = new LearnedSkillIdView(learnedSkills);
            SeedInnateSkills();
        }

        public IReadOnlyList<string> LearnedSkillIdsView => _learnedSkillIdsView;
        public IReadOnlyList<string> InnateSkillIdsView => innateSkillIds;

        void SeedInnateSkills()
        {
            innateSkillIds.Clear();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in InnateSkillIds)
            {
                if (string.IsNullOrEmpty(id) || seen.Contains(id))
                {
                    continue;
                }

                seen.Add(id);
                innateSkillIds.Add(id);
            }
        }

        static void LoadGrantedList(List<GrantedSkillEntry> target, List<GrantedSkillEntry> fromSave)
        {
            target.Clear();
            if (fromSave == null)
            {
                return;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in fromSave)
            {
                if (row == null || string.IsNullOrEmpty(row.SkillId) || seen.Contains(row.SkillId))
                {
                    continue;
                }

                seen.Add(row.SkillId);
                target.Add(new GrantedSkillEntry
                {
                    SkillId = row.SkillId,
                    Level = Math.Max(1, row.Level),
                });
            }
        }

        static void WriteGrantedList(List<GrantedSkillEntry> source, List<GrantedSkillEntry> dest)
        {
            dest.Clear();
            foreach (var e in source)
            {
                if (e == null || string.IsNullOrEmpty(e.SkillId))
                {
                    continue;
                }

                dest.Add(new GrantedSkillEntry
                {
                    SkillId = e.SkillId,
                    Level = Math.Max(1, e.Level),
                });
            }
        }

        static bool ValidateGrantSkill(string skillId, bool mustBePassive)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            return cfg != null && cfg.IsPassive == mustBePassive;
        }

        static bool TryUpsertGranted(List<GrantedSkillEntry> list, string skillId, int level)
        {
            level = Math.Max(1, level);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && string.Equals(list[i].SkillId, skillId, StringComparison.Ordinal))
                {
                    list[i].Level = level;
                    return true;
                }
            }

            list.Add(new GrantedSkillEntry { SkillId = skillId, Level = level });

            Debug.Log("TryUpsertGranted ???" + skillId);

            return true;
        }

        static int FindGrantedLevel(List<GrantedSkillEntry> list, string skillId)
        {
            foreach (var e in list)
            {
                if (e != null && string.Equals(e.SkillId, skillId, StringComparison.Ordinal))
                {
                    return Math.Max(1, e.Level);
                }
            }

            return 0;
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            LogicManager = ctx;
            SeedInnateSkills();
            learnedSkills.Clear();
            grantedPassives.Clear();
            grantedActives.Clear();

            if (savingData == null)
            {
                return;
            }

            SaveData.EnsureHydrated(savingData);
            var pd = savingData.PlayerData;

            if (pd.LearnedSkills is { Count: > 0 })
            {
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
            }

            LoadGrantedList(grantedPassives, pd.GrantedPassives);
            LoadGrantedList(grantedActives, pd.GrantedActives);

            ApplyNormalSlotOverridesFromSave(pd.NormalSkillSlotOverrides);
            ApplyPassiveSlotOverridesFromSave(pd.PassiveSkillSlotOverrides);
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void GrantTempSkill(string skillId, float durationSec = 0f)
        {
            if (string.IsNullOrEmpty(skillId) || SkillLibrary.GetSkillConfig(skillId) == null)
            {
                _tempSkillId = null;
                _tempSkillRemainingSec = 0f;
                _tempSkillLastCeilSec = -1;
                return;
            }

            _tempSkillId = skillId;
            _tempSkillRemainingSec = durationSec > 0f ? durationSec : 0f;
            _tempSkillLastCeilSec = durationSec > 0f ? Mathf.CeilToInt(durationSec) : -1;
            NotifyTempSkillChanged();
        }

        public void ClearTempSkill()
        {
            if (!HasTempSkill)
            {
                return;
            }

            _tempSkillId = null;
            _tempSkillRemainingSec = 0f;
            _tempSkillLastCeilSec = -1;
            NotifyTempSkillChanged();
        }

        public bool ConsumeTempSkillIfMatch(string usedSkillId)
        {
            if (!IsTempSkill(usedSkillId))
            {
                return false;
            }

            ClearTempSkill();
            return true;
        }

        public void Tick(float dt)
        {
            if (!HasTempSkill || _tempSkillRemainingSec <= 0f)
            {
                return;
            }

            int ceilBefore = Mathf.CeilToInt(_tempSkillRemainingSec);
            _tempSkillRemainingSec -= dt;
            if (_tempSkillRemainingSec <= 0f)
            {
                ClearTempSkill();
                return;
            }

            int ceilAfter = Mathf.CeilToInt(_tempSkillRemainingSec);
            if (ceilAfter != ceilBefore && ceilAfter != _tempSkillLastCeilSec)
            {
                _tempSkillLastCeilSec = ceilAfter;
                NotifyTempSkillChanged();
            }
        }

        static void NotifyTempSkillChanged()
        {
            PlayerEventBus.Publish(new PlayerTempSkillChangedEvent());
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

            data.PlayerData.PassiveSkillSlotOverrides = new List<string>(PassiveSlotCount);
            for (int i = 0; i < PassiveSlotCount; i++)
            {
                data.PlayerData.PassiveSkillSlotOverrides.Add(PassiveSkillSlots[i] ?? string.Empty);
            }

            WriteGrantedList(grantedPassives, data.PlayerData.GrantedPassives);
            WriteGrantedList(grantedActives, data.PlayerData.GrantedActives);
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

        private void ApplyPassiveSlotOverridesFromSave(List<string> saved)
        {
            if (saved == null || saved.Count == 0)
            {
                return;
            }

            for (int i = 0; i < PassiveSlotCount && i < saved.Count; i++)
            {
                string v = saved[i];
                if (!string.IsNullOrEmpty(v))
                {
                    PassiveSkillSlots[i] = v;
                }
            }
        }

        public bool IsPassiveEquipped(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            for (int i = 0; i < PassiveSkillSlots.Length; i++)
            {
                if (string.Equals(PassiveSkillSlots[i], skillId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSkillLearned(string skillId)
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

        public bool IsLearned(string skillId) => IsSkillLearned(skillId);

        public bool IsGrantedPassive(string skillId) => FindGrantedLevel(grantedPassives, skillId) > 0;

        public bool IsGrantedActive(string skillId) => FindGrantedLevel(grantedActives, skillId) > 0;

        public int GetGrantedPassiveLevel(string skillId) => FindGrantedLevel(grantedPassives, skillId);

        public int GetGrantedActiveLevel(string skillId) => FindGrantedLevel(grantedActives, skillId);

        public bool TryGrantPassive(string skillId, int level)
        {
            if (!ValidateGrantSkill(skillId, mustBePassive: true))
            {
                return false;
            }

            return TryUpsertGranted(grantedPassives, skillId, level);
        }

        public bool TryGrantActive(string skillId, int level)
        {
            if (!ValidateGrantSkill(skillId, mustBePassive: false))
            {
                return false;
            }

            return TryUpsertGranted(grantedActives, skillId, level);
        }

        public bool TryRevokePassive(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            return grantedPassives.RemoveAll(e =>
                e != null && string.Equals(e.SkillId, skillId, StringComparison.Ordinal)) > 0;
        }

        public bool TryRevokeActive(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            return grantedActives.RemoveAll(e =>
                e != null && string.Equals(e.SkillId, skillId, StringComparison.Ordinal)) > 0;
        }

        public int ResolvePassiveBuffLayerLevel(string skillId)
        {
            int granted = FindGrantedLevel(grantedPassives, skillId);
            if (granted > 0)
            {
                return ClampPassiveBuffLayer(skillId, granted);
            }

            if (!IsSkillLearned(skillId) || !IsPassiveEquipped(skillId))
            {
                return 0;
            }

            return ClampPassiveBuffLayer(skillId, GetSkillLevel(skillId));
        }

        public static int ClampPassiveBuffLayer(string skillId, int level)
        {
            return SkillPassiveBuffUtil.ClampLayerForPassiveSkill(skillId, level);
        }

        public int GetSkillLevel(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || !IsSkillLearned(skillId))
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
            if (string.IsNullOrEmpty(skillId) || !IsSkillLearned(skillId) || level < 1)
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

        public bool TryAddSkillLearned(string skillId, int level = 1)
        {
            if (string.IsNullOrEmpty(skillId) || IsSkillLearned(skillId))
            {
                return false;
            }

            learnedSkills.Add(new LearnedSkillEntry { SkillId = skillId, Level = Math.Max(1, level) });
            return true;
        }

        public bool TryAddLearned(string skillId) => TryAddSkillLearned(skillId);

        public bool TryRemoveLearned(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            int removed = learnedSkills.RemoveAll(e =>
                e != null && string.Equals(e.SkillId, skillId, StringComparison.Ordinal));
            if (removed > 0)
            {
                ClearSkillIdFromPassiveSlots(skillId);
            }

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

            if (!IsSkillLearned(newSkillId))
            {
                learnedSkills.Add(new LearnedSkillEntry { SkillId = newSkillId, Level = carry });
            }

            ReplaceSkillIdInNormalSlots(oldSkillId, newSkillId);
            ReplaceSkillIdInPassiveSlots(oldSkillId, newSkillId);
            return true;
        }

        public bool TryAssignNormalSlot(int slotIndex, string skillId, bool allowDuplicateSwap, out string failReason)
        {
            failReason = null;
            if (slotIndex < 0 || slotIndex > 7)
            {
                failReason = "invalid_slot";
                return false;
            }

            if (string.IsNullOrEmpty(skillId))
            {
                failReason = "empty_skill";
                return false;
            }

            if (!IsSkillLearned(skillId))
            {
                failReason = "not_learned";
                return false;
            }

            if (IsGrantedPassive(skillId) || IsGrantedActive(skillId))
            {
                failReason = "granted_not_slotable";
                return false;
            }

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg != null && cfg.IsPassive)
            {
                failReason = "passive_not_allowed";
                return false;
            }

            if (!allowDuplicateSwap)
            {
                for (int i = 0; i <= 7; i++)
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
                for (int i = 0; i <= 7; i++)
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

        public bool TryClearPassiveSlot(int slotIndex, out string failReason)
        {
            failReason = null;
            if (slotIndex < 0 || slotIndex >= PassiveSkillSlots.Length)
            {
                failReason = "invalid_slot";
                return false;
            }

            PassiveSkillSlots[slotIndex] = null;
            return true;
        }

        public bool TryClearNormalSlot(int slotIndex, out string failReason)
        {
            failReason = null;
            if (slotIndex < 3 || slotIndex > 7)
            {
                failReason = "fixed_slot";
                return false;
            }

            if (string.IsNullOrEmpty(NormalSkillSlots[slotIndex]))
            {
                failReason = "empty_slot";
                return false;
            }

            var skillId = NormalSkillSlots[slotIndex];
            if (IsGrantedPassive(skillId) || IsGrantedActive(skillId))
            {
                failReason = "granted_not_slotable";
                return false;
            }

            NormalSkillSlots[slotIndex] = null;
            return true;
        }

        public bool TryAssignPassiveSlot(int slotIndex, string skillId, bool allowDuplicateSwap, out string failReason)
        {
            failReason = null;
            if (slotIndex < 0 || slotIndex >= PassiveSkillSlots.Length)
            {
                failReason = "invalid_slot";
                return false;
            }

            if (string.IsNullOrEmpty(skillId))
            {
                failReason = "empty_skill";
                return false;
            }

            if (!IsSkillLearned(skillId))
            {
                failReason = "not_learned";
                return false;
            }

            if (IsGrantedPassive(skillId))
            {
                failReason = "granted_not_slotable";
                return false;
            }

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg == null || !cfg.IsPassive)
            {
                failReason = "not_passive";
                return false;
            }

            if (!allowDuplicateSwap)
            {
                for (int i = 0; i < PassiveSkillSlots.Length; i++)
                {
                    if (i != slotIndex && PassiveSkillSlots[i] == skillId)
                    {
                        failReason = "duplicate";
                        return false;
                    }
                }
            }
            else
            {
                for (int i = 0; i < PassiveSkillSlots.Length; i++)
                {
                    if (i != slotIndex && PassiveSkillSlots[i] == skillId)
                    {
                        PassiveSkillSlots[i] = PassiveSkillSlots[slotIndex];
                        break;
                    }
                }
            }

            PassiveSkillSlots[slotIndex] = skillId;
            return true;
        }

        public bool BelongsToSchoolPool(int schoolId, string skillId) =>
            SkillLearnCatalog.SkillDefinedInSchool(schoolId, skillId);

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

        public void ReplaceSkillIdInPassiveSlots(string oldSkillId, string newSkillId)
        {
            if (string.IsNullOrEmpty(oldSkillId))
            {
                return;
            }

            for (int i = 0; i < PassiveSkillSlots.Length; i++)
            {
                var s = PassiveSkillSlots[i];
                if (s != null && string.Equals(s, oldSkillId, StringComparison.Ordinal))
                {
                    PassiveSkillSlots[i] = newSkillId;
                }
            }
        }

        void ClearSkillIdFromPassiveSlots(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            for (int i = 0; i < PassiveSkillSlots.Length; i++)
            {
                if (string.Equals(PassiveSkillSlots[i], skillId, StringComparison.Ordinal))
                {
                    PassiveSkillSlots[i] = null;
                }
            }
        }
    }
}
