using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        void SyncRootTransformFromLogic()
        {
            if (UnitEntity == null)
            {
                return;
            }

            var world = MapLogicPosition.LogicToWorld(UnitEntity.Pos, UnitEntity.LogicY);
            if ((transform.position - world).sqrMagnitude > 1e-8f)
            {
                transform.position = world;
            }
        }
    }
}
