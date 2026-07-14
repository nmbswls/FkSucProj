using cfg.demo;
using My.Map.Logic;
using My.Quest;
using My.Saving;
using System;
using System.Collections.Generic;
using My;
using My.Config;
using UnityEngine;

namespace My.Player
{
    public sealed class RuneProgressionProvider : IProgressionSource, IProgressionSkillSource
    {
        readonly PlayerRuneSystem _system;

        public RuneProgressionProvider(PlayerRuneSystem system)
        {
            _system = system;
        }

        public EProgressionModule ModuleName => EProgressionModule.Rune;
        public event Action<IProgressionSource> OnStatsChanged;

        public void EvaluateStats(StatMap targetMap)
        {
            _system?.AccumulateProgressionStats(targetMap);
        }

        public void CollectContributedSkills(HashSet<string> applied, List<(string skillId, int level)> output)
        {
            if (output == null || _system == null)
            {
                return;
            }

            var ids = new List<string>();
            _system.CollectEquippedPassiveSkillIds(applied, ids);
            for (int i = 0; i < ids.Count; i++)
            {
                var skillId = ids[i];
                if (string.IsNullOrEmpty(skillId))
                {
                    continue;
                }

                output.Add((skillId, 1));
            }
        }

        public void NotifyChanged()
        {
            OnStatsChanged?.Invoke(this);
        }
    }

    public class PlayerRuneSystem : IPlayerSystem
    {
        const string LogTag = "[PlayerRuneSystem]";

        readonly PlayerSystemManager _owner;
        GameLogicManager _logic;
        readonly HashSet<string> _owned = new(StringComparer.Ordinal);
        readonly HashSet<string> _unlockedUpgrades = new(StringComparer.Ordinal);
        readonly Dictionary<ERuneEquipSlot, string> _equipped = new();
        readonly RuneProgressionProvider _progressionProvider;

        public RuneProgressionProvider ProgressionProvider => _progressionProvider;

        public PlayerRuneSystem(PlayerSystemManager owner)
        {
            _owner = owner;
            _progressionProvider = new RuneProgressionProvider(this);
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _logic = ctx;
            _owned.Clear();
            _unlockedUpgrades.Clear();
            _equipped.Clear();

            var pd = savingData?.PlayerData;
            if (pd?.OwnedRuneIds != null)
            {
                foreach (var id in pd.OwnedRuneIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _owned.Add(id);
                    }
                }
            }

            if (pd?.UnlockedRuneUpgradeIds != null)
            {
                foreach (var id in pd.UnlockedRuneUpgradeIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _unlockedUpgrades.Add(id);
                    }
                }
            }

