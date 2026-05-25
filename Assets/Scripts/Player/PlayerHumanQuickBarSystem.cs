using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Saving;
using My.UI;
using UnityEngine;

namespace My.Player
{
    // 人类/未暴露真身：武器快捷槽 + 消耗品轮盘（绑定背包 itemId 或 itemId+instanceId）
    public sealed class PlayerHumanQuickBarSystem : IPlayerSystem
    {
        readonly PlayerSystemManager _player;

        public QuickSlotBinding[] WeaponSlots { get; } = new QuickSlotBinding[HumanQuickBarDefs.WeaponSlotCount];
        public QuickSlotBinding[] ConsumableSlots { get; } = new QuickSlotBinding[HumanQuickBarDefs.ConsumableSlotCount];

        public int ActiveWeaponIndex { get; private set; } = -1;
        public int ActiveConsumableIndex { get; private set; }

        public PlayerHumanQuickBarSystem(PlayerSystemManager player)
        {
            _player = player;
            ClearAllSlots();
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            var pd = savingData?.PlayerData;
            if (pd == null)
            {
                ClearAllSlots();
                ActiveWeaponIndex = -1;
                ActiveConsumableIndex = 0;
                return;
            }

            CopyBindingListToArray(pd.WeaponQuickSlotOverrides, WeaponSlots);
            CopyBindingListToArray(pd.ConsumableQuickSlotOverrides, ConsumableSlots);
            ActiveWeaponIndex = pd.ActiveWeaponSlotIndex;
            ActiveConsumableIndex = ClampConsumableIndex(pd.ActiveConsumableIndex);
            PruneInvalidSlots();
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void WriteToSave(PlayerData pd)
        {
            pd.WeaponQuickSlotOverrides = new List<QuickSlotBindingPersist>(WeaponSlots.Length);
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                pd.WeaponQuickSlotOverrides.Add(ToPersist(WeaponSlots[i]));
            }

            pd.ConsumableQuickSlotOverrides = new List<QuickSlotBindingPersist>(ConsumableSlots.Length);
            for (int i = 0; i < ConsumableSlots.Length; i++)
            {
                pd.ConsumableQuickSlotOverrides.Add(ToPersist(ConsumableSlots[i]));
            }

            pd.ActiveWeaponSlotIndex = ActiveWeaponIndex;
            pd.ActiveConsumableIndex = ActiveConsumableIndex;
        }

        public void Tick(float dt)
        {
        }

        public void PruneInvalidSlots()
        {
            var inv = _player.InventorySystem;
            if (inv == null)
            {
                return;
            }

            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                if (!WeaponSlots[i].IsEmpty && !inv.CheckQuickSlotBindingAvailable(WeaponSlots[i]))
                {
                    WeaponSlots[i] = QuickSlotBinding.Empty;
                    if (ActiveWeaponIndex == i)
                    {
                        ActiveWeaponIndex = -1;
                    }
                }
            }

            for (int i = 0; i < ConsumableSlots.Length; i++)
            {
                if (!ConsumableSlots[i].IsEmpty && !inv.CheckQuickSlotBindingAvailable(ConsumableSlots[i]))
                {
                    ConsumableSlots[i] = QuickSlotBinding.Empty;
                }
            }
        }

        public bool TryAssignWeaponSlot(int slotIndex, string itemId, long itemInstanceId, out string failReason)
        {
            failReason = null;
            if (!InWeaponRange(slotIndex))
            {
                failReason = "slot_range";
                return false;
            }

            if (!QuickSlotAssignRules.TryNormalizeAssign(itemId, itemInstanceId, out var binding, out failReason))
            {
                return false;
            }

            if (!ItemCatalog.IsQuickBarWeapon(itemId))
            {
                failReason = "need_weapon_item";
                return false;
            }

            if (_player.InventorySystem != null && !_player.InventorySystem.CheckQuickSlotBindingAvailable(binding))
            {
                failReason = "not_in_bag";
                return false;
            }

            WeaponSlots[slotIndex] = binding;
            if (ActiveWeaponIndex == slotIndex)
            {
                ApplyWeaponToRuntime();
            }

            return true;
        }

        public bool TryAssignConsumableSlot(int slotIndex, string itemId, long itemInstanceId, out string failReason)
        {
            failReason = null;
            if (!InConsumableRange(slotIndex))
            {
                failReason = "slot_range";
                return false;
            }

            if (!QuickSlotAssignRules.TryNormalizeAssign(itemId, itemInstanceId, out var binding, out failReason))
            {
                return false;
            }

            if (!ItemCatalog.IsQuickBarConsumable(itemId) || !ItemCatalog.CanUse(itemId))
            {
                failReason = "need_consumable_item";
                return false;
            }

            if (_player.InventorySystem != null && !_player.InventorySystem.CheckQuickSlotBindingAvailable(binding))
            {
                failReason = "not_in_bag";
                return false;
            }

            ConsumableSlots[slotIndex] = binding;
            return true;
        }

        public void ClearWeaponSlot(int slotIndex)
        {
            if (!InWeaponRange(slotIndex))
            {
                return;
            }

            WeaponSlots[slotIndex] = QuickSlotBinding.Empty;
            if (ActiveWeaponIndex == slotIndex)
            {
                ActiveWeaponIndex = -1;
            }

            ApplyWeaponToRuntime();
        }

        public void ClearConsumableSlot(int slotIndex)
        {
            if (InConsumableRange(slotIndex))
            {
                ConsumableSlots[slotIndex] = QuickSlotBinding.Empty;
            }
        }

        public void SwapWeaponSlotIndices(int slotA, int slotB)
        {
            if (!InWeaponRange(slotA) || !InWeaponRange(slotB))
            {
                return;
            }

            (WeaponSlots[slotA], WeaponSlots[slotB]) = (WeaponSlots[slotB], WeaponSlots[slotA]);

            if (ActiveWeaponIndex == slotA)
            {
                ActiveWeaponIndex = slotB;
            }
            else if (ActiveWeaponIndex == slotB)
            {
                ActiveWeaponIndex = slotA;
            }

            if (ActiveWeaponIndex >= 0)
            {
                ApplyWeaponToRuntime();
            }
        }

        public void SwapConsumableSlotIndices(int slotA, int slotB)
        {
            if (!InConsumableRange(slotA) || !InConsumableRange(slotB))
            {
                return;
            }

            (ConsumableSlots[slotA], ConsumableSlots[slotB]) = (ConsumableSlots[slotB], ConsumableSlots[slotA]);

            if (ActiveConsumableIndex == slotA)
            {
                ActiveConsumableIndex = slotB;
            }
            else if (ActiveConsumableIndex == slotB)
            {
                ActiveConsumableIndex = slotA;
            }
        }

        public void SelectWeaponSlot(int slotIndex)
        {
            if (!InWeaponRange(slotIndex))
            {
                return;
            }

            if (WeaponSlots[slotIndex].IsEmpty)
            {
                ActiveWeaponIndex = -1;
                ApplyWeaponToRuntime();
                return;
            }

            ActiveWeaponIndex = ActiveWeaponIndex == slotIndex ? -1 : slotIndex;
            ApplyWeaponToRuntime();
        }

        public void ClearActiveWeapon()
        {
            ActiveWeaponIndex = -1;
            ApplyWeaponToRuntime();
        }

        public void SelectConsumableSlot(int slotIndex)
        {
            if (InConsumableRange(slotIndex))
            {
                ActiveConsumableIndex = slotIndex;
            }
        }

        public void ApplyWeaponToRuntime()
        {
            var itemId = _player.logicManager != null && _player.logicManager.IsHumanQuickBarAvailable()
                ? GetActiveWeaponItemId()
                : null;

            var weaponView = MainGameManager.Instance?.playerScenePresenter?.HumanWeaponView;
            if (weaponView != null)
            {
                if (string.IsNullOrEmpty(itemId))
                {
                    weaponView.Unequip();
                }
                else
                {
                    weaponView.Equip(itemId);
                }
            }

            _player.SyncLearnedSkillsToPlayerEntity();
            PlayerHumanItemBarPanel.RefreshFromGame();
        }

        public string GetActiveWeaponItemId()
        {
            if (ActiveWeaponIndex < 0 || !InWeaponRange(ActiveWeaponIndex))
            {
                return null;
            }

            var binding = WeaponSlots[ActiveWeaponIndex];
            if (binding.IsEmpty
                || _player.InventorySystem == null
                || !_player.InventorySystem.CheckQuickSlotBindingAvailable(binding))
            {
                return null;
            }

            return binding.ItemId;
        }

        public Dictionary<string, string> BuildCastParamsForActiveWeapon()
        {
            return HumanWeaponCatalog.BuildCastParams(GetActiveWeaponItemId());
        }

