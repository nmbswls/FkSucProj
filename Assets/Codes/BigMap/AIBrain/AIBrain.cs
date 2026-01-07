using Map.Entity.AI.Action;
using Map.Logic;
using My.Map.Entity.AI.Action;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using static UnityEditor.VersionControl.Asset;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.InputSystem.DefaultInputActions;
using static UnityEngine.Rendering.CoreUtils;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Entity.AI
{

    public enum AIActionStatus { Idle, Running, Success, Failure, Interrupted }

    public class AIBrainBlackboard
    {
        public Vector2 SpawnPos;
        
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

        public string? CurrIntentSkill = null;

        public bool CanLeaveAttract;

        public bool AttractTrigger;

        public Vector2 AttractPos;
        public long AttractSrcId;
        public int AttractLevel;

        public Vector2 EnterAttractPos; // 
        public int CurrentAttractLevel; // 当前吸引等级
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
        public NpcUnitLogicEntity NpcEntity;
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


        public AIBrainParamsConfig brainConfig;
        public AIBrainBlackboard blackboard = new();


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

        protected Dictionary<string, AIAction> _actions = new();

        protected List<AITransition> _commonTransitions = new();

        protected float _lastActionsUpdate = 0f;
        protected float _lastDecisionsUpdate = 0f;

        //public static T DeepCopyByJson<T>(T src)
        //{
        //    if (src == null) return default;
        //    var json = JsonConvert.SerializeObject(src);
        //    var copy = JsonConvert.DeserializeObject(json, src.GetType());
        //    return (T)copy;
        //}

        public static AIAction CreateActionFromCfg(MapUnitAIBrain brain, AIActionCfg cfg)
        {
            AIAction action = null;
            switch(cfg)
            {
                case AIActionCfgDoNothing:
                    {
                        action = new AIActionDoNothing(brain, cfg);
                    }
                    break;
                case AIActionCfgDoSomething:
                    {
                        action = new AIActionDoSomething(brain, cfg);
                    }
                    break;
                case AIActionCfgRecoveryFromAttract:
                    {
                        action = new AIActionRecoveryFromAttract(brain, cfg);
                    }
                    break;
                case AIActionCfgTryRecovery:
                    {
                        action = new AIActionTryRecovery(brain, cfg);
                    }
                    break;
                case AIActionCfgNormalMoveDaemon:
                    {
                        action = new AIActionNormalMoveDaemon(brain, cfg);
                    }
                    break;
                case AIActionCfgMoveInPatrolGroup:
                    {
                        action = new AIActionMoveInPatrolGroup(brain, cfg);
                    }
                    break;
                case AIActionCfgMoveDoPath:
                    {
                        action = new AIActionMoveDoPath(brain, cfg);
                    }
                    break;
                case AIActionCfgMoveHunting:
                    {
                        action = new AIActionMoveHunting(brain, cfg);
                    }
                    break;
                case AIActionCfgCombatMain:
                    {
                        action = new AIActionCombatMain(brain, cfg);
                    }
                    break;
                case AIActionCfgTryUseSkill:
                    {
                        action = new AIActionTryUseSkill(brain, cfg);
                    }
                    break;
                case AIActionCfgDistanceControl:
                    {
                        action = new AIActionDistanceControl(brain, cfg);
                    }
                    break;
                case AIActionCfgCombatQuickCloser:
                    {
                        action = new AIActionCombatQuickCloser(brain, cfg);
                    }
                    break;
                case AIActionCfgAttractedDaemon:
                    {
                        action = new AIActionAttractedDaemon(brain, cfg);
                    }
                    break;
                case AIActionCfgAttractedMove:
                    {
                        action = new AIActionAttractedMove(brain, cfg);
                    }
                    break;
                case AIActionCfgChangeFace:
                    {
                        action = new AIActionChangeFace(brain, cfg);
                    }
                    break;
                case AIActionCfgXianZhuShou:
                    {
                        action = new AIActionXianZhuShou(brain, cfg);
                    }
                    break;
                default:
                    {
                        Debug.LogError("");
                        break;
                    }
            }

            return action;
        }

        public void InitilaizeAll(NpcUnitLogicEntity npcEntity, IVisionSenser2D vision, Vector2 spawnPos)
        {
            this.Vision = vision;
            this.NpcEntity = npcEntity;
            this.blackboard.SpawnPos = spawnPos;
            this.PlayerEntity = npcEntity.LogicManager.playerLogicEntity;

            _actions.Clear();
            _commonTransitions.Clear();

            string confId = "BasicUnit";

            brainConfig = AIBrainParamsConfigLoader.Load(npcEntity.unitCfg.AIBrainParamsCfgId);

            var conf = AITemplateConfigLoader.Load(confId);

            foreach(var actionCfg in conf.Actions)
            {
                var action = CreateActionFromCfg(this, actionCfg);
                if (action == null) continue;
                //var newAction = DeepCopyByJson(action);
                _actions.Add(action.Name, action);
            }


            foreach (var trans in conf.CommonTransitions)
            {
                _commonTransitions.Add(trans);
            }
            foreach (var stateInfo in conf.States)
            {
                var state = new AIBrainState(this) { StateName = stateInfo.Name };
                
                foreach (var trans in stateInfo.Transitions)
                {
                    state.Transitions.Add(trans);
                }

                foreach(var actionName in stateInfo.ActionNames)
                {
                    _actions.TryGetValue(actionName, out var action);
                    if(action != null)
                    {
                        if(action.cfg.IsDecorate)
                        {
                            state.DecorateActions.Add(action);
                        }
                        else
                        {
                            state.NormalActions.Add(action);
                        }
                    }
                }
                state.Initialization();
                States.Add(state);
            }


            //var idleState = new AIBrainState(this) { StateName = "Idle" };
            //States.Add(idleState);
            //var moveState = new AIBrainState(this) { StateName = "FollowPatrolGroup" };
            //{
            //    var action1 = new AIActionFollowPatrolGroup()
            //    {

            //    };
            //    action1.Initialization(this);
            //    moveState.Actions.Add(action1);
            //}
            //States.Add(moveState);

            //var huntingState = new AIBrainState(this) { StateName = "Hunting" };
            //States.Add(huntingState);

            //{
            //    var huntingAction = new AIActionHuntingPlayer();
            //    huntingAction.Initialization(this);
            //    huntingState.Actions.Add(huntingAction);

            //    var attackingAction = new AIActionTryUseSkill();
            //    attackingAction.Initialization(this);
            //    huntingState.Actions.Add(attackingAction);

            //    var distanceControl = new AIActionDistanceControl();
            //    distanceControl.goodDistance = 0.8f;
            //    distanceControl.Initialization(this);
            //    huntingState.Actions.Add(distanceControl);

            //    //var quickControl = new AIActionQuickMove();
            //    //quickControl.goodDistance = 0.8f;
            //    //quickControl.Initialization(this);
            //    //huntingState.Actions.Add(quickControl);

            //}


            TransitionToState("Idle");
        }

        public void TriggerUpdateImmediately()
        {
            _lastActionsUpdate = 0;
            _lastDecisionsUpdate = 0;
        }
        /// <summary>
		/// Stores the last known position of the target
		/// </summary>
		protected virtual void UpdateBlackboardData()
        {
            blackboard.Distance = Vector2.Distance(NpcEntity.Pos, PlayerEntity.Pos);
            blackboard.AngleToPlayer = Vector2.SignedAngle(NpcEntity.FaceDir, (PlayerEntity.Pos - NpcEntity.Pos));
            blackboard.CanSee = Vision.CanSee(NpcEntity.Pos, NpcEntity.FaceDir, PlayerEntity.Pos, brainConfig.VisionRange, brainConfig.VisionFOV);

            // 有问题 会丢事件
            if (blackboard.CanSee)
            {
                blackboard.LoseTargetTimer = brainConfig.LoseTargetGrace;
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
            float dist = Vector2.Distance(NpcEntity.Pos, center);
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


                if(_commonTransitions.Count > 0)
                {
                    for (int i = 0; i < _commonTransitions.Count; i++)
                    {
                        bool pass = true;
                        foreach (var decision in _commonTransitions[i].Decisions)
                        {
                            if (!decision.Decide(this))
                            {
                                pass = false; break;
                            }
                        }

                        if (pass)
                        {
                            if (!string.IsNullOrEmpty(_commonTransitions[i].TrueState))
                            {
                                TransitionToState(_commonTransitions[i].TrueState);
                                break;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(_commonTransitions[i].FalseState))
                            {
                                TransitionToState(_commonTransitions[i].FalseState);
                                break;
                            }
                        }
                    }
                }
            }

            TimeInThisState += dt;
        }
    }
}

