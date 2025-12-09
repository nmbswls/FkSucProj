
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public partial class NpcUnitLogicEntity
    {

        /// <summary>
        /// 吸引源信息
        /// </summary>
        public class AttractInfo
        {
            public float AttractPower;
            public Vector2 Pos;
            public IAttractSource? AttractSource;
            public float LastTriggerTime;
        }

        public AttractInfo? attractInfo;

        public void TickAttractState()
        {
            if(attractInfo == null)
            {
                return;
            }

            if(LogicTime.time - attractInfo.LastTriggerTime > 5.0f)
            {
                attractInfo = null;
                return;
            }

            if(combatStateComp.CombatState != NpcCombatStateComp.ECombatState.NotCombat)
            {
                attractInfo = null;
                return;
            }
        }

        public void ApplyAttracted(Vector2 pos, float power, IAttractSource? attractSrc)
        {

            if (attractInfo != null && attractInfo.AttractPower > power && LogicTime.time - attractInfo.LastTriggerTime < 5.0f)
            {
                Debug.Log("");
                return;
            }

            attractInfo = new();
            attractInfo.Pos = pos;
            attractInfo.AttractPower = power;
            attractInfo.LastTriggerTime = LogicTime.time;
            attractInfo.AttractSource = attractSrc;
        }
    }
}