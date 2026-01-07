
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

            if (cacheCfg.IgnoreAttractLevel >= attractLevel)
            {
                return;
            }

            if (attractLevel > 0)
            {
                if (AIBrain != null)
                {
                    AIBrain.blackboard.AttractTrigger = true;
                    AIBrain.blackboard.AttractPos = pos;
                    AIBrain.blackboard.AttractSrcId = attractSrc?.Id ?? 0;
                    AIBrain.blackboard.AttractLevel = attractLevel;
                }

                UpdateLookIntent(new UnitLookIntent()
                {
                    LockEntityId = attractSrc?.Id ?? 0,
                    HappenTime = LogicTime.time,
                    Duration = attractLevel > 1 ? 5.0f : 3.0f,
                });
            }
        }
    }
}