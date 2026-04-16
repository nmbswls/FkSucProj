
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public partial class NpcUnitLogicEntity
    {

        public void TickAttractState()
        {

        }

        public void ApplyAttracted(Vector2 pos, float power, ILogicEntity? attractSrc)
        {
            // 仅处理玩家产生的吸引
            if(attractSrc == null || attractSrc is not PlayerLogicEntity playerEntity)
            {
                return;
            }

            int attractLevel = playerEntity.AttractLevel;

            if (attractLevel == 0 || NpcConfig.IgnoreAttractLevel >= attractLevel)
            {
                return;
            }

            if (AIBrain != null && AIBrain.CurrentState.CanBeAttract)
            {
                AIBrain.AttractTrigger = true;
                AIBrain.AddAttractInfo(pos, attractLevel, attractSrc?.Id ?? 0);
            }
        }

        public void ApplySocialCharmed(PlayerLogicEntity srcPlayer)
        {
            AIBrain.CharmedTrigger = true;

            LogicManager.globalBuffManager.AddBuff(this.Id, "social_charmed", overrideDuration : 15.0f, casterId: srcPlayer.Id);
        }
    }
}