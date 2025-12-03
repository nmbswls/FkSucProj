using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Config.Unit;
using Map.Entity.AI.Action;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Config.Unit.EntitySkillCfg;
using static My.Map.BaseUnitLogicEntity;
using static My.Map.Entity.MapEntityAbilityExecutor;
using static My.Map.EntityCombatStateComp;
using static UnityEngine.GraphicsBuffer;

namespace My.Map.Entity.AI
{
    public static class AIActionUtils
    {
        public static (Vector2?, long?) GetSkillUseParams(EntitySkillCfg skillConf, BaseUnitLogicEntity caster)
        {
            //
            switch(skillConf.SelectPolicy)
            {
                case ESelectPolicy.None:
                    {
                        return (null, null);
                    }
                    break;
                case ESelectPolicy.PrimaryTarget:
                    {
                        var target = caster.LogicManager.GetLogicEntity(caster.combatStateComp.PrimaryTargetId, false);
                        return (target.Pos, target.Id);
                    }
                    break;
                case ESelectPolicy.Random:
                    {
                        return (null, null);
                    }
                    break;
                default:
                    {
                        Debug.Log("GetSkillUseParams type error");
                        return (null, null);
                    }
            }

            
            //if (skillConf.SelectPolicy == ESelectPolicy.None)
            //{
            //    if (caster.combatStateComp.PrimaryTargetId != 0)
            //    {
                    
            //    }
            //    else
            //    {
            //        Debug.LogError("AIActionUtils GetAbilityUseParams no primary target ");
            //    }
            //}

            //// 目标为点 对于ai来说 
            //if (abilityConf.TargetType == MapAbilitySpecConfig.ETargetType.Point
            //    || abilityConf.TargetType == MapAbilitySpecConfig.ETargetType.Circle)
            //{
                
            //}
            //else if (abilityConf.TargetType == MapAbilitySpecConfig.ETargetType.LockTarget)
            //{
            //    if (abilityConf.SelectPolicy == 0)
            //    {
            //        if (caster.combatStateComp.PrimaryTargetId != 0)
            //        {
            //            caster.ablilityManager.UseSkill(abilityConf.Id, Vector2.zero, Vector2.zero, caster.combatStateComp.PrimaryTargetId);
            //        }
            //        else
            //        {
            //            Debug.LogError("AIActionUtils GetAbilityUseParams no primary target ");
            //        }
            //    }
            //}
            //else if (abilityConf.TargetType == MapAbilitySpecConfig.ETargetType.NoTarget)
            //{
            //    caster.ablilityManager.UseSkill(abilityConf.Id, Vector2.zero, Vector2.zero, 0);
            //}
            //else
            //{
            //    Debug.LogError($"AIActionUtils GetAbilityUseParams unsopoorted {abilityConf.TargetType}");
            //}
        }
    }


    public enum EAIActionType
    {
        DoNothing,
        DoOneThing,
    }

    [Serializable]
    public abstract class AIAction
    {
        public enum InitializationModes { EveryTime, OnlyOnce, }
        /// whether initialization should happen only once, or every time the brain is reset
        public InitializationModes InitializationMode;
        protected bool _initialized { get; set; }

        public abstract string Name { get; }
        protected MapUnitAIBrain _brain;

        public virtual bool IsExclusive { get { return false; } }

        public AIActionStatus Status { get; set; }

        protected virtual bool ShouldInitialize
        {
            get
            {
                switch (InitializationMode)
                {
                    case InitializationModes.EveryTime:
                        return true;
                    case InitializationModes.OnlyOnce:
                        return _initialized == false;
                }
                return true;
            }
        }

        /// <summary>
		/// Initializes the action. Meant to be overridden
		/// </summary>
		public virtual void Initialization(MapUnitAIBrain aIBrain)
        {
            this._brain = aIBrain;
            _initialized = true;
        }

        public virtual float RateScore()
        {
            return 0;
        }

        public virtual void Start()
        {
            Status = AIActionStatus.Running;
        }

        public virtual void Tick()
        {
            if (Status != AIActionStatus.Running) return;
        }

        public virtual void Stop(AIActionStatus endStatus)
        {
            if (Status == AIActionStatus.Idle) return;
            Status = endStatus;
        }

        /// <summary>
        /// Describes what happens when the brain enters the state this action is in. Meant to be overridden.
        /// </summary>
        public virtual void OnEnterState()
        {
            Status = AIActionStatus.Idle;
        }

