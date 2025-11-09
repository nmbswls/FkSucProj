using Map.Entity.AI.Action;
using Map.Entity.Attr;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Map.Entity.AI.MapUnitAIBrain;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Map.Entity.AI
{
    // IAIBrainState.cs
    public interface IAIBrainState
    {
        string Name { get; }
        void OnEnter();
        void OnUpdate();
        void OnExit();

        bool IsChasing { get; }

        void OnEvent(AIBrainEvent ev);

        void RegisterAIAction(IAIAction action);
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

        public float DeltaTime;
        public float Time;
        public Vector2? LastLeaveMoveModePos;

        public bool LastPeriodSee; // 看见或不久前看见

        public bool IsInHMode;

        #endregion

        private List<AIBrainEvent> _pendingBrainEvents = new();


        private readonly Dictionary<string, IAIBrainState> _states = new();
        public IAIBrainState Current { get; private set; }


        private BasicAIActionSelector _actionSelector;
        public interface IAIActionSelector
        {
            IAIAction Select(MapUnitAIBrain aiBrain, IList<IAIAction> actionSet);
        }

        public class BasicAIActionSelector : IAIActionSelector
        {

            public IAIAction Select(MapUnitAIBrain brain, IList<IAIAction> list)
            {
                IAIAction best = null;
                float bestScore = float.NegativeInfinity;
                foreach (var s in list)
                {
                    float u = s.Evaluate(brain);
                    if (u <= 0) continue;
                    //float score = s.BasePriority * PriorityWeight + u * UtilityWeight;
                    float score = u;
                    if (score > bestScore) { bestScore = score; best = s; }
                }
                return best;
            }
        }

        protected void TickBrainEvents()
        {

        }

        public void Initilaize(BaseUnitLogicEntity unitEntity, IVisionSenser2D vision, Vector2 spawnPos)
        {
            this.Vision = vision;
            this.UnitEntity = unitEntity;
            this.SpawnPos = spawnPos;

            PlayerEntity = unitEntity.LogicManager.playerLogicEntity;

            var idleState = new IdleBrainState(this);
            idleState.RegisterAIAction(new AIActionChangeFace());

            var followState = new FollowPatrolGroupBrainState(this);
            followState.RegisterAIAction(new AIActionFollower());
        }

        public void RegisterState(IAIBrainState state) => _states[state.Name] = state;

        public void ChangeState(string name)
        {
            if (Current?.Name == name) return;
            Current?.OnExit();
            if (_states.TryGetValue(name, out var next))
            {
                Current = next;
                Current.OnEnter();
            }
        }

        public void ResetState()
        {
            Current = null;
            _states.Clear();
        }


        private float _tickTimer;
        public float TickInteval = 0.25f;


        public void Tick(float now, float dt)
        {
            _tickTimer -= dt;
            if (_tickTimer > 0)
            {
                return;
            }

            _tickTimer = TickInteval;

            Time = now;
            DeltaTime = TickInteval;
            Distance = Vector2.Distance(UnitEntity.Pos, PlayerEntity.Pos);
            AngleToPlayer = Vector2.SignedAngle(UnitEntity.FaceDir, (PlayerEntity.Pos - UnitEntity.Pos));
            CanSee = Vision.CanSee(UnitEntity.Pos, UnitEntity.FaceDir, PlayerEntity.Pos, VisionRange, VisionFOV);


            if (CanSee)
            {
                LoseTargetTimer = LoseTargetGrace;
                if(!LastPeriodSee)
                {
                    _pendingBrainEvents.Add(new AIBrainEvent()
                    {
                        Type = AIBrainEvent.EBrainEventType.FindTarget,
                    });
                    LastPeriodSee = true;
                }
            }
            else 
            { 
                LoseTargetTimer = Mathf.Max(0, LoseTargetTimer - dt);
            }

            if(LastPeriodSee && LoseTargetTimer <= 0)
            {
                LastPeriodSee = false;
                _pendingBrainEvents.Add(new AIBrainEvent()
                {
                    Type = AIBrainEvent.EBrainEventType.LostTarget,
                });
            }

            // 边界
            var center = SpawnPos;
            float dist = Vector2.Distance(UnitEntity.Pos, center);
            bool nowIn = dist <= BoundaryRadius;
            if (InBoundary && !nowIn)
            {
                InBoundary = false;
                _pendingBrainEvents.Add(new AIBrainEvent()
                {
                    Type = AIBrainEvent.EBrainEventType.LeaveBound,
                });
            }
            else if (!InBoundary && nowIn) 
            { 
                InBoundary = true; 
            }

            

            ComsumeEvents();


            
            Current?.OnUpdate();
        }
    
    
        public void ComsumeEvents()
        {
            foreach (var ev in _pendingBrainEvents)
            {
                switch (ev.Type)
                {
                    case AIBrainEvent.EBrainEventType.FindTarget:
                        {
                            if (IsInHMode)
                            {
                                ChangeState("HModeChase");
                            }
                            else if(Current.IsChasing)
                            {
                                ChangeState("CombatChase");
                            }
                        }
                        break;
                    case AIBrainEvent.EBrainEventType.LostTarget:
                        {
                            if (Current.IsChasing)
                            {
                                ChangeState("Return");
                            }
                        }
                        break;
                }
            }

            _pendingBrainEvents.Clear();
        }
    }


   

    public class IdleBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;

        public string Name => "Idle";
        public bool IsChasing => false;

        private List<IAIAction> _availableActions = new();

        public IdleBrainState(MapUnitAIBrain brain) { Brain = brain; }

        public void RegisterAIAction(IAIAction action)
        {
            _availableActions.Add(action);
        }


        public void OnEvent(AIBrainEvent ev)
        {

        }

        public void OnEnter()
        {
            Brain.UnitEntity.StopTargetteMove();
        }

        public void OnUpdate()
        {
            
        }

        public void OnExit() { }
    }




    public class FollowPatrolGroupBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;

        public string Name => "FollowPatrolGroup";
        public bool IsChasing => false;

        private List<IAIAction> _availableActions = new();

        public FollowPatrolGroupBrainState(MapUnitAIBrain brain) { Brain = brain; }

        private Vector2? _lastPos = null;
        private float _lastNavTimer = 0;

        public void RegisterAIAction(IAIAction action)
        {
            _availableActions.Add(action);
        }

        public void OnEvent(AIBrainEvent ev)
        {

        }

        public void OnEnter()
        {
            Brain.UnitEntity.StopTargetteMove();
        }

        public void OnUpdate()
        {
            if(Brain.UnitEntity.FollowPatrolId == 0)
            {
                return;
            }
            _lastNavTimer -= Time.time;
            if(_lastNavTimer > 0)
            {
                return;
            }

            _lastNavTimer = 1f;

            var patrolGroup = Brain.UnitEntity.LogicManager.AreaManager.GetLogicEntiy(Brain.UnitEntity.FollowPatrolId);
            var followPos = patrolGroup.Pos;
            
            Brain.UnitEntity.StartTargettedMove(followPos, 0.3f);
        }

        public void OnExit() { }
    }

    public class HModeChaseBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;

        public string Name => "HModeChase";
        public bool IsChasing => true;

        private List<IAIAction> _availableActions = new();

        public HModeChaseBrainState(MapUnitAIBrain brain) { Brain = brain; }

        public void RegisterAIAction(IAIAction action)
        {
            _availableActions.Add(action);
        }


        public void OnEvent(AIBrainEvent ev)
        {

        }

        public void OnEnter()
        {
            //Brain.UnitEntity.StopTargetteMove();
        }

        public void OnUpdate()
        {
            var diff = (Brain.PlayerEntity.Pos - Brain.UnitEntity.Pos);

            // 正在使用技能 暂停
            if(Brain.UnitEntity.abilityController.IsRunning)
            {
                return;
            }

            //// 检查脱战
            //bool sees = Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV);
            //if (!sees && Brain.LoseTargetTimer <= 0)
            //{
            //    Brain.BrainStateMachine.Change("Return");
            //    return;
            //}

            //// 脱离h
            //var hVal = Brain.UnitEntity.GetAttr(AttrIdConsts.UnitEnterHVal);
            //// 检查退出h模式
            //if(hVal == 0)
            //{
            //    Brain.BrainStateMachine.Change("CombatChase");
            //    return;
            //}
            
            //if (diff.magnitude > 1.0f)
            //{
            //    Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos, 0.5f);
            //}
            //else 
            //{
            //    Brain.UnitEntity.abilityController.TryUseAbility("qinfan", castDir: diff);
            //}
        }

        public void OnExit() { }
    }

    //public class HuntingBrainState : IAIBrainState
    //{
    //    private readonly MapUnitAIBrain Brain;

    //    public string Name => "Hunting";
    //    public bool IsChasing => true;

    //    private List<IAIAction> _availableActions = new();

    //    public HuntingBrainState(MapUnitAIBrain brain) { Brain = brain; }

    //    public void RegisterAIAction(IAIAction action)
    //    {
    //        _availableActions.Add(action);
    //    }

    //    public void OnEvent(AIBrainEvent ev)
    //    {

    //    }

    //    public void OnEnter()
    //    {
    //        Brain.UnitEntity.StopTargetteMove();
    //    }

    //    public void OnUpdate()
    //    {
    //        // 看到玩家 -> CombatChase
    //        // 事件也会发布，但为了响应更快，这里直接判断
    //        if (Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV))
    //        {
    //            Brain.BrainStateMachine.Change("CombatChase");
    //            return;
    //        }
    //    }

    //    public void OnExit() { }
    //}


    public class CombatChaseBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;
        public string Name => "CombatChase";
        public bool IsChasing => true;

        private List<IAIAction> _availableActions = new();

        public CombatChaseBrainState(MapUnitAIBrain brain) { Brain = brain; }

        public void RegisterAIAction(IAIAction action)
        {
            _availableActions.Add(action);
        }

        public void OnEvent(AIBrainEvent ev)
        {

        }


        public void OnEnter()
        {
            
        }

        public void OnUpdate()
        {

            //// 越界或彻底失去目标 -> 交给 Return 等状态（此处仅演示，直接继续追击）
            //bool sees = Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV);
            //if (!sees && Brain.LoseTargetTimer <= 0)
            //{
            //    Brain.BrainStateMachine.Change("Return"); 
            //    return;
            //}
            //Vector2 logicPos = Brain.UnitEntity.Pos;
            //if ((logicPos - Brain.SpawnPos).magnitude > 5f)
            //{
            //    Brain.BrainStateMachine.Change("Return");
            //    return;
            //}

            //// 脱离h
            //var hVal = Brain.UnitEntity.GetAttr(AttrIdConsts.UnitEnterHVal);
            //// 检查进入h模式
            //if (hVal > 0)
            //{
            //    Brain.BrainStateMachine.Change("HModeChase");
            //    return;
            //}

            //UpdateSnapshot();

            //// 基础追击移动：若没有独占策略运行，维持追击
            //if (!_manager.HasExclusiveRunning())
            //{
            //    Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos, 0.5f);
            //}
            //else
            //{
            //    // 独占策略控制速度，避免叠加
            //}

            //// 策略更新
            //_manager.Update(_sc);

            //// 触发“玩家靠近”事件（防抖：每帧都发可以，但建议在你的项目中加冷却）
            //if (_sc.Distance <= 1.2f)
            //{
            //    Brain.Events.Publish(new MapUnitAIBrainEvents.BrainEvent(MapUnitAIBrainEvents.BrainEventType.PlayerClose));
            //}
        }

        public void OnExit()
        {
            //Brain.Events.Unsubscribe(MapUnitAIBrainEvents.BrainEventType.PlayerClose, _onPlayerClose);
            //Brain.Events.Unsubscribe(MapUnitAIBrainEvents.BrainEventType.TookDamage, _onTookDamage);
            // 停下
            Brain.UnitEntity.StopTargetteMove();
        }

    }


    public class FleeAwayBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;
        public string Name => "FleeAway";
        public bool IsChasing => false;
        public FleeAwayBrainState(MapUnitAIBrain brain, float fleeInterval, float safeDistance) 
        { 
            Brain = brain;
            this.FleeInterval = fleeInterval;
            this.SafeDistance = safeDistance;
        }

        private float _fleeTimer;
        private float _safeTimer;

        public float FleeInterval;
        public float SafeDistance;

        private List<IAIAction> _availableActions = new();

        public void RegisterAIAction(IAIAction action)
        {
            _availableActions.Add(action);
        }

        public void OnEvent(AIBrainEvent ev)
        {

        }

        public void OnEnter()
        {
            _fleeTimer = FleeInterval + 1;
            _safeTimer = 0;
        }

        public void OnUpdate()
        {
            //var diff = Brain.PlayerEntity.Pos - Brain.UnitEntity.Pos;

            //// 尝试累计安全计时器
            //if (diff.magnitude > SafeDistance)
            //{
            //    _safeTimer += Time.time;
            //}
            //else
            //{
            //    _safeTimer = 0;
            //}

            //// 安全时间足够 返回
            //if (_safeTimer > 5f)
            //{
            //    Brain.BrainStateMachine.Change("Return");
            //    return;
            //}

            //_fleeTimer += Time.time;
            //if(_fleeTimer > FleeInterval)
            //{
            //    _fleeTimer = 0;

            //    //Brain.Vision.f
            //    var x = UnityEngine.Random.Range(-2000,2000);
            //    var y = UnityEngine.Random.Range(-2000, 2000);

            //    // 裁剪到合法区域
            //    Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos + new Vector2(x * 0.001f, y * 0.001f), 0.2f);
            //}
        }

        public void OnExit() { Brain.UnitEntity.StopTargetteMove(); }
    }


    public class ReturnBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;
        public string Name => "Return";
        public bool IsChasing => false;

        private List<IAIAction> _availableActions = new();

        public ReturnBrainState(MapUnitAIBrain brain) { Brain = brain; }

        public void RegisterAIAction(IAIAction action)
        {
            _availableActions.Add(action);
        }

        public void OnEvent(AIBrainEvent ev)
        {

        }

        public void OnEnter()
        {
            // 切换为回归动画/速度（使用巡逻速度）
        }

        public void OnUpdate()
        {
            //// 如果重新看到玩家且在边界内 -> 立即转战斗追逐
            //if (Brain.InBoundary && Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV))
            //{
            //    Brain.BrainStateMachine.Change("CombatChase");
            //    return;
            //}

            //// 计算回归目标（出生点或最近巡逻点）
            //Vector2 home = Brain.SpawnPos;
            //Brain.UnitEntity.StartTargettedMove(home, 0.1f);

            //if (Vector2.Distance(Brain.UnitEntity.Pos, home) <= 1e-1)
            //{
            //    Brain.Events.Publish(new MapUnitAIBrainEvents.BrainEvent(MapUnitAIBrainEvents.BrainEventType.ArrivedHome));
            //    Brain.BrainStateMachine.Change("Idle");
            //}
        }

        public void OnExit() { Brain.UnitEntity.StopTargetteMove(); }
    }

}

