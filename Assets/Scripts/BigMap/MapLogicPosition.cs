using UnityEngine;

namespace My.Map
{
    // Pos：Tilemap 平面坐标 (x,y)，移动 / AOI / 物理均在此平面。
    // LogicY：逻辑高度（层叠、支撑面、后续跳跃落地），不参与根节点 world 坐标。
    public static class MapLogicPosition
    {
        // 根节点世界坐标 = 平面 Pos（LogicY 仅逻辑侧使用）
        public static Vector3 LogicToWorld(Vector2 pos, float logicY)
        {
            return new Vector3(pos.x, pos.y, 0f);
        }

        public static Vector3 LogicToWorld(ILogicEntity entity)
        {
            if (entity == null)
            {
                return Vector3.zero;
            }

            return LogicToWorld(entity.Pos, entity.LogicY);
        }

        public static Vector2 WorldToLogicPos(Vector3 world, float logicY)
        {
            return new Vector2(world.x, world.y);
        }

        public static void WorldToLogic(Vector3 world, float currentLogicY, out Vector2 pos, out float outLogicY)
        {
            pos = WorldToLogicPos(world, currentLogicY);
            outLogicY = currentLogicY;
        }

        // 命中 / 排序等：LogicY + Buff OffsetZ
        public static float GetEffectiveLogicY(ILogicEntity entity)
        {
            if (entity == null)
            {
                return 0f;
            }

            return entity.LogicY + entity.OffsetZ;
        }
    }
}
