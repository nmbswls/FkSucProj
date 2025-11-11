using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Map.Entity.AI.Action
{

    

    



    //public class AIActionFollower : IAIAction
    //{
    //    public MapUnitAIBrain AIBrain;
    //    public AIActionStatus Status { get; set; }

    //    private float _lastNavTimer;
    //    private Vector2? _lastNavPos;


    //    public AIActionFollower(MapUnitAIBrain aIBrain)
    //    {
    //        this.AIBrain = aIBrain;
    //    }

    //    public bool CanInterrupt(MapUnitAIBrain aiBrain, string reason, bool hard)
    //    {
    //        return true;
    //    }

    //    public float Evaluate(MapUnitAIBrain aiBrain)
    //    {
    //        return 1;
    //    }

    //    public void Start(MapUnitAIBrain aiBrain)
    //    {
    //        AIBrain.UnitEntity.FaceDir = Random.insideUnitCircle.normalized;
    //    }

    //    public void Stop(MapUnitAIBrain aiBrain, AIActionStatus endReason)
    //    {
    //    }
    //    public void Tick(MapUnitAIBrain aiBrain)
    //    {
    //        _lastNavTimer -= Time.time;
    //        if (_lastNavTimer > 0)
    //        {
    //            return;
    //        }

    //        _lastNavTimer = 0.5f;

    //        var patrolGroup = AIBrain.UnitEntity.LogicManager.AreaManager.GetLogicEntiy(AIBrain.UnitEntity.FollowPatrolId);
    //        var followPos = patrolGroup.Pos;

    //        if (_lastNavPos == null || (_lastNavPos.Value - followPos).magnitude > 0.1f)
    //        {
    //            AIBrain.UnitEntity.StartTargettedMove(followPos, 0.3f);
    //        }
    //    }
    //}


    //public class AIActionDistanceControl : IAIAction
    //{
    //    public MapUnitAIBrain AIBrain;

    //    public AIActionStatus Status { get; set; }
    //    public float desireDist = 1.0f;
    //    public AIActionDistanceControl(MapUnitAIBrain aIBrain, float desireDist)
    //    {
    //        this.AIBrain = aIBrain;
    //        this.desireDist = desireDist;
    //    }

    //    public bool CanInterrupt(MapUnitAIBrain aiBrain, string reason, bool hard)
    //    {
    //        return true;
    //    }

    //    public float Evaluate(MapUnitAIBrain aiBrain)
    //    {
    //        if (aiBrain.Distance < desireDist * 1.1f && aiBrain.Distance < desireDist * 0.9f)
    //        {
    //            return 0;
    //        }
    //        return 1;
    //    }

    //    public void Start(MapUnitAIBrain aiBrain)
    //    {
    //        AIBrain.UnitEntity.FaceDir = Random.insideUnitCircle.normalized;
    //    }

    //    public void Stop(MapUnitAIBrain aiBrain, AIActionStatus endReason)
    //    {
    //    }
    //    public void Tick(MapUnitAIBrain aiBrain)
    //    {
            
    //    }
    //}

    //public class AIActionMoveToReturnPos : IAIAction
    //{
    //    public MapUnitAIBrain AIBrain;
    //    public AIActionStatus Status { get; set; }
    //    public float desireDist = 1.0f;
    //    public Vector2 targetPos;
    //    public AIActionMoveToReturnPos(MapUnitAIBrain aIBrain)
    //    {
    //        this.AIBrain = aIBrain;
    //        if (AIBrain.LastLeaveMoveModePos != null)
    //        {

    //        }
    //    }

    //    public bool CanInterrupt(MapUnitAIBrain aiBrain, string reason, bool hard)
    //    {
    //        return true;
    //    }

    //    public float Evaluate(MapUnitAIBrain aiBrain)
    //    {
    //        return 1;
    //    }

    //    public void Start(MapUnitAIBrain aiBrain)
    //    {
    //        //AIBrain.UnitEntity.FaceDir = Random.insideUnitCircle.normalized;
    //    }

    //    public void Stop(MapUnitAIBrain aiBrain, AIActionStatus endReason)
    //    {
    //    }
    //    public void Tick(MapUnitAIBrain aiBrain)
    //    {
    //        if((AIBrain.UnitEntity.Pos - targetPos).magnitude < 0.1f)
    //        {
    //            AIBrain.ChangeState("Idle");
    //        }
    //    }
    //}

    //public class PrimaryUseSkillStrategy : IAIAction
    //{
    //    public MapUnitAIBrain AIBrain;
    //    public AIActionStatus Status { get; set; }
    //    public float DistanceRequirement;

    //    private float durationTimer;
    //    private float lastEndTimer;

    //    private float relaxDuringAttack = 1f;

    //    private MapAbilitySpecConfig? currentWantUseAbility = null;
    //    private bool isCastAbility = false;

    //    public PrimaryUseSkillStrategy(MapUnitAIBrain aIBrain, float relaxDuringAttack)
    //    {
    //        this.AIBrain = aIBrain;
    //        this.relaxDuringAttack = relaxDuringAttack;
    //    }

    //    public float Evaluate(MapUnitAIBrain aiBrain)
    //    {
    //        bool findUse = false;
    //        foreach (var state in aiBrain.UnitEntity.abilityController.AbilityStateInfos)
    //        {
    //            if (state.Value.cacheConfig.IsPassive)
    //            {
    //                continue;
    //            }
    //            if (state.Value.cacheConfig.TypeTag != Map.Entity.AbilityTypeTag.Combat)
    //            {
    //                continue;
    //            }
    //            if (aiBrain.Time > state.Value.lastUseTime + state.Value.lastUseCd)
    //            {
    //                findUse = true;
    //                break;
    //            }
    //        }

    //        if (!findUse) return 0;

    //        if (aiBrain.Time - lastEndTimer < relaxDuringAttack)
    //        {
    //            return 0;
    //        }

    //        return 11;
    //    }

    //    public void Start(MapUnitAIBrain aiBrain)
    //    {
    //        Status = AIActionStatus.Running;

    //        foreach (var state in aiBrain.UnitEntity.abilityController.AbilityStateInfos.Values)
    //        {
    //            if (state.cacheConfig.IsPassive)
    //            {
    //                continue;
    //            }
    //            if (state.cacheConfig.TypeTag != Map.Entity.AbilityTypeTag.Combat)
    //            {
    //                continue;
    //            }
    //            if (aiBrain.Time > state.lastUseTime + state.lastUseCd)
    //            {
    //                currentWantUseAbility = state.cacheConfig;
    //                break;
    //            }
    //        }

    //        durationTimer = 5.0f;
    //        isCastAbility = false;

    //        if (currentWantUseAbility == null)
    //        {
    //            Debug.LogError("currentWantUseAbility not found");
    //            return;
    //        }
    //        var targetPos = aiBrain.Vision.ChoosePointAwayFromTarget(aiBrain.UnitEntity.Pos, aiBrain.PlayerEntity.Pos, currentWantUseAbility.DesiredUseDistance);
    //        aiBrain.UnitEntity.StartTargettedMove(targetPos, 0.1f);
    //    }

    //    public void Tick(MapUnitAIBrain aiBrain)
    //    {
    //        if (Status != AIActionStatus.Running) return;

    //        // 保底中断 避免卡在那里
    //        durationTimer -= aiBrain.DeltaTime;
    //        if (durationTimer <= 0)
    //        {
    //            Stop(aiBrain, AIActionStatus.Success);
    //            return;
    //        }

    //        if (currentWantUseAbility == null)
    //        {
    //            return;
    //        }
    //        if (!isCastAbility && currentWantUseAbility.DesiredUseDistance > 0 && AIBrain.Distance < currentWantUseAbility.DesiredUseDistance)
    //        {
    //            var dir = AIBrain.PlayerEntity.Pos - AIBrain.UnitEntity.Pos;
    //            AIBrain.UnitEntity.abilityController.TryUseAbility(currentWantUseAbility.name, dir);
    //            isCastAbility = true;
    //        }
    //        else if (isCastAbility)
    //        {
    //            if (!AIBrain.UnitEntity.abilityController.IsRunning)
    //            {
    //                Stop(AIBrain, AIActionStatus.Success);
    //            }
    //        }
    //    }

    //    public void Stop(MapUnitAIBrain aiBrain, AIActionStatus endStatus)
    //    {
    //        if (Status == AIActionStatus.Idle) return;
    //        lastEndTimer = aiBrain.Time;
    //        //Status = endStatus;
    //        currentWantUseAbility = null;
    //    }

    //    public bool CanInterrupt(MapUnitAIBrain ctx, string reason, bool hard) => false;

    //    public void UpdateCooldown(float dt) {; }
    //}
}


