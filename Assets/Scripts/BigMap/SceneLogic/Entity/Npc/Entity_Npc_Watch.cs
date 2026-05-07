
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public partial class NpcUnitLogicEntity
    {
        public override void InitVisionSystem()
        {
            VisionSystem = new(this);

            VisionSystem.EventOnMarkVisible += (targetId) =>
            {
                var targetEntity = LogicManager.GetLogicEntity(targetId);
                if(targetEntity is BaseUnitLogicEntity unitTarget)
                {
                    unitTarget.OnGazeEnter(Id);
                }
            };

            VisionSystem.EventOnMarkHidden += (targetId) =>
            {
                var targetEntity = LogicManager.GetLogicEntity(targetId);
                if (targetEntity is BaseUnitLogicEntity unitTarget)
                {
                    unitTarget.OnGazeLeave(Id);
                }
            };
            
        }
    }
}