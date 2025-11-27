
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public partial class BaseUnitLogicEntity
    {
        /// <summary>
        /// todo À©³äÎª¶à¸ö
        /// </summary>
        public bool IsWatchingPlayer;

        public virtual bool CanWatch()
        {
            return false;
        }

        public void TickGaze()
        {
            if(!CanWatch())
            {
                return;
            }

            IsWatchingPlayer = false;

            if(combatStateComp.CombatState != EntityCombatStateComp.ECombatState.NotCombat)
            {
                return;
            }

            if (NoticeRecordComp.IsTargetVisible(LogicManager.playerLogicEntity.Id))
            {
                if(LogicManager.playerLogicEntity.WillBeGazed())
                {
                    LogicManager.playerLogicEntity.UpdateWatchedInfo(this.Id);
                    IsWatchingPlayer = true;
                }
            }
        }
    }
}