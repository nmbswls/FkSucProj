

using System.Collections.Generic;
using Config.Unit;
using My.Map.Entity;
using My.Map.Fight;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static My.Map.BaseUnitLogicEntity;

namespace My.Map.Unit
{

    public static class AIActionUtils
    {
        public static (Vector2?, ILogicEntity?) GetSkillCastParams(MapAbilitySpecConfig abilityCfg, NpcUnitLogicEntity caster, long policyTargetId)
        {
            //
            switch (abilityCfg.CastType)
            {
                case MapAbilitySpecConfig.ECastType.NoTarget:
                    {
                        return (null, null);
                    }
                    break;
                case MapAbilitySpecConfig.ECastType.Point:
                case MapAbilitySpecConfig.ECastType.Circle:
                case MapAbilitySpecConfig.ECastType.Directional:
                    {
                        var target = caster.LogicManager.GetLogicEntity(policyTargetId, false);
                        if (target == null)
                        {
                            Debug.LogError("GetSkillUseParams not found primary target");
                            return (caster.Pos + caster.FinalLook.normalized * abilityCfg.Range1 * 0.7f, null);
                        }
                        return (target.Pos, null);
                    }
                    break;
                    
                case MapAbilitySpecConfig.ECastType.LockTarget:
                    {
                        var target = caster.LogicManager.GetLogicEntity(policyTargetId, false);
                        if (target == null)
                        {
                            Debug.LogError("GetSkillUseParams not found primary target");
                            return (null, null);
                        }
                        return (null, target);
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
            // 随机转头逻辑

            // 站着需要经常
            brain.HomePos = brain.NpcEntity.Pos;
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


            // 站着需要经常
            brain.HomePos = brain.NpcEntity.Pos;
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

            // 站着需要经常
            brain.HomePos = brain.NpcEntity.Pos;
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
        public override bool CanBeAttract => true;

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


            if(_brain.AttractTrigger)
            {
                _brain.ChangeState(_brain.StateAttracted);
                return;
            }

            if (_brain.SuspiciousPos != Vector3.zero)
            {
                _brain.ChangeState(_brain.StateSearch);
                return;
            }

            // 检查是否需要进入通缉状态
            if(_brain.Config.IsGuard)
            {
                int wantedVal = _brain.LogicManager.WantedManager.CurrentWantedVal;
                if (wantedVal > 20000 && _brain.NpcEntity.IsTargetVisible(_brain.LogicManager.playerLogicEntity.Id))
                {
                    _brain.ChangeState(_brain.StateChaseWanted);
                    return;
                }
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

        public AIStateAttracted(AIBrainV2 brain) : base(brain)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (_brain.LatestAttrctInfo == null || LogicTime.time - _brain.LatestAttrctInfo.HappenTime > 5.0f)
            {
                return;
            }
            _brain.NpcEntity.RegisterGaze("Attracted", _brain.LatestAttrctInfo.AttractSrcId, _brain.LatestAttrctInfo.HappenPos, EGazePriority.Interact, 0);
        }

        private float _enterAttractedTimer = 0;
        public override void OnUpdate()
        {
            if(_brain.LatestAttrctInfo == null || LogicTime.time - _brain.LatestAttrctInfo.HappenTime > 5.0f)
            {
                _brain.ChangeState(_brain.StateReturn);
                return;
            }


            IAttractSource attractSource = null;
            if (_brain.LatestAttrctInfo.AttractSrcId != 0)
            {
                attractSource = _brain.NpcEntity.LogicManager.GetLogicEntity(_brain.LatestAttrctInfo.AttractSrcId) as IAttractSource;
                if (attractSource != null)
                {
                    _brain.LatestAttrctInfo.HappenPos = attractSource.Pos;
                    _brain.LatestAttrctInfo.AttractLevel = attractSource.AttractLevel;
                }
            }


            // 进行移动
            if (_brain.LatestAttrctInfo.AttractLevel >= 3)
            {
                _brain.NpcEntity.TryMoveTo(_brain.LatestAttrctInfo.HappenPos, moveSpeedRate: 0.9f);
                //_brain.NpcEntity.LogicManager.viewer.ShowFakeFxEffect("attract fast", _brain.NpcEntity.Pos);
            }
            else if (_brain.LatestAttrctInfo.AttractLevel >= 2)
            {
                // 2级以上 
                _brain.NpcEntity.TryMoveTo(attractSource.Pos, moveSpeedRate: 0.1f);
                _brain.NpcEntity.LogicManager.viewer.ShowFakeFxEffect("attract slow", _brain.NpcEntity.Pos);
            }
            else
            {
                _brain.NpcEntity.StopMove();
            }

            // 条件满足时执行揩油
            if (_brain.LatestAttrctInfo.AttractLevel >= 2 && _brain.NpcEntity.abilityController.IsActionable())
            {
                if (attractSource is PlayerLogicEntity playerEntity && !playerEntity.CheckHasState(AttrIdConsts.ImmumeKaiYou))
                {
                    var diff = playerEntity.Pos - _brain.NpcEntity.Pos;
                    if (diff.magnitude < 0.8f)
                    {
                        _brain.NpcEntity.abilityController.TryUseAbility("close_kaiyou", target: playerEntity);
                    }
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            _brain.NpcEntity.UnregisterGazeBySourceTag("Attracted");
        }
    }


    public class AIStateCombat : AIBaseState
    {
        private float _attackTimer; // 攻击冷却计时器
        private BaseUnitLogicEntity _currentTarget;

        public float OverTimeLimit = 15f;

        private float attackRestTimer = 0; // 暂停攻击逻辑
        private EntitySkillCfg? intentSkillCfgOrigin;
        private MapAbilitySpecConfig? intentAbilityCfgCurrent;
        private bool hasCastAbility;
        private float castOverTimer;

        //private string currComboAbilityName;

        public override string StateName => "Combat";

        public AIStateCombat(AIBrainV2 brain) : base(brain)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _attackTimer = 1f; // 刚进入战斗通常可以立即攻击，或者根据设计设为 0.5f 延迟
            _brain.NpcEntity.StopMove(); // 先停一下，重新评估路径

            ResetAttackState();

            attackRestTimer = LogicTime.time + 5.0f; // 进入状态时先休眠一会
        }

        private void ResetAttackState()
        {
            attackRestTimer = LogicTime.time;
            intentSkillCfgOrigin = null;
            intentAbilityCfgCurrent = null;
            hasCastAbility = false;
            castOverTimer = 0;
        }

        /// <summary>
        /// 检查选择使用技能
        /// </summary>
        private void TryChooseOriginSkillUse()
        {
            if(intentSkillCfgOrigin != null)
            {
                return;
            }

            if(attackRestTimer != 0 && LogicTime.time - attackRestTimer < 3.0f)
            {
                return;
            }

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
            if (skillCfg == null)
            {
                return;
            }


            if(skillCfg.IsCombo)
            {
                var comboNode = _brain.NpcEntity.ablilityManager.comboOrchestrator.GetEntryComboNode(new SkillInput() { SkillId = skillCfg.SkillId });
                intentAbilityCfgCurrent = AbilityLibrary.GetAbilityConfig(comboNode.AbilityId);
            }
            else
            {
                intentAbilityCfgCurrent = AbilityLibrary.GetAbilityConfig(skillCfg.MainAbilityId);
            }

            if(intentAbilityCfgCurrent == null)
            {
                Debug.LogError($"skill not found good ability {skillCfg.SkillId}.");
                return;
            }

            hasCastAbility = false;
            castOverTimer = LogicTime.time + OverTimeLimit;
            intentSkillCfgOrigin = skillCfg;
        }

        /// <summary>
        /// 检查是否要退出战斗状态
        /// </summary>
        /// <returns></returns>
        private bool CheckLeaveCombat()
        {
            // --- 1. 获取目标 (数据验证) ---
            var targetId = _brain.Aggro.CurrentTargetId;

            var entity = _brain.LogicManager.GetLogicEntity(targetId);
            _currentTarget = entity as BaseUnitLogicEntity;
            // 如果仇恨列表空了，或者目标销毁了
            if (_currentTarget == null)
            {
                HandleTargetLost();
                return true;
            }

            if(_brain.NpcEntity.IsTargetInvisibleFromSelf(_currentTarget.Id))
            {
                HandleTargetLost();
                return true;
            }

            float distToTarget = Vector3.Distance(_brain.NpcEntity.Pos, _currentTarget.Pos);
            if (distToTarget > _brain.Config.ChaseRange)
            {
                // 放弃追击，清除仇恨，回家
                _brain.Aggro.ClearTarget();
                _brain.ChangeState(_brain.StateReturn);
                return true;
            }

            return false;
        }

        public override void OnUpdate()
        {
            // 检查是否退出战斗状态
            if(CheckLeaveCombat())
            {
                return;
            }

            // 检查中止技能释放
            ChecStopCastSkill();

            // 检查是否要中止使用技能
            TryChooseOriginSkillUse();

            TickCastSkill();

            // 没有技能需要释放时 进行走位
            if (intentSkillCfgOrigin == null)
            {
                var diff = _brain.NpcEntity.Pos - _currentTarget.Pos;
                var distToTarget = diff.magnitude;

                // 超过远距离
                if (_brain.Config.CombatFarDistance > 0 && distToTarget > _brain.Config.CombatFarDistance)
                {
                    Debug.Log("fast mo TryMoveTo player");
                    // 快速移动
                    _brain.NpcEntity.TryMoveTo(_currentTarget.Pos);
                }
                // 低于最近距离
                else if (_brain.Config.CombatCloseDistance > 0 && distToTarget <= _brain.Config.CombatCloseDistance)
                {
                    Debug.Log("too close");

                    _brain.NpcEntity.TryMoveTo(_brain.NpcEntity.Pos + (diff.normalized) * 0.5f, moveSpeedRate: 0.5f);
                }
                else
                {
                    Debug.Log("keep distance");
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

        private void ChecStopCastSkill()
        {

            if(intentSkillCfgOrigin == null)
            {
                return;
            }

            //bool stopSkill = false;
            //do
            //{
            //    // 禁止操作时 跳出
            //    if (_brain.NpcEntity.CheckHasState(AttrIdConsts.ForbidSkillOp))
            //    {
            //        stopSkill = true;
            //        break;
            //    }

            //    if (_brain.NpcEntity.IsTargetInvisibleFromSelf(_currentTarget.Id))
            //    {
            //        stopSkill = true;
            //        break;
            //    }
            //}
            //while (false);
            
            //if(stopSkill)
            //{
            //    currIntentSkillCfg = null;
            //    _brain.NpcEntity.StopMove();
            //}
        }


        /// <summary>
        /// 使用技能过程中
        /// </summary>
        private void TickCastSkill()
        {
            if (intentSkillCfgOrigin == null)
            {
                return;
            }

            // 正在释放技能时，检查是否进行连击
            if(hasCastAbility)
            {
                do
                {
                    // 继续等待action
                    if (!_brain.NpcEntity.abilityController.IsActionable())
                    {
                        break;
                    }

                    var trans = _brain.NpcEntity.ablilityManager.comboOrchestrator.GetPossibleTransition();
                    // 不可接技能 跳出
                    if (trans == null || trans.Count == 0)
                    {
                        if (!_brain.NpcEntity.abilityController.IsRunning)
                        {
                            // 重置技能释放
                            ResetAttackState();
                            return;
                        }
                        break;
                    }

                    var firstTran = trans[0];
                    var goodNode = _brain.NpcEntity.ablilityManager.comboOrchestrator.GetComboNode(firstTran.toNodeId);
                    if(goodNode == null)
                    {
                        ResetAttackState();
                        return;
                    }

                    intentAbilityCfgCurrent = AbilityLibrary.GetAbilityConfig(goodNode.AbilityId);
                    hasCastAbility = false;
                    castOverTimer = LogicTime.time + OverTimeLimit;

                }
                while (false);
            }


            // 还在技能走位阶段
            if (!hasCastAbility)
            {
                var targetId = EntityAbilityHelper.GetTargetByPolicy(intentAbilityCfgCurrent.TargetSelectPolicy, _brain.NpcEntity);

                bool canCast = false;
                do
                {
                    if (targetId == 0)
                    {
                        canCast = true;
                        break;
                    }
                    var target = _brain.LogicManager.GetLogicEntity(targetId, false) as BaseUnitLogicEntity;
                    if (target == null)
                    {
                        canCast = true;
                        break;
                    }

                    switch (intentAbilityCfgCurrent.CastType)
                    {
                        case MapAbilitySpecConfig.ECastType.NoTarget:
                        case MapAbilitySpecConfig.ECastType.LockTarget:

                            {
                                // 无目标类型的技能 盯紧目标点
                                _brain.NpcEntity.RegisterGaze("Combat", targetId, target.Pos, EGazePriority.CastSkill, 0.5f);

                                var diff = target.Pos - _brain.NpcEntity.Pos;
                                if (diff.magnitude < 0.05f)
                                {
                                    canCast = true;
                                    break;
                                }

                                var angle = Vector2.Angle(diff.normalized, _brain.NpcEntity.CurrentLook);
                                if(angle < 5 
                                    && diff.magnitude < intentAbilityCfgCurrent.DesiredUseDistance)
                                {
                                    canCast = true;
                                    break;
                                }

                                if (diff.magnitude > intentAbilityCfgCurrent.DesiredUseDistance)
                                {
                                    _brain.NpcEntity.TryMoveTo(_currentTarget.Pos, 0.5f, 1.2f);
                                }
                            }
                            break;
                        
                        case MapAbilitySpecConfig.ECastType.Point:
                        case MapAbilitySpecConfig.ECastType.Directional:
                            {
                                // 无目标类型的技能 盯紧目标点
                                _brain.NpcEntity.RegisterGaze("Combat", 0, target.Pos, EGazePriority.CastSkill, 0.5f);
                                var diff = target.Pos - _brain.NpcEntity.Pos;
                                if (diff.magnitude < 0.05f)
                                {
                                    canCast = true;
                                    break;
                                }

                                var angle = Vector2.Angle(diff.normalized, _brain.NpcEntity.CurrentLook);
                                if (angle < 5
                                    && diff.magnitude < intentAbilityCfgCurrent.Range1)
                                {
                                    canCast = true;
                                    break;
                                }

                                if (diff.magnitude > intentAbilityCfgCurrent.Range1)
                                {
                                    _brain.NpcEntity.TryMoveTo(_currentTarget.Pos, 0.5f, 1.2f);
                                }
                            }
                            break;
                        
                        case MapAbilitySpecConfig.ECastType.Circle:
                            {
                                // todo 对施法点做周围探测 尽量覆盖更多单位
                                var adjustedCastVec = target.Pos + UnityEngine.Random.insideUnitCircle * 0.5f;
                                var diff = adjustedCastVec - _brain.NpcEntity.Pos;
                                if (diff.magnitude < 0.05f)
                                {
                                    canCast = true;
                                    break;
                                }
                                // 无目标类型的技能 盯紧目标点
                                _brain.NpcEntity.RegisterGaze("Combat", 0, adjustedCastVec, EGazePriority.CastSkill, 0.5f);

                                var angle = Vector2.Angle(diff.normalized, _brain.NpcEntity.CurrentLook);
                                if (angle < 5
                                    && diff.magnitude < intentAbilityCfgCurrent.Range1)
                                {
                                    canCast = true;
                                    break;
                                }

                                if (diff.magnitude > intentAbilityCfgCurrent.Range1)
                                {
                                    _brain.NpcEntity.TryMoveTo(_currentTarget.Pos, 0.5f, 1.2f);
                                }
                            }
                            break;

                    }

                }
                while (false);


                if(canCast)
                {
                    (Vector2? vecParam, ILogicEntity? targetParam) = AIActionUtils.GetSkillCastParams(intentAbilityCfgCurrent, _brain.NpcEntity, targetId);
                    _brain.NpcEntity.ablilityManager.UseSkill(intentSkillCfgOrigin.SkillId, castVec: vecParam, target: targetParam);
                    _brain.NpcEntity.StopMove();
                    hasCastAbility = true;
                    return;
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
        public override bool CanBeAttract => true;

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
                if (_brain.Config.IsPeace)
                {
                    _brain.ChangeState(_brain.StateFlee);
                }
                else
                {
                    _brain.ChangeState(_brain.StateCombat);
                }
                return;
            }


            if (_brain.AttractTrigger)
            {
                _brain.ChangeState(_brain.StateAttracted);
                return;
            }

            if (_brain.SuspiciousPos != Vector3.zero)
            {
                _brain.ChangeState(_brain.StateSearch);
                return;
            }

            // 检查是否需要进入通缉状态
            if (_brain.Config.IsGuard)
            {
                int wantedVal = _brain.LogicManager.WantedManager.CurrentWantedVal;
                if (wantedVal > 20000 && _brain.NpcEntity.IsTargetVisible(_brain.LogicManager.playerLogicEntity.Id))
                {
                    _brain.ChangeState(_brain.StateChaseWanted);
                    return;
                }
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

    public class AIStateChaseWanted : AIBaseState
    {
        public override string StateName => "ChaseWanted";

        private float chaseChillTimer = 0;
        private long wantedUnitId;

        public AIStateChaseWanted(AIBrainV2 brain) : base(brain)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            chaseChillTimer = 0;
            wantedUnitId = _brain.NpcEntity.LogicManager.playerLogicEntity.Id;

            _brain.NpcEntity.viewer.ShowMapSpeachBubble(_brain.NpcEntity.Id, "抓你。", 2.0f);
        }

        public override void OnUpdate()
        {
            int wantedVal = _brain.GetAreaWantedVal();
            if (wantedVal < 0)
            {
                if(chaseChillTimer == 0)
                {
                    chaseChillTimer = LogicTime.time + 1.0f;
                    _brain.NpcEntity.viewer.ShowMapSpeachBubble(_brain.NpcEntity.Id, "没事了。", 2.0f);
                }
            }
            else
            {
                chaseChillTimer = 0;
            }

            // 时间到了 可以退出
            if(chaseChillTimer != 0 && LogicTime.time > chaseChillTimer)
            {
                _brain.ChangeState(_brain.StateIdle);
                return;
            }

            // 什么时候退出追逐？
            bool lostTarget = false;
            Vector2 searchPos = _brain.NpcEntity.Pos;
            if (_brain.NpcEntity.VisionSystem.VisibleMap.TryGetValue(wantedUnitId, out var visibilityEntry))
            {
                if(!visibilityEntry.IsInView)
                {
                    lostTarget = true;
                    searchPos = visibilityEntry.LastKnownPos;
                }
            }
            else
            {
                lostTarget = true;
                searchPos = _brain.NpcEntity.Pos;
            }

            // 丢失目标 进行search
            if (lostTarget)
            {
                _brain.ChangeState(_brain.StateSearch);
                _brain.SuspiciousPos = searchPos;
                return;
            }

            var searchTarget = _brain.NpcEntity.LogicManager.GetLogicEntity(wantedUnitId, false);
            if(searchTarget == null)
            {
                _brain.ChangeState(_brain.StateIdle);
                return;
            }

            // 移动
            _brain.NpcEntity.TryMoveTo(searchTarget.Pos, moveSpeedRate: 1f);

            var diff = searchTarget.Pos - _brain.NpcEntity.Pos;
            // 
            if(diff.magnitude < 0.3f)
            {
                // 出现对话
                _brain.NpcEntity.LogicManager.viewer.PlayDialog("wanted_arrest", srcEntityId: _brain.NpcEntity.Id, pause:true);

                _brain.ChangeState(_brain.StateIdle);
                return;
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            _brain.NpcEntity.UnregisterGazeBySourceTag("ChaseWanted");
        }
    }
}