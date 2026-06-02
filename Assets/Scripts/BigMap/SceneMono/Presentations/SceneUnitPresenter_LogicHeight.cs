using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        // 逻辑 Pos 与 rb 不一致时（如 Teleport）才回写，避免每帧与物理抢写
        void SyncRootTransformFromLogic()
        {
            if (UnitEntity == null)
            {
                return;
            }

            var world = MapLogicPosition.LogicToWorld(UnitEntity.Pos);
            if (rb != null)
            {
                if ((rb.position - (Vector2)world).sqrMagnitude > 1e-6f)
                {
                    rb.MovePosition(world);
                }
            }
            else if ((transform.position - world).sqrMagnitude > 1e-6f)
            {
                transform.position = world;
            }
        }
    }
}
