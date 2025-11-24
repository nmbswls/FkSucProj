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
        public abstract bool Decide();

        public string Label;
        public virtual bool DecisionInProgress { get; set; }
        protected MapUnitAIBrain _brain { get; set; }

        /// <summary>
        /// Meant to be overridden, called when the game starts
        /// </summary>
        public virtual void Initialization(MapUnitAIBrain brain)
        {
            this._brain = brain;
        }

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
        public override bool Decide()
        {
            return true;
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
        /// On init we grab our Character component
        /// </summary>
        public override void Initialization(MapUnitAIBrain brain)
        {
            base.Initialization(brain);
        }

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide()
        {
            return _brain.UnitEntity.CheckHasBuff(BuffId);
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
        /// On init we grab our Character component
        /// </summary>
        public override void Initialization(MapUnitAIBrain brain)
        {
            base.Initialization(brain);
        }

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide()
        {
            bool attracted ;
            do
            {
                if (_brain.UnitEntity.attractInfo == null)
                {
                    attracted = false; ;
                    break;
                }

                if (LogicTime.time > _brain.UnitEntity.attractInfo.LastTriggerTime + 15.0f)
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

    public class AIDecisionCheckIsInPatrolGroup : AIDecision
    {
        public bool IsHas; // 是或否
        /// <summary>
        /// On init we grab our Character component
        /// </summary>
        public override void Initialization(MapUnitAIBrain brain)
        {
            base.Initialization(brain);
        }

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide()
        {
            if(_brain.UnitEntity.MoveBehaveMode == BaseUnitLogicEntity.EMoveBehaveType.InPatrolGroup)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
	/// 
	/// </summary>
    [Serializable]

    public class AIDecisionCheckReturn : AIDecision
    {
        /// <summary>
        /// On init we grab our Character component
        /// </summary>
        public override void Initialization(MapUnitAIBrain brain)
        {
            base.Initialization(brain);
        }

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide()
        {
            if(_brain.UnitEntity.LastInterruptMovePos != null)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
	/// 
	/// </summary>
    [Serializable]

    public class AIDecisionCheckIsHunting : AIDecision
    {
        public bool IsHas; // 是或否
        /// <summary>
        /// On init we grab our Character component
        /// </summary>
        public override void Initialization(MapUnitAIBrain brain)
        {
            base.Initialization(brain);
        }

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide()
        {
            if (_brain.UnitEntity.MoveBehaveMode == BaseUnitLogicEntity.EMoveBehaveType.Hunting)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
	/// 
	/// </summary>
    [Serializable]
    //[PolymorphTag((int)EAIDecisionType.HasBuff)]

    public class AIDecisionCheckCombatState : AIDecision
    {
        public EntityCombatStateComp.ECombatState CheckState = 0;

        /// <summary>
        /// On init we grab our Character component
        /// </summary>
        public override void Initialization(MapUnitAIBrain brain)
        {
            base.Initialization(brain);
        }

        /// <summary>
        /// On Decide we check what state we're in
        /// </summary>
        /// <returns></returns>
        public override bool Decide()
        {
            return _brain.UnitEntity.CombatState == (EntityCombatStateComp.ECombatState)CheckState;
        }
    }
}

