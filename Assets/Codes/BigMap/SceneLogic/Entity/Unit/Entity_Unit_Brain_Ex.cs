

using Config.Unit;
using My.Map.Entity;
using My.Map.Fight;
using UnityEngine;

namespace My.Map.Unit
{

    public static class AIActionUtils
    {
        public static (Vector2?, long?) GetSkillUseParams(EntitySkillCfg skillConf, NpcUnitLogicEntity caster)
        {
            //
            switch (skillConf.SelectPolicy)
            {
                case FightStruct.ESelectPolicy.None:
                    {
                        return (null, null);
                    }
                    break;
                case FightStruct.ESelectPolicy.PrimaryTarget:
                    {
                        var target = caster.LogicManager.GetLogicEntity(caster.AggroSystem.CurrentTargetId, false);
                        if (target == null)
                        {
                            Debug.LogError("GetSkillUseParams not found primary target");
                            return (caster.Pos + caster.FinalLook, null);
                        }
                        return (target.Pos, target.Id);
                    }
                    break;
                case FightStruct.ESelectPolicy.Random:
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
        }
    }

    // 策略接口
    public interface IIdlePolicy
    {
        void OnEnter(AIBrainV2 brain);
        void OnTick(AIBrainV2 brain, float dt);
        void OnExit(AIBrainV2 brain);
    }

    // 简单的工厂类
    public static class MovePolicyFactory
    {
        public static IIdlePolicy Create(IdleType type)
        {
            switch (type)
            {
                case IdleType.Patrol: return new Policy_Patrol();
                case IdleType.StandStill: return new Policy_StandStill();
                case IdleType.Wander: return new Policy_Wander();
                default: return new Policy_StandStill();
            }
        }
    }

    // --- 具体策略实现 ---

    // 1. 原地站立策略
    public class Policy_StandStill : IIdlePolicy
    {
        public void OnEnter(AIBrainV2 brain)
        {
            brain.NpcEntity.StopMove();
        }

        public void OnTick(AIBrainV2 brain, float dt)
        {
            // 可以加一些随机转头逻辑
        }

        public void OnExit(AIBrainV2 brain) { }
    }

    // 1. 原地站立策略
    public class Policy_Wander : IIdlePolicy
    {

        private float _wanderTimer;
        private Vector2? _currWanderPoint = null;
        public void OnEnter(AIBrainV2 brain)
        {
            brain.NpcEntity.StopMove();
        }

        public void OnTick(AIBrainV2 brain, float dt)
        {
        }

        protected void TickWanderPoint(AIBrainV2 brain)
        {
            if(LogicTime.time - _wanderTimer < brain.Config.WanderInterval)
            {
                return;
            }

            _wanderTimer = LogicTime.time;

            Vector2 wandarOrg = brain.HomePos == null ? brain.NpcEntity.Pos : brain.HomePos.Value;
            _currWanderPoint = UnityEngine.Random.insideUnitCircle * 1.0f + wandarOrg;

            brain.NpcEntity.TryMoveTo(_currWanderPoint.Value);
        }


        public void OnExit(AIBrainV2 brain) { }
    }


    // 2. 巡逻策略
    public class Policy_Patrol : IIdlePolicy
    {
        private int _index = 0;

        public void OnEnter(AIBrainV2 brain)
        {
            MoveToNext(brain);
        }

        public void OnTick(AIBrainV2 brain, float dt)
        {
            var points = brain.Config.PatrolPoints;
            if (points == null || points.Count == 0) return;

            Vector3 target = points[_index];

            // 简单判断是否到达
            if (Vector3.Distance(brain.NpcEntity.Pos, target) < 0.5f)
            {
                // 到了，去下一个点
                _index = (_index + 1) % points.Count;
                MoveToNext(brain);
            }
        }

        public void OnExit(AIBrainV2 brain)
        {
            brain.NpcEntity.StopMove();
        }

        private void MoveToNext(AIBrainV2 brain)
        {
            var points = brain.Config.PatrolPoints;
            if (points != null && points.Count > 0)
            {
                brain.NpcEntity.TryMoveTo(points[_index]);
            }
        }
    }

    // --- Idle 状态 ---
    public class AIStateIdle : AIBaseState
    {
        private IIdlePolicy _idlePolicy;

        public override string StateName => "Idle";

