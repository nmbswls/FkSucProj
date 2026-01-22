
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public partial class NpcUnitLogicEntity
    {

        public void TickAttractState()
        {

        }

        public void ApplyAttracted(Vector2 pos, float power, IAttractSource? attractSrc)
        {

            if(attractSrc == null || attractSrc is not PlayerLogicEntity playerEntity)
            {
                return;
            }

            int attractLevel = playerEntity.GetAttractLevel();

            if (NpcConfig.IgnoreAttractLevel >= attractLevel)
            {
                return;
            }

            if (attractLevel > 0)
            {
                if (AIBrain != null)
                {
                    AIBrain.AttractTrigger = true;
                    AIBrain.AddAttractInfo(attractSrc?.Id ?? 0, pos, attractLevel);
                    //AIBrain.blackboard.AttractPos = pos;
                    //AIBrain.blackboard.AttractSrcId = attractSrc?.Id ?? 0;
                    //AIBrain.blackboard.AttractLevel = attractLevel;
                }
            }
        }
    }
}