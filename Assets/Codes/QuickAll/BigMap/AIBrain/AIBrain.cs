using Map.Entity.AI.Action;
using Map.Logic;
using My.Map.Entity.AI.Action;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using static UnityEditor.VersionControl.Asset;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.InputSystem.DefaultInputActions;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Entity.AI
{

    public enum AIActionStatus { Idle, Running, Success, Failure, Interrupted }

    public class AIBrainBlackboard
    {
        public Vector2 SpawnPos;
        public float VisionRange = 7f;
        public float VisionFOV = 160f;
        public float LoseTargetGrace = 1.2f;
        public float LoseTargetTimer;

        // 每帧更新的感知快照
        public float Distance;
        public float AngleToPlayer;
        public bool CanSee;
        public bool InBoundary = true;
        public float BoundaryRadius = 14f;

        public Vector2? LastLeaveMoveModePos;

        public bool LastPeriodSee; // 看见或不久前看见

        public bool IsInHMode;

        public string? CurrIntentAbility = null;
    }

    /// <summary>
    /// Transitions are a combination of one or more decisions and destination states whether or not these transitions are true or false. An example of a transition could be "_if an enemy gets in range, transition to the Shooting state_".
    /// </summary>
    [System.Serializable]
    public class AITransition
    {
        [SerializeReference]
        /// this transition's decision
        public List<AIDecision> Decisions = new();
        /// the state to transition to if this Decision returns true
        public string TrueState;
        /// the state to transition to if this Decision returns false
        public string FalseState;
    }

    public class MapUnitAIBrain
    {
        public IVisionSenser2D Vision;
        public BaseUnitLogicEntity UnitEntity;
        public PlayerLogicEntity PlayerEntity;

        public Animator Animator;

        public class AIBrainEvent
        {
            public enum EBrainEventType
            {
                Invalid,
                LeaveBound,
                FindTarget,
                LostTarget,
            }
            public EBrainEventType Type;
            public int Param1;
            public int Param2;
        }

        #region blackboard

        public AIBrainBlackboard blackboard = new();

        #endregion

        public bool BrainActive = true;

        public float ActionsFrequency = 0.1f;
        public float DecisionFrequency = 0.1f;

        private List<AIBrainEvent> _pendingBrainEvents = new();

        public float TickInteval = 0.25f;
        public List<AIBrainState> States = new();
        /// this brain's current state
        public virtual AIBrainState CurrentState { get; protected set; }
        public float TimeInThisState;

        protected AIBrainState _initialState;
        protected AIBrainState _newState;

        protected List<AIDecision> _decisions;
        protected List<AIAction> _actions;
        protected float _lastActionsUpdate = 0f;
        protected float _lastDecisionsUpdate = 0f;

        public void InitilaizeAll(BaseUnitLogicEntity unitEntity, IVisionSenser2D vision, Vector2 spawnPos)
        {
            this.Vision = vision;
            this.UnitEntity = unitEntity;
            this.blackboard.SpawnPos = spawnPos;
            this.PlayerEntity = unitEntity.LogicManager.playerLogicEntity;

            var idleState = new AIBrainState(this) { StateName = "Idle" };
            States.Add(idleState);
            var moveState = new AIBrainState(this) { StateName = "FollowPatrolGroup" };
            {
                var action1 = new AIActionFollowPatrolGroup()
                {

                };
                action1.Initialization(this);
                moveState.Actions.Add(action1);
            }
            States.Add(moveState);

            var huntingState = new AIBrainState(this) { StateName = "Hunting" };
            States.Add(huntingState);

            {
                var huntingAction = new AIActionHuntingPlayer();
                huntingAction.Initialization(this);
                huntingState.Actions.Add(huntingAction);

                var attackingAction = new AIActionTryUseSkill();
                attackingAction.Initialization(this);
                huntingState.Actions.Add(attackingAction);

                var distanceControl = new AIActionDistanceControl();
                distanceControl.goodDistance = 0.8f;
                distanceControl.Initialization(this);
                huntingState.Actions.Add(distanceControl);

                //var quickControl = new AIActionQuickMove();
                //quickControl.goodDistance = 0.8f;
                //quickControl.Initialization(this);
                //huntingState.Actions.Add(quickControl);
                
            }
            

            if (unitEntity.MoveBehaveMode == BaseUnitLogicEntity.EMoveBehaveType.InPatrolGroup)
            {
                TransitionToState("FollowPatrolGroup");
            }
            else if(unitEntity.MoveBehaveMode == BaseUnitLogicEntity.EMoveBehaveType.Hunting)
            {
                TransitionToState("Hunting");
            }
            else
            {
                TransitionToState("Idle");
            }
        }

        /// <summary>
		/// Stores the last known position of the target
		/// </summary>
		protected virtual void UpdateBlackboardData()
        {
            blackboard.Distance = Vector2.Distance(UnitEntity.Pos, PlayerEntity.Pos);
            blackboard.AngleToPlayer = Vector2.SignedAngle(UnitEntity.FaceDir, (PlayerEntity.Pos - UnitEntity.Pos));
            blackboard.CanSee = Vision.CanSee(UnitEntity.Pos, UnitEntity.FaceDir, PlayerEntity.Pos, blackboard.VisionRange, blackboard.VisionFOV);

            // 有问题 会丢事件
            if (blackboard.CanSee)
            {
                blackboard.LoseTargetTimer = blackboard.LoseTargetGrace;
                if (!blackboard.LastPeriodSee)
                {
                    blackboard.LastPeriodSee = true;
                }
            }
            else
            {
                blackboard.LoseTargetTimer = Mathf.Max(0, blackboard.LoseTargetTimer - LogicTime.deltaTime);
            }

            if (blackboard.LastPeriodSee && blackboard.LoseTargetTimer <= 0)
            {
                blackboard.LastPeriodSee = false;
                //_pendingBrainEvents.Add(new AIBrainEvent()
                //{
                //    Type = AIBrainEvent.EBrainEventType.LostTarget,
                //});
            }

            // 边界
            var center = blackboard.SpawnPos;
            float dist = Vector2.Distance(UnitEntity.Pos, center);
            bool nowIn = dist <= blackboard.BoundaryRadius;
            if (blackboard.InBoundary && !nowIn)
            {
                blackboard.InBoundary = false;
                _pendingBrainEvents.Add(new AIBrainEvent()
                {
                    Type = AIBrainEvent.EBrainEventType.LeaveBound,
                });
            }
            else if (!blackboard.InBoundary && nowIn)
            {
                blackboard.InBoundary = true;
            }
        }


        /// <summary>
        /// Resets the brain, forcing it to enter its first state
        /// </summary>
        public virtual void ResetBrain()
        {
            InitializeDecisions();
            InitializeActions();
            BrainActive = true;

            if (CurrentState != null)
            {
                CurrentState.OnExitState();
                OnExitState();
            }

            if (States.Count > 0)
            {
                _newState = States[0];
                //AIStateEvent.Trigger(this, CurrentState, _newState);
                CurrentState = _newState;
                CurrentState?.OnEnterState();
            }
        }

        /// <summary>
		/// When exiting a state we reset our time counter
		/// </summary>
		protected virtual void OnExitState()
        {
            TimeInThisState = 0f;
        }


        protected virtual void InitializeDecisions()
        {
            foreach (AIDecision decision in _decisions)
            {
                decision.Initialization(this);
            }
        }

        protected virtual void InitializeActions()
        {
            foreach (var action in _actions)
            {
                action.Initialization(this);
            }
        }

        /// <summary>
		/// Returns a state based on the specified state name
		/// </summary>
		/// <param name="stateName"></param>
		/// <returns></returns>
		protected AIBrainState FindState(string stateName)
        {
            foreach (var state in States)
            {
                if (state.StateName == stateName)
                {
                    return state;
                }
            }
            if (stateName != "")
            {
            }
            return null;
        }


        public void RegisterState(AIBrainState state) => States.Add(state);

        /// <summary>
		/// Transitions to the specified state, trigger exit and enter states events
		/// </summary>
		/// <param name="newStateName"></param>
		public virtual void TransitionToState(string newStateName)
        {
            _newState = FindState(newStateName);
            //AIStateEvent.Trigger(this, CurrentState, _newState);

            if (CurrentState == null)
            {
                CurrentState = _newState;
                if (CurrentState != null)
                {
                    CurrentState.OnEnterState();
                }
                return;
            }
            if (newStateName != CurrentState.StateName)
            {
                CurrentState.OnExitState();
                OnExitState();

                CurrentState = _newState;
                if (CurrentState != null)
                {
                    CurrentState.OnEnterState();
                }
            }
        }

        public void Tick(float dt)
        {
            if (!BrainActive || (CurrentState == null))
            {
                return;
            }

            if (!BrainActive)
            {
                return;
            }

            UpdateBlackboardData();


            if (LogicTime.time - _lastActionsUpdate > ActionsFrequency)
            {
                CurrentState.PerformActions();
                _lastActionsUpdate = LogicTime.time;
            }

            if (LogicTime.time - _lastDecisionsUpdate > DecisionFrequency)
            {
                CurrentState.EvaluateTransitions();
                _lastDecisionsUpdate = LogicTime.time;
            }

            TimeInThisState += dt;
        }
    }
}

