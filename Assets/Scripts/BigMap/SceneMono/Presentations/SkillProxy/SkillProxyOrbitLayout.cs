using UnityEngine;

namespace My.Map.Scene
{
    // 轨道弹药槽布局计算，仅供表现层使用。
    public static class SkillProxyOrbitLayout
    {
        public static float ComputeOrbitAngleDeg(float orbitInitialAngle, float orbitAngularSpeed, float orbitStartTime)
        {
            return orbitInitialAngle + orbitAngularSpeed * (Time.time - orbitStartTime);
        }

        public static Vector2 ComputeSlotLocalOffset(
            int slotIndex,
            int visibleCount,
            float orbitAngleDeg,
            float orbitRadius)
        {
            if (visibleCount <= 0 || slotIndex < 0 || slotIndex >= visibleCount)
            {
                return Vector2.zero;
            }

            float stepDeg = 360f / visibleCount;
            float angleDeg = orbitAngleDeg + stepDeg * slotIndex;
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * orbitRadius, Mathf.Sin(rad) * orbitRadius);
        }

        public static int ResolveConsumableSlotIndex(int currentAmmo)
        {
            return currentAmmo - 1;
        }

        public static Vector2 ComputeSlotLocalOffsetWithStep(
            int slotIndex,
            float stepDeg,
            float orbitAngleDeg,
            float orbitRadius)
        {
            float angleDeg = orbitAngleDeg + stepDeg * slotIndex;
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * orbitRadius, Mathf.Sin(rad) * orbitRadius);
        }
    }
}