        /// <summary>
        /// Describes what happens when the brain exits the state this action is in. Meant to be overridden.
        /// </summary>
        public virtual void OnExitState()
        {
        }

        public virtual bool CanInterrupt(string reason, bool hard)
        {
            return true;
        }
    }

    [Serializable]
    public class AIActionDoNothing : AIAction
    {

        public override string Name => "DoNothing";


        public string DoNothing;


        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick()
        {

        }
    }

    [Serializable]
    public class AIActionRecoveryFromAttract : AIAction
    {
        public override string Name => "RecoveryFromAttract";
        public float MinRecoverTime = 1.5f;

        private float _recoverTimer;

        public override void Start()
        {
            base.Start();

            _recoverTimer = LogicTime.time;
            _brain.UnitEntity.entityMotorComp.StopMove();
        }

        public override float RateScore()
        {
            return 1;
        }

        public override void Tick()
        {

            if (_recoverTimer + MinRecoverTime > LogicTime.time)
            {
                return;
            }

            bool recovered = false;

            do
            {
                if (_brain.blackboard.LastLeaveMoveModePos == null)
                {
                    recovered = true;
                    break;
                }

                if (!_brain.UnitEntity.entityMotorComp.CheckIsMovingTo(_brain.blackboard.LastLeaveMoveModePos.Value))
                {
                    _brain.UnitEntity.entityMotorComp.MoveTo(_brain.blackboard.LastLeaveMoveModePos.Value);
                }

                var diff = _brain.blackboard.LastLeaveMoveModePos.Value - _brain.UnitEntity.Pos;
                if (diff.magnitude < 0.3f)
                {
                    recovered = true;
                    break;
                }
            }
            while (false);
        }
    }


    [Serializable]
    public class AIActionTryRecovery : AIAction
    {
        public override string Name => "TryRecovery";
        public float MinRecoverTime = 1.5f;

        private float _recoverTimer;

        public override void Start()
        {
            base.Start();

            _recoverTimer = LogicTime.time;
            _brain.UnitEntity.entityMotorComp.StopMove();
        }

        public override float RateScore()
        {
            return 1;
        }

        public override void Tick()
        {

            if(_recoverTimer + MinRecoverTime > LogicTime.time)
            {
                return;
            }

            bool recovered = false;

            do
            {
                if (_brain.UnitEntity.unitCfg.RecoverReturn)
                {
                    if (_brain.blackboard.LastLeaveMoveModePos == null)
                    {
                        recovered = true;
                        break;
                    }

                    if (!_brain.UnitEntity.entityMotorComp.CheckIsMovingTo(_brain.blackboard.LastLeaveMoveModePos.Value))
                    {
                        _brain.UnitEntity.entityMotorComp.MoveTo(_brain.blackboard.LastLeaveMoveModePos.Value);
                    }

                    var diff = _brain.blackboard.LastLeaveMoveModePos.Value - _brain.UnitEntity.Pos;
                    if (diff.magnitude < 0.3f)
                    {
                        recovered = true;
                        break;
                    }
                }
                else
                {
                    recovered = true;
                    break;
                }
            }
            while (false);
            
            if(recovered)
            {
                _brain.UnitEntity.combatStateComp.CombatState = ECombatState.NotCombat;
            }
        }
    }

    [Serializable]
    public class AIActionNormalMoveDaemon : AIAction
    {
        public override string Name => "NormalMoveDaemon";

        public override float RateScore()
        {
            if(Status == AIActionStatus.Running)
            {
                return 0;
            }
            return 100;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _brain.blackboard.LastLeaveMoveModePos = null; ;
        }

        public override void OnExitState()
        {
            base.OnExitState();

            _brain.UnitEntity.entityMotorComp.StopMove();
            _brain.blackboard.LastLeaveMoveModePos = _brain.UnitEntity.Pos;
        }
    }


    [Serializable]
    public class AIActionMoveInPatrolGroup : AIAction
    {

        public override string Name => "MoveInPatrolGroup";

