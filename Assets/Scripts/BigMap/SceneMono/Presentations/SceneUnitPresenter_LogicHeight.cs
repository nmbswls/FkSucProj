using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        // 逻辑层 Pos 驱动根节点；LogicY 不参与 world 坐标
        void SyncRootTransformFromLogic()
        {
            if (UnitEntity == null)
            {
                return;
            }

            var world = MapLogicPosition.LogicToWorld(UnitEntity.Pos, UnitEntity.LogicY);
            if (rb != null)
            {
                if ((rb.position - (Vector2)world).sqrMagnitude > 1e-8f)
                {
                    rb.MovePosition(world);
                }
            }
            else if ((transform.position - world).sqrMagnitude > 1e-8f)
            {
                transform.position = world;
            }
        }
    }
}
