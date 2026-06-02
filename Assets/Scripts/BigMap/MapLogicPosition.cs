using UnityEngine;

namespace My.Map
{
    // Pos：Tilemap 平面坐标 (x,y)，移动 / AOI / 物理 / 表现根节点均只用 Pos。
    // LogicY：逻辑高度（层叠、支撑面），仅用于战斗高度判定与存档，不参与 world 坐标。
    public static class MapLogicPosition
    {
        public static Vector3 LogicToWorld(Vector2 pos)
        {
            return new Vector3(pos.x, pos.y, 0f);
        }

        public static Vector3 LogicToWorld(ILogicEntity entity)
        {
            if (entity == null)
            {
                return Vector3.zero;
            }

            return LogicToWorld(entity.Pos);
        }

        public static Vector2 WorldToLogicPos(Vector3 world)
        {
            return new Vector2(world.x, world.y);
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

        // 攻击判定点高度（逻辑高度 + 身体/武器带宽）
        public static float ResolveAttackHitHeight(ILogicEntity attacker, float bodyBand = 0.3f)
        {
            if (attacker == null)
            {
                return bodyBand;
            }

            return GetEffectiveLogicY(attacker) + bodyBand;
        }
    }
}
