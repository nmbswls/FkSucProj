

using System.Numerics;
using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {

        private float _pendingOffsetZ = 0;
        public void UpdateOffsetZView()
        {
            if (AgentView == null) return;

            float offsetZ = 0;

            foreach(var buffInst in UnitEntity.BuffContainer.Values)
            {
                if(buffInst.Def.ZOffsetOverride > 0)
                {
                    offsetZ = buffInst.Def.ZOffsetOverride;
                }
            }

            float targetOffsetZ = offsetZ;

            _pendingOffsetZ = Mathf.Lerp(_pendingOffsetZ, targetOffsetZ, 3f * LogicTime.deltaTime);

            this.AgentView.transform.localPosition = new(this.AgentView.transform.localPosition.x, _pendingOffsetZ, 0);
        }
    }
}