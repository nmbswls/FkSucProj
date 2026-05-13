

using System.Collections.Generic;
using My.MapExport;
using cfg.demo;
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
        public static IIdlePolicy CreateFromMoveBehave(UnitMoveBehaveInfo behave)
        {
            if (behave == null)
            {
                return new Policy_StandStill();
            }

            switch (behave.MoveBehaveMode)
            {
                case UnitMoveBehaveInfo.EMoveBehaveType.Patrol:
                    if (behave.PatrolCycleNodeIds != null && behave.PatrolCycleNodeIds.Count >= 2)
                    {
                        return new Policy_GraphPatrol();
                    }

                    Debug.LogWarning("[MovePolicyFactory] Patrol mode requires PatrolCycleNodeIds (>=2); falling back to StandStill.");
                    return new Policy_StandStill();
                case UnitMoveBehaveInfo.EMoveBehaveType.MovePath:
                case UnitMoveBehaveInfo.EMoveBehaveType.NoMove:
                case UnitMoveBehaveInfo.EMoveBehaveType.Hunting:
                case UnitMoveBehaveInfo.EMoveBehaveType.InPatrolGroup:
                default:
                    return new Policy_StandStill();
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
            //if(LogicTime.time - _wanderTimer < brain.Config.WanderInterval)
            //{
            //    return;
            //}

            //_wanderTimer = LogicTime.time;

            //Vector2 wandarOrg = brain.HomePos == null ? brain.NpcEntity.Pos : brain.HomePos.Value;
            //_currWanderPoint = UnityEngine.Random.insideUnitCircle * 1.0f + wandarOrg;

            //brain.NpcEntity.TryMoveTo(_currWanderPoint.Value);
        }


        public void OnExit(AIBrainV2 brain) { }
    }

    public class Policy_GraphPatrol : IIdlePolicy
    {
        private readonly List<Vector2> _worldPath = new();

        private int _idx;
        private bool _ok;

        public void OnEnter(AIBrainV2 brain)
        {
            _worldPath.Clear();
            _idx = 0;
            _ok = false;
            var db = brain.LogicManager.AreaManager.cacheDatabase;
            var behave = brain.NpcEntity.MoveBehaveInfo;
            if (db == null || behave.PatrolCycleNodeIds == null || behave.PatrolCycleNodeIds.Count < 2)
            {
                return;
            }

            if (!PortalPatrolPathBuilder.TryBuildCycleWorldPath(
                    db,
                    behave.PatrolPortalNetworkId,
                    behave.PatrolCycleNodeIds,
                    _worldPath)
                || _worldPath.Count == 0)
            {
                Debug.LogWarning("[Policy_GraphPatrol] Failed to build patrol path; NPC stays idle.");
                return;
            }

            _ok = true;
            TryMoveToCurrent(brain);
        }

        public void OnTick(AIBrainV2 brain, float dt)
        {
            if (!_ok || _worldPath.Count == 0)
            {
                brain.HomePos = brain.NpcEntity.Pos;
                return;
            }

            Vector2 target = _worldPath[_idx];
            if (Vector2.Distance(brain.NpcEntity.Pos, target) < 0.5f)
            {
                _idx = (_idx + 1) % _worldPath.Count;
                TryMoveToCurrent(brain);
            }

            brain.HomePos = brain.NpcEntity.Pos;
        }

        public void OnExit(AIBrainV2 brain)
        {
            brain.NpcEntity.StopMove();
        }

        void TryMoveToCurrent(AIBrainV2 brain)
        {
            if (_worldPath.Count == 0)
            {
                return;
            }

            brain.NpcEntity.TryMoveTo(_worldPath[_idx]);
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
            _idlePolicy = MovePolicyFactory.CreateFromMoveBehave(brain.NpcEntity.MoveBehaveInfo);
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

            if (_brain.SuspiciousPos != null)
            {
                _brain.ChangeState(_brain.StateSearch);
                return;
            }

            // 检查是否需要进入通缉状态（与 wanted_level_info 星级一致）
            if(_brain.Config.IsGuard)
            {
                if (_brain.LogicManager.WantedManager.GetWantedStarLevel() >= 1
                    && _brain.NpcEntity.IsTargetVisible(_brain.LogicManager.playerLogicEntity.Id))
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

    // --- attracted 状态 ---
    /// <summary>
    /// 
    /// </summary>
    public class AIStateAttracted : AIBaseState
    {
        public override string StateName => "Attracted";

        public AIStateAttracted(AIBrainV2 brain) : base(brain)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (_brain.NpcEntity.CurrentFocus == null || LogicTime.time - _brain.NpcEntity.CurrentFocus.Timestamp > 15.0f)
            {
                return;
            }
            //_brain.NpcEntity.RegisterGaze("Attracted", _brain.NpcEntity.LatestAttrctInfo.AttractSrcId, _brain.NpcEntity.LatestAttrctInfo.HappenPos, EGazePriority.Interact, 0);
            _brain.NpcEntity.RegisterGaze("Attracted", _brain.NpcEntity.CurrentFocus.SourceID, _brain.NpcEntity.CurrentFocus.Position, EGazePriority.Interact, 0);
        }

        private float _enterAttractedTimer = 0;
        public override void OnUpdate()
        {
            if(_brain.NpcEntity.CurrentFocus == null || LogicTime.time - _brain.NpcEntity.CurrentFocus.Timestamp > 15.0f)
            {
                _brain.ChangeState(_brain.StateSearch);
                return;
            }

            switch(_brain.NpcEntity.CurrentFocus.Type)
            {
                case EStimulusType.Player_Mist:
                    {
                        UpdateAttractByMist();
                    }
                    break;
                case EStimulusType.Src_Entity:
                    {
                        var attractSrc = _brain.NpcEntity.LogicManager.GetLogicEntity(_brain.NpcEntity.CurrentFocus.SourceID) as IAttractSource;
                        if (attractSrc != null)
                        {
                            _brain.NpcEntity.TryMoveTo(attractSrc.Pos, moveSpeedRate: 0.5f);
                        }
                    }
                    break;
                case EStimulusType.Audio_Normal:
                    {
                        _brain.NpcEntity.TryMoveTo(_brain.NpcEntity.CurrentFocus.Position, moveSpeedRate: 0.5f);
                    }
                    break;
                case EStimulusType.Player_Attract:
                    {
                        var playerEntity = _brain.NpcEntity.LogicManager.GetLogicEntity(_brain.NpcEntity.CurrentFocus.SourceID) as PlayerLogicEntity;

                        if (playerEntity == null)
                        {
                            Debug.LogError("AIStateAttracted err 1");
                            break;
                        }

                        var attractLevel = PlayerGamePlayRule.CalculateUnitAttractedLevel(_brain.NpcEntity.LogicManager, _brain.NpcEntity.GetAttr(AttrIdConsts.Will));

                        // 进行移动
                        if (attractLevel >= 3)
                        {
                            _brain.NpcEntity.TryMoveTo(playerEntity.Pos, moveSpeedRate: 0.8f);
                        }
                        else if (attractLevel >= 2)
                        {
                            // 2级以上 
                            _brain.NpcEntity.TryMoveTo(playerEntity.Pos, moveSpeedRate: 0.4f);
                        }
                        else
                        {
                            _brain.NpcEntity.StopMove();
                        }

                        // 条件满足时执行揩油
                        if (attractLevel >= 2 && _brain.NpcEntity.abilityController.IsActionable())
                        {
                            if (!playerEntity.CheckHasState(AttrIdConsts.ImmumeKaiYou))
                            {
                                var diff = playerEntity.Pos - _brain.NpcEntity.Pos;
                                if (diff.magnitude < 0.8f)
                                {
                                    _brain.NpcEntity.abilityController.TryUseAbility("close_kaiyou", target: playerEntity);
                                }
                            }
                        }
                    }
                    break;
            }

            
        }


        private void UpdateAttractByMist()
        {
            // 寻找附近的entity
            const string mistCfg = "player_pink_mist_trail";
            var candidates = _brain.NpcEntity.LogicManager.FindEntityInRange(_brain.NpcEntity.Pos, 3f);

            AreaEffectLogicEntity lastestOne = null;

            foreach (var candidate in candidates)
            {
                if(candidate is not AreaEffectLogicEntity aeEntity)
                {
                    continue;
                }

                if(aeEntity.CfgId != mistCfg)
                {
                    continue;
                }

                if(lastestOne == null || aeEntity.LifeTime >= lastestOne.LifeTime)
                {
                    lastestOne = aeEntity;
                }
            }
            
            if(lastestOne == null)
            {
                _brain.ChangeState(_brain.StateSearch);
            }
            else
            {
                _brain.NpcEntity.TryMoveTo(lastestOne.Pos, moveSpeedRate: 0.5f);
            }
        }


        public override void OnExit()
        {
            base.OnExit();

            _brain.NpcEntity.UnregisterGazeBySourceTag("Attracted");
        }
    }

    public class AIStateCharmedFollow : AIBaseState
    {
        public override string StateName => "CharmedFollow";

        private float _endCharmdTimer;

        public AIStateCharmedFollow(AIBrainV2 brain) : base(brain)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            var buffInstance = _brain.NpcEntity.FindBuffById("social_charmed");
            if(buffInstance != null)
            {
                var srcEntityId = buffInstance.CasterId;
                var srcEntity = _brain.LogicManager.GetLogicEntity(srcEntityId, false);
                if(srcEntity != null)
                {
                    _brain.NpcEntity.RegisterGaze("Charmed", srcEntity.Id, srcEntity.Pos, EGazePriority.Interact, 0);
                }
            }
            _endCharmdTimer = 0;
        }

        private float _enterAttractedTimer = 0;
        public override void OnUpdate()
        {
            var buffInstance = _brain.NpcEntity.FindBuffById("social_charmed");
            if (buffInstance == null)
            {
                if (_endCharmdTimer == 0)
                {
                    _endCharmdTimer = LogicTime.time;
                    //
                    _brain.NpcEntity.LogicManager.viewer.ShowMapSpeachBubble(_brain.NpcEntity.Id, "我在做什么?", 2f);

                    _brain.NpcEntity.StopMove();
                }

                if (LogicTime.time - _endCharmdTimer > 3.0f)
                {
                    _brain.ChangeState(_brain.StateReturn);
                    _brain.NpcEntity.LogicManager.viewer.ShowMapSpeachBubble(_brain.NpcEntity.Id, "赶紧走", 2f);
                }

                return;
            }

            var srcEntityId = buffInstance.CasterId;
            var srcEntity = _brain.LogicManager.GetLogicEntity(srcEntityId, false);
            if(srcEntity == null)
            {
                _brain.ChangeState(_brain.StateReturn);
                return;
            }

            _brain.NpcEntity.TryMoveFollow(srcEntity, 0, Vector2.zero, stopDistance:0.25f, moveSpeedRate: 0.4f);
        } 

        public override void OnExit()
        {
            base.OnExit();

            _brain.NpcEntity.UnregisterGazeBySourceTag("Charmed");
        }
    }


    public class AIStateCombat : AIBaseState
    {
        private float _attackTimer; // 攻击冷却计时器
        private BaseUnitLogicEntity _currentTarget;

        public float OverTimeLimit = 15f;

        private float attackRestTimer = 0; // 暂停攻击逻辑


        private EntitySkillData? intentSkillCfgOrigin;
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

            attackRestTimer = LogicTime.time + 0.5f; // 进入状态时先休眠一会
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

            Debug.Log($"npc:{_brain.NpcEntity.CfgId} {_brain.NpcEntity.Id} prepare to use skill {skillCfg.SkillId}.");
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

            float distToHome = Vector3.Distance(_brain.NpcEntity.Pos, _brain.HomePos.Value);
            //float distToTarget = Vector3.Distance(_brain.NpcEntity.Pos, _currentTarget.Pos);
            if (distToHome > _brain.Config.ChaseRange)
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


            if (_brain.SuspiciousPos != null)
            {
                _brain.ChangeState(_brain.StateSearch);
                return;
            }

            // 检查是否需要进入通缉状态（与 wanted_level_info 星级一致）
            if (_brain.Config.IsGuard)
            {
                if (_brain.LogicManager.WantedManager.GetWantedStarLevel() >= 1
                    && _brain.NpcEntity.IsTargetVisible(_brain.LogicManager.playerLogicEntity.Id))
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
                    if (Vector3.Distance(_brain.NpcEntity.Pos, searchOrgPoint) < 0.3f)
                    {
                        // 到达，开始四处张望
                        _phase = SearchPhase.LookingAround;
                        _lookAroundTimer = LogicTime.time + _brain.Config.SearchDuration;
                        _brain.NpcEntity.StopMove();
                        break;
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
            if (_brain.LogicManager.WantedManager.GetWantedStarLevel() <= 0)
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
            _brain.NpcEntity.TryMoveTo(searchTarget.Pos, stopDistance:0.1f, moveSpeedRate: 1f);

            var diff = searchTarget.Pos - _brain.NpcEntity.Pos;
            // 
            if(diff.magnitude < 0.5f)
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