
using static UnityEngine.InputSystem.DefaultInputActions;
using System.Collections.Generic;
using UnityEngine;

namespace Map.Entity.AI
{
    //public class AIBrainState_Idle : AIBrainState
    //{
    //    public string Name => "Idle";
    //    public bool IsChasing => false;

    //    public AIBrainState_Idle(MapUnitAIBrain brain) : base(brain) 
    //    {
            
    //    }


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

    //    }

    //    public void OnExit() { }
    //}




    //public class FollowPatrolGroupBrainState : IAIBrainState
    //{
    //    private readonly MapUnitAIBrain Brain;

    //    public string Name => "FollowPatrolGroup";
    //    public bool IsChasing => false;

    //    private List<IAIAction> _availableActions = new();
    //    public List<IAIAction> Actions { get { return _availableActions; } }
    //    public FollowPatrolGroupBrainState(MapUnitAIBrain brain) { Brain = brain; }

    //    private Vector2? _lastPos = null;
    //    private float _lastNavTimer = 0;

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
    //        if (Brain.UnitEntity.FollowPatrolId == 0)
    //        {
    //            return;
    //        }
    //        _lastNavTimer -= Time.deltaTime;
    //        if (_lastNavTimer > 0)
    //        {
    //            return;
    //        }

    //        _lastNavTimer = 1f;

    //        var patrolGroup = Brain.UnitEntity.LogicManager.AreaManager.GetLogicEntiy(Brain.UnitEntity.FollowPatrolId);
    //        var followPos = patrolGroup.Pos;

    //        Brain.UnitEntity.StartTargettedMove(followPos, 0.3f);
    //    }

    //    public void OnExit() { }
    //}

    //public class HModeChaseBrainState : IAIBrainState
    //{
    //    private readonly MapUnitAIBrain Brain;

    //    public string Name => "HModeChase";
    //    public bool IsChasing => true;

    //    private List<IAIAction> _availableActions = new();
    //    public List<IAIAction> Actions { get { return _availableActions; } }
    //    public HModeChaseBrainState(MapUnitAIBrain brain) { Brain = brain; }

    //    public void RegisterAIAction(IAIAction action)
    //    {
    //        _availableActions.Add(action);
    //    }


    //    public void OnEvent(AIBrainEvent ev)
    //    {

    //    }

    //    public void OnEnter()
    //    {
    //        //Brain.UnitEntity.StopTargetteMove();
    //    }

    //    public void OnUpdate()
    //    {
    //        var diff = (Brain.PlayerEntity.Pos - Brain.UnitEntity.Pos);

    //        // 正在使用技能 暂停
    //        if (Brain.UnitEntity.abilityController.IsRunning)
    //        {
    //            return;
    //        }

    //        //// 检查脱战
    //        //bool sees = Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV);
    //        //if (!sees && Brain.LoseTargetTimer <= 0)
    //        //{
    //        //    Brain.BrainStateMachine.Change("Return");
    //        //    return;
    //        //}

    //        //// 脱离h
    //        //var hVal = Brain.UnitEntity.GetAttr(AttrIdConsts.UnitEnterHVal);
    //        //// 检查退出h模式
    //        //if(hVal == 0)
    //        //{
    //        //    Brain.BrainStateMachine.Change("CombatChase");
    //        //    return;
    //        //}

    //        //if (diff.magnitude > 1.0f)
    //        //{
    //        //    Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos, 0.5f);
    //        //}
    //        //else 
    //        //{
    //        //    Brain.UnitEntity.abilityController.TryUseAbility("qinfan", castDir: diff);
    //        //}
    //    }

    //    public void OnExit() { }
    //}

    ////public class HuntingBrainState : IAIBrainState
    ////{
    ////    private readonly MapUnitAIBrain Brain;

    ////    public string Name => "Hunting";
    ////    public bool IsChasing => true;

    ////    private List<IAIAction> _availableActions = new();