        public override float RateScore()
        {
            if(_brain.UnitEntity.MoveBehaveInfo.MoveBehaveMode != UnitMoveBehaveInfo.EMoveBehaveType.InPatrolGroup)
            {
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick()
        {
            if(!_brain.UnitEntity.entityMotorComp.CheckIsFollowTarget(_brain.UnitEntity.MoveBehaveInfo.FollowPatrolId))
            {
                var followedEntity = _brain.UnitEntity.LogicManager.GetLogicEntity(_brain.UnitEntity.MoveBehaveInfo.FollowPatrolId);
                _brain.UnitEntity.entityMotorComp.MoveFollow(followedEntity, 0.5f, _brain.UnitEntity.MoveBehaveInfo.PatrolGroupRelativePos);
            }
        }
    }




    [Serializable]
    public class AIActionMoveDoPath : AIAction
    {

        public override string Name => "MoveDoPath";

        private int _currPathIdx = 0;
        private Vector2? _currPathPoint;

        public override float RateScore()
        {
            if (_brain.UnitEntity.MoveBehaveInfo.MoveBehaveMode != UnitMoveBehaveInfo.EMoveBehaveType.MovePath)
            {
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick()
        {
            var pathName = _brain.UnitEntity.MoveBehaveInfo.MovePath;
            if(string.IsNullOrEmpty(pathName))
            {
                return;
            }

            var path = _brain.UnitEntity.LogicManager.AreaManager.GetRuntimePath(pathName);
            if(path == null)
            {
                return;
            }

            if(_currPathPoint == null)
            {
                if(_currPathIdx < path.PointList.Count - 1)
                {
                    var pVec = path.PointList[_currPathIdx + 1];
                    _currPathPoint = pVec;
                }
            }

            // 不断移动
            if(_currPathPoint != null && !_brain.UnitEntity.entityMotorComp.CheckIsMovingTo(_currPathPoint.Value))
            {
                _brain.UnitEntity.entityMotorComp.MoveTo(_currPathPoint.Value);
            }

            if(_currPathPoint != null)
            {
                var diff = _currPathPoint.Value - _brain.UnitEntity.Pos;
                if(diff.magnitude < 0.5f)
                {
                    _currPathPoint = null;
                    _currPathIdx += 1;
                }
            }

            //var followPos = followedEntity.Pos + new Vector2(_brain.UnitEntity.PatrolGroupRelativePos.x, _brain.UnitEntity.PatrolGroupRelativePos.y);

            //_brain.UnitEntity.entityMotorComp.MoveFollow(followedEntity, 0.5f, _brain.UnitEntity.PatrolGroupRelativePos);

            // _brain.UnitEntity.StartTargettedMove(BaseUnitLogicEntity.TargettedMoveIntent.ETargettedMoveType.FixPoint, null, followPos, 0.1f);
        }
    }

    [Serializable]
    public class AIActionMoveHunting : AIAction
    {
        public override string Name => "MoveHunting";


        private float _Timer;

        public override float RateScore()
        {
            if (_brain.UnitEntity.MoveBehaveInfo.MoveBehaveMode != UnitMoveBehaveInfo.EMoveBehaveType.Hunting)
            {
                return 0;
            }
            
            return 1;
        }

        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick()
        {

            if (LogicTime.time - _Timer < 1f)
            {
                return;
            }

            _Timer = LogicTime.time;

            var followedEntity = _brain.UnitEntity.LogicManager.playerLogicEntity;
            if (!_brain.UnitEntity.entityMotorComp.CheckIsFollowTarget(followedEntity.Id))
            {
                _brain.UnitEntity.entityMotorComp.MoveFollow(followedEntity, 0.5f, Vector2.zero, 0.35f);
            }
        }
    }

    [Serializable]
    public class AIActionCombatMain : AIAction
    {
        public override string Name => "CombatMain";

        public override float RateScore()
        {
            if (Status == AIActionStatus.Running)
            {
                return 0;
            }
            return 100;
        }

        public override void Tick()
        {
            if(_brain.UnitEntity.CombatState != ECombatState.InCombat)
            {
                return;
            }

            if (_brain.blackboard.LastLeaveMoveModePos != null)
            {
                if ((_brain.UnitEntity.Pos - _brain.blackboard.LastLeaveMoveModePos.Value).magnitude > _brain.brainConfig.ExitChasingRange)
                {
                    _brain.UnitEntity.combatStateComp.ExitCombat();
                }
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
        }

        public override void OnExitState()
        {
            base.OnEnterState();

            _brain.UnitEntity.entityMotorComp.StopMove();
        }
    }


    [Serializable]
    public class AIActionTryUseSkill : AIAction
    {

        public override string Name => "TryUseSkill";

        public float OverTimeLimit = 99f;
        private float _overTimer;
        private bool hasCastAbility = false;
        private bool isTryingCombo = false;

        private EntitySkillCfg? _config;

        public override float RateScore()
        {
            if (_brain.UnitEntity.CheckHasState(AttrIdConsts.ForbidOp))
            {
                return 0;
            }

            if(_brain.UnitEntity.unitCfg.IsPeace)
            {
                return 0;
            }

            if (!string.IsNullOrEmpty(_brain.blackboard.CurrIntentSkill))
            {
                return 0;
            }

            // 检查 有任意技能可使用
            var anyReady = _brain.UnitEntity.ablilityManager.CheckAnyReadySkill();
            if(anyReady)
            {
                return 10;
            }
            return 0;
        }

        public override void Start()
        {
            Status = AIActionStatus.Running;
            if(!string.IsNullOrEmpty(_brain.blackboard.CurrIntentSkill))
            {
                Debug.LogError("AIActionTryUseSkill confict occur old ability " + _brain.blackboard.CurrIntentSkill);
                Stop(AIActionStatus.Success);
                return;
            }

            var skills = _brain.UnitEntity.ablilityManager.GetAllReadySkills();

            if(skills.Count == 0)
            {
                Stop(AIActionStatus.Success);
                return;
            }
            skills.Sort((itemA, itemB) =>
            {
                if (itemA.cacheConfig.Priority != itemB.cacheConfig.Priority)
                {
                    return itemB.cacheConfig.Priority.CompareTo(itemA.cacheConfig.Priority);
                }
                return itemA.lastUseTime.CompareTo(itemB.lastUseTime);
            });

            var best = skills[0];

            var skillConf = SkillLibrary.GetSkillConfig(best.SkillName);
            // 更新状态
            _brain.blackboard.CurrIntentSkill = skillConf.SkillId;
            _overTimer = LogicTime.time + OverTimeLimit;
            hasCastAbility = false;
            _config = skillConf;

            var targetPos = _brain.Vision.ChoosePointAwayFromTarget(_brain.UnitEntity.Pos, _brain.PlayerEntity.Pos, best.cacheConfig.DesiredUseDistance);
            Debug.Log($"AIActionTryUseSkill move pos {targetPos}");
            _brain.UnitEntity.entityMotorComp.MoveTo(targetPos);
        }

        public override void Tick()
        {
            if (Status != AIActionStatus.Running) return;

            // 保底中断 避免卡在那里
            if (LogicTime.time > _overTimer)
            {
                Stop(AIActionStatus.Success);
                return;
            }

            if (_config == null)
            {
                return;
            }

            // 禁止操作时 跳出
            if (_brain.UnitEntity.CheckHasState(AttrIdConsts.ForbidOp))
            {
                Stop(AIActionStatus.Success);
                return;
            }

            // 未使用技能
            if (!hasCastAbility)
            {
                // 距离满足施法条件 使用
                if (_config.DesiredUseDistance > 0 && _brain.blackboard.Distance < _config.DesiredUseDistance)
                {
                    var dir = _brain.PlayerEntity.Pos - _brain.UnitEntity.Pos;

                    (Vector2? vec, long? targetId) = AIActionUtils.GetSkillUseParams(_config, _brain.UnitEntity);
                    _brain.UnitEntity.ablilityManager.UseSkill(_config.SkillId, castVec: vec, target: targetId != null ? _brain.UnitEntity.LogicManager.GetLogicEntity(targetId.Value, false) : null);

                    hasCastAbility = true;
                    return;
                }
                else
                {
                    _brain.UnitEntity.entityMotorComp.MoveTo(_brain.PlayerEntity.Pos, _config.DesiredUseDistance, 1.0f);
                }
            }
            // 正在使用技能
            else
            {
                // 继续等待actiona
                if(!_brain.UnitEntity.abilityController.IsActionable())
                {
                    return;
                }

                var trans = _brain.UnitEntity.ablilityManager.comboOrchestrator.GetPossibleTransition();
                // 不可接技能 跳出
                if (trans == null || trans.Count == 0)
                {
                    if (!_brain.UnitEntity.abilityController.IsRunning)
                    {
                        Stop(AIActionStatus.Success);
                    }
                    return;
                }

                Debug.Log($"AIActionTryUseSkill try to do derived");
                var firstTran = trans[0];
                var node = _brain.UnitEntity.ablilityManager.comboOrchestrator.GetComboNode(firstTran.toNodeId);

                var skillConf = SkillLibrary.GetSkillConfig(node.AbilityId);
                (Vector2? vec, long? targetId) = AIActionUtils.GetSkillUseParams(skillConf, _brain.UnitEntity);
                //_brain.UnitEntity.ablilityManager.GetCanDerive
                _brain.UnitEntity.ablilityManager.UseSkill(node.AbilityId, castVec: vec, target: targetId != null ? _brain.UnitEntity.LogicManager.GetLogicEntity(targetId.Value) : null);

                // 修改技能释放条件
                _brain.blackboard.CurrIntentSkill = skillConf.SkillId;
                _overTimer = LogicTime.time + OverTimeLimit;
                isTryingCombo = true;
                _config = skillConf;
            }
        }

        public override void Stop(AIActionStatus endStatus)
        {
            if (Status == AIActionStatus.Idle) return;
            Status = endStatus;

            _overTimer = 0;
            hasCastAbility = false;
            if(_brain.blackboard.CurrIntentSkill != null && _brain.blackboard.CurrIntentSkill == _config.SkillId)
            {
                _brain.blackboard.CurrIntentSkill = null;
            }
            _config = null;

            _brain.UnitEntity.entityMotorComp.StopMove();
        }

        public override void OnExitState()
        {
            base.OnExitState();
            _overTimer = 0;
            hasCastAbility = false;
            if (_brain.blackboard.CurrIntentSkill != null && _brain.blackboard.CurrIntentSkill == _config.SkillId)
            {
                _brain.blackboard.CurrIntentSkill = null;
            }
            _config = null;
        }

        public override bool CanInterrupt(string reason, bool hard) => false;
    }

    [Serializable]
    public class AIActionDistanceControl : AIAction
    {

        public override string Name => "DistanceControl";

        // 参数列表
        public float goodDistance;
        public float goodDiff;

        private float _Timer;
        private float _lastSlowTime;


        public override float RateScore()
        {
            if (_brain.UnitEntity.CheckHasState(AttrIdConsts.Unmovable))
            {
                return 0;
            }

            if (_brain.UnitEntity.unitCfg.IsPeace)
            {
                return 0;
            }

            if (!string.IsNullOrEmpty(_brain.blackboard.CurrIntentSkill))
            {
                return 0;
            }
 
            return 1;
        }

        public override void Start()
        {
            base.Start();

        }

        public override void Tick()
        {

            if (_brain.UnitEntity.CheckHasState(AttrIdConsts.Unmovable))
            {
                Stop(AIActionStatus.Success);
                return;
            }

            if (!string.IsNullOrEmpty(_brain.blackboard.CurrIntentSkill))
            {
                Stop(AIActionStatus.Success);
                return;
            }

            
            if (LogicTime.time - _Timer < 0.2f)
            {
                return;
            }

            _Timer = LogicTime.time;

            var targetEntity = _brain.UnitEntity.LogicManager.playerLogicEntity;
            _brain.UnitEntity.entityMotorComp.MoveFollow(_brain.PlayerEntity, 0.3f, Vector2.zero, 1.0f, moveSpeedRate: 0.3f);

        }

        public override void Stop(AIActionStatus endStatus)
        {
            base.Stop(endStatus);
        }

        public override bool CanInterrupt(string reason, bool hard) => true;

    }

    [Serializable]
    public class AIActionCombatQuickCloser : AIAction
    {

        public override string Name => "QuickCloser";

        private float _Timer;


        public override float RateScore()
        {
            if (_brain.UnitEntity.CheckHasState(AttrIdConsts.Unmovable))
            {
                return 0;
            }

            if (!string.IsNullOrEmpty(_brain.blackboard.CurrIntentSkill))
            {
                return 0;
            }

            if (_brain.blackboard.Distance > _brain.brainConfig.GoodBattleDistance + 1.0f)
            {
                return 10;
            }

            return 0;
        }

        public override void Start()
        {
            base.Start();

            _brain.UnitEntity.entityMotorComp.MoveTo(_brain.PlayerEntity.Pos, _brain.brainConfig.GoodBattleDistance);
        }

        public override void Tick()
        {
            if (_brain.UnitEntity.CheckHasState(AttrIdConsts.Unmovable))
            {
                Stop(AIActionStatus.Success);
                return;
            }

            if (!string.IsNullOrEmpty(_brain.blackboard.CurrIntentSkill))
            {
                Stop(AIActionStatus.Success);
                return;
            }

            if (_brain.blackboard.Distance < _brain.brainConfig.GoodBattleDistance)
            {
                Stop(AIActionStatus.Success);
                return;
            }

            var targetEntity = _brain.UnitEntity.LogicManager.playerLogicEntity;
            //var targetPos = _brain.Vision.ChoosePointAwayFromTarget(_brain.UnitEntity.Pos + UnityEngine.Random.insideUnitCircle * 0.3f, _brain.PlayerEntity.Pos, goodDistance);
            _brain.UnitEntity.entityMotorComp.MoveTo(_brain.PlayerEntity.Pos, _brain.brainConfig.GoodBattleDistance);
        }

        public override void Stop(AIActionStatus endStatus)
        {
            base.Stop(endStatus);
        }

        public override bool CanInterrupt(string reason, bool hard) => true;

    }


    [Serializable]
    public class AIActionAttractedBehave : AIAction
    {
        public override string Name => "AttractedBehave";

        public override float RateScore()
        {
            if (_brain.UnitEntity.attractInfo == null)
            {
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick()
        {
            if (_brain.UnitEntity.attractInfo == null || LogicTime.time - _brain.UnitEntity.attractInfo.LastTriggerTime > 15.0f)
            {
                Stop(AIActionStatus.Interrupted);
                return;
            }

            //_brain.UnitEntity.entityMotorComp.MoveTo(_currAttractePos);
        }
    }


    [Serializable]
    public class AIActionAttractedMove : AIAction
    {

        public override string Name => "AttractedMove";


        public float StayDuration = 5.0f;
        public float WatchDistance = 0.8f;

        private Vector2 _currAttractePos;
        private float _attarctLastTriggerTime;


        public override void OnEnterState()
        {
            base.OnEnterState();

            //_brain.blackboard.LastInterruptMovePos = _brain.UnitEntity.Pos;
        }

        public override float RateScore()
        {
            return 10;
        }


        public override void Start()
        {
            base.Start();

            // 进入时赋值
            if(_brain.UnitEntity.attractInfo != null)
            {
                _currAttractePos = _brain.UnitEntity.attractInfo.Pos;
                _attarctLastTriggerTime = _brain.UnitEntity.attractInfo.LastTriggerTime;
                _brain.blackboard.CanLeaveAttract = false;
                _brain.UnitEntity.entityMotorComp.StopMove();
            }
            else
            {
                _brain.blackboard.CanLeaveAttract = true;
            }
        }

        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick()
        {
            // 如果当前有吸引事件 持续更新信息
            if (_brain.UnitEntity.attractInfo != null)
            {
                // 更新触发事件
                if (_brain.UnitEntity.attractInfo.LastTriggerTime != _attarctLastTriggerTime)
                {
                    _attarctLastTriggerTime = _brain.UnitEntity.attractInfo.LastTriggerTime;
                }

                // 尝试启动首次寻路 或改变目标寻路
                if (_brain.UnitEntity.attractInfo.Pos != _currAttractePos)
                {
                    _currAttractePos = _brain.UnitEntity.attractInfo.Pos;
                    Debug.Log("init or change attracted pos " + _brain.UnitEntity.attractInfo.Pos + (_brain.UnitEntity.attractInfo.AttractSource != null ? _brain.UnitEntity.attractInfo.AttractSource.Id : "0"));

                    var diff = _brain.UnitEntity.Pos - _currAttractePos;
                    if(diff.magnitude > WatchDistance)
                    {
                        var watchPos = _brain.Vision.ChoosePointAwayFromTarget(_brain.UnitEntity.Pos, _currAttractePos, WatchDistance);
                        _brain.UnitEntity.entityMotorComp.MoveTo(watchPos, moveSpeedRate: 0.1f);
                    }
                }
            }

            // 待购时间退出
            if(LogicTime.time - _attarctLastTriggerTime > StayDuration)
            {
                _brain.blackboard.CanLeaveAttract = true;
            }

        }

        public override void OnExitState()
        {
            base.OnExitState();
        }
    }
}

