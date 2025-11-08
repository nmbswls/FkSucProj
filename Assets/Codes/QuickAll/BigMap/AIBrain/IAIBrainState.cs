using Map.Entity.AI.Stategy;
using Map.Entity.Attr;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    }


    public class MapUnitAIBrainEvents
    {
        public enum BrainEventType
        {
            PlayerSpotted,
            PlayerLost,
            PlayerClose,       // 自定义靠近事件
            TookDamage,        // 受击
            LeftBoundary,
            ArrivedHome,
            BrainStateChanged,
            Custom
        }
        public struct BrainEvent
        {
            public BrainEventType Type;
            public object Payload;
            public BrainEvent(BrainEventType type, object payload = null) { Type = type; Payload = payload; }
        }

        private readonly Dictionary<BrainEventType, Action<BrainEvent>> _map = new();
        public void Subscribe(BrainEventType type, Action<BrainEvent> h)
        {
            _map[type] = _map.TryGetValue(type, out var e) ? e + h : h;
        }
        public void Unsubscribe(BrainEventType type, Action<BrainEvent> h)
        {
            if (_map.TryGetValue(type, out var e)) _map[type] = e - h;
        }
        public void Publish(BrainEvent ev)
        {
            if (_map.TryGetValue(ev.Type, out var e)) e?.Invoke(ev);
        }
    }

    

    public class MapUnitAIBrain
    {
        // 处理
        public IVisionSenser2D Vision;
        public BaseUnitLogicEntity UnitEntity;
        public Vector2 SpawnPos;
        public PlayerLogicEntity PlayerEntity;

        public Animator Animator;

        public float VisionRange = 7f;
        public float VisionFOV = 160f;
        public float LoseTargetGrace = 1.2f;
        public float BoundaryRadius = 14f;

        public Vector2? LastLeaveMoveModePos;


        public bool InBoundary = true;
        public float LoseTargetTimer;

        public MapUnitAIBrainEvents Events { get; private set; }
        public AIBrainStateMachine BrainStateMachine { get; private set; }

        public int CombatStrategyCfg;


        public void Initilaize(BaseUnitLogicEntity unitEntity, IVisionSenser2D vision, Vector2 spawnPos)
        {
            this.Vision = vision;
            this.UnitEntity = unitEntity;
            this.SpawnPos = spawnPos;

            Events = new MapUnitAIBrainEvents();
            BrainStateMachine = new AIBrainStateMachine(Events);

            PlayerEntity = unitEntity.LogicManager.playerLogicEntity;

            //BrainStateMachine.Reset();

            //BrainStateMachine.Register(new IdleBrainState(this));
            //if(CombatStrategyCfg != 0)
            //{
            //    BrainStateMachine.Register(new CombatChaseBrainState(this));
            //}
            
            //BrainStateMachine.Register(new ReturnBrainState(this));
            //// 你可以注册 Idle/Patrol/Return 等状态，这里示例聚焦 CombatChase。
            //BrainStateMachine.Change("Idle");
        }

        public void Tick(float dt)
        {
            // 感知
            bool sees = Vision.CanSee(UnitEntity.Pos, UnitEntity.FaceDir, PlayerEntity.Pos, VisionRange, VisionFOV);
            if (sees) LoseTargetTimer = LoseTargetGrace;
            else LoseTargetTimer = Mathf.Max(0, LoseTargetTimer - Time.deltaTime);

            // 边界
            var center = SpawnPos;
            float dist = Vector2.Distance(UnitEntity.Pos, center);
            bool nowIn = dist <= BoundaryRadius;
            if (InBoundary && !nowIn) { InBoundary = false; Events.Publish(new MapUnitAIBrainEvents.BrainEvent(MapUnitAIBrainEvents.BrainEventType.LeftBoundary)); }
            else if (!InBoundary && nowIn) InBoundary = true;

            BrainStateMachine.Update();
        }
    }


    public class AIBrainStateMachine
    {
        public IAIBrainState Current { get; private set; }
        private readonly Dictionary<string, IAIBrainState> _states = new();
        private readonly MapUnitAIBrainEvents _events;

        public AIBrainStateMachine(MapUnitAIBrainEvents events) { _events = events; }

        public void Register(IAIBrainState state) => _states[state.Name] = state;

        public void Change(string name)
        {
            if (Current?.Name == name) return;
            Current?.OnExit();
            if (_states.TryGetValue(name, out var next))
            {
                Current = next;
                Current.OnEnter();
                _events.Publish(new MapUnitAIBrainEvents.BrainEvent(MapUnitAIBrainEvents.BrainEventType.BrainStateChanged, name));
            }
        }

        public void Update() => Current?.OnUpdate();

        public void Reset()
        {
            Current = null;
             _states.Clear();
        }
    }

    public class IdleBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;

        public string Name => "Idle";
        public IdleBrainState(MapUnitAIBrain brain) { Brain = brain; }

        public void OnEnter()
        {
            Brain.UnitEntity.StopTargetteMove();
        }

        public void OnUpdate()
        {
            // 看到玩家 -> CombatChase
            if (Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir,  Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV))
            {
                Brain.BrainStateMachine.Change("CombatChase");
                return;
            }
        }

        public void OnExit() { }
    }

    public class FollowPatrolGroupBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;

        public string Name => "FollowPatrolGroup";
        public FollowPatrolGroupBrainState(MapUnitAIBrain brain) { Brain = brain; }

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
        public HModeChaseBrainState(MapUnitAIBrain brain) { Brain = brain; }

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

            // 检查脱战
            bool sees = Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV);
            if (!sees && Brain.LoseTargetTimer <= 0)
            {
                Brain.BrainStateMachine.Change("Return");
                return;
            }

            // 脱离h
            var hVal = Brain.UnitEntity.GetAttr(AttrIdConsts.UnitEnterHVal);
            // 检查退出h模式
            if(hVal == 0)
            {
                Brain.BrainStateMachine.Change("CombatChase");
                return;
            }
            
            if (diff.magnitude > 1.0f)
            {
                Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos, 0.5f);
            }
            else 
            {
                Brain.UnitEntity.abilityController.TryUseAbility("qinfan", castDir: diff);
            }
        }

        public void OnExit() { }
    }

    public class HuntingBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;

        public string Name => "Hunting";
        public HuntingBrainState(MapUnitAIBrain brain) { Brain = brain; }

        public void OnEnter()
        {
            Brain.UnitEntity.StopTargetteMove();
        }

        public void OnUpdate()
        {
            // 看到玩家 -> CombatChase
            // 事件也会发布，但为了响应更快，这里直接判断
            if (Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV))
            {
                Brain.BrainStateMachine.Change("CombatChase");
                return;
            }
        }

        public void OnExit() { }
    }


    public class CombatChaseBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;
        public string Name => "CombatChase";

        private AICombatStrategyManager _manager;
        private BasicAISelector _selector;
        private AIStrategyContext _sc;


        private Dictionary<string, IAICombatStrategy> _innerStrategies = new();


        public CombatChaseBrainState(MapUnitAIBrain brain) { Brain = brain; }

        public void Initialize()
        {
            _selector = new BasicAISelector { PriorityWeight = 1f, UtilityWeight = 1f };
            _manager = new AICombatStrategyManager(_selector);


            // 上下文
            _sc = new AIStrategyContext
            {
                AIBrain = Brain,
                SelfEntity = Brain.UnitEntity,
                PlayerEntity = Brain.PlayerEntity,
            };
        }

        public void RegisterStrategy(IAICombatStrategy strategy)
        {
            _innerStrategies[strategy.Name] = strategy;
            _manager.AddStrategy(strategy);
        }

        public void OnEnter()
        {
            
        }

        public void OnUpdate()
        {
            if (_sc.PlayerEntity == null) return;

            // 越界或彻底失去目标 -> 交给 Return 等状态（此处仅演示，直接继续追击）
            bool sees = Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV);
            if (!sees && Brain.LoseTargetTimer <= 0)
            {
                Brain.BrainStateMachine.Change("Return"); 
                return;
            }
            Vector2 logicPos = Brain.UnitEntity.Pos;
            if ((logicPos - Brain.SpawnPos).magnitude > 5f)
            {
                Brain.BrainStateMachine.Change("Return");
                return;
            }

            // 脱离h
            var hVal = Brain.UnitEntity.GetAttr(AttrIdConsts.UnitEnterHVal);
            // 检查进入h模式
            if (hVal > 0)
            {
                Brain.BrainStateMachine.Change("HModeChase");
                return;
            }

            UpdateSnapshot();

            // 基础追击移动：若没有独占策略运行，维持追击
            if (!_manager.HasExclusiveRunning())
            {
                Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos, 0.5f);
            }
            else
            {
                // 独占策略控制速度，避免叠加
            }

            // 策略更新
            _manager.Update(_sc);

            // 触发“玩家靠近”事件（防抖：每帧都发可以，但建议在你的项目中加冷却）
            if (_sc.Distance <= 1.2f)
            {
                Brain.Events.Publish(new MapUnitAIBrainEvents.BrainEvent(MapUnitAIBrainEvents.BrainEventType.PlayerClose));
            }
        }

        public void OnExit()
        {
            //Brain.Events.Unsubscribe(MapUnitAIBrainEvents.BrainEventType.PlayerClose, _onPlayerClose);
            //Brain.Events.Unsubscribe(MapUnitAIBrainEvents.BrainEventType.TookDamage, _onTookDamage);
            // 停下
            Brain.UnitEntity.StopTargetteMove();
        }

        private void UpdateSnapshot()
        {
            _sc.DeltaTime = Time.deltaTime; _sc.Time = Time.time;

            _sc.Distance = Vector2.Distance(Brain.UnitEntity.Pos, Brain.PlayerEntity.Pos);
            _sc.AngleToPlayer = Vector2.SignedAngle(Brain.UnitEntity.FaceDir, (Brain.PlayerEntity.Pos - Brain.UnitEntity.Pos));
            _sc.LineOfSight = Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV);
            _sc.InBoundary = Brain.InBoundary;
            //_sc.Ammo = Brain.Combat.CurrentAmmo;
        }
    }


    public class FleeAwayBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;
        public string Name => "FleeAway";
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

        public void OnEnter()
        {
            _fleeTimer = FleeInterval + 1;
            _safeTimer = 0;
        }

        public void OnUpdate()
        {
            var diff = Brain.PlayerEntity.Pos - Brain.UnitEntity.Pos;

            // 尝试累计安全计时器
            if (diff.magnitude > SafeDistance)
            {
                _safeTimer += Time.time;
            }
            else
            {
                _safeTimer = 0;
            }

            // 安全时间足够 返回
            if (_safeTimer > 5f)
            {
                Brain.BrainStateMachine.Change("Return");
                return;
            }

            _fleeTimer += Time.time;
            if(_fleeTimer > FleeInterval)
            {
                _fleeTimer = 0;

                //Brain.Vision.f
                var x = UnityEngine.Random.Range(-2000,2000);
                var y = UnityEngine.Random.Range(-2000, 2000);

                // 裁剪到合法区域
                Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos + new Vector2(x * 0.001f, y * 0.001f), 0.2f);
            }
        }

        public void OnExit() { Brain.UnitEntity.StopTargetteMove(); }
    }


    public class ReturnBrainState : IAIBrainState
    {
        private readonly MapUnitAIBrain Brain;
        public string Name => "Return";
        public ReturnBrainState(MapUnitAIBrain brain) { Brain = brain; }

        public void OnEnter()
        {
            // 切换为回归动画/速度（使用巡逻速度）
        }

        public void OnUpdate()
        {
            // 如果重新看到玩家且在边界内 -> 立即转战斗追逐
            if (Brain.InBoundary && Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV))
            {
                Brain.BrainStateMachine.Change("CombatChase");
                return;
            }

            // 计算回归目标（出生点或最近巡逻点）
            Vector2 home = Brain.SpawnPos;
            Brain.UnitEntity.StartTargettedMove(home, 0.1f);

            if (Vector2.Distance(Brain.UnitEntity.Pos, home) <= 1e-1)
            {
                Brain.Events.Publish(new MapUnitAIBrainEvents.BrainEvent(MapUnitAIBrainEvents.BrainEventType.ArrivedHome));
                Brain.BrainStateMachine.Change("Idle");
            }
        }

        public void OnExit() { Brain.UnitEntity.StopTargetteMove(); }
    }

}

