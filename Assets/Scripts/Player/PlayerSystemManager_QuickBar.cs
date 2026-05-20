using cfg.demo;
using My.Config;
using My.Saving;
using My.UI;
using UnityEngine;

namespace My.Player
{
    // 人类/未暴露真身快捷栏：武器槽 + 消耗轮盘（混合绑定：可堆叠 itemId，实例型 itemId+instanceId）
    public partial class PlayerSystemManager
    {
        public QuickSlotBinding[] WeaponQuickSlots = new QuickSlotBinding[PlayerQuickBarDefs.WeaponSlotCount];
        public QuickSlotBinding[] ConsumableQuickSlots = new QuickSlotBinding[PlayerQuickBarDefs.ConsumableSlotCount];

        public int ActiveWeaponSlotIndex = -1;
        public int ActiveConsumableIndex;

        void InitQuickBarDefaults()
        {
            for (int i = 0; i < WeaponQuickSlots.Length; i++)
            {
                WeaponQuickSlots[i] = QuickSlotBinding.Empty;
            }

            for (int i = 0; i < ConsumableQuickSlots.Length; i++)
            {
                ConsumableQuickSlots[i] = QuickSlotBinding.Empty;
            }
        }

        void HydrateQuickSlotsFromSave(SaveData savingData)
        {
            var pd = savingData?.PlayerData;
            if (pd == null)
            {
                InitQuickBarDefaults();
                return;
            }

            CopyBindingListToArray(pd.WeaponQuickSlotOverrides, WeaponQuickSlots);
            CopyBindingListToArray(pd.ConsumableQuickSlotOverrides, ConsumableQuickSlots);
            ActiveWeaponSlotIndex = pd.ActiveWeaponSlotIndex;
            ActiveConsumableIndex = ClampConsumableIndex(pd.ActiveConsumableIndex);
            PruneInvalidQuickSlots();
        }

        static void CopyBindingListToArray(System.Collections.Generic.List<QuickSlotBindingPersist> list, QuickSlotBinding[] arr)
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

        public void PruneInvalidQuickSlots()
        {
            var inv = InventorySystem;
            if (inv == null)
            {
                return;
            }

            for (int i = 0; i < WeaponQuickSlots.Length; i++)
            {
                if (!WeaponQuickSlots[i].IsEmpty && !inv.CheckQuickSlotBindingAvailable(WeaponQuickSlots[i]))
                {
                    WeaponQuickSlots[i] = QuickSlotBinding.Empty;
                    if (ActiveWeaponSlotIndex == i)
                    {
                        ActiveWeaponSlotIndex = -1;
                    }
                }
            }

            for (int i = 0; i < ConsumableQuickSlots.Length; i++)
            {
                if (!ConsumableQuickSlots[i].IsEmpty && !inv.CheckQuickSlotBindingAvailable(ConsumableQuickSlots[i]))
                {
                    ConsumableQuickSlots[i] = QuickSlotBinding.Empty;
                }
            }
        }

        public bool TryAssignWeaponQuickSlot(int slotIndex, string itemId, long itemInstanceId, out string failReason)
        {
            failReason = null;
            if (slotIndex < 0 || slotIndex >= WeaponQuickSlots.Length)
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

            if (InventorySystem != null && !InventorySystem.CheckQuickSlotBindingAvailable(binding))
            {
                failReason = "not_in_bag";
                return false;
            }

            WeaponQuickSlots[slotIndex] = binding;
            if (ActiveWeaponSlotIndex == slotIndex)
            {
                ApplyWeaponQuickBarRuntime();
            }

            return true;
        }

        public bool TryAssignConsumableQuickSlot(int slotIndex, string itemId, long itemInstanceId, out string failReason)
        {
            failReason = null;
            if (slotIndex < 0 || slotIndex >= ConsumableQuickSlots.Length)
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

            if (InventorySystem != null && !InventorySystem.CheckQuickSlotBindingAvailable(binding))
            {
                failReason = "not_in_bag";
                return false;
            }

            ConsumableQuickSlots[slotIndex] = binding;
            return true;
        }

        public void ClearWeaponQuickSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < WeaponQuickSlots.Length)
            {
                WeaponQuickSlots[slotIndex] = QuickSlotBinding.Empty;
                if (ActiveWeaponSlotIndex == slotIndex)
                {
                    ActiveWeaponSlotIndex = -1;
                }
            }

