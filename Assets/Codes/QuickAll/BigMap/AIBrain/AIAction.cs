using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Map.Entity.AI.Action;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static My.Map.Entity.MapEntityAbilityController;

namespace My.Map.Entity.AI
{

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

        public string Label;
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
        public string DoNothing;
        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick()
        {

        }
    }

    [Serializable]
    public class AIActionFollowPatrolGroup : AIAction
    {

        private float _followTimer;

        public override float RateScore()
        {
            if(_brain.UnitEntity.MoveActMode != BaseUnitLogicEntity.EUnitMoveActMode.PatrolFollow)
            {
                return 0;
            }
            if (_brain.UnitEntity.IsInBattle)
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
            if(_brain.UnitEntity.IsInBattle)
            {
                Stop(AIActionStatus.Success);
                return;
            }

            if (LogicTime.time - _followTimer < 0.3f)
            {
                return;
            }

            _followTimer = LogicTime.time;

            var followedEntity = _brain.UnitEntity.LogicManager.GetLogicEntity(_brain.UnitEntity.FollowPatrolId);
            var followPos = followedEntity.Pos + new Vector2(_brain.UnitEntity.PatrolGroupRelativePos.x, _brain.UnitEntity.PatrolGroupRelativePos.y);

            if (_brain.UnitEntity.targettedMoveIntent != null)
            {
                if ((_brain.UnitEntity.targettedMoveIntent.MoveTarget - followPos).magnitude < 0.2f)
                {
                    return;
                }
            }

            _brain.UnitEntity.StartTargettedMove(followPos, 0.3f);
        }
    }

    [Serializable]
    public class AIActionHuntingPlayer : AIAction
    {

        private float _Timer;

        public override float RateScore()
        {
            if (_brain.UnitEntity.MoveActMode != BaseUnitLogicEntity.EUnitMoveActMode.Hunting)
            {
                return 0;
            }
            if (_brain.UnitEntity.IsInBattle)
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
            if (_brain.UnitEntity.IsInBattle)
            {
                Stop(AIActionStatus.Success);
                return;
            }

            if (LogicTime.time - _Timer < 0.3f)
            {
                return;
            }

            _Timer = LogicTime.time;

            var followedEntity = _brain.UnitEntity.LogicManager.playerLogicEntity;
            var followPos = followedEntity.Pos + new Vector2(0.5f, 0.5f);

            if (_brain.UnitEntity.targettedMoveIntent != null)
            {
                if ((_brain.UnitEntity.targettedMoveIntent.MoveTarget - followPos).magnitude < 0.2f)
                {
                    return;
                }
            }

            _brain.UnitEntity.StartTargettedMove(followPos, 0.3f);
        }
    }


    [Serializable]
    public class AIActionTryUseSkill : AIAction
    {

        private float _overTimer;
        private bool hasCastAbility = false;
        private MapAbilitySpecConfig? _config;

        public override float RateScore()
        {
            if(!_brain.UnitEntity.IsInBattle)
            {
                return 0;
            }
            
            // 检查 有任意技能可使用
            var anyReady = _brain.UnitEntity.abilityController.CheckAnyReadyAbility();
            if(anyReady)
            {
                return 10;
            }
            return 0;
        }

        public override void Start()
        {
            Status = AIActionStatus.Running;
            if(!string.IsNullOrEmpty(_brain.blackboard.CurrIntentAbility))
            {
                Debug.LogError("AIActionTryUseSkill confict occur old ability " + _brain.blackboard.CurrIntentAbility);
                Stop(AIActionStatus.Success);
                return;
            }

            var skills = _brain.UnitEntity.abilityController.GetAllReadyAbilities();

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

            // 更新状态
            _brain.blackboard.CurrIntentAbility = best.AbilityName;
            _overTimer = LogicTime.time + 5.0f;
            hasCastAbility = false;
            _config = best.cacheConfig;

            var targetPos = _brain.Vision.ChoosePointAwayFromTarget(_brain.UnitEntity.Pos, _brain.PlayerEntity.Pos, best.cacheConfig.DesiredUseDistance);
            Debug.Log($"AIActionTryUseSkill move pos {targetPos}");
            _brain.UnitEntity.StartTargettedMove(targetPos, 0.1f);
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

            // 未使用技能
            if (!hasCastAbility)
            {
                // 距离满足施法条件 使用
                if (_config.DesiredUseDistance > 0 && _brain.blackboard.Distance < _config.DesiredUseDistance * 1.1f)
                {
                    var dir = _brain.PlayerEntity.Pos - _brain.UnitEntity.Pos;
                    _brain.UnitEntity.abilityController.TryUseAbility(_config.Id, dir);
                    hasCastAbility = true;
                    return;
                }
                else
                {
                    // 玩家位置偏移 需要更新位置
                    bool needReMove = false;
                    if (_brain.UnitEntity.targettedMoveIntent == null)
                    {
                        needReMove = true;
                    }
                    else
                    {
                        var dir = _brain.PlayerEntity.Pos - _brain.UnitEntity.targettedMoveIntent.MoveTarget;
                        // 如果玩家位置与目标移动位置 大于理想距离了 需要重新调整移动位置
                        if (dir.sqrMagnitude > _config.DesiredUseDistance * _config.DesiredUseDistance)
                        {
                            needReMove = true;
                        }
                    }

                    if (needReMove)
                    {
                        var targetPos = _brain.Vision.ChoosePointAwayFromTarget(_brain.UnitEntity.Pos, _brain.PlayerEntity.Pos, _config.DesiredUseDistance);
                        Debug.Log($"AIActionTryUseSkill remove move pos {targetPos}");
                        _brain.UnitEntity.StartTargettedMove(targetPos, 0.1f);
                        return;
                    }
                }
            }
            // 正在使用技能 等待技能结束
            else
            {
                if (!_brain.UnitEntity.abilityController.IsRunning)
                {
                    Stop(AIActionStatus.Success);
                }
            }
        }

        public override void Stop(AIActionStatus endStatus)
        {
            if (Status == AIActionStatus.Idle) return;
            Status = endStatus;

            _overTimer = 0;
            hasCastAbility = false;
            if(_brain.blackboard.CurrIntentAbility != null && _brain.blackboard.CurrIntentAbility == _config.Id)
            {
                _brain.blackboard.CurrIntentAbility = null;
            }
            _config = null;
        }

        public override bool CanInterrupt(string reason, bool hard) => false;
    }

    [Serializable]
    public class AIActionDistanceControl : AIAction
    {

        // 参数列表
        public float goodDistance;
        public float goodDiff;

        private float _Timer;


        public override float RateScore()
        {
            if(!_brain.UnitEntity.IsInBattle)
            {
                return 0;
            }

            if(!string.IsNullOrEmpty(_brain.blackboard.CurrIntentAbility))
            {
                return 0;
            }

            if (_brain.blackboard.Distance > goodDistance * 1.1f)
            {
                return 10;
            }

            if (_brain.blackboard.Distance < goodDistance * 0.9f)
            {
                return 1;
            }

            return 1;
        }

        public override void Start()
        {
            base.Start();

            var targetPos = _brain.Vision.ChoosePointAwayFromTarget(_brain.UnitEntity.Pos, _brain.PlayerEntity.Pos, goodDistance);
            _brain.UnitEntity.StartTargettedMove(targetPos, 0.1f);
        }

        public override void Tick()
        {
            if (_brain.blackboard.Distance < goodDistance * 1.1f && _brain.blackboard.Distance > goodDistance * 0.9f)
            {
                Stop(AIActionStatus.Success);
                return;
            }

            if (LogicTime.time - _Timer < 0.3f)
            {
                return;
            }

            _Timer = LogicTime.time;

            var targetEntity = _brain.UnitEntity.LogicManager.playerLogicEntity;

            var targetPos = _brain.Vision.ChoosePointAwayFromTarget(_brain.UnitEntity.Pos + UnityEngine.Random.insideUnitCircle * 0.3f, _brain.PlayerEntity.Pos, goodDistance);
            if (_brain.UnitEntity.targettedMoveIntent != null)
            {
                if ((_brain.UnitEntity.targettedMoveIntent.MoveTarget - targetPos).magnitude < 0.2f)
                {
                    return;
                }
            }

            _brain.UnitEntity.StartTargettedMove(targetPos, 0.1f);
        }

        public override void Stop(AIActionStatus endStatus)
        {

        }

        public override bool CanInterrupt(string reason, bool hard) => true;

    }
}

