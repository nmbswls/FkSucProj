using My.Map;
using My.Map.Fight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static My.GameLogicManager;
using static My.Map.BaseUnitLogicEntity;
using static My.Map.Fight.FightStruct;
using static Unity.Collections.Unicode;
using static UnityEngine.EventSystems.EventTrigger;

namespace My.Map.Entity
{
    public class InterruptUtils
    {
        
    }


    

    public static class EntityAbilityHelper
    {
        // 圆形范围内最近敌对单位，使用物理圆查询 + 精确距离过滤。
        public static BaseUnitLogicEntity FindNearestEnemyInRadius(
            GameLogicManager logicManager,
            BaseUnitLogicEntity factionRef,
            Vector2 center,
            float acquireRadius,
            params long[] excludeIds)
        {
            if (logicManager == null || factionRef == null || logicManager.visionSenser == null)
            {
                return null;
            }

            float radius = acquireRadius > 0.01f ? acquireRadius : 8f;
            float radiusSqr = radius * radius;
            var filter = new EntityFilterParam
            {
                CampFilterType = ECampFilterType.NotSelf,
                SelfCampId = factionRef.FactionId,
            };

            BaseUnitLogicEntity best = null;
            float bestSqr = float.MaxValue;

            foreach (var one in logicManager.visionSenser.OverlapCircleAllEntity(
                         center,
                         radius,
                         filter,
                         MapLogicPosition.ResolveAttackHitHeight(factionRef)))
            {
                if (one is not BaseUnitLogicEntity unit)
                {
                    continue;
                }

                if (excludeIds != null)
                {
                    bool excluded = false;
                    for (int i = 0; i < excludeIds.Length; i++)
                    {
                        if (unit.Id == excludeIds[i])
                        {
                            excluded = true;
                            break;
                        }
                    }

                    if (excluded)
                    {
                        continue;
                    }
                }

                if (unit.MarkDestroyed || unit.IsDead)
                {
                    continue;
                }

                float sqr = (unit.Pos - center).sqrMagnitude;
                if (sqr > radiusSqr)
                {
                    continue;
                }

                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = unit;
                }
            }

            return best;
        }

