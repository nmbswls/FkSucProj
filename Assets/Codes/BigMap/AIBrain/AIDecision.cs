using System;
using UnityEngine;

namespace My.Map.Entity.AI
{
    public enum EAIDecisionType
    {
        None,
        HasBuff,
    }

    /// <summary>
    /// be evaluated by transitions
    /// </summary>
    [Serializable]
    public abstract class AIDecision
    {
        public abstract bool Decide(MapUnitAIBrain brain);

        public string Label;
        public virtual bool DecisionInProgress { get; set; }

        /// <summary>
        /// Meant to be overridden, called when the Brain enters a State this Decision is in
        /// </summary>
        public virtual void OnEnterState()
        {
            DecisionInProgress = true;
        }

        /// <summary>
        /// Meant to be overridden, called when the Brain exits a State this Decision is in
        /// </summary>
        public virtual void OnExitState()
        {
            DecisionInProgress = false;
        }
    }

    /// <summary>
	/// 
	/// </summary>
    [Serializable]
    //[PolymorphTag((int)EAIDecisionType.None)]
    public class AIDecisionNone : AIDecision
    {
        public override bool Decide(MapUnitAIBrain brain)
        {
            return true;
        }
    }


    /// <summary>
	/// 
	/// </summary>
    [Serializable]
    //[PolymorphTag((int)EAIDecisionType.None)]
    public class AIDecisionCanLeaveAttact : AIDecision
    {
        public override bool Decide(MapUnitAIBrain brain)
        {
            return brain.blackboard.CanLeaveAttract;
        }
    }

    /// <summary>
	/// 
	/// </summary>
    [Serializable]
    //[PolymorphTag((int)EAIDecisionType.HasBuff)]

    public class AIDecisionHasBuff : AIDecision
    {
        public string BuffId;

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide(MapUnitAIBrain brain)
        {
            return brain.NpcEntity.CheckHasBuff(BuffId);
        }
    }

    /// <summary>
	/// 
	/// </summary>
    [Serializable]
    //[PolymorphTag((int)EAIDecisionType.HasBuff)]

    public class AIDecisionCheckAttracted : AIDecision
    {
        public bool IsHas; // 是或否

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide(MapUnitAIBrain brain)
        {
            bool attracted ;
            do
            {
                var unit = brain.NpcEntity as NpcUnitLogicEntity;
                if (unit == null) return false;
                if (unit.attractInfo == null)
                {
                    attracted = false; ;
                    break;
                }

                if (LogicTime.time > unit.attractInfo.LastTriggerTime + 15.0f)
                {
                    attracted = false;
                    break;
                }

                attracted = true;

            } while (false);
            

            
            return IsHas == attracted;
        }
    }

    /// <summary>
	/// 
	/// </summary>
    [Serializable]
    //[PolymorphTag((int)EAIDecisionType.HasBuff)]

    public class AIDecisionCheckHasMoveBehave : AIDecision
    {

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide(MapUnitAIBrain brain)
        {
            return brain.NpcEntity.MoveBehaveInfo.MoveBehaveMode != UnitMoveBehaveInfo.EMoveBehaveType.NoMove;
        }
    }


    //   /// <summary>
    ///// 
    ///// </summary>
    //   [Serializable]

    //   public class AIDecisionCheckIsInPatrolGroup : AIDecision
    //   {
    //       public bool IsHas; // 是或否
    //       /// <summary>
    //       /// On init we grab our Character component
    //       /// </summary>
    //       public override void Initialization(MapUnitAIBrain brain)
    //       {
    //           base.Initialization(brain);
    //       }

    //       /// <summary>
    //       /// On Decide we check what state we're in
    //       /// </summary>
    //       /// <returns></returns>
    //       public override bool Decide()
    //       {
    //           if(_brain.UnitEntity.MoveBehaveMode == BaseUnitLogicEntity.EMoveBehaveType.InPatrolGroup)
    //           {
    //               return true;
    //           }

    //           return false;
    //       }
    //   }

    ///// <summary>
    ///// 
    ///// </summary>
    //[Serializable]

    //public class AIDecisionCheckReturn : AIDecision
    //{
    //    /// <summary>
    //    /// On init we grab our Character component
    //    /// </summary>
    //    public override void Initialization(MapUnitAIBrain brain)
    //    {
    //        base.Initialization(brain);
    //    }

    //    /// <summary>
    //    /// On Decide we check what state we're in
    //    /// </summary>
    //    /// <returns></returns>
    //    public override bool Decide()
    //    {
    //        if(_brain.UnitEntity.LastInterruptPos != null)
    //        {
    //            return true;
    //        }

    //        return false;
    //    }
    //}


    /// <summary>
	/// 
	/// </summary>
    [Serializable]
    //[PolymorphTag((int)EAIDecisionType.HasBuff)]

    public class AIDecisionCheckCombatState : AIDecision
    {
        public NpcCombatStateComp.ECombatState CheckState = 0;

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide(MapUnitAIBrain brain)
        {
            return brain.NpcEntity.CombatState == (NpcCombatStateComp.ECombatState)CheckState;
        }
    }

    [Serializable]
    //[PolymorphTag((int)EAIDecisionType.HasBuff)]

    public class AIDecisionCheckIsPeace : AIDecision
    {
        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide(MapUnitAIBrain brain)
        {
            return brain.NpcEntity.unitCfg.IsPeace;
        }
    }

    /// <summary>
	/// 
	/// </summary>
    [Serializable]
    public class AIDecisionReachMoveInterrupt : AIDecision
    {
        public override bool Decide(MapUnitAIBrain brain)
        {
            if(brain.blackboard.LastLeaveMoveModePos == null)
            {
                return true;
            }

            var diff = brain.blackboard.LastLeaveMoveModePos.Value - brain.NpcEntity.Pos;
            if(diff.magnitude < 0.2f)
            {
                return true;
            }
            return false;
        }
    }

}

