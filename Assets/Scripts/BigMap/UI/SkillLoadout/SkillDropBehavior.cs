using System;
using My.Config;
using My.Map.Entity;
using My.Player;
using UnityEngine;

namespace My.UI.SkillLoadout
{
    public interface ISkillDropBehavior
    {
        void OnBeginDragFromPool(string skillId);

        void OnDragFromPool(Vector2 screenPos);

        bool TryDropOnSlot(SkillLoadoutPanel panel, PlayerSkillSystem sys, SkillLoadoutSlotKind slotKind,
            int slotIndex, string skillId, out string failReason);

        // 拖拽结束时没有落到任何 DropZone，由 behavior 决定是否执行"空投"操作
        void OnDropToEmpty(SkillLoadoutPanel panel, PlayerSkillSystem sys);
    }

    public sealed class SchoolFilteredNormalSlotDropBehavior : ISkillDropBehavior
    {
        public void OnBeginDragFromPool(string skillId) =>
            SkillDragSession.Begin(skillId, this);

        public void OnDragFromPool(Vector2 screenPos) =>
            SkillDragSession.FollowScreenPoint(screenPos);

        public bool TryDropOnSlot(SkillLoadoutPanel panel, PlayerSkillSystem sys, SkillLoadoutSlotKind slotKind,
            int slotIndex, string skillId, out string failReason)
        {
            if (panel.ActiveSchoolId <= 0 ||
                !sys.BelongsToSchoolPool(panel.ActiveSchoolId, skillId))
            {
                failReason = "not_in_school_pool";
                return false;
            }

            var entry = SkillLearnCatalog.TryFindLearnEntryBySkillId(skillId);
            var slotType = SkillLoadoutSlotTypeUtil.ResolveSlotType(entry, skillId);
            if (!SkillLoadoutSlotTypeUtil.CanAssignToSlot(slotType, slotKind, slotIndex, out failReason))
            {
                return false;
            }

            if (slotKind == SkillLoadoutSlotKind.Active)
            {
                return sys.TryAssignNormalSlot(slotIndex, skillId, allowDuplicateSwap: true, out failReason);
            }

            return sys.TryAssignPassiveSlot(slotIndex, skillId, allowDuplicateSwap: true, out failReason);
        }

        // 从技能池拖到空白处：不做任何操作
        public void OnDropToEmpty(SkillLoadoutPanel panel, PlayerSkillSystem sys) { }
    }

    // 从已装备槽拖出：落到其他槽位时交换/移动，落到空白处时卸下
    public sealed class SlotUnequipDragBehavior : ISkillDropBehavior
    {
        readonly SkillLoadoutSlotKind _slotKind;
        readonly int _slotIndex;

        public SlotUnequipDragBehavior(SkillLoadoutSlotKind slotKind, int slotIndex)
        {
            _slotKind = slotKind;
            _slotIndex = slotIndex;
        }

        public void OnBeginDragFromPool(string skillId) =>
            SkillDragSession.Begin(skillId, this);

        public void OnDragFromPool(Vector2 screenPos) =>
            SkillDragSession.FollowScreenPoint(screenPos);

        public bool TryDropOnSlot(SkillLoadoutPanel panel, PlayerSkillSystem sys, SkillLoadoutSlotKind targetKind,
            int targetIndex, string skillId, out string failReason)
        {
            failReason = null;

            if (_slotKind == targetKind && _slotIndex == targetIndex)
            {
                failReason = "same_slot";
                return false;
            }

            if (_slotKind != targetKind)
            {
                failReason = "wrong_skill_kind";
                return false;
            }

            string sourceSkill = GetSlotSkill(sys, _slotKind, _slotIndex);
            if (string.IsNullOrEmpty(skillId)
                || !string.Equals(sourceSkill, skillId, StringComparison.Ordinal))
            {
                failReason = "stale_drag";
                return false;
            }

            string targetSkill = GetSlotSkill(sys, targetKind, targetIndex);

            if (!CanPlaceSkillInSlot(sys, skillId, targetKind, targetIndex, out failReason))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(targetSkill)
                && !CanPlaceSkillInSlot(sys, targetSkill, _slotKind, _slotIndex, out failReason))
            {
                return false;
            }

            SetSlotSkill(sys, _slotKind, _slotIndex, targetSkill);
            SetSlotSkill(sys, targetKind, targetIndex, skillId);
            return true;
        }

        public void OnDropToEmpty(SkillLoadoutPanel panel, PlayerSkillSystem sys)
        {
            bool cleared = _slotKind == SkillLoadoutSlotKind.Passive
                ? sys.TryClearPassiveSlot(_slotIndex, out _)
                : sys.TryClearNormalSlot(_slotIndex, out _);

            if (!cleared)
            {
                return;
            }

            string defaultSkill = _slotKind == SkillLoadoutSlotKind.Passive
                ? PlayerSkillSystem.GetDefaultPassiveSlotSkill(_slotIndex)
                : PlayerSkillSystem.GetDefaultNormalSlotSkill(_slotIndex);

            if (!string.IsNullOrEmpty(defaultSkill))
            {
                if (_slotKind == SkillLoadoutSlotKind.Passive)
                {
                    sys.TryAssignPassiveSlot(_slotIndex, defaultSkill, allowDuplicateSwap: false, out _);
                }
                else
                {
                    sys.TryAssignNormalSlot(_slotIndex, defaultSkill, allowDuplicateSwap: false, out _);
                }
            }

            panel.ApplyLoadoutToEntity();
            panel.RefreshAll();
            SkillDragSession.End();
        }

        static string GetSlotSkill(PlayerSkillSystem sys, SkillLoadoutSlotKind kind, int index)
        {
            if (kind == SkillLoadoutSlotKind.Passive)
            {
                return index >= 0 && index < sys.PassiveSkillSlots.Length
                    ? sys.PassiveSkillSlots[index]
                    : null;
            }

            return index >= 0 && index < sys.NormalSkillSlots.Length
                ? sys.NormalSkillSlots[index]
                : null;
        }

        static void SetSlotSkill(PlayerSkillSystem sys, SkillLoadoutSlotKind kind, int index, string skillId)
        {
            if (kind == SkillLoadoutSlotKind.Passive)
            {
                sys.PassiveSkillSlots[index] = skillId;
                return;
            }

            sys.NormalSkillSlots[index] = skillId;
        }

        static bool CanPlaceSkillInSlot(
            PlayerSkillSystem sys,
            string skillId,
            SkillLoadoutSlotKind kind,
            int index,
            out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(skillId))
            {
                return true;
            }

            if (sys.IsGrantedPassive(skillId) || sys.IsGrantedActive(skillId))
            {
                failReason = "granted_not_slotable";
                return false;
            }

            if (!sys.IsSkillLearned(skillId))
            {
                failReason = "not_learned";
                return false;
            }

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (kind == SkillLoadoutSlotKind.Passive)
            {
                if (cfg == null || !cfg.IsPassive)
                {
                    failReason = "not_passive";
                    return false;
                }
            }
            else if (cfg != null && cfg.IsPassive)
            {
                failReason = "passive_not_allowed";
                return false;
            }

            var entry = SkillLearnCatalog.TryFindLearnEntryBySkillId(skillId);
            var slotType = SkillLoadoutSlotTypeUtil.ResolveSlotType(entry, skillId);
            return SkillLoadoutSlotTypeUtil.CanAssignToSlot(slotType, kind, index, out failReason);
        }
    }
}
