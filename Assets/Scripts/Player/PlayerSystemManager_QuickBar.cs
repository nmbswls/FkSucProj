using cfg.demo;
using My.Config;
using My.Saving;
using My.UI;
using UnityEngine;

namespace My.Player
{
    // 人类/未暴露真身快捷栏：武器槽 + 消耗轮盘
    public partial class PlayerSystemManager
    {
        public string[] WeaponQuickSlotItemSet = new string[PlayerQuickBarDefs.WeaponSlotCount];
        public string[] ConsumableQuickSlotItemSet = new string[PlayerQuickBarDefs.ConsumableSlotCount];

        public int ActiveWeaponSlotIndex = -1;
        public int ActiveConsumableIndex;

        void InitQuickBarDefaults()
        {
            WeaponQuickSlotItemSet[0] = "small_knife";
        }

        void HydrateQuickSlotsFromSave(SaveData savingData)
        {
            var pd = savingData?.PlayerData;
            if (pd == null)
            {
                return;
            }

            CopyListToArray(pd.WeaponQuickSlotOverrides, WeaponQuickSlotItemSet);
            CopyListToArray(pd.ConsumableQuickSlotOverrides, ConsumableQuickSlotItemSet);
            ActiveWeaponSlotIndex = pd.ActiveWeaponSlotIndex;
            ActiveConsumableIndex = ClampConsumableIndex(pd.ActiveConsumableIndex);
        }

        static void CopyListToArray(System.Collections.Generic.List<string> list, string[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = list != null && i < list.Count ? (list[i] ?? string.Empty) : string.Empty;
            }
        }

        public bool TryAssignWeaponQuickSlot(int slotIndex, string itemId, out string failReason)
        {
            failReason = null;
            if (slotIndex < 0 || slotIndex >= WeaponQuickSlotItemSet.Length)
            {
                failReason = "slot_range";
                return false;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                failReason = "empty_item";
                return false;
            }

            if (!ItemCatalog.IsQuickBarWeapon(itemId))
            {
                failReason = "need_weapon_item";
                return false;
            }

            WeaponQuickSlotItemSet[slotIndex] = itemId;
            if (ActiveWeaponSlotIndex == slotIndex)
            {
                ApplyWeaponQuickBarRuntime();
            }

            return true;
        }

        public bool TryAssignConsumableQuickSlot(int slotIndex, string itemId, out string failReason)
        {
            failReason = null;
            if (slotIndex < 0 || slotIndex >= ConsumableQuickSlotItemSet.Length)
            {
                failReason = "slot_range";
                return false;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                failReason = "empty_item";
                return false;
            }

            if (!ItemCatalog.IsQuickBarConsumable(itemId) || !ItemCatalog.CanUse(itemId))
            {
                failReason = "need_consumable_item";
                return false;
            }

            ConsumableQuickSlotItemSet[slotIndex] = itemId;
            return true;
        }

        public void ClearWeaponQuickSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < WeaponQuickSlotItemSet.Length)
            {
                WeaponQuickSlotItemSet[slotIndex] = string.Empty;
                if (ActiveWeaponSlotIndex == slotIndex)
                {
                    ActiveWeaponSlotIndex = -1;
                }
            }

            ApplyWeaponQuickBarRuntime();
        }

        public void ClearConsumableQuickSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < ConsumableQuickSlotItemSet.Length)
            {
                ConsumableQuickSlotItemSet[slotIndex] = string.Empty;
            }
        }

        public void SwapWeaponQuickSlotIndices(int slotA, int slotB)
        {
            if (!InWeaponRange(slotA) || !InWeaponRange(slotB))
            {
                return;
            }

            (WeaponQuickSlotItemSet[slotA], WeaponQuickSlotItemSet[slotB]) =
                (WeaponQuickSlotItemSet[slotB], WeaponQuickSlotItemSet[slotA]);

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

            (ConsumableQuickSlotItemSet[slotA], ConsumableQuickSlotItemSet[slotB]) =
                (ConsumableQuickSlotItemSet[slotB], ConsumableQuickSlotItemSet[slotA]);

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

            if (string.IsNullOrEmpty(WeaponQuickSlotItemSet[slotIndex]))
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
            if (ConsumableQuickSlotItemSet.Length == 0 || delta == 0)
            {
                return;
            }

            int start = ActiveConsumableIndex;
            for (int step = 0; step < ConsumableQuickSlotItemSet.Length; step++)
            {
                int next = ActiveConsumableIndex + (delta > 0 ? 1 : -1);
                if (next < 0)
                {
                    next = ConsumableQuickSlotItemSet.Length - 1;
                }
                else if (next >= ConsumableQuickSlotItemSet.Length)
                {
                    next = 0;
                }

                ActiveConsumableIndex = next;
                if (!string.IsNullOrEmpty(ConsumableQuickSlotItemSet[ActiveConsumableIndex])
                    || step == ConsumableQuickSlotItemSet.Length - 1)
                {
                    break;
                }
            }

            if (ActiveConsumableIndex == start && ConsumableQuickSlotItemSet.Length > 1)
            {
                // 全空时仍允许索引移动
                int next = ActiveConsumableIndex + (delta > 0 ? 1 : -1);
                if (next < 0)
                {
                    next = ConsumableQuickSlotItemSet.Length - 1;
                }
                else if (next >= ConsumableQuickSlotItemSet.Length)
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

            var itemId = WeaponQuickSlotItemSet[ActiveWeaponSlotIndex];
            if (string.IsNullOrEmpty(itemId) || !CheckHaveItem(itemId, 1))
            {
                return null;
            }

            return ResolveWeaponSkillIdFromItem(itemId);
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

        // 人类快捷栏：左键实际释放的技能（武器优先，否则 HumanSkillSlots[0]）
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

        public string GetActiveConsumableItemId()
        {
            if (!InConsumableRange(ActiveConsumableIndex))
            {
                return null;
            }

            var id = ConsumableQuickSlotItemSet[ActiveConsumableIndex];
            return string.IsNullOrEmpty(id) ? null : id;
        }

        void WriteQuickBarToSave(PlayerData pd)
        {
            pd.WeaponQuickSlotOverrides = new System.Collections.Generic.List<string>(WeaponQuickSlotItemSet.Length);
            for (int i = 0; i < WeaponQuickSlotItemSet.Length; i++)
            {
                pd.WeaponQuickSlotOverrides.Add(WeaponQuickSlotItemSet[i] ?? string.Empty);
            }

            pd.ConsumableQuickSlotOverrides = new System.Collections.Generic.List<string>(ConsumableQuickSlotItemSet.Length);
            for (int i = 0; i < ConsumableQuickSlotItemSet.Length; i++)
            {
                pd.ConsumableQuickSlotOverrides.Add(ConsumableQuickSlotItemSet[i] ?? string.Empty);
            }

            pd.ActiveWeaponSlotIndex = ActiveWeaponSlotIndex;
            pd.ActiveConsumableIndex = ActiveConsumableIndex;
        }

        static bool InWeaponRange(int idx) => idx >= 0 && idx < PlayerQuickBarDefs.WeaponSlotCount;

        static bool InConsumableRange(int idx) => idx >= 0 && idx < PlayerQuickBarDefs.ConsumableSlotCount;

        int ClampConsumableIndex(int idx)
        {
            if (ConsumableQuickSlotItemSet.Length == 0)
            {
                return 0;
            }

            if (idx < 0)
            {
                return 0;
            }

            if (idx >= ConsumableQuickSlotItemSet.Length)
            {
                return ConsumableQuickSlotItemSet.Length - 1;
            }

            return idx;
        }
    }
}