    ////    public HuntingBrainState(MapUnitAIBrain brain) { Brain = brain; }

    ////    public void RegisterAIAction(IAIAction action)
    ////    {
    ////        _availableActions.Add(action);
    ////    }

    ////    public void OnEvent(AIBrainEvent ev)
    ////    {

    ////    }

    ////    public void OnEnter()
    ////    {
    ////        Brain.UnitEntity.StopTargetteMove();
    ////    }

    ////    public void OnUpdate()
    ////    {
    ////        // 看到玩家 -> CombatChase
    ////        // 事件也会发布，但为了响应更快，这里直接判断
    ////        if (Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV))
    ////        {
    ////            Brain.BrainStateMachine.Change("CombatChase");
    ////            return;
    ////        }
    ////    }

    ////    public void OnExit() { }
    ////}


    //public class CombatChaseBrainState : IAIBrainState
    //{
    //    private readonly MapUnitAIBrain Brain;
    //    public string Name => "CombatChase";
    //    public bool IsChasing => true;

    //    private List<IAIAction> _availableActions = new();
    //    public List<IAIAction> Actions { get { return _availableActions; } }
    //    public CombatChaseBrainState(MapUnitAIBrain brain) { Brain = brain; }

    //    public void RegisterAIAction(IAIAction action)
    //    {
    //        _availableActions.Add(action);
    //    }

    //    public void OnEvent(AIBrainEvent ev)
    //    {

    //    }


    //    public void OnEnter()
    //    {

    //    }

    //    public void OnUpdate()
    //    {

    //        //// 越界或彻底失去目标 -> 交给 Return 等状态（此处仅演示，直接继续追击）
    //        //bool sees = Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV);
    //        //if (!sees && Brain.LoseTargetTimer <= 0)
    //        //{
    //        //    Brain.BrainStateMachine.Change("Return"); 
    //        //    return;
    //        //}
    //        //Vector2 logicPos = Brain.UnitEntity.Pos;
    //        //if ((logicPos - Brain.SpawnPos).magnitude > 5f)
    //        //{
    //        //    Brain.BrainStateMachine.Change("Return");
    //        //    return;
    //        //}

    //        //// 脱离h
    //        //var hVal = Brain.UnitEntity.GetAttr(AttrIdConsts.UnitEnterHVal);
    //        //// 检查进入h模式
    //        //if (hVal > 0)
    //        //{
    //        //    Brain.BrainStateMachine.Change("HModeChase");
    //        //    return;
    //        //}

    //        //UpdateSnapshot();

    //        //// 基础追击移动：若没有独占策略运行，维持追击
    //        //if (!_manager.HasExclusiveRunning())
    //        //{
    //        //    Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos, 0.5f);
    //        //}
    //        //else
    //        //{
    //        //    // 独占策略控制速度，避免叠加
    //        //}

    //        //// 策略更新
    //        //_manager.Update(_sc);

    //        //// 触发“玩家靠近”事件（防抖：每帧都发可以，但建议在你的项目中加冷却）
    //        //if (_sc.Distance <= 1.2f)
    //        //{
    //        //    Brain.Events.Publish(new MapUnitAIBrainEvents.BrainEvent(MapUnitAIBrainEvents.BrainEventType.PlayerClose));
    //        //}
    //    }

    //    public void OnExit()
    //    {
    //        //Brain.Events.Unsubscribe(MapUnitAIBrainEvents.BrainEventType.PlayerClose, _onPlayerClose);
    //        //Brain.Events.Unsubscribe(MapUnitAIBrainEvents.BrainEventType.TookDamage, _onTookDamage);
    //        // 停下
    //        Brain.UnitEntity.StopTargetteMove();
    //    }

    //}


    //public class FleeAwayBrainState : IAIBrainState
    //{
    //    private readonly MapUnitAIBrain Brain;
    //    public string Name => "FleeAway";
    //    public bool IsChasing => false;