        public AIStateIdle(AIBrainV2 brain) : base(brain)
        {
            // 从配置创建策略
            _idlePolicy = MovePolicyFactory.Create(brain.Config.IdleType);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _idlePolicy.OnEnter(_brain);
        }

        public override void OnUpdate()
        {
            if (_brain.Aggro.HasHostile)
            {
                if(_brain.Config.IsPeace)
                {
                    _brain.ChangeState(_brain.StateFlee);
                }
                else
                {
                    _brain.ChangeState(_brain.StateCombat);
                }
                return;
            }

            if (_brain.SuspiciousPos != Vector3.zero)
            {

            }

            // 3. 执行闲置策略
            _idlePolicy.OnTick(_brain, Time.deltaTime);
        }

        public override void OnExit()
        {
            base.OnExit();
            _idlePolicy.OnExit(_brain);
            _brain.HomePos = _brain.NpcEntity.Pos; // 更新复位坐标点
        }
    }

    // --- Idle 状态 ---
    public class AIStateAttracted : AIBaseState
    {
        public override string StateName => "Attracted";

        private float attractDuration = 0;

        public AIStateAttracted(AIBrainV2 brain) : base(brain)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

        }

        public override void OnUpdate()
        {
            if(_brain.LatestAttrctInfo.)

            if (LogicTime.time - lastAttractTime > 3.0f)
            {
                _brain.blackboard.CanLeaveAttract = true;
                return;
            }
            PlayerLogicEntity srcPlayer = null;

            _brain.blackboard.CurrentAttractLevel = 0;
            if (_brain.blackboard.AttractSrcId != 0)
            {
                srcPlayer = _brain.NpcEntity.LogicManager.GetLogicEntity(_brain.blackboard.AttractSrcId) as PlayerLogicEntity;
                if (srcPlayer != null)
                {
                    _brain.blackboard.CurrentAttractLevel = srcPlayer.GetAttractLevel();
                }
            }

            // 进行移动
            if (_brain.blackboard.CurrentAttractLevel >= 3)
            {
                // 2级以上 
                if (srcPlayer != null)
                {
                    _brain.NpcEntity.entityMotorComp.TryMoveTo(srcPlayer.Pos, moveSpeedRate: 0.9f);
                    _brain.NpcEntity.LogicManager.viewer.ShowFakeFxEffect("attract fast", _brain.NpcEntity.Pos);
                }
            }
            else if (_brain.blackboard.CurrentAttractLevel >= 2)
            {
                // 2级以上 
                if (srcPlayer != null)
                {
                    _brain.NpcEntity.entityMotorComp.TryMoveTo(srcPlayer.Pos, moveSpeedRate: 0.1f);
                    _brain.NpcEntity.LogicManager.viewer.ShowFakeFxEffect("attract slow", _brain.NpcEntity.Pos);
                }
            }
            else
            {
                _brain.NpcEntity.entityMotorComp.StopMove();
            }

            // 条件满足时执行揩油
            if (_brain.blackboard.CurrentAttractLevel >= 2 && _brain.NpcEntity.abilityController.IsActionable())
            {
                if (srcPlayer != null && !srcPlayer.CheckHasState(AttrIdConsts.ImmumeKaiYou))
                {
                    var diff = srcPlayer.Pos - _brain.NpcEntity.Pos;
                    if (diff.magnitude < 0.5f)
                    {
                        _brain.NpcEntity.abilityController.TryUseAbility("close_kaiyou", target: srcPlayer);
                    }
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            _idlePolicy.OnExit(_brain);
            _brain.HomePos = _brain.NpcEntity.Pos; // 更新复位坐标点
        }
    }


    public class AIStateCombat : AIBaseState
    {
        private float _attackTimer; // 攻击冷却计时器
        //private long _currentTarget; // 缓存当前目标 (防止一帧内变化)
        private BaseUnitLogicEntity _currentTarget;

        public float OverTimeLimit = 15f;

        private bool hasCastAbility;
        private EntitySkillCfg? currIntentSkillCfg;
        private float castOverTimer;
        private string currComboAbilityName;

        public override string StateName => "Combat";

        public AIStateCombat(AIBrainV2 brain) : base(brain)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _attackTimer = 1f; // 刚进入战斗通常可以立即攻击，或者根据设计设为 0.5f 延迟
            _brain.NpcEntity.StopMove(); // 先停一下，重新评估路径

            // 播放战斗姿态动画
            // _brain.NpcEntity.PlayAnim("BattleStance");
        }

        public override void OnUpdate()
        {
            // --- 1. 获取目标 (数据验证) ---
            var targetId = _brain.Aggro.CurrentTargetId;

            var entity = _brain.LogicManager.GetLogicEntity(targetId);
            _currentTarget = entity as BaseUnitLogicEntity;
            // 如果仇恨列表空了，或者目标销毁了
            if (_currentTarget == null)
            {
                HandleTargetLost();
                return;
            }


            float distToTarget = Vector3.Distance(_brain.NpcEntity.Pos, _currentTarget.Pos);
            if (distToTarget > _brain.Config.ChaseRange)
            {
                // 放弃追击，清除仇恨，回家
                _brain.Aggro.ClearTarget();
                _brain.ChangeState(_brain.StateReturn);
                return;
            }

            // --- 2. 自身状态检测 (生存本能) ---
            // 假设 Entity 有 HP 属性，低于 20% 逃跑
            /* 
            if (_brain.NpcEntity.HPPercentage < 0.2f)
            {
                _brain.ChangeState(_brain.StateFlee);
                return;
            }
            */

            // 检查是否要中止使用技能
            ChecCanCastSkill();
            TickCastSkill(distToTarget);

            // 使用技能视图接近
            if (currIntentSkillCfg == null)
            {
                // 超过远距离
                if (_brain.Config.CombatFarDistance > 0 && distToTarget > _brain.Config.CombatFarDistance)
                {
                    // 快速移动
                    _brain.NpcEntity.TryMoveTo(_currentTarget.Pos);
                }
                // 低于最近距离
                else if (_brain.Config.CombatCloseDistance > 0 && distToTarget <= _brain.Config.CombatCloseDistance)
                {
                    var diff = _brain.NpcEntity.Pos - _currentTarget.Pos;
                    _brain.NpcEntity.TryMoveTo(_brain.NpcEntity.Pos + (diff.normalized) * 0.5f, moveSpeedRate: 0.5f);
                }
                else
                {
                    Debug.Log("DistanceControl TryMoveTo player");
                    var diff = _currentTarget.Pos - _brain.NpcEntity.Pos;
                    // 计算切线方向 (左手定则或右手定则)
                    Vector2 tangentDir = new Vector3(-diff.y, diff.x);
                    // 根据时间计算偏移量 (-1 到 1 之间波动)
                    float sineValue = Mathf.Sin(LogicTime.time * 1.0f + 0.0f);
                    var _strafeAmplitude = 0.5f;

                    // 最终目标点 = 槽位中心 + 切线方向偏移
                    _brain.NpcEntity.TryMoveTo(_brain.NpcEntity.Pos + (tangentDir * sineValue * _strafeAmplitude), moveSpeedRate: 0.25f);
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            _brain.NpcEntity.StopMove(); // 退出战斗时刹车
        }

        private void ChecCanCastSkill()
        {

            if(currIntentSkillCfg == null)
            {
                return;
            }

            bool stopSkill = false;
            do
            {
                if (_currentTarget == null)
                {
                    stopSkill = true;
                    break;
                }

                // 禁止操作时 跳出
                if (_brain.NpcEntity.CheckHasState(AttrIdConsts.ForbidSkillOp))
                {
                    stopSkill = true;
                    break;
                }

                if (_brain.NpcEntity.IsTargetInvisibleFromSelf(_currentTarget.Id))
                {
                    stopSkill = true;
                    break;
                }
            }
            while (false);
            
            if(stopSkill)
            {
                currIntentSkillCfg = null;
                _brain.NpcEntity.StopMove();
            }
        }


        /// <summary>
        /// 检查使用技能
        /// </summary>
        private void TickCastSkill(float dist)
        {

            if (currIntentSkillCfg == null)
            {
                var anyReady = _brain.NpcEntity.ablilityManager.CheckAnyReadySkill();
                if (!anyReady)
                {
                    return;
                }

                var skills = _brain.NpcEntity.ablilityManager.GetAllReadySkills();

                if (skills.Count == 0)
                {
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

                var skillCfg = SkillLibrary.GetSkillConfig(best.SkillName);
                if(skillCfg == null)
                {
                    return;
                }
                castOverTimer = LogicTime.time + OverTimeLimit;
                hasCastAbility = false;
                currComboAbilityName = string.Empty;

                currIntentSkillCfg = skillCfg;

                var targetPos = _brain.Vision.ChoosePointAwayFromTarget(_brain.NpcEntity.Pos, _currentTarget?.Pos ??_brain.NpcEntity.Pos, best.cacheConfig.DesiredUseDistance);
                Debug.Log($"ChecCastSkill move pos {targetPos}");
                _brain.NpcEntity.TryMoveTo(targetPos);
            }
            else
            {
                // 未使用技能
                if (!hasCastAbility)
                {
                    // 距离满足施法条件 使用
                    if (currIntentSkillCfg.DesiredUseDistance == 0 || dist < currIntentSkillCfg.DesiredUseDistance)
                    {
                        var dir = _currentTarget.Pos - _brain.NpcEntity.Pos;

                        (Vector2? vec, long? targetId) = AIActionUtils.GetSkillUseParams(currIntentSkillCfg, _brain.NpcEntity);
                        _brain.NpcEntity.ablilityManager.UseSkill(currIntentSkillCfg.SkillId, castVec: vec, target: targetId != null ? _brain.NpcEntity.LogicManager.GetLogicEntity(targetId.Value, false) : null);

                        hasCastAbility = true;
                        return;
                    }
                    else
                    {
                        _brain.NpcEntity.TryMoveTo(_currentTarget.Pos, currIntentSkillCfg.DesiredUseDistance, 1.2f);
                    }
                }
                // 正在使用技能
                else
                {
                    // 继续等待actiona
                    if (!_brain.NpcEntity.abilityController.IsActionable())
                    {
                        return;
                    }

                    var trans = _brain.NpcEntity.ablilityManager.comboOrchestrator.GetPossibleTransition();
                    // 不可接技能 跳出
                    if (trans == null || trans.Count == 0)
                    {
                        if (!_brain.NpcEntity.abilityController.IsRunning)
                        {
                            //Stop(AIActionStatus.Success);
                            currIntentSkillCfg = null;
                        }
                        return;
                    }

                    var firstTran = trans[0];
                    var node = _brain.NpcEntity.ablilityManager.comboOrchestrator.GetComboNode(firstTran.toNodeId);

                    //_brain.NpcEntity.ForceSetFaceTarget(_brain.NpcEntity.DesiredFaceDir, false);

                    // 一定无目标参数
                    if (_brain.NpcEntity.ablilityManager.UseSkill(firstTran.triggerInput.SkillId))
                    {
                        // 修改技能释放条件
                        castOverTimer = LogicTime.time + OverTimeLimit;
                        currComboAbilityName = node.AbilityId;
                    }
                    Debug.Log($"AIActionTryUseSkill try to do derived " + currComboAbilityName);
                }
            }

        }

        private void HandleTargetLost()
        {
            // 关键逻辑：连接 Search 状态
            // 尝试获取目标"最后一次出现的位置"
            Vector2? lastKnownPos = _brain.Aggro.LastKnownTargetPos;

            if (lastKnownPos != null)
            {
                // 1. 记录可疑位置到黑板
                _brain.SuspiciousPos = lastKnownPos.Value;
                // 2. 切换到搜索状态
                //_brain.ChangeState(_brain.StateSearch);
                _brain.ChangeState(_brain.StateReturn);
            }
            else
            {
                // 完全没头绪，只能回家
                _brain.ChangeState(_brain.StateReturn);
            }
        }
    }

    // --- Return 状态 ---
    public class AIStateReturn : AIBaseState
    {

        public AIStateReturn(AIBrainV2 brain) : base(brain) {  }

        public override string StateName => "Return";

        private float _homeLessMinStayTime = 5.0f;

        public override void OnEnter()
        {
            base.OnEnter();
            if(_brain.HomePos != null)
            {
                _brain.NpcEntity.TryMoveTo(_brain.HomePos.Value, moveSpeedRate: 0.7f);
            }
        }

        public override void OnUpdate()
        {
            if (_brain.Aggro.HasHostile)
            {
                // 回家路上被打，是否反击？看设计。这里假设继续跑。
            }

            if (_brain.HomePos != null)
            {
                if (Vector3.Distance(_brain.NpcEntity.Pos, _brain.HomePos.Value) < 0.5f)
                {
                    // 到家了，切回 Idle
                    _brain.ChangeState(_brain.StateIdle);
                    return;
                }

                // 防卡死：定期重设路径
                if (Time.frameCount % 60 == 0)
                {
                    _brain.NpcEntity.TryMoveTo(_brain.HomePos.Value, moveSpeedRate: 0.7f);
                }
            }
            else
            {
                // 无家可归 
                if (Duration > _homeLessMinStayTime)
                {
                    _brain.ChangeState(_brain.StateIdle);
                }
            }

        }
    }

    // --- Flee 状态 ---
    public class AIStateFlee : AIBaseState
    {
        public AIStateFlee(AIBrainV2 brain) : base(brain) {  }

        public override string StateName => "Flee";

        public override void OnEnter()
        {
            base.OnEnter();

            _brain.NpcEntity.LogicManager.viewer.ShowMapSpeachBubble(_brain.NpcEntity.Id, "我逃", 2f);
            // _brain.NpcEntity.PlayAnimation("Panic");
            //_brain.Aggro.ClearGridSignal();
        }

        public override void OnUpdate()
        {
            // 如果没有敌人了，或者已经跑得足够远且血量恢复了
            if (!_brain.Aggro.HasHostile && !_brain.NpcEntity.IsEvilAlert)
            {
                _brain.ChangeState(_brain.StateReturn); // 逃完回家
                return;
            }

            // 简单的反向移动逻辑
            // 假设 Aggro 系统能给出一个 "所有敌人的重心位置" EnemyCenter
            //Vector3 enemyPos = _brain.Aggro.GetNearestHostilePos();
            Vector2? enemyPos = _brain.Aggro.LastKnownTargetPos;
            Vector2 runDir;
            if (enemyPos != null)
            {
                runDir = (_brain.NpcEntity.Pos - enemyPos.Value).normalized;
            }
            else
            {
                runDir = UnityEngine.Random.insideUnitCircle.normalized;
            }
            Vector2 dest = _brain.NpcEntity.Pos + runDir * 5.0f;

            _brain.NpcEntity.TryMoveTo(dest);
        }
    }

    public class AIStateSearch : AIBaseState
    {
        // 搜索阶段
        private enum SearchPhase { MovingToPos, LookingAround }
        private SearchPhase _phase;
        private float _lookAroundTimer;
        private Vector2 searchOrgPoint = Vector2.zero;

        public AIStateSearch(AIBrainV2 brain) : base(brain)
        {
        }

        public override string StateName => "Search";

        public override void OnEnter()
        {
            base.OnEnter();

            // 1. 开始阶段：前往可疑点
            _phase = SearchPhase.MovingToPos;

            if (_brain.SuspiciousPos == null)
            {
                searchOrgPoint = _brain.NpcEntity.Pos;
            }
            else
            {
                searchOrgPoint = _brain.SuspiciousPos.Value;
            }
            
            _brain.NpcEntity.TryMoveTo(searchOrgPoint);
        }

        public override void OnUpdate()
        {
            // --- 任何时候发现真敌人，切战斗 ---
            if (_brain.Aggro.HasHostile)
            {
                _brain.ChangeState(_brain.StateCombat);
                return;
            }

            switch (_phase)
            {
                case SearchPhase.MovingToPos:
                    // 检测是否到达可疑点
                    if (Vector3.Distance(_brain.NpcEntity.Pos, searchOrgPoint) < 1.0f)
                    {
                        // 到达，开始四处张望
                        _phase = SearchPhase.LookingAround;
                        _lookAroundTimer = LogicTime.time + _brain.Config.SearchDuration;
                        _brain.NpcEntity.StopMove();
                    }

                    // 超时强制结束 (防止路不通一直走)
                    if (Duration > 10.0f) _brain.ChangeState(_brain.StateReturn);
                    break;

                case SearchPhase.LookingAround:
                    // 倒计时

                    // 这里可以加逻辑：每秒随机转个身
                    // RotateToRandomDir();

                    if (LogicTime.time > _lookAroundTimer)
                    {
                        // 搜完了没结果，放弃，回家
                        _brain.SuspiciousPos = null; // 清除信号
                        _brain.ChangeState(_brain.StateReturn);
                    }
                    break;
            }
        }
    }
}