        public void CycleConsumableSelection(int delta)
        {
            if (ConsumableSlots.Length == 0 || delta == 0)
            {
                return;
            }

            int start = ActiveConsumableIndex;
            for (int step = 0; step < ConsumableSlots.Length; step++)
            {
                int next = ActiveConsumableIndex + (delta > 0 ? 1 : -1);
                if (next < 0)
                {
                    next = ConsumableSlots.Length - 1;
                }
                else if (next >= ConsumableSlots.Length)
                {
                    next = 0;
                }

                ActiveConsumableIndex = next;
                if (!ConsumableSlots[ActiveConsumableIndex].IsEmpty
                    || step == ConsumableSlots.Length - 1)
                {
                    break;
                }
            }

            if (ActiveConsumableIndex == start && ConsumableSlots.Length > 1)
            {
                int next = ActiveConsumableIndex + (delta > 0 ? 1 : -1);
                if (next < 0)
                {
                    next = ConsumableSlots.Length - 1;
                }
                else if (next >= ConsumableSlots.Length)
                {
                    next = 0;
                }

                ActiveConsumableIndex = next;
            }
        }

        public string GetActiveWeaponSkillId()
        {
            if (ActiveWeaponIndex < 0 || !InWeaponRange(ActiveWeaponIndex))
            {
                return null;
            }

            var binding = WeaponSlots[ActiveWeaponIndex];
            if (binding.IsEmpty
                || _player.InventorySystem == null
                || !_player.InventorySystem.CheckQuickSlotBindingAvailable(binding))
            {
                return null;
            }

            return ResolveWeaponSkillId(binding.ItemId);
        }

        public string ResolveLeftClickSkillId()
        {
            if (_player.IsUsingFaQingSkillBar())
            {
                return null;
            }

            if (_player.logicManager != null && _player.logicManager.IsHumanQuickBarAvailable())
            {
                var weaponSkill = GetActiveWeaponSkillId();
                if (!string.IsNullOrEmpty(weaponSkill))
                {
                    return weaponSkill;
                }
            }

            var showSkills = _player.GetSkillSlotsByState();
            if (showSkills == null || showSkills.Length == 0)
            {
                return null;
            }

            return showSkills[0];
        }

        public QuickSlotBinding GetActiveConsumableBinding()
        {
            if (!InConsumableRange(ActiveConsumableIndex))
            {
                return QuickSlotBinding.Empty;
            }

            return ConsumableSlots[ActiveConsumableIndex];
        }

        public string GetActiveConsumableItemId()
        {
            var binding = GetActiveConsumableBinding();
            return binding.IsEmpty ? null : binding.ItemId;
        }

        static string ResolveWeaponSkillId(string itemId)
        {
            return HumanWeaponCatalog.GetSkillId(itemId);
        }

        void ClearAllSlots()
        {
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                WeaponSlots[i] = QuickSlotBinding.Empty;
            }

            for (int i = 0; i < ConsumableSlots.Length; i++)
            {
                ConsumableSlots[i] = QuickSlotBinding.Empty;
            }
        }

        static void CopyBindingListToArray(List<QuickSlotBindingPersist> list, QuickSlotBinding[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (list != null && i < list.Count && !string.IsNullOrEmpty(list[i]?.ItemId))
                {
                    arr[i] = QuickSlotBinding.Pinned(list[i].ItemId, list[i].ItemInstanceId);
                }
                else
                {
                    arr[i] = QuickSlotBinding.Empty;
                }
            }
        }

        static QuickSlotBindingPersist ToPersist(QuickSlotBinding b)
        {
            return new QuickSlotBindingPersist
            {
                ItemId = b.ItemId ?? string.Empty,
                ItemInstanceId = b.ItemInstanceId,
            };
        }

        static bool InWeaponRange(int idx) => idx >= 0 && idx < HumanQuickBarDefs.WeaponSlotCount;

        static bool InConsumableRange(int idx) => idx >= 0 && idx < HumanQuickBarDefs.ConsumableSlotCount;

        int ClampConsumableIndex(int idx)
        {
            if (ConsumableSlots.Length == 0)
            {
                return 0;
            }

            if (idx < 0)
            {
                return 0;
            }

            if (idx >= ConsumableSlots.Length)
            {
                return ConsumableSlots.Length - 1;
            }

            return idx;
        }
    }
}
