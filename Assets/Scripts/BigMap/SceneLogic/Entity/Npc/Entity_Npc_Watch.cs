
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public partial class NpcUnitLogicEntity
    {
        ///// <summary>
        ///// todo 扩充为多个
        ///// </summary>
        //public bool IsWatchingPlayer;

        //public virtual bool CanWatch()
        //{
        //    return false;
        //}

        //public void TickGaze()
        //{
        //    if(!CanWatch())
        //    {
        //        return;
        //    }

        //    IsWatchingPlayer = false;

        //    if(combatStateComp.CombatState != NpcCombatStateComp.ECombatState.NotCombat)
        //    {
        //        return;
        //    }

        //    if (VisibilityComp.IsTargetVisible(LogicManager.playerLogicEntity.Id))
        //    {
        //        if(LogicManager.playerLogicEntity.WillBeGazed())
        //        {
        //            LogicManager.playerLogicEntity.UpdateWatchedInfo(this.Id);
        //            IsWatchingPlayer = true;
        //        }
        //    }
        //}
    }
}