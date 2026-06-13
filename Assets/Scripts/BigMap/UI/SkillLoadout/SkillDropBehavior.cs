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

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            var isPassive = cfg != null && cfg.IsPassive;
            if (slotKind == SkillLoadoutSlotKind.Active)
            {
                if (isPassive)
                {
                    failReason = "wrong_skill_kind";
                    return false;
                }

                return sys.TryAssignNormalSlot(slotIndex, skillId, allowDuplicateSwap: true, out failReason);
            }

            if (!isPassive)
            {
                failReason = "wrong_skill_kind";
                return false;
            }

            return sys.TryAssignPassiveSlot(slotIndex, skillId, allowDuplicateSwap: true, out failReason);
        }

        // 从技能池拖到空白处：不做任何操作
        public void OnDropToEmpty(SkillLoadoutPanel panel, PlayerSkillSystem sys) { }
    }

    // 从已装备槽拖到空白处以卸下技能
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

        // 拖到另一个槽位：先卸下本槽，再装入目标槽
        public bool TryDropOnSlot(SkillLoadoutPanel panel, PlayerSkillSystem sys, SkillLoadoutSlotKind targetKind,
            int targetIndex, string skillId, out string failReason)
        {
            // 不允许在槽位间互换（可根据需要放开）
            failReason = "slot_drag_no_swap";
            return false;
        }

        // 拖到空白处：卸下并恢复默认
        public void OnDropToEmpty(SkillLoadoutPanel panel, PlayerSkillSystem sys)
        {
            bool cleared = _slotKind == SkillLoadoutSlotKind.Passive
                ? sys.TryClearPassiveSlot(_slotIndex, out _)
                : sys.TryClearNormalSlot(_slotIndex, out _);

            if (!cleared)
            {
                return;
            }

            // 若该槽有默认技能则恢复
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
        }
    }
}