            ApplyWeaponQuickBarRuntime();
        }

        public void ClearConsumableQuickSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < ConsumableQuickSlots.Length)
            {
                ConsumableQuickSlots[slotIndex] = QuickSlotBinding.Empty;
            }
        }

        public void SwapWeaponQuickSlotIndices(int slotA, int slotB)
        {
            if (!InWeaponRange(slotA) || !InWeaponRange(slotB))
            {
                return;
            }

            (WeaponQuickSlots[slotA], WeaponQuickSlots[slotB]) = (WeaponQuickSlots[slotB], WeaponQuickSlots[slotA]);

            if (ActiveWeaponSlotIndex == slotA)
            {
                ActiveWeaponSlotIndex = slotB;
            }
            else if (ActiveWeaponSlotIndex == slotB)
            {
                ActiveWeaponSlotIndex = slotA;
            }

            if (ActiveWeaponSlotIndex >= 0)
            {
                ApplyWeaponQuickBarRuntime();
            }
        }

        public void SwapConsumableQuickSlotIndices(int slotA, int slotB)
        {
            if (!InConsumableRange(slotA) || !InConsumableRange(slotB))
            {
                return;
            }

            (ConsumableQuickSlots[slotA], ConsumableQuickSlots[slotB]) =
                (ConsumableQuickSlots[slotB], ConsumableQuickSlots[slotA]);

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

            if (WeaponQuickSlots[slotIndex].IsEmpty)
            {
                ActiveWeaponSlotIndex = -1;
                ApplyWeaponQuickBarRuntime();
                return;
            }

            if (ActiveWeaponSlotIndex == slotIndex)
            {
                ActiveWeaponSlotIndex = -1;
            }
            else
            {
                ActiveWeaponSlotIndex = slotIndex;
            }

            ApplyWeaponQuickBarRuntime();
        }

        public void ClearActiveWeaponSelection()
        {
            ActiveWeaponSlotIndex = -1;
            ApplyWeaponQuickBarRuntime();
        }

        public void ApplyWeaponQuickBarRuntime()
        {
            SyncLearnedSkillsToPlayerEntity();
            OverworldHUDPanel.Instance?.RefreshItemQuickBar();
        }

        public void CycleConsumableSelection(int delta)
        {
            if (ConsumableQuickSlots.Length == 0 || delta == 0)
            {
                return;
            }

            int start = ActiveConsumableIndex;
            for (int step = 0; step < ConsumableQuickSlots.Length; step++)
            {
                int next = ActiveConsumableIndex + (delta > 0 ? 1 : -1);
                if (next < 0)
                {
                    next = ConsumableQuickSlots.Length - 1;
                }
                else if (next >= ConsumableQuickSlots.Length)
                {
                    next = 0;
                }

                ActiveConsumableIndex = next;
                if (!ConsumableQuickSlots[ActiveConsumableIndex].IsEmpty
                    || step == ConsumableQuickSlots.Length - 1)
                {
                    break;
                }
            }

            if (ActiveConsumableIndex == start && ConsumableQuickSlots.Length > 1)
            {
                int next = ActiveConsumableIndex + (delta > 0 ? 1 : -1);
                if (next < 0)
                {
                    next = ConsumableQuickSlots.Length - 1;
                }
                else if (next >= ConsumableQuickSlots.Length)
                {
                    next = 0;
                }

                ActiveConsumableIndex = next;
            }
        }

        public string GetActiveWeaponSkillId()
        {
            if (ActiveWeaponSlotIndex < 0 || !InWeaponRange(ActiveWeaponSlotIndex))
            {
                return null;
            }

            var binding = WeaponQuickSlots[ActiveWeaponSlotIndex];
            if (binding.IsEmpty || InventorySystem == null || !InventorySystem.CheckQuickSlotBindingAvailable(binding))
            {
                return null;
            }

            return ResolveWeaponSkillIdFromItem(binding.ItemId);
        }

        static string ResolveWeaponSkillIdFromItem(string itemId)
        {
            if (WeaponQuickBarSkillBinding.TryResolveSkillId(itemId, out var skillId))
            {
                return skillId;
            }

            var use = ItemCatalog.GetPrimaryUse(itemId);
            if (use == null || !use.Usable || use.UseType != EItemUseType.UseSkill)
            {
                return null;
            }

            return string.IsNullOrEmpty(use.S1) ? null : use.S1;
        }

        public string ResolveHumanLeftClickSkillId()
        {
            if (IsUsingFaQingSkillBar())
            {
                return null;
            }

            if (logicManager != null && logicManager.IsHumanQuickBarAvailable())
            {
                var weaponSkill = GetActiveWeaponSkillId();
                if (!string.IsNullOrEmpty(weaponSkill))
                {
                    return weaponSkill;
                }
            }

            var showSkills = GetSkillSlotsByState();
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

            return ConsumableQuickSlots[ActiveConsumableIndex];
        }

        public string GetActiveConsumableItemId()
        {
            var binding = GetActiveConsumableBinding();
            return binding.IsEmpty ? null : binding.ItemId;
        }

        void WriteQuickBarToSave(PlayerData pd)
        {
            pd.WeaponQuickSlotOverrides = new System.Collections.Generic.List<QuickSlotBindingPersist>(WeaponQuickSlots.Length);
            for (int i = 0; i < WeaponQuickSlots.Length; i++)
            {
                pd.WeaponQuickSlotOverrides.Add(ToPersist(WeaponQuickSlots[i]));
            }

            pd.ConsumableQuickSlotOverrides = new System.Collections.Generic.List<QuickSlotBindingPersist>(ConsumableQuickSlots.Length);
            for (int i = 0; i < ConsumableQuickSlots.Length; i++)
            {
                pd.ConsumableQuickSlotOverrides.Add(ToPersist(ConsumableQuickSlots[i]));
            }

            pd.ActiveWeaponSlotIndex = ActiveWeaponSlotIndex;
            pd.ActiveConsumableIndex = ActiveConsumableIndex;
        }

        static QuickSlotBindingPersist ToPersist(QuickSlotBinding b)
        {
            return new QuickSlotBindingPersist
            {
                ItemId = b.ItemId ?? string.Empty,
                ItemInstanceId = b.ItemInstanceId,
            };
        }

        static bool InWeaponRange(int idx) => idx >= 0 && idx < PlayerQuickBarDefs.WeaponSlotCount;

        static bool InConsumableRange(int idx) => idx >= 0 && idx < PlayerQuickBarDefs.ConsumableSlotCount;

        int ClampConsumableIndex(int idx)
        {
            if (ConsumableQuickSlots.Length == 0)
            {
                return 0;
            }

            if (idx < 0)
            {
                return 0;
            }

            if (idx >= ConsumableQuickSlots.Length)
            {
                return ConsumableQuickSlots.Length - 1;
            }

            return idx;
        }
    }
}
