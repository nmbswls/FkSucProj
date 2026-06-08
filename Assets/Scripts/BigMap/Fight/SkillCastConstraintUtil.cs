using cfg.demo;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Fight
{
    // 技能施法约束：优先读 EntitySkillData，回落 Ability 默认值
    public static class SkillCastConstraintUtil
    {
        public static float GetDesiredUseAngle(EntitySkillData skill, MapAbilitySpecConfig ability)
        {
            if (skill != null && skill.DesiredUseAngle > 0f)
            {
                return skill.DesiredUseAngle;
            }

            if (ability != null && ability.DesiredUseAngle > 0f)
            {
                return ability.DesiredUseAngle;
            }

            return 5f;
        }

        public static float GetDesiredUseDistance(EntitySkillData skill, MapAbilitySpecConfig ability)
        {
            if (skill != null && skill.DesiredUseDistance > 0f)
            {
                return skill.DesiredUseDistance;
            }

            if (ability != null && ability.DesiredUseDistance > 0f)
            {
                return ability.DesiredUseDistance;
            }

            if (ability != null && ability.Range1 > 0f)
            {
                return ability.Range1;
            }

            return 1f;
        }

        // halfAngleDeg：半角（全角 = half * 2）
        public static bool IsInFrontSector(Vector2 origin, Vector2 faceDir, Vector2 targetPos, float halfAngleDeg, float maxRange)
        {
            var toTarget = targetPos - origin;
            if (toTarget.sqrMagnitude > maxRange * maxRange)
            {
                return false;
            }

            if (toTarget.sqrMagnitude <= 1e-8f)
            {
                return true;
            }

            var look = faceDir.sqrMagnitude > 1e-8f ? faceDir.normalized : Vector2.right;
            var angle = Vector2.Angle(look, toTarget.normalized);
            return angle <= halfAngleDeg;
        }
    }
}
