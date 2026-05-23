using System;
using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Map.Logic;
using My.Saving;

namespace My.Player
{
    public class PlayerRuneSystem : IPlayerSystem
    {
        readonly PlayerSystemManager _owner;
        readonly HashSet<string> _owned = new(StringComparer.Ordinal);
        readonly Dictionary<ERuneEquipSlot, string> _equipped = new();

        public PlayerRuneSystem(PlayerSystemManager owner)
        {
            _owner = owner;
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _owned.Clear();
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

            return true;
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

        public void CollectEquippedPassiveSkillIds(HashSet<string> applied, List<string> output)
        {
            foreach (var slot in RuneCatalog.EquipSlots)
            {
                if (!_equipped.TryGetValue(slot, out var runeId) || string.IsNullOrEmpty(runeId))
                {
                    continue;
                }

                var def = RuneCatalog.GetOrDefault(runeId);
                if (def == null || string.IsNullOrEmpty(def.PassiveSkillId))
                {
                    continue;
                }

                if (applied != null && applied.Contains(def.PassiveSkillId))
                {
                    continue;
                }

                output.Add(def.PassiveSkillId);
            }
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

            if (def.FuncOpenType > 0 && Enum.IsDefined(typeof(EFuncOpenType), def.FuncOpenType))
            {
                _owner.FuncOpenSystem.FuncOpenSet.Add((EFuncOpenType)def.FuncOpenType);
            }
        }
    }
}