    //    public List<IAIAction> Actions { get { return _availableActions; } }
    //    public FleeAwayBrainState(MapUnitAIBrain brain, float fleeInterval, float safeDistance)
    //    {
    //        Brain = brain;
    //        this.FleeInterval = fleeInterval;
    //        this.SafeDistance = safeDistance;
    //    }

    //    private float _fleeTimer;
    //    private float _safeTimer;

    //    public float FleeInterval;
    //    public float SafeDistance;


    //    public void RegisterAIAction(IAIAction action)
    //    {
    //        _availableActions.Add(action);
    //    }

    //    public void OnEvent(AIBrainEvent ev)
    //    {

    //    }

    //    public void OnEnter()
    //    {
    //        _fleeTimer = FleeInterval + 1;
    //        _safeTimer = 0;
    //    }

    //    public void OnUpdate()
    //    {
    //        //var diff = Brain.PlayerEntity.Pos - Brain.UnitEntity.Pos;

    //        //// 尝试累计安全计时器
    //        //if (diff.magnitude > SafeDistance)
    //        //{
    //        //    _safeTimer += Time.time;
    //        //}
    //        //else
    //        //{
    //        //    _safeTimer = 0;
    //        //}

    //        //// 安全时间足够 返回
    //        //if (_safeTimer > 5f)
    //        //{
    //        //    Brain.BrainStateMachine.Change("Return");
    //        //    return;
    //        //}

    //        //_fleeTimer += Time.time;
    //        //if(_fleeTimer > FleeInterval)
    //        //{
    //        //    _fleeTimer = 0;

    //        //    //Brain.Vision.f
    //        //    var x = UnityEngine.Random.Range(-2000,2000);
    //        //    var y = UnityEngine.Random.Range(-2000, 2000);

    //        //    // 裁剪到合法区域
    //        //    Brain.UnitEntity.StartTargettedMove(Brain.PlayerEntity.Pos + new Vector2(x * 0.001f, y * 0.001f), 0.2f);
    //        //}
    //    }

    //    public void OnExit() { Brain.UnitEntity.StopTargetteMove(); }
    //}


    //public class ReturnBrainState : IAIBrainState
    //{
    //    private readonly MapUnitAIBrain Brain;
    //    public string Name => "Return";
    //    public bool IsChasing => false;

    //    private List<IAIAction> _availableActions = new();
    //    public List<IAIAction> Actions { get { return _availableActions; } }
    //    public ReturnBrainState(MapUnitAIBrain brain) { Brain = brain; }

    //    public void RegisterAIAction(IAIAction action)
    //    {
    //        _availableActions.Add(action);
    //    }

    //    public void OnEvent(AIBrainEvent ev)
    //    {

    //    }

    //    public void OnEnter()
    //    {
    //        // 切换为回归动画/速度（使用巡逻速度）
    //    }

    //    public void OnUpdate()
    //    {
    //        //// 如果重新看到玩家且在边界内 -> 立即转战斗追逐
    //        //if (Brain.InBoundary && Brain.Vision.CanSee(Brain.UnitEntity.Pos, Brain.UnitEntity.FaceDir, Brain.PlayerEntity.Pos, Brain.VisionRange, Brain.VisionFOV))
    //        //{
    //        //    Brain.BrainStateMachine.Change("CombatChase");
    //        //    return;
    //        //}

    //        //// 计算回归目标（出生点或最近巡逻点）
    //        //Vector2 home = Brain.SpawnPos;
    //        //Brain.UnitEntity.StartTargettedMove(home, 0.1f);

    //        //if (Vector2.Distance(Brain.UnitEntity.Pos, home) <= 1e-1)
    //        //{
    //        //    Brain.Events.Publish(new MapUnitAIBrainEvents.BrainEvent(MapUnitAIBrainEvents.BrainEventType.ArrivedHome));
    //        //    Brain.BrainStateMachine.Change("Idle");
    //        //}
    //    }

    //    public void OnExit() { Brain.UnitEntity.StopTargetteMove(); }
    //}
}