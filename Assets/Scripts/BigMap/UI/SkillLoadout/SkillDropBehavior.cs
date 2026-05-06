using My.Player;
using UnityEngine;

namespace My.UI.SkillLoadout
{
    public interface ISkillDropBehavior
    {
        void OnBeginDragFromPool(string skillId);

        void OnDragFromPool(Vector2 screenPos);

        void OnEndDragFromPool();

        bool TryDropOnCustomNormalSlot(SkillLoadoutPanel panel, PlayerSkillSystem sys, int slotIndex, string skillId,
            out string failReason);
    }

    public sealed class SchoolFilteredNormalSlotDropBehavior : ISkillDropBehavior
    {
        public void OnBeginDragFromPool(string skillId) =>
            SkillDragSession.Begin(skillId, this);

        public void OnDragFromPool(Vector2 screenPos) =>
            SkillDragSession.FollowScreenPoint(screenPos);

        public void OnEndDragFromPool() =>
            SkillDragSession.End();

        public bool TryDropOnCustomNormalSlot(SkillLoadoutPanel panel, PlayerSkillSystem sys, int slotIndex,
            string skillId, out string failReason)
        {
            if (string.IsNullOrEmpty(panel.ActiveSchoolId) ||
                !sys.BelongsToSchoolPool(panel.ActiveSchoolId, skillId))
            {
                failReason = "not_in_school_pool";
                return false;
            }

            return sys.TryAssignNormalSlot(slotIndex, skillId, allowDuplicateSwap: true, out failReason);
        }
    }
}
