using UnityEngine;

namespace My.Map
{
    // Pos：Tilemap 平面坐标 (x,y)，与移动/AOI 一致。
    // LogicY：层高度附加值；根节点 world.y = Pos.y + LogicY（LogicY=0 时与改前一致）。
    public static class MapLogicPosition
    {
        public static Vector3 LogicToWorld(Vector2 pos, float logicY)
        {
            return new Vector3(pos.x, pos.y + logicY, 0f);
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
            return new Vector2(world.x, world.y - logicY);
        }

        public static float WorldToLogicY(float logicY)
        {
            return logicY;
        }

        public static void WorldToLogic(Vector3 world, float currentLogicY, out Vector2 pos, out float outLogicY)
        {
            pos = WorldToLogicPos(world, currentLogicY);
            outLogicY = currentLogicY;
        }

        // 命中高度：LogicY + Buff OffsetZ
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