            if (pd?.EquippedRunes != null)
            {
                foreach (var entry in pd.EquippedRunes)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.RuneId))
                    {
                        continue;
                    }

                    if (Enum.IsDefined(typeof(ERuneEquipSlot), entry.Slot))
                    {
                        _equipped[(ERuneEquipSlot)entry.Slot] = entry.RuneId;
                    }
                }
            }

            EnsureAllInitialUpgradesUnlocked();
            ApplyAllUnlockedUpgradeEffects();
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void Tick(float dt)
        {
        }

        public void WriteToSave(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.OwnedRuneIds ??= new List<string>();
            pd.OwnedRuneIds.Clear();
            pd.OwnedRuneIds.AddRange(_owned);

            pd.UnlockedRuneUpgradeIds ??= new List<string>();
            pd.UnlockedRuneUpgradeIds.Clear();
            pd.UnlockedRuneUpgradeIds.AddRange(_unlockedUpgrades);

            pd.EquippedRunes ??= new List<RuneEquipPersist>();
            pd.EquippedRunes.Clear();
            foreach (var kv in _equipped)
            {
                pd.EquippedRunes.Add(new RuneEquipPersist
                {
                    Slot = (int)kv.Key,
                    RuneId = kv.Value,
                });
            }
        }

        public bool OwnsRune(string runeId)
        {
            return !string.IsNullOrEmpty(runeId) && _owned.Contains(runeId);
        }

        public bool HasUpgrade(string upgradeId)
        {
            return !string.IsNullOrEmpty(upgradeId) && _unlockedUpgrades.Contains(upgradeId);
        }

        public bool TryGrantRune(string runeId)
        {
            var def = RuneCatalog.GetOrDefault(runeId);
            if (def == null)
            {
                Debug.LogWarning($"{LogTag} Grant rune failed: invalid rune '{runeId}'.");
                return false;
            }

            if (_owned.Contains(runeId))
            {
                Debug.LogWarning($"{LogTag} Grant rune failed: already owned '{runeId}'.");
                return false;
            }

            _owned.Add(runeId);
            if (EnsureInitialUpgradeUnlocked(runeId))
            {
                ApplyUpgradeEffects(RuneUpgradeCatalog.GetOrDefault(def.InitialUpgradeId));
            }

            _progressionProvider.NotifyChanged();

            PlayerEventBus.Publish(new PlayerRuneGrantedEvent { RuneId = runeId });
            return true;
        }

        public bool CanUnlockUpgrade(string upgradeId)
        {
            return ValidateUnlockUpgrade(upgradeId, logOnFail: false);
        }

        public bool TryUnlockUpgrade(string upgradeId)
        {
            if (!ValidateUnlockUpgrade(upgradeId, logOnFail: true))
            {
                return false;
            }

            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
            _unlockedUpgrades.Add(upgradeId);
            ApplyUpgradeEffects(def);
            _progressionProvider.NotifyChanged();

            PlayerEventBus.Publish(new PlayerRuneUpgradeUnlockedEvent
            {
                UpgradeId = upgradeId,
                BaseRuneId = def.BaseRuneId,
            });
            return true;
        }

        public ERuneUpgradeNodeState GetUpgradeNodeState(string upgradeId)
        {
            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
            if (def == null)
            {
                return ERuneUpgradeNodeState.Locked;
            }

            if (_unlockedUpgrades.Contains(upgradeId)
                || (RuneUpgradeCatalog.IsInitialUpgrade(def) && _owned.Contains(def.BaseRuneId)))
            {
                return ERuneUpgradeNodeState.Unlocked;
            }

            if (ValidateUnlockUpgrade(upgradeId, logOnFail: false))
            {
                return ERuneUpgradeNodeState.Available;
            }

            return ERuneUpgradeNodeState.Locked;
        }

        public bool TryEquip(ERuneEquipSlot slot, string runeId)
        {
            if (slot == ERuneEquipSlot.None)
            {
                Debug.LogWarning($"{LogTag} Equip rune failed: invalid slot.");
                return false;
            }

            var def = RuneCatalog.GetOrDefault(runeId);
            if (def == null || def.RuneType != ERuneType.Equippable)
            {
                Debug.LogWarning($"{LogTag} Equip rune failed: invalid equippable rune '{runeId}'.");
                return false;
            }

            if (!_owned.Contains(runeId))
            {
                Debug.LogWarning($"{LogTag} Equip rune failed: not owned '{runeId}'.");
                return false;
            }

            if (def.EquipSlot != slot)
            {
                Debug.LogWarning($"{LogTag} Equip rune failed: slot mismatch rune='{runeId}' slot={slot}.");
                return false;
            }

            _equipped[slot] = runeId;
            return true;
        }

        public bool TryUnequip(ERuneEquipSlot slot)
        {
            if (slot == ERuneEquipSlot.None)
            {
                Debug.LogWarning($"{LogTag} Unequip rune failed: invalid slot.");
                return false;
            }

            if (!_equipped.Remove(slot))
            {
                Debug.LogWarning($"{LogTag} Unequip rune failed: empty slot {slot}.");
                return false;
            }

            return true;
        }

        public string GetEquipped(ERuneEquipSlot slot)
        {
            return _equipped.TryGetValue(slot, out var id) ? id : null;
        }

        public bool IsEquipSlotOpen(ERuneEquipSlot slot)
        {
            if (slot == ERuneEquipSlot.None)
            {
                return false;
            }

            var def = RuneCatalog.GetEquipSlotDef(slot);
            if (def == null)
            {
                return true;
            }

            if (_logic == null)
            {
                return false;
            }

            return _logic.CheckCommonCondsAll(def.UnlockConds);
        }
        public IEnumerable<RuneData> GetOwnedByType(ERuneType runeType)
        {
            foreach (var id in _owned)
            {
                var def = RuneCatalog.GetOrDefault(id);
                if (def != null && def.RuneType == runeType)
                {
                    yield return def;
                }
            }
        }

        public void EnsureAllInitialUpgradesUnlocked()
        {
            foreach (var id in _owned)
            {
                EnsureInitialUpgradeUnlocked(id);
            }
        }

        public void ApplyAllUnlockedUpgradeEffects()
        {
            foreach (var upgradeId in _unlockedUpgrades)
            {
                var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
                if (def != null)
                {
                    ApplyUpgradeEffects(def);
                }
            }
        }

        // 已装配 + 已拥有的永久符文中，当前应生效的被动技能
        public void CollectPassiveSkillIds(HashSet<string> applied, List<string> output)
        {
            CollectPassiveFromEquippedRunes(applied, output);
            CollectPassiveFromOwnedPermanentRunes(applied, output);
        }

        public void CollectEquippedPassiveSkillIds(HashSet<string> applied, List<string> output)
        {
            CollectPassiveSkillIds(applied, output);
        }

        public void AccumulateProgressionStats(StatMap targetMap)
        {
            if (targetMap == null)
            {
                return;
            }

            foreach (string upgradeId in _unlockedUpgrades)
            {
                var upgrade = RuneUpgradeCatalog.GetOrDefault(upgradeId);
                if (upgrade?.StatBonuses == null)
                {
                    continue;
                }

                for (int i = 0; i < upgrade.StatBonuses.Count; i++)
                {
                    var bonus = upgrade.StatBonuses[i];
                    if (bonus == null || bonus.AttrId == 0 || bonus.Val == 0)
                    {
                        continue;
                    }

                    targetMap.Add(bonus.AttrId, bonus.Val);
                }
            }
        }

        bool ValidateUnlockUpgrade(string upgradeId, bool logOnFail)
        {
            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
            if (def == null)
            {
                if (logOnFail)
                {
                    Debug.LogWarning($"{LogTag} Unlock upgrade failed: invalid upgrade '{upgradeId}'.");
                }

                return false;
            }

            if (_unlockedUpgrades.Contains(upgradeId))
            {
                if (logOnFail)
                {
                    Debug.LogWarning($"{LogTag} Unlock upgrade failed: already unlocked '{upgradeId}'.");
                }

                return false;
            }

            if (!_owned.Contains(def.BaseRuneId))
            {
                if (logOnFail)
                {
                    Debug.LogWarning($"{LogTag} Unlock upgrade failed: base rune not owned '{def.BaseRuneId}' for '{upgradeId}'.");
                }

                return false;
            }

            if (!RuneUpgradeCatalog.ArePrerequisitesMet(def, HasUpgrade))
            {
                if (logOnFail)
                {
                    Debug.LogWarning($"{LogTag} Unlock upgrade failed: prerequisite missing for '{upgradeId}'.");
                }

                return false;
            }

            return true;
        }

        void CollectPassiveFromEquippedRunes(HashSet<string> applied, List<string> output)
        {
            foreach (var slot in RuneCatalog.EquipSlots)
            {
                if (!_equipped.TryGetValue(slot, out var runeId) || string.IsNullOrEmpty(runeId))
                {
                    continue;
                }

                CollectPassiveForRune(runeId, applied, output);
            }
        }

        void CollectPassiveFromOwnedPermanentRunes(HashSet<string> applied, List<string> output)
        {
            foreach (var runeId in _owned)
            {
                var def = RuneCatalog.GetOrDefault(runeId);
                if (def == null || def.RuneType != ERuneType.Permanent)
                {
                    continue;
                }

                CollectPassiveForRune(runeId, applied, output);
            }
        }

        void CollectPassiveForRune(string runeId, HashSet<string> applied, List<string> output)
        {
            foreach (var upgrade in RuneUpgradeCatalog.GetUpgradesForRune(runeId))
            {
                if (!_unlockedUpgrades.Contains(upgrade.UpgradeId))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(upgrade.PassiveSkillId))
                {
                    TryAppendPassive(upgrade.PassiveSkillId, applied, output);
                }
            }
        }

        static void TryAppendPassive(string skillId, HashSet<string> applied, List<string> output)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            if (applied != null && applied.Contains(skillId))
            {
                return;
            }

            output.Add(skillId);
        }

        bool EnsureInitialUpgradeUnlocked(string runeId)
        {
            var runeDef = RuneCatalog.GetOrDefault(runeId);
            if (runeDef == null || string.IsNullOrEmpty(runeDef.InitialUpgradeId))
            {
                return false;
            }

            if (_unlockedUpgrades.Contains(runeDef.InitialUpgradeId))
            {
                return false;
            }

            if (RuneUpgradeCatalog.GetOrDefault(runeDef.InitialUpgradeId) == null)
            {
                return false;
            }

            _unlockedUpgrades.Add(runeDef.InitialUpgradeId);
            return true;
        }

        void ApplyUpgradeEffects(RuneUpgradeInfo def)
        {
            if (def == null || _owner == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(def.FuncUnlockKey))
            {
                _owner.SetVariable(def.FuncUnlockKey);
            }

            if (def.FuncOpenType != EFuncOpenType.Invalid)
            {
                _owner.FuncOpenSystem?.TryOpenFunc(def.FuncOpenType);
            }
        }
    }
}