        /// <summary>
        /// 根据目标筛选类型 
        /// </summary>
        /// <param name="targetSelectPolicy"></param>
        /// <param name="casterUnit"></param>
        /// <returns></returns>
        public static long GetTargetByPolicy(FightStruct.ETargetSelectPolicy targetSelectPolicy, BaseUnitLogicEntity casterUnit, float acquireRadius = 0f)
        {
            switch(targetSelectPolicy)
            {
                case ETargetSelectPolicy.Self:
                    {
                        return casterUnit.Id;
                    }
                    break;
                case ETargetSelectPolicy.PrimaryTarget:
                    {
                        return casterUnit.CurrentTargetId;
                    }
                    break;
                case ETargetSelectPolicy.LowHpAlly:
                    {
                        var rangeAllies = casterUnit.FindEntityInRange(casterUnit.Pos, 3.0f);
                        List<(BaseUnitLogicEntity, long)> candidates = new();
                        foreach(var oneEntity in rangeAllies)
                        {
                            if(oneEntity is not BaseUnitLogicEntity unitEntity)
                            {
                                continue;
                            }
                            if(oneEntity.FactionId != casterUnit.FactionId)
                            {
                                continue;
                            }
                            candidates.Add((unitEntity, unitEntity.GetAttr(AttrIdConsts.HP)));
                        }

                        candidates.Sort((itemA, itemB) => { return itemA.Item2.CompareTo(itemB.Item2); });

                        if(candidates.Count > 0)
                        {
                            return candidates[0].Item1.Id;
                        }

                        return 0;
                    }
                    break;
                case ETargetSelectPolicy.NearestEnemyInRadius:
                    {
                        var best = FindNearestEnemyInRadius(
                            casterUnit.LogicManager,
                            casterUnit,
                            casterUnit.Pos,
                            acquireRadius,
                            casterUnit.Id);
                        return best != null ? best.Id : 0;
                    }
                    break;
            }

            return 0;
        }
    }


    public class MapEntityAbilityExecutor
    {
        public BaseUnitLogicEntity EntityOwner { get; set; }

        protected virtual ILogicEntity GetSpatialHost() => EntityOwner;

        protected virtual ILogicEntity GetCombatSource() => EntityOwner;

        protected virtual bool IsDetachedHost() => false;

        protected virtual IEntityBuffOwner GetBuffCheckOwner() => EntityOwner;

        protected virtual Vector2 ResolveSpatialFaceDir()
        {
            if (GetSpatialHost() is BaseUnitLogicEntity unit)
            {
                return unit.FinalLook;
            }

            if (EntityOwner != null)
            {
                return EntityOwner.FinalLook;
            }

            return Vector2.right;
        }

        public class AbilityRunningContext
        {
            public ILogicEntity Actor;         // 施动者
            public ILogicEntity Target;        // 目标对象（如门或敌人），可为空
            public Vector2 FaceDir;           // 面朝方向
            public Vector2? InputVec;           // 额外输入方向
            public Vector2? CastVec1;           // 面朝方向
            public Vector2? Position;         // 施放位置（如脚下或点击点）
            public Dictionary<string, object> UserData = new();
            // 时间推进
            public float AbilityTime;        // 自开始起累计时间

            public MapAbilitySpecConfig AbilityConfig;
            public int PhaseIndex;
            public float PhaseElapsed;       // 当前阶段已用时
            public float PhaseDuration;      // 当前阶段时间
            public float LastPhaseHoldTime;  // 上次续长按时间
            public bool PhaseMarkSkip;

            //public List<Modifier> PhaseModifiers = new();
            public List<long> PhaseBindBuffs = new();
            public List<long> phaseHitWindows = new();
            public List<int> phaseBindEffectIds = new();
            public int PhaseIntentEffectId = 0;
            public bool PhaseIntentIsProgressEffect;

            public string? openClickkkType;
            public float? openClickkkDuration;

            public ISceneAbilityViewer? viewer; // 表现层接口

            public string DebugSavedAnimTag;
            public float DebugSavedAnimTagTimer;

            public long AbilityAnimSessionId;
            public long PhaseAnimHandle;

            public List<ScheduledEvent> _scheduled = new();

            public float PhaseStartLogicTime = 0f;

            // 变量集合
            public Dictionary<string, string> RunningVariables = new();
            public Dictionary<string, string> PhaseOverrideAnims = null;

            public Dictionary<string, long> RunningStorage = new();

            public Dictionary<string, float> PhaseXuLiInfos = new();


            public Action<bool> OneAbilityEnd = null;

            public string GetVariatyRawVal(OneVariaty oneVariaty)
            {
                if (oneVariaty.ValType == EOneVariatyType.Invalid)
                {
                    return string.Empty;
                }

                string strVal = oneVariaty.RawVal;
                if (!string.IsNullOrEmpty(oneVariaty.ReferName))
                {
                    do
                    {
                        if (RunningVariables != null && RunningVariables.TryGetValue(oneVariaty.ReferName, out var runningVal))
                        {
                            strVal = runningVal;
                            break;
                        }

                        if (AbilityConfig.Variables != null && AbilityConfig.Variables.TryGetValue(oneVariaty.ReferName, out var configVal))
                        {
                            strVal = configVal;
                            break;
                        }
                    }
                    while (false);
                }

                return strVal;
            }
        }

        public AbilityRunningContext CurrentCtx;
        private bool _running = false;

        private static long s_abilityAnimSessionSeq;

        //private Dictionary<string, float> _sharedCooldown = new();

        public event Action<string> EventOnUseAbility;
        public event Action<string, int> EventOnAbilityEnd;
        public event Action EventOnInputCancelPhaseStart;


        public bool IsRunning { get { return _running; } }

        public bool IsActionable()
        {
            if(!IsRunning)
            {
                return true;
            }
            var phase = GetCurrentPhase();
            if(phase == null)
            {
                return true;
            }

            if(phase.InterruptMask.HasFlag(EAbilityInterruptMask.Cast))
            {
                return true;
            }

            return false;
        }

        public class ScheduledEvent
        {
            public float FireTime;     // 相对阶段开始的时间
            public PhaseEffectEvent Source;
            public int Left;
            public float NextInterval;
        }


        public MapEntityAbilityExecutor(BaseUnitLogicEntity owner)
        {
            this.EntityOwner = owner;
        }

        
        public virtual bool TryUseAbility(string abilityName, Vector2? inputVec = null, Vector2? castVec = null, ILogicEntity target = null, 
            Dictionary<string, string> overrideParams = null, Dictionary<string, string> phaseOverrideAnims = null, 
            Action<bool> onAbilityEnd = null)
        {
            var config = AbilityLibrary.GetAbilityConfig(abilityName);
            if (config == null)
            {
                return false;
            }

            return TryStart(config, inputVec: inputVec, castVec1: castVec, target: target, runningOverrides: overrideParams, phaseOverrideAnims: phaseOverrideAnims, onAbilityEnd: onAbilityEnd);
        }

        public void Tick(float dt)
        {
            if (!_running) return;
            TickIntern(dt);
        }


        /// <summary>
        /// 使用技能
        /// </summary>
        /// <param name="abState"></param>
        /// <param name="castVec1"></param>
        /// <param name="target"></param>
        /// <param name="runningOverrides"></param>
        /// <param name="phaseOverrideAnims"></param>
        /// <returns></returns>
        protected bool TryStart(MapAbilitySpecConfig abilityConf, Vector2? inputVec = null, Vector2? castVec1 = null, ILogicEntity target = null, Dictionary<string, string> runningOverrides = null, Dictionary<string, string> phaseOverrideAnims = null, string? groupOwnerName = null, Action<bool> onAbilityEnd = null)
        {
            var spatialHost = GetSpatialHost();

            if (!IsDetachedHost() && abilityConf.IsDodge)
            {
                EntityOwner.TryInterrupt(new InterruptRequest()
                {
                    source = EInterruptSource.Dodge,
                });
            }

            // 检查可打断
            if (!IsActionable())
            {
                return false;
            }

            if (!IsDetachedHost())
            {
                EntityOwner.TryInterrupt(new InterruptRequest()
                {
                    source = EInterruptSource.Cast,
                });
            }


            if (IsRunning)
            {
                return false;
            }

            // 检查强制转向
            if (!IsDetachedHost() && abilityConf.AdjustFaceDir)
            {
                if(castVec1 != null)
                {
                    var dir = castVec1.Value - EntityOwner.Pos;
                    EntityOwner.ForceSetFaceTarget(dir, false);
                }
                else
                {

                }
            }


            CurrentCtx = new AbilityRunningContext
            {
                Actor = EntityOwner,
                Target = target,
                AbilityTime = 0f,
                PhaseIndex = 0,
                PhaseElapsed = 0f,
                AbilityConfig = abilityConf,
                viewer = EntityOwner.viewer,
                RunningVariables = runningOverrides,
                InputVec = inputVec,
                CastVec1 = castVec1,
                FaceDir = ResolveSpatialFaceDir(),
                Position = spatialHost.Pos,

                OneAbilityEnd = onAbilityEnd,

                PhaseOverrideAnims = phaseOverrideAnims,

                AbilityAnimSessionId = System.Threading.Interlocked.Increment(ref s_abilityAnimSessionSeq),
            };
            foreach (var e in abilityConf.OnStartEffects)
            {
                var effectCtx = GenerateEfffectContextByAbility(CurrentCtx);
                EntityOwner.LogicManager.HandleLogicFightEffect(e, effectCtx);
            }
            EnterPhase(0);
            _running = true;

            
            EventOnUseAbility?.Invoke(abilityConf.Id);

            Debug.Log($"entity {EntityOwner.Id} TryStart {abilityConf.Id}");

            return true;
        }

        private static void ResolveStepSnapParams(MapAbilityPhase phase, MapAbilitySpecConfig spec, out float defaultStep, out bool addTargetCorrection, out float maxCorrectionValue, out float goodCorrectionDist)
        {
            if (phase.StepSnapSource == EPhaseStepSnapSource.PhaseCustom)
            {
                defaultStep = phase.DefaultStepDistance;
                addTargetCorrection = phase.AddTargetCorrection;
                maxCorrectionValue = phase.MaxCorrectionValue;
                goodCorrectionDist = phase.GoodCorrectionnDist;
            }
            else if(phase.StepSnapSource == EPhaseStepSnapSource.InheritFromAbility)
            {
                defaultStep = spec.DefaultStepDistance;
                addTargetCorrection = spec.AddTargetCorrection;
                maxCorrectionValue = spec.MaxCorrectionValue;
                goodCorrectionDist = spec.GoodCorrectionnDist;
            }
            else
            {
                defaultStep = 0;
                addTargetCorrection = false;
                maxCorrectionValue = 0;
                goodCorrectionDist= 0;
            }
        }

        private BaseUnitLogicEntity ResolveSnapTargetUnit()
        {
            if (CurrentCtx?.Target is BaseUnitLogicEntity ctxUnit && !ctxUnit.MarkDestroyed && !ctxUnit.IsDead)
            {
                return ctxUnit;
            }

            var mainTargetId = EntityOwner.CurrentTargetId;
            if (mainTargetId == 0)
            {
                return null;
            }

            var mainTarget = EntityOwner.LogicManager.GetLogicEntity(mainTargetId, false);
            if (mainTarget is not BaseUnitLogicEntity mainTargetUnit)
            {
                return null;
            }

            if (mainTargetUnit.IsDead || mainTargetUnit.MarkDestroyed)
            {
                return null;
            }

            return mainTargetUnit;
        }

        /// <summary>
        /// 在阶段开始时执行吸附/垫步（在 UsePhaseHitAsTarget 刷新 CurrentCtx.Target 之后调用）
        /// </summary>
        private void ApplyPhaseStepSnap(MapAbilityPhase phase, MapAbilitySpecConfig abilityConf)
        {
            ResolveStepSnapParams(phase, abilityConf, out var defaultStep, out var addTargetCorrection, out var maxCorrectionValue, out var goodCorrectionDist);

            bool withTargetCorrection = false;
            Vector2? correctPoint = null;
            do
            {
                if (!addTargetCorrection)
                {
                    break;
                }

                var mainTargetUnit = ResolveSnapTargetUnit();
                if (mainTargetUnit == null)
                {
                    break;
                }

                withTargetCorrection = true;
                var diff = mainTargetUnit.Pos - EntityOwner.Pos;
                if (diff.magnitude < goodCorrectionDist)
                {
                    correctPoint = null;
                }
                else
                {
                    correctPoint = EntityOwner.Pos + diff.normalized * maxCorrectionValue;
                }
            }
            while (false);

            if (!withTargetCorrection)
            {
                if (defaultStep > 0)
                {
                    correctPoint = EntityOwner.Pos + EntityOwner.FinalLook * defaultStep;
                }
            }

            if (correctPoint != null)
            {
                var dashDir = correctPoint.Value - EntityOwner.Pos;
                if (dashDir.magnitude < 0.05f)
                {
                    return;
                }

                float correctionTime = 0.15f;
                EntityOwner.StartDash(dashDir.normalized, correctionTime, dashDir.magnitude / correctionTime, null);
            }
        }


        private void EnterPhase(int index)
        {
            if (CurrentCtx == null) return; // 技能可能已打断
            var ctx = CurrentCtx;
            ctx.PhaseIndex = index;
            ctx.PhaseElapsed = 0f;
            
            var phase = ctx.AbilityConfig.Phases[index];

            if(phase.HoldingPhase)
            {
                ctx.LastPhaseHoldTime = LogicTime.time;
            }
            // 锁动作
            var phaseDurRaw = ctx.GetVariatyRawVal(phase.DurationValue);
            var phaseDur = 0f;
            if (!string.IsNullOrEmpty(phaseDurRaw) && !float.TryParse(phaseDurRaw, out phaseDur))
            {
                Debug.LogError("TickIntern wrong param");
            }
            ctx.PhaseDuration = phaseDur;

            // 默认：进入即播放自身AnimTag（也可当作一个事件）
            var animTag = phase.AnimTag;
            if (ctx.PhaseOverrideAnims != null && ctx.PhaseOverrideAnims.TryGetValue(phase.PhaseName, out var overrideAnim))
            {
                animTag = overrideAnim;
            }
            if (!string.IsNullOrEmpty(animTag))
            {
                ctx.DebugSavedAnimTag = animTag;
                var policy = phase.AnimReleasePolicy == 0
                    ? AnimReleasePolicyUtil.DefaultAbilityPhase
                    : phase.AnimReleasePolicy;
                var resolved = EntityOwner.GetAnimOverride(animTag);
                ctx.PhaseAnimHandle = EntityOwner.PushAnimRequest(new AnimPlayRequest
                {
                    AnimName = resolved,
                    Layer = 0,
                    Source = EAnimRequestSource.AbilityPhase,
                    ReleasePolicy = policy,
                    AbilitySessionId = CurrentCtx.AbilityAnimSessionId,
                    AbilityPhaseIndex = index,
                });
            }
            else
            {
                ctx.PhaseAnimHandle = 0;
            }
            if (!string.IsNullOrEmpty(phase.EnterDebugString))
            {
                EntityOwner.viewer.ShowFakeFxEffect(phase.EnterDebugString, EntityOwner.Pos);
            }

            if (EntityOwner.Type == EEntityType.Player && EntityOwner.viewer != null)
            {
                float startTime = LogicTime.time;
                if (phase.WithProgress)
                {
                    ctx.PhaseStartLogicTime = startTime;
                    EntityOwner.viewer.ShowBottomProgress("Checking", ctx.PhaseDuration, startTime);
                }
                else if (phase.HoldingPhase)
                {
                    // HoldingPhase 使用 ProgressEffectNormalizeDuration 作为底部进度条满时长
                    float normalDur = phase.ProgressEffectNormalizeDuration > 0f
                        ? phase.ProgressEffectNormalizeDuration
                        : ctx.PhaseDuration;
                    if (normalDur > 0f && normalDur < 50f)
                    {
                        ctx.PhaseStartLogicTime = startTime;
                        EntityOwner.viewer.ShowBottomProgress("Checking", normalDur, startTime);
                    }
                }
            }

            if(!string.IsNullOrEmpty(phase.UsePhaseHitAsTarget))
            {
                var targetStorageName = $"{phase.UsePhaseHitAsTarget}.FirstHit";
                if(ctx.RunningStorage.TryGetValue(targetStorageName, out var entityId))
                {
                    var targetE = EntityOwner.LogicManager.GetLogicEntity(entityId, false);
                    if (targetE != null)
                    {
                        Debug.Log($"entity:{EntityOwner.Id} ability:{ctx.AbilityConfig.Id} phase:{phase.PhaseName} change target :{entityId}");
                        CurrentCtx.Target = targetE;
                    }
                }
            }

            if (!IsDetachedHost())
            {
                ApplyPhaseStepSnap(phase, CurrentCtx.AbilityConfig);
            }

            // 安排该阶段的事件
            ctx._scheduled.Clear();
            foreach (var ev in phase.Events)
            {
                if (ev.Kind == PhaseEventKind.OnEnter)
                {
                    var effectCtx = GenerateEfffectContextByAbility(ctx);
                    EntityOwner.LogicManager.HandleLogicFightEffect(ev.Effect, effectCtx);

                    //if(effectCtx.BindSceneFxIds.Count > 0)
                    //{
                    //    BindSceneFxIds
                    //}
                }
                else if (ev.Kind == PhaseEventKind.Timed)
                {
                    CurrentCtx._scheduled.Add(new ScheduledEvent
                    {
                        FireTime = Mathf.Max(0, ev.TimeOffset),
                        Source = ev,
                        Left = Mathf.Max(1, ev.Repeat),
                        NextInterval = ev.RepeatInterval
                    });
                }
            }

            if (CurrentCtx == null) return; // 技能可能已打断

            // 世家buff
            if (phase.PhaseBuff != null)
            {
                foreach(var buffId in phase.PhaseBuff)
                {
                    var instId = EntityOwner.BuffManager.AddBuff(EntityOwner.Id, buffId, casterId: EntityOwner.Id);
                    ctx.PhaseBindBuffs.Add(instId);
                }
            }

            if (phase.LockMovement)
            {
                var instId = EntityOwner.BuffManager.AddBuff(EntityOwner.Id, "lock_move");
                //var srcKey = new SourceKey()
                //{
                //    type = SourceType.Skill,
                //    instanceId = 0,
                //};
                //var modifier = EntityOwner.AddAttrModifier(srcKey, AttrIdConsts.Unmovable, 1);
                ctx.PhaseBindBuffs.Add(instId);
            }
            if (phase.LockRotation)
            {
                var instId = EntityOwner.BuffManager.AddBuff(EntityOwner.Id, "lock_face");
                //var srcKey = new SourceKey()
                //{
                //    type = SourceType.Skill,
                //    instanceId = 0,
                //};
                //var modifier = EntityOwner.AddAttrModifier(srcKey, AttrIdConsts.LockFace, 1);
                ctx.PhaseBindBuffs.Add(instId);
            }
            if (phase.ImmuneKnock)
            {
                var instId = EntityOwner.BuffManager.AddBuff(EntityOwner.Id, "immune_knock");
                ctx.PhaseBindBuffs.Add(instId);
            }
            

            ctx.PhaseIntentIsProgressEffect = false;
            if (!string.IsNullOrEmpty(phase.ProgressSceneEffect))
            {
                ctx.PhaseIntentEffectId = EntityOwner.viewer.ShowProgressSceneEffect(
                    phase.ProgressSceneEffect,
                    EntityOwner.Pos,
                    EntityOwner.Id);
                ctx.PhaseIntentIsProgressEffect = ctx.PhaseIntentEffectId != 0;
            }
            else if (phase.ShowRangePreview)
            {
                var eId = EntityOwner.LogicManager.viewer.ShowRangeWarnEffect(phase.PreviewIntent.ShapeInfo,  EntityOwner.Pos, EntityOwner.FinalLook, phaseDur, phase.PreviewIntent.FaceOffset);
                ctx.PhaseIntentEffectId = eId;
            }

            if(phase.InterruptMask.HasFlag(EAbilityInterruptMask.Cast))
            {
                EventOnInputCancelPhaseStart?.Invoke();
            }
        }

        private void ExitPhase(int index)
        {
            var ctx = CurrentCtx;
            var phase = ctx.AbilityConfig.Phases[index];

            string phaseName = phase.PhaseName;
            ctx.PhaseXuLiInfos[phaseName] = ctx.PhaseElapsed;

            // 触发 OnExit
            foreach (var ev in phase.Events)
            {
                if (ev.Kind == PhaseEventKind.OnExit)
                {
                    var effectCtx = GenerateEfffectContextByAbility(ctx);
                    EntityOwner.LogicManager.HandleLogicFightEffect(ev.Effect, effectCtx);
                }
            }
            ctx._scheduled.Clear();

            CleanupPhase(ctx);
        }



        public bool TryInterrupt(InterruptRequest req)
        {
            if(CurrentCtx == null)
            {
                return false;
            }

            var phase = GetCurrentPhase();
            if (phase == null)
            {
                return false;
            }
            
            switch(req.source)
            {
                case EInterruptSource.Move:
                    {
                        if (!phase.InterruptMask.HasFlag(EAbilityInterruptMask.Move))
                        {
                            return false;
                        }
                    }
                    break;
                case EInterruptSource.InputCancel:
                    {
                        if (!phase.InterruptMask.HasFlag(EAbilityInterruptMask.Cancel))
                        {
                            return false;
                        }
                    }
                    break;
                case EInterruptSource.Hit:
                    {
                        if (!phase.InterruptMask.HasFlag(EAbilityInterruptMask.Hit))
                        {
                            return false;
                        }
                    }
                    break;
                case EInterruptSource.Dodge:
                    {
                        if (phase.ForbidDodge)
                        {
                            return false;
                        }
                    }
                    break;
                case EInterruptSource.Cast:
                    {
                        if (!phase.InterruptMask.HasFlag(EAbilityInterruptMask.Cast))
                        {
                            return false;
                        }
                    }
                    break;
            }
            

            Cancel();
            return true;
        }

        public void CleanupPhase(AbilityRunningContext ctx, bool isInterrupt = false)
        {
            // 关闭命中盒、停止位移曲线、回收特效、重置输入锁等
            if (ctx.PhaseAnimHandle != 0)
            {
                EntityOwner.ReleaseAnimRequestForced(ctx.PhaseAnimHandle);
                ctx.PhaseAnimHandle = 0;
            }

            // 移除phase附加状态
            foreach (var buffId in ctx.PhaseBindBuffs)
            {
                EntityOwner.LogicManager.globalBuffManager.RequestRemoveBuff(null, buffId);
            }

            ctx.PhaseMarkSkip = false;

            ctx.PhaseBindBuffs.Clear();

            // 检查是否要立刻中止冲刺 现在一定中止
            if (EntityOwner.controlledMoveCtx != null 
                && !string.IsNullOrEmpty(EntityOwner.controlledMoveCtx.BindAbilityId)
                && EntityOwner.controlledMoveCtx.BindAbilityId == ctx.AbilityConfig.Id && EntityOwner.controlledMoveCtx.BindAbilityPhaseIdx == ctx.PhaseIndex)
            {
                EntityOwner.EndControlledMove(5);
            }

            if (ctx.phaseHitWindows.Count > 0)
            {
                foreach(var winId in ctx.phaseHitWindows)
                {
                    EntityOwner.HitWindowRegistry.CloseHitWindow(winId);
                }

                ctx.phaseHitWindows.Clear();
            }

            if (!string.IsNullOrEmpty(ctx.openClickkkType))
            {
                EntityOwner.LogicManager.viewer.CloseClickkkWindow(ctx.openClickkkType, isInterrupt);
                ctx.openClickkkType = null;
            }

            //if(CurrentCtx.phaseBindEffectIds.Count > 0)
            //{
            //    foreach(var id in CurrentCtx.phaseBindEffectIds)
            //    {
            //        EntityOwner.LogicManager.viewer.HideRangeWarnEffect(id);
            //    }

            //    CurrentCtx.phaseBindEffectIds.Clear();
            //}

            if (ctx.PhaseIntentEffectId != 0)
            {
                EntityOwner.LogicManager.viewer.DestroySceneFxEffect(ctx.PhaseIntentEffectId);
                ctx.PhaseIntentEffectId = 0;
                ctx.PhaseIntentIsProgressEffect = false;
            }

            // 底部进度条清理（WithProgress 与 HoldingPhase 统一，仅玩家单位）
            if (ctx.PhaseStartLogicTime > 0f && EntityOwner.Type == EEntityType.Player && EntityOwner.viewer != null)
            {
                EntityOwner.viewer.TryCancelButtomProgress(ctx.PhaseStartLogicTime);
                ctx.PhaseStartLogicTime = 0f;
            }
        }

        public virtual GameLogicManager.LogicFightEffectContext GenerateEfffectContextByAbility(AbilityRunningContext abilityCtx)
        {
            var combatSource = GetCombatSource();
            var spatialHost = GetSpatialHost();
            var sourceInfo = new EffectSourceInfo()
            {
                SrcType = ESourceType.Ability,
                SrcEntityId = combatSource.Id,
                SrcFactionId = combatSource.FactionId,

                SrcAbilityId = abilityCtx.AbilityConfig.Id,
                SrcAbilityPhaseId = abilityCtx.PhaseIndex,
            };
            var ctx = new GameLogicManager.LogicFightEffectContext(EntityOwner.LogicManager, EFightCtxType.Ability, sourceInfo);
            ctx.TargetId = abilityCtx.Target ?.Id ?? 0;
            ctx.TriggerPos = spatialHost.Pos;

            if(abilityCtx.CastVec1 != null)
            {
                ctx.CastVec1 = abilityCtx.CastVec1;
            }
            else
            {
                ctx.CastVec1 = spatialHost.Pos + abilityCtx.FaceDir;
            }

            ctx.InputVec = abilityCtx.InputVec;

            ctx.TriggerPos = abilityCtx.Position;

            ctx.RunningVariables = abilityCtx.RunningVariables;

            foreach(var xuliInfo in abilityCtx.PhaseXuLiInfos)
            {
                string key = $"{xuliInfo.Key}.Timed";
                ctx.RunningStorage[key] = (long)(xuliInfo.Value * 1000);
            }

            FightCastAttrUtil.MergeCastRunningVars(ctx.RunningVariables, ctx.CacheAttrVal);

            return ctx;
        }


        float ResolveProgressSceneEffect01(AbilityRunningContext ctx, MapAbilityPhase phase)
        {
            float denom = phase.ProgressEffectNormalizeDuration > 0f
                ? phase.ProgressEffectNormalizeDuration
                : Mathf.Max(ctx.PhaseDuration, 1e-4f);
            return Mathf.Clamp01(ctx.PhaseElapsed / denom);
        }

        private void TickIntern(float dt)
        {
            var ctx = CurrentCtx;

            ctx.AbilityTime += dt;
            ctx.PhaseElapsed += dt;

            // debug
            if (!string.IsNullOrEmpty(ctx.DebugSavedAnimTag))
            {
                ctx.DebugSavedAnimTagTimer += dt;
                if (ctx.DebugSavedAnimTagTimer > 0.2f)
                {
                    //EntityOwner.viewer.ShowFakeFxEffect(CurrentCtx.DebugSavedAnimTag, EntityOwner.Pos);
                    ctx.DebugSavedAnimTagTimer = 0;
                }
            }

            // 执行定时事件（相对当前阶段时间）
            for (int i = 0; i < ctx._scheduled.Count; ++i)
            {
                var s = ctx._scheduled[i];
                while (s.Left > 0 && ctx.PhaseElapsed >= s.FireTime)
                {
                    //var executor = GetExecutor(s.Source.Effect);
                    //executor?.Apply(s.Source.Effect, CurrentCtx);
                    var buffCheckOwner = GetBuffCheckOwner();
                    if(!string.IsNullOrEmpty(s.Source.CheckNeedBuff))
                    {
                        if (!buffCheckOwner.CheckHasBuff(s.Source.CheckNeedBuff))
                        {
                            Debug.Log($"tick ability skip need buff :{s.Source.Effect.EffectType}");
                            continue;
                        }
                    }

                    if (!string.IsNullOrEmpty(s.Source.CheckNoBuff))
                    {
                        if (buffCheckOwner.CheckHasBuff(s.Source.CheckNoBuff))
                        {
                            Debug.Log($"tick ability skip no buff :{s.Source.Effect.EffectType}");
                            continue;
                        }
                    }

                    var effectCtx = GenerateEfffectContextByAbility(ctx);
                    EntityOwner.LogicManager.HandleLogicFightEffect(s.Source.Effect, effectCtx);
                    s.Left--;
                    s.FireTime += s.NextInterval > 0 ? s.NextInterval : float.MaxValue;

                    if(effectCtx.OutHitWindowIds.Count > 0)
                    {
                        ctx.phaseHitWindows.AddRange(effectCtx.OutHitWindowIds);
                    }
                }
            }


            // 检查是否是引导
            var phase = ctx.AbilityConfig.Phases[ctx.PhaseIndex];
            if(phase != null) 
            {
                if (ctx.PhaseIntentEffectId != 0)
                {
                    if (ctx.PhaseIntentIsProgressEffect)
                    {
                        EntityOwner.viewer.UpdateSceneEffectProgress(
                            ctx.PhaseIntentEffectId,
                            ResolveProgressSceneEffect01(ctx, phase));
                    }
                    else
                    {
                        EntityOwner.LogicManager.viewer.UpdateRangeWarnEffect(ctx.PhaseIntentEffectId, EntityOwner.Pos, EntityOwner.FinalLook);
                    }
                }
            }

            bool phaseFinish = false;
            if(ctx.PhaseMarkSkip)
            {
                phaseFinish = true;
            }
            // 检查非holding阶段
            else if(!phase.HoldingPhase)
            {
                if(ctx.PhaseElapsed >= ctx.PhaseDuration)
                {
                    phaseFinish = true;
                }
            }
            else if(phase.HoldingPhase)
            {
                if(LogicTime.time - ctx.LastPhaseHoldTime > 0.5f)
                {
                    phaseFinish = true;
                }
                
                // 时间太长也不行
                if(ctx.PhaseDuration != 0 && ctx.PhaseElapsed >= ctx.PhaseDuration)
                {
                    phaseFinish = true;
                }
            }

            // 阶段结束
            if (phaseFinish)
            {
                ExitPhase(ctx.PhaseIndex);
                var next = ctx.PhaseIndex + 1;
                if (next < ctx.AbilityConfig.Phases.Count)
                {
                    EnterPhase(next);
                }
                else
                {
                    Complete();
                }
            }

        }

        //private void ProcessHitWindows(float dt, AbilityPhase phase)
        //{
        //    if (phase == null) return;
        //    foreach (var hw in Current.HitWindows)
        //    {
        //        if (hw.Phase != phase.Kind) continue;
        //        float t = Ctx.PhaseElapsed;
        //        if (t - dt <= hw.StartOffset && t >= hw.StartOffset)
        //        {
        //            // 窗口开始时刻触发一次；也可在窗口期间循环
        //            var dmgEffect = new DealDamageEffect(hw.Damage, hw.DamageType, knockback: 8f);
        //            dmgEffect.Apply(Current, Ctx);
        //            // 这里简化为一次触发；实际可在窗口期间多次采样
        //        }
        //    }
        //}


        private void Complete()
        {
            var abName = CurrentCtx.AbilityConfig.Id;

            foreach (var e in CurrentCtx.AbilityConfig.OnCompleteEffects)
            {
                var effectCtx = GenerateEfffectContextByAbility(CurrentCtx);
                EntityOwner.LogicManager.HandleLogicFightEffect(e, effectCtx);
            }

            //_cooldownEnd = Time.time + CurrentCtx.AbilityConfig.Cooldown;

            CleanupPhase(CurrentCtx);

            var sessionId = CurrentCtx.AbilityAnimSessionId;
            CurrentCtx.OneAbilityEnd?.Invoke(true);

            _running = false;
            Debug.Log($"Ability {CurrentCtx.AbilityConfig.Id} complete");
            EntityOwner.ReleaseAnimRequestsByAbilitySession(sessionId);
            CurrentCtx = null;

            EventOnAbilityEnd?.Invoke(abName, 0);
        }

        public void Cancel()
        {
            if (!_running) return;
            var abName = CurrentCtx.AbilityConfig.Id;
            foreach (var e in CurrentCtx.AbilityConfig.OnCancelEffects)
            {
                var effectCtx = GenerateEfffectContextByAbility(CurrentCtx);
                EntityOwner.LogicManager.HandleLogicFightEffect(e, effectCtx);
            }

            CleanupPhase(CurrentCtx);

            var sessionId = CurrentCtx.AbilityAnimSessionId;

            _running = false;
            Debug.Log($"Ability {CurrentCtx.AbilityConfig.Id} Cancel");

            CurrentCtx.OneAbilityEnd?.Invoke(false);

            EntityOwner.ReleaseAnimRequestsByAbilitySession(sessionId);
            CurrentCtx = null;

            //  清理绑定绑定的冲刺呢？

            EventOnAbilityEnd?.Invoke(abName, 1);
        }

        private void ReleaseLocks()
        {
            //var mover = GetComponent<CharacterMover>();
            //if (mover) mover.LockMovement = false;
        }

        public MapAbilityPhase GetCurrentPhase()
        {
            if (CurrentCtx == null) return null;
            if (CurrentCtx.PhaseIndex >= CurrentCtx.AbilityConfig.Phases.Count)
            {
                return null;
            }

            var phase = CurrentCtx.AbilityConfig.Phases[CurrentCtx.PhaseIndex];
            return phase;
        }

        

        

        

        public void OpenClickkkWindow(string windowType, float duration)
        {
            if (CurrentCtx.openClickkkType != null)
            {
                Debug.LogError($"OpenClickkkWindow already have clickkk");
                return;
            }
            CurrentCtx.openClickkkType = windowType;
            CurrentCtx.openClickkkDuration = duration;
        }

        //public void ApplyHitWindow(string weaponName)
        //{
        //    // 统一为hitwindow处理
        //    long hitId = ++HitWiindowIdCounter;

        //    EventOnApplyUseWeapon?.Invoke(hitId, weaponName);
        //}
    }
}
