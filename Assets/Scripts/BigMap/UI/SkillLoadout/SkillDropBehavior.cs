using My.Map.Entity;
using My.Player;
using UnityEngine;

namespace My.UI.SkillLoadout
{
    public interface ISkillDropBehavior
    {
        void OnBeginDragFromPool(string skillId);

        void OnDragFromPool(Vector2 screenPos);

        void OnEndDragFromPool();

        bool TryDropOnSlot(SkillLoadoutPanel panel, PlayerSkillSystem sys, SkillLoadoutSlotKind slotKind,
            int slotIndex, string skillId, out string failReason);
    }

    public sealed class SchoolFilteredNormalSlotDropBehavior : ISkillDropBehavior
    {
        public void OnBeginDragFromPool(string skillId) =>
            SkillDragSession.Begin(skillId, this);

        public void OnDragFromPool(Vector2 screenPos) =>
            SkillDragSession.FollowScreenPoint(screenPos);

        public void OnEndDragFromPool() =>
            SkillDragSession.End();

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
    }
}
