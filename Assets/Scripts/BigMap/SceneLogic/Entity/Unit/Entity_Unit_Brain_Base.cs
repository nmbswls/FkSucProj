using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Unit
{
    public enum ECombatMoveStyle
    {
        Default,
        HitAndRun,
        Kiting,
        Caster,
        SlowHeavy,
    }

    // --- 配置 ---
    [System.Serializable]
    public class AIBrainConfig
    {
        public float PatrolRadius = 5f;
        public float AttackRange = 2.0f;

        public float ChaseRange = 15.0f;
        public float SearchDuration = 5.0f;  // 搜索持续时间
        public float IdleWaitTime = 3.0f;

        public bool IsPeace; // 和平单位只会逃
        public float CombatCloseDistance = 2.0f;
        public float CombatFarDistance = 5.0f;

        public ECombatMoveStyle CombatMoveStyle = ECombatMoveStyle.Default;
        public float AttackRestDuration = 3.0f;
        public float PostAttackRetreatDist = 3.0f;

        public string SpecialAnimTag1;
        public string SpecialAnimTag2;
        public string SpecialAnimTag3;
        public string SpecialAnimTag4;

        public bool IsGuard;

        // 临时字段，迁到 Luban 前用；配表 ai_brain_id 选模板，NPC 不硬编码 brain 名
        public bool StartAsSentry;
        public bool LockAggroToSelf;

        public static AIBrainConfig CreateDefault() => new AIBrainConfig();
    }

    // 临时：brain 参数未进表前在此注册；key 与 unit_npc.ai_brain_id 一致
    public static class AIBrainParamsConfigLoader
    {
        public static Dictionary<string, AIBrainConfig> _configs = null;

        public static AIBrainConfig Load(string name)
        {
            if (_configs == null)
            {
                _configs = new();
                _configs["default"] = AIBrainConfig.CreateDefault();
                _configs["basic_unit_peace"] = AIBrainConfig.CreateDefault();

                var guard = AIBrainConfig.CreateDefault();
                guard.IsGuard = true;
                _configs["default_guard"] = guard;

                var hSpirit = AIBrainConfig.CreateDefault();
                hSpirit.ChaseRange = 999;
                _configs["h_spirit"] = hSpirit;

                var turret = AIBrainConfig.CreateDefault();
                turret.ChaseRange = 1f;
                turret.StartAsSentry = true;
                _configs["fixed_turret"] = turret;

                var selfLockedCombat = AIBrainConfig.CreateDefault();
                selfLockedCombat.LockAggroToSelf = true;
                selfLockedCombat.AttackRestDuration = 0f;
                selfLockedCombat.CombatCloseDistance = 0f;
                selfLockedCombat.CombatFarDistance = 0f;
                _configs["self_locked_combat"] = selfLockedCombat;

                var agileMelee = AIBrainConfig.CreateDefault();
                agileMelee.CombatMoveStyle = ECombatMoveStyle.HitAndRun;
                agileMelee.CombatCloseDistance = 1.5f;
                agileMelee.CombatFarDistance = 4.0f;
                agileMelee.AttackRestDuration = 1.0f;
                agileMelee.PostAttackRetreatDist = 3.0f;
                _configs["agile_melee"] = agileMelee;

                var slowMelee = AIBrainConfig.CreateDefault();
                slowMelee.CombatMoveStyle = ECombatMoveStyle.SlowHeavy;
                slowMelee.CombatCloseDistance = 2.5f;
                slowMelee.CombatFarDistance = 3.5f;
                slowMelee.AttackRestDuration = 2.5f;
                _configs["slow_melee"] = slowMelee;

                var rangedKiting = AIBrainConfig.CreateDefault();
                rangedKiting.CombatMoveStyle = ECombatMoveStyle.Kiting;
                rangedKiting.CombatCloseDistance = 4.0f;
                rangedKiting.CombatFarDistance = 7.0f;
                rangedKiting.AttackRestDuration = 1.5f;
                _configs["ranged_kiting"] = rangedKiting;

                var caster = AIBrainConfig.CreateDefault();
                caster.CombatMoveStyle = ECombatMoveStyle.Caster;
                caster.CombatCloseDistance = 5.0f;
                caster.CombatFarDistance = 8.0f;
                caster.AttackRestDuration = 2.0f;
                _configs["caster"] = caster;
            }

            if (!_configs.TryGetValue(name, out var result) || result == null)
            {
                return AIBrainConfig.CreateDefault();
            }

            return result;
        }
    }

    public static class AIBrainFactory
    {
        public static AIBrainV2 CreateAIBrain(NpcUnitLogicEntity npcOwner)
        {
            return new AIBrainV2(npcOwner);
        }
    }

    public enum IdleType 
    { 
        StandStill, 
        Patrol, 
        Wander,
        Hunting,
        FollowGroup,
    }

    public enum EUnitReturnReason
    {
        Default,
        TargetLost,
        ChaseRangeExceeded,
        EncounterBoundary,
        Scripted,
    }

    public readonly struct AIReturnContext
    {
        public readonly EUnitReturnReason Reason;
        public readonly string SourceId;
        public readonly Vector2? TargetPosition;
        public readonly float MoveSpeedRate;
        public readonly float ReengageDelay;
        public readonly bool IgnoreSuspicion;
        public readonly bool IgnoreWanted;
        public readonly bool IgnoreAttract;
        public readonly float HealthRecoverDuration;
        public readonly bool InvulnerableDuringReturn;

        public AIReturnContext(
            EUnitReturnReason reason,
            string sourceId = null,
            Vector2? targetPosition = null,
            float moveSpeedRate = 0.7f,
            float reengageDelay = 5f,
            bool ignoreSuspicion = false,
            bool ignoreWanted = false,
            bool ignoreAttract = false,
            float healthRecoverDuration = 0f,
            bool invulnerableDuringReturn = false)
        {
            Reason = reason;
            SourceId = sourceId ?? string.Empty;
            TargetPosition = targetPosition;
            MoveSpeedRate = moveSpeedRate;
            ReengageDelay = reengageDelay;
            IgnoreSuspicion = ignoreSuspicion;
            IgnoreWanted = ignoreWanted;
            IgnoreAttract = ignoreAttract;
            HealthRecoverDuration = healthRecoverDuration;
            InvulnerableDuringReturn = invulnerableDuringReturn;
        }

        public static AIReturnContext Default => new(EUnitReturnReason.Default);
    }

    public readonly struct CombatDisengageRequest
    {
        public readonly string SourceId;
        public readonly EUnitReturnReason Reason;
        public readonly Vector2 ReturnPosition;
        public readonly float MoveSpeedRate;
        public readonly float RecoverDuration;
        public readonly float ReengageDelay;
        public readonly bool InvulnerableDuringReturn;

        public CombatDisengageRequest(
            string sourceId,
            EUnitReturnReason reason,
            Vector2 returnPosition,
            float moveSpeedRate,
            float recoverDuration,
            float reengageDelay,
            bool invulnerableDuringReturn)
        {
            SourceId = sourceId ?? string.Empty;
            Reason = reason;
            ReturnPosition = returnPosition;
            MoveSpeedRate = moveSpeedRate;
            RecoverDuration = recoverDuration;
            ReengageDelay = reengageDelay;
            InvulnerableDuringReturn = invulnerableDuringReturn;
        }
    }

    // --- 大脑 (Controller) ---
    public partial class AIBrainV2
    {
        // 组件引用
        public NpcUnitLogicEntity NpcEntity; // 实体逻辑
        public AIBrainConfig Config;         // 配置

        public GameLogicManager LogicManager { get {  return NpcEntity.LogicManager; } }

        // 状态机
        public AIBaseState CurrentState { get; private set; }

        public IVisionSenser2D Vision { get { return LogicManager.visionSenser; } }

        // 预加载状态 (避免GC)
        public AIStateIdle StateIdle;
        public AIStateSentry StateSentry;
        public AIStateCombat StateCombat; 
        public AIStateReturn StateReturn;
        public AIStateFlee StateFlee;
        public AIStateSearch StateSearch;
        public AIStateAttracted StateAttracted;
        public AIStateChaseWanted StateChaseWanted;
        public AIStateCharmedFollow StateCharmedFollow;
        public AIStateScriptedMicroPlot StateScriptedMicroPlot;
        public AIStatePoisonBait StatePoisonBait;
        public AIStateHKnockdownFollowup StateHKnockdownFollowup;

        // per-NPC：H 倒地 Closeup 触发 CD
        public float KnockdownCloseupCdUntil;


        // 黑板 (Blackboard) - 状态间共享数据
        public Vector2? HomePos;

        public Vector2? GetBehaviorReturnPosition()
        {
            return NpcEntity?.MoveBehaveInfo?.MoveBehaveMode == UnitMoveBehaveInfo.EMoveBehaveType.MoveToPoint
                ? NpcEntity.MoveBehaveInfo.MoveToTarget
                : HomePos;
        }
        public Vector2? SuspiciousPos; // <--- 搜索目标点 (最后目击位置/声音来源)

        public long PoisonBaitTargetInteractInstId;

        /// <summary> Idle 中对诱饵嗅探下一次允许探测的逻辑时间（降频）。 </summary>
        public float NextPoisonBaitProbeLogicTime;

        public UnitAggroSystem Aggro => NpcEntity.AggroSystem;

        private bool _isChangingState;
        private bool _deferSearchFromInvalidAttractEnter;
        private Vector2? _deferSuspiciousPosForSearch;

        public const float AttractFocusMaxAgeSeconds = 15f;

        public float ActionsFrequency = 0.2f;
        private float _lastBrainUpdate = 0;

        public string BrainConfigId { get; private set; }

        public bool IsRestoringFromPersist { get; private set; }

        public AIBrainV2(NpcUnitLogicEntity npcOwner)
        {
            this.NpcEntity = npcOwner;
            this.HomePos = npcOwner.Pos; // 记录出生点

            string cfgId = "default";
            if (npcOwner.NpcConfig.IsPeace)
            {
                cfgId = "basic_unit_peace";
            }
            if (!string.IsNullOrEmpty(npcOwner.NpcConfig.AiBrainId))
            {
                cfgId = npcOwner.NpcConfig.AiBrainId;
            }

            BrainConfigId = cfgId;
            Config = AIBrainParamsConfigLoader.Load(cfgId);
            if (Config.LockAggroToSelf)
            {
                npcOwner.AggroSystem?.LockTargetToSelf();
            }

            InitializeStates();
        }

        /// <summary>
        /// 初始化状态
        /// </summary>
        protected virtual void InitializeStates()
        {
            // 初始化状态
            StateIdle = new AIStateIdle(this);
            StateSentry = new AIStateSentry(this);
            StateCombat = new AIStateCombat(this);
            StateReturn = new AIStateReturn(this);
            StateFlee = new AIStateFlee(this);
            StateSearch = new AIStateSearch(this);
            StateAttracted = new AIStateAttracted(this);
            StateCharmedFollow = new AIStateCharmedFollow(this);
            StateScriptedMicroPlot = new AIStateScriptedMicroPlot(this);
            StatePoisonBait = new AIStatePoisonBait(this);
            StateHKnockdownFollowup = new AIStateHKnockdownFollowup(this);

            if (Config.IsGuard)
            {
                StateChaseWanted = new AIStateChaseWanted(this);
            }

            ChangeState(Aggro.IsTargetLockedToSelf ? StateCombat : Config.StartAsSentry ? StateSentry : StateIdle);
        }

        public void TriggerUpdateImmediately()
        {
            _lastBrainUpdate = 0;
        }

        public void ResetBrain()
        {
            // 重置
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public void Tick(float dt)
        {
            if(NpcEntity.LogicManager.IsDialogPlayering)
            {
                return;
            }

            if (LogicTime.time - _lastBrainUpdate > ActionsFrequency)
            {
                CurrentState?.Update();
                _lastBrainUpdate = LogicTime.time;


                if (CurrentState == StateIdle)
                {
                    if (NpcEntity.CheckHasBuff("social_charmed"))
                    {
                        ChangeState(StateCharmedFollow);
                    }
                }
            }
        }

        public void ChangeState(AIBaseState newState)
        {
            if (_isChangingState) return;
            if (CurrentState == newState) return;

            _isChangingState = true;

            CurrentState?.OnExit();
            CurrentState = newState;
            CurrentState?.OnEnter();

            _isChangingState = false;

            if (_deferSearchFromInvalidAttractEnter)
            {
                _deferSearchFromInvalidAttractEnter = false;
                if (_deferSuspiciousPosForSearch != null)
                {
                    SuspiciousPos = _deferSuspiciousPosForSearch;
                }

                _deferSuspiciousPosForSearch = null;
                ChangeState(StateSearch);
                return;
            }

            if (newState.CanBeAttract)
            {
                TryArmAttractTriggerForExistingFocus();
            }
        }

        public void RefreshIdlePolicy()
        {
            StateIdle.RefreshIdlePolicy();
            TriggerUpdateImmediately();
        }

        public bool TryDisengageFromCombat(CombatDisengageRequest request)
        {
            if (NpcEntity == null || NpcEntity.IsDead || NpcEntity.MarkDestroyed)
            {
                return false;
            }

            if (CurrentState == StateReturn && StateReturn.IsFromSource(request.SourceId))
            {
                return true;
            }

            if (CurrentState != StateCombat
                && CurrentState != StateReturn
                && !Aggro.CombatEngaged)
            {
                return false;
            }

            NpcEntity.abilityController?.Cancel();
            NpcEntity.StopMove();
            Aggro.ClearTarget(request.ReengageDelay);
            SuspiciousPos = null;

            StateReturn.Prepare(new AIReturnContext(
                request.Reason,
                request.SourceId,
                request.ReturnPosition,
                request.MoveSpeedRate,
                request.ReengageDelay,
                ignoreSuspicion: true,
                ignoreWanted: true,
                ignoreAttract: true,
                request.RecoverDuration,
                request.InvulnerableDuringReturn));
            if (CurrentState == StateReturn)
            {
                StateReturn.RestartPreparedReturn();
                return true;
            }
            ChangeState(StateReturn);
            return CurrentState == StateReturn;
        }

        // 进入 Idle/Return 等可吸引状态时，焦点未变也应能再次进入 Attracted
        void TryArmAttractTriggerForExistingFocus()
        {
            var f = NpcEntity.CurrentFocus;
            if (f == null || LogicTime.time - f.Timestamp > AttractFocusMaxAgeSeconds)
            {
                return;
            }

            AttractTrigger = true;
        }

        // AIStateAttracted.OnEnter 发现焦点非法时不能嵌套 ChangeState，延迟到本次切换结束后再进 Search
        public void RequestDeferredSearchFromAttractEnter(Vector2? suspiciousPos)
        {
            _deferSearchFromInvalidAttractEnter = true;
            _deferSuspiciousPosForSearch = suspiciousPos;
        }

        public bool CharmedTrigger;

        public bool AttractTrigger;
        
        

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetAreaWantedVal()
        {
            return LogicManager.WantedManager.CurrentWantedVal;
        }
    }

    // --- 状态基类 ---
    public abstract class AIBaseState
    {
        public abstract string StateName { get; }
        protected AIBrainV2 _brain;
        protected float startTime;
        protected float Duration => LogicTime.time - startTime;

        public virtual bool CanBeAttract { get { return false; } }
        public virtual bool CanEnterCombat { get { return false; } }

        // 为 true 时跳过 AIBaseState.Update 内共用的吸引/魅惑/参战等自动跳转（小剧情剧本态使用）。
        public virtual bool SuppressSharedBrainTransitions => false;

        public AIBaseState(AIBrainV2 brain) { _brain = brain; }

        public virtual void OnEnter() { startTime = Time.time; }

        public void Update()
        {
            OnUpdate();

            if (SuppressSharedBrainTransitions)
            {
                return;
            }

            if(CanEnterCombat)
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
            }

            if(CanBeAttract)
            {
                if(_brain.AttractTrigger)
                {
                    _brain.AttractTrigger = false;

                    var f = _brain.NpcEntity.CurrentFocus;
                    if (f != null && LogicTime.time - f.Timestamp <= AIBrainV2.AttractFocusMaxAgeSeconds)
                    {
                        _brain.ChangeState(_brain.StateAttracted);
                    }
                    else
                    {
                        if (f != null)
                        {
                            _brain.SuspiciousPos = new Vector2(f.Position.x, f.Position.y);
                        }

                        _brain.ChangeState(_brain.StateSearch);
                    }
                }
            }

            // 只要能上到charm 就能程序
            if (_brain.CharmedTrigger)
            {
                _brain.CharmedTrigger = false;

                _brain.ChangeState(_brain.StateCharmedFollow);
            }

            if(CanKaiYou())
            {
                //// 条件满足时执行揩油
                //if (_brain.LatestAttrctInfo.AttractLevel >= 2 && _brain.NpcEntity.abilityController.IsActionable())
                //{
                //    if (attractSource is PlayerLogicEntity playerEntity && !playerEntity.CheckHasState(AttrIdConsts.ImmumeKaiYou))
                //    {
                //        var diff = playerEntity.Pos - _brain.NpcEntity.Pos;
                //        if (diff.magnitude < 0.8f)
                //        {
                //            _brain.NpcEntity.abilityController.TryUseAbility("close_kaiyou", target: playerEntity);
                //        }
                //    }
                //}
            }
        }


        public abstract void OnUpdate();
        public virtual void OnExit() { }
        public virtual void OnFixedUpdate() { }

        public virtual bool CanKaiYou()
        {
            return false;
        }
    }
}


