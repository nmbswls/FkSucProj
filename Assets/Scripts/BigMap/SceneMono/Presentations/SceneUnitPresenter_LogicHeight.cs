using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        // 传送等逻辑强制位移：同步 rb，避免与插值物理每帧抢写
        public override void OnEntityMove(long entityId, Vector2 oldPos, Vector2 newPos)
        {
            base.OnEntityMove(entityId, oldPos, newPos);

            if (rb == null)
            {
                return;
            }

            var world = MapLogicPosition.LogicToWorld(newPos);
            rb.position = world;
        }
    }
}
