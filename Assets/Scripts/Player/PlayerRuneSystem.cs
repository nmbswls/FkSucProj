using cfg.demo;
using My.Map.Logic;
using My.Quest;
using My.Saving;
using System;
using System.Collections.Generic;
using My;
using My.Config;

namespace My.Player
{
    public class PlayerRuneSystem : IPlayerSystem
    {
        readonly PlayerSystemManager _owner;
        readonly HashSet<string> _owned = new(StringComparer.Ordinal);
        readonly HashSet<string> _unlockedUpgrades = new(StringComparer.Ordinal);
        readonly Dictionary<ERuneEquipSlot, string> _equipped = new();

        public PlayerRuneSystem(PlayerSystemManager owner)
        {
            _owner = owner;
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
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

            ApplyAllPermanentEffects();
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

        public bool TryGrantRune(string runeId, out string failReason)
        {
            failReason = null;
            var def = RuneCatalog.GetOrDefault(runeId);
            if (def == null)
            {
                failReason = "invalid_rune";
                return false;
            }

            if (_owned.Contains(runeId))
            {
                failReason = "already_owned";
                return false;
            }

            _owned.Add(runeId);
            if (def.RuneType == ERuneType.Permanent)
            {
                ApplyPermanentEffects(def);
            }

            PlayerEventBus.Publish(new PlayerRuneGrantedEvent { RuneId = runeId });
            return true;
        }

        public bool CanUnlockUpgrade(string upgradeId, out string failReason)
        {
            failReason = null;
            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
            if (def == null)
            {
                failReason = "invalid_upgrade";
                return false;
            }

            if (_unlockedUpgrades.Contains(upgradeId))
            {
                failReason = "already_unlocked";
                return false;
            }

            if (!_owned.Contains(def.BaseRuneId))
            {
                failReason = "base_rune_not_owned";
                return false;
            }

            if (!RuneUpgradeCatalog.ArePrerequisitesMet(def, HasUpgrade))
            {
                failReason = "prerequisite_missing";
                return false;
            }

            return true;
        }

        public bool TryUnlockUpgrade(string upgradeId, out string failReason)
        {
            if (!CanUnlockUpgrade(upgradeId, out failReason))
            {
                return false;
            }

            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
            _unlockedUpgrades.Add(upgradeId);
            ApplyUpgradeEffects(def);

            PlayerEventBus.Publish(new PlayerRuneUpgradeUnlockedEvent
            {
                UpgradeId = upgradeId,
                BaseRuneId = def.BaseRuneId,
            });
            return true;
        }

        public ERuneUpgradeNodeState GetUpgradeNodeState(string upgradeId, out string lockReason)
        {
            lockReason = null;
            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
            if (def == null)
            {
                lockReason = "invalid_upgrade";
                return ERuneUpgradeNodeState.Locked;
            }

            if (_unlockedUpgrades.Contains(upgradeId))
            {
                return ERuneUpgradeNodeState.Unlocked;
            }

            if (CanUnlockUpgrade(upgradeId, out lockReason))
            {
                return ERuneUpgradeNodeState.Available;
            }

            return ERuneUpgradeNodeState.Locked;
        }

        public RuneUpgradeTreeView BuildUpgradeTreeView(string baseRuneId)
        {
            var view = new RuneUpgradeTreeView
            {
                BaseRuneId = baseRuneId,
                BaseRune = RuneCatalog.GetOrDefault(baseRuneId),
            };

            if (string.IsNullOrEmpty(baseRuneId))
            {
                return view;
            }

            foreach (var branchId in RuneUpgradeCatalog.GetBranchIdsForRune(baseRuneId))
            {
                var branch = new RuneUpgradeBranchView { BranchId = branchId };
                foreach (var def in RuneUpgradeCatalog.GetUpgradesInBranch(baseRuneId, branchId))
                {
                    branch.Nodes.Add(BuildNodeView(def));
                }

                view.Branches.Add(branch);
            }

            foreach (var def in RuneUpgradeCatalog.GetUpgradesInBranch(baseRuneId, null))
            {
                view.RootUpgrades.Add(BuildNodeView(def));
            }

            return view;
        }

        RuneUpgradeNodeView BuildNodeView(RuneUpgradeInfo def)
        {
            var state = GetUpgradeNodeState(def.UpgradeId, out var lockReason);
            return new RuneUpgradeNodeView
            {
                Def = def,
                State = state,
                LockReason = lockReason,
            };
        }

        public bool TryEquip(ERuneEquipSlot slot, string runeId, out string failReason)
        {
            failReason = null;
            if (slot == ERuneEquipSlot.None)
            {
                failReason = "invalid_slot";
                return false;
            }

            var def = RuneCatalog.GetOrDefault(runeId);
            if (def == null || def.RuneType != ERuneType.Equippable)
            {
                failReason = "invalid_rune";
                return false;
            }

            if (!_owned.Contains(runeId))
            {
                failReason = "not_owned";
                return false;
            }

            if (def.EquipSlot != slot)
            {
                failReason = "slot_mismatch";
                return false;
            }

            _equipped[slot] = runeId;
            return true;
        }

        public bool TryUnequip(ERuneEquipSlot slot, out string failReason)
        {
            failReason = null;
            if (slot == ERuneEquipSlot.None)
            {
                failReason = "invalid_slot";
                return false;
            }

            if (!_equipped.Remove(slot))
            {
                failReason = "empty_slot";
                return false;
            }

            return true;
        }

        public string GetEquipped(ERuneEquipSlot slot)
        {
            return _equipped.TryGetValue(slot, out var id) ? id : null;
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

        public void ApplyAllPermanentEffects()
        {
            foreach (var id in _owned)
            {
                var def = RuneCatalog.GetOrDefault(id);
                if (def != null && def.RuneType == ERuneType.Permanent)
                {
                    ApplyPermanentEffects(def);
                }
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
            var def = RuneCatalog.GetOrDefault(runeId);
            if (def != null && !string.IsNullOrEmpty(def.PassiveSkillId))
            {
                TryAppendPassive(def.PassiveSkillId, applied, output);
            }

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

        void ApplyPermanentEffects(RuneData def)
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
