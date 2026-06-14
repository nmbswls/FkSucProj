using UnityEngine;

namespace My.Map.Entity
{
    // 轨道弹药球布局：逻辑与表现共用同一套角度/槽位计算。
    public static class SkillProxyOrbLayout
    {
        public const string ParabolaLaunchZStorageKey = "skillProxy.parabolaLaunchZ";

        public static float ComputeOrbitAngleDeg(float orbitInitialAngle, float orbitAngularSpeed, float orbitStartLogicTime)
        {
            return orbitInitialAngle + orbitAngularSpeed * (LogicTime.time - orbitStartLogicTime);
        }

        // visibleCount 为当前弹药数；slotIndex 为 0..visibleCount-1
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

        // 下一次施法将消耗的 orb：当前弹药中最末槽（current 扣 1 后消失）
        public static int ResolveConsumableSlotIndex(int currentAmmo)
        {
            return currentAmmo - 1;
        }

        // 子弹地面 XY = Owner 脚底 + 轨道 X 偏移
        public static Vector2 ResolveBulletSpawnGroundPos(Vector2 ownerPos, Vector2 slotLocalOffset)
        {
            return ownerPos + new Vector2(slotLocalOffset.x, 0f);
        }

        // 抛物线初始高度 = 表现 FollowOffset.y + 轨道 Y
        public static float ResolveParabolaLaunchZ(Vector2 followVisualOffset, Vector2 slotLocalOffset, float minLaunchZ = 0.12f)
        {
            return Mathf.Max(minLaunchZ, followVisualOffset.y + slotLocalOffset.y);
        }

        // 表现层专用：支持外部传入插值 stepDeg（平滑重排过渡期使用）
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
