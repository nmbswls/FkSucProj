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


    public class AbilityHitWindow
    {
        public long hitId;
        public string weaponName;
        public float openTime;
        public float durationTime;
        public List<long> HitRecord = new();

        public int HitParam0;
        public int HitParam1;

        public List<MapFightEffectCfg> OnHitEffects; // 原始数据 还是生成hitwindow专用数据放入？

        // 来源是weapon 还是 技能？
    }

    public static class EntityAbilityHelper
    {

        /// <summary>
        /// 根据目标筛选类型 
        /// </summary>
        /// <param name="targetSelectPolicy"></param>
        /// <param name="casterUnit"></param>
        /// <returns></returns>
        public static long GetTargetByPolicy(FightStruct.ETargetSelectPolicy targetSelectPolicy, BaseUnitLogicEntity casterUnit)
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
            }

            return 0;
        }
    }


    public class MapEntityAbilityExecutor
    {
        public BaseUnitLogicEntity EntityOwner { get; set; }

        public long HitWiindowIdCounter = 10000;

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
            public Dictionary<long, AbilityHitWindow> phaseHitWindows = new();
            public List<int> phaseBindEffectIds = new();
            public int PhaseIntentEffectId = 0;

            public string? openClickkkType;
            public float? openClickkkDuration;

            public ISceneAbilityViewer? viewer; // 表现层接口

            public string DebugSavedAnimTag;
            public float DebugSavedAnimTagTimer;

            public List<ScheduledEvent> _scheduled = new();

            public long ShowProgressShowId = 0;

            // 变量集合
            public Dictionary<string, string> RunningVariables = new();
            public Dictionary<string, string> PhaseOverrideAnims = null;

            public Dictionary<string, long> RunningStorage = new();

            public string GroupOwnerName = null;

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


        //private Dictionary<string, float> _sharedCooldown = new();

        public event Action<long, string, float> EventOnApplyUseWeapon;
        public event Action<string, long> EventOnCloseHitWindow;
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

        
        public virtual bool TryUseAbility(string abilityName, Vector2? inputVec = null, Vector2? castVec = null, ILogicEntity target = null, Dictionary<string, string> overrideParams = null, Dictionary<string, string> phaseOverrideAnims = null, string? groupOwnerName = null)
        {
            var config = AbilityLibrary.GetAbilityConfig(abilityName);
            if (config == null)
            {
                return false;
            }

            return TryStart(config, inputVec: inputVec, castVec1: castVec, target: target, runningOverrides: overrideParams, phaseOverrideAnims: phaseOverrideAnims, groupOwnerName: groupOwnerName);
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
        protected bool TryStart(MapAbilitySpecConfig abilityConf, Vector2? inputVec = null, Vector2? castVec1 = null, ILogicEntity target = null, Dictionary<string, string> runningOverrides = null, Dictionary<string, string> phaseOverrideAnims = null, string? groupOwnerName = null)
        {

            if(abilityConf.IsDodge)
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

            EntityOwner.TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.Cast,
            });


            if (IsRunning)
            {
                return false;
            }

            // 检查强制转向
            if(abilityConf.AdjustFaceDir)
            {
                if(castVec1 != null)
                {
                    var dir = castVec1.Value - EntityOwner.Pos;
                    EntityOwner.ForceSetFaceTarget(dir, false);
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
                FaceDir = EntityOwner.FinalLook,
                Position = EntityOwner.Pos,

                PhaseOverrideAnims = phaseOverrideAnims,
                GroupOwnerName = groupOwnerName,
            };
            foreach (var e in abilityConf.OnStartEffects)
            {
                var effectCtx = GenerateEfffectContextByAbility();
                EntityOwner.LogicManager.HandleLogicFightEffect(e, effectCtx);
            }
            EnterPhase(0);
            _running = true;

            
            EventOnUseAbility?.Invoke(abilityConf.Id);

            CheckApplyAdditiveMove(abilityConf);

            Debug.Log($"entity {EntityOwner.Id} TryStart {abilityConf.Id}");

            return true;
        }

        private void CheckApplyAdditiveMove(MapAbilitySpecConfig abilityConf)
        {
            bool withTargetCorrection = false;
            Vector2? correctPoint = null;
            do
            {

                if (!abilityConf.AddTargetCorrection)
                {
                    break;
                }

                var mainTargetId = EntityOwner.CurrentTargetId;
                if(mainTargetId == 0)
                {
                    break;
                }
                var mainTarget = EntityOwner.LogicManager.GetLogicEntity(mainTargetId, false);
                if(mainTarget == null || mainTarget is not BaseUnitLogicEntity mainTargetUnit)
                {
                    break;
                }
                if(mainTargetUnit.IsDead || mainTargetUnit.MarkDestroyed)
                {
                    break;
                }

                withTargetCorrection = true;
                var diff = mainTarget.Pos - EntityOwner.Pos;
                // 不需要吸附
                if (diff.magnitude < abilityConf.GoodCorrectionnDist)
                {
                    correctPoint = null;
                }
                else
                {
                    correctPoint = EntityOwner.Pos + diff.normalized * abilityConf.MaxCorrectionValue;
                }
            }
            while (false);
            
            // 没有吸附时才处理默认垫步
            if(!withTargetCorrection)
            {
                if (abilityConf.DefaultStepDistance > 0)
                {
                    correctPoint = EntityOwner.Pos + EntityOwner.FinalLook * abilityConf.DefaultStepDistance;
                }
            }

            if(correctPoint != null)
            {
                var dashDir = correctPoint.Value - EntityOwner.Pos;
                if(dashDir.magnitude < 0.05f)
                {
                    return;
                }
                float correctionTime = 0.15f;
                EntityOwner.StartDash(dashDir.normalized, correctionTime, dashDir.magnitude / correctionTime, null);
            }
        }


        private void EnterPhase(int index)
        {
            CurrentCtx.PhaseIndex = index;
            CurrentCtx.PhaseElapsed = 0f;
            
            var phase = CurrentCtx.AbilityConfig.Phases[index];

            if(phase.HoldingPhase)
            {
                CurrentCtx.LastPhaseHoldTime = LogicTime.time;
            }
            // 锁动作
            var phaseDurRaw = CurrentCtx.GetVariatyRawVal(phase.DurationValue);
            var phaseDur = 0f;
            if (!phase.HoldingPhase && !float.TryParse(phaseDurRaw, out phaseDur))
            {
                Debug.LogError("TickIntern wrong param");
            }
            CurrentCtx.PhaseDuration = phaseDur;

            // 默认：进入即播放自身AnimTag（也可当作一个事件）
            var animTag = phase.AnimTag;
            if (CurrentCtx.PhaseOverrideAnims != null && CurrentCtx.PhaseOverrideAnims.TryGetValue(phase.PhaseName, out var overrideAnim))
            {
                animTag = overrideAnim;
            }
            if (!string.IsNullOrEmpty(animTag))
            {
                CurrentCtx.DebugSavedAnimTag = animTag;
                EntityOwner.PlayerAnim(animTag, 0);
                //var executor = GetExecutor(e);
                //new PlayAnimEffect { AnimTag = phase.AnimTag }.Apply(Current, Ctx);
            }
            if (!string.IsNullOrEmpty(phase.EnterDebugString))
            {
                EntityOwner.viewer.ShowFakeFxEffect(phase.EnterDebugString, EntityOwner.Pos);
            }

            if (phase.WithProgress)
            {
                CurrentCtx.ShowProgressShowId = EntityOwner.viewer.ShowBottomProgress("Checking", CurrentCtx.PhaseDuration);
            }

            // 安排该阶段的事件
            CurrentCtx._scheduled.Clear();
            foreach (var ev in phase.Events)
            {
                if (ev.Kind == PhaseEventKind.OnEnter)
                {
                    var effectCtx = GenerateEfffectContextByAbility();
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

            // 世家buff
            if(phase.PhaseBuff != null)
            {
                foreach(var buffId in phase.PhaseBuff)
                {
                    var instId = EntityOwner.BuffManager.AddBuff(EntityOwner.Id, buffId, casterId: EntityOwner.Id);
                    CurrentCtx.PhaseBindBuffs.Add(instId);
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
                CurrentCtx.PhaseBindBuffs.Add(instId);
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
                CurrentCtx.PhaseBindBuffs.Add(instId);
            }
            if (phase.ImmuneKnock)
            {
                var instId = EntityOwner.BuffManager.AddBuff(EntityOwner.Id, "immune_knock");
                CurrentCtx.PhaseBindBuffs.Add(instId);
            }
            

            if (phase.ShowRangePreview)
            {
                var eId = EntityOwner.LogicManager.viewer.ShowRangeWarnEffect(phase.PreviewIntent.ShapeInfo,  EntityOwner.Pos, EntityOwner.FinalLook, phaseDur, phase.PreviewIntent.FaceOffset);
                CurrentCtx.PhaseIntentEffectId = eId;
            }

            if(phase.InterruptMask.HasFlag(EAbilityInterruptMask.Cast))
            {
                EventOnInputCancelPhaseStart?.Invoke();
            }
        }

        private void ExitPhase(int index)
        {
            var phase = CurrentCtx.AbilityConfig.Phases[index];

            // 触发 OnExit
            foreach (var ev in phase.Events)
            {
                if (ev.Kind == PhaseEventKind.OnExit)
                {
                    var effectCtx = GenerateEfffectContextByAbility();
                    EntityOwner.LogicManager.HandleLogicFightEffect(ev.Effect, effectCtx);
                }
            }
            CurrentCtx._scheduled.Clear();

            CleanupPhase();
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

        public void CleanupPhase(bool isInterrupt = false)
        {
            // 关闭命中盒、停止位移曲线、回收特效、重置输入锁等

            // 移除phase附加状态
            foreach (var buffId in CurrentCtx.PhaseBindBuffs)
            {
                EntityOwner.LogicManager.globalBuffManager.RequestRemoveBuff(null, buffId);
            }

            CurrentCtx.PhaseMarkSkip = false;

            CurrentCtx.PhaseBindBuffs.Clear();

            if (CurrentCtx.phaseHitWindows.Count > 0)
            {
                foreach (var hitWindow in CurrentCtx.phaseHitWindows.Values)
                {
                    EventOnCloseHitWindow?.Invoke(hitWindow.weaponName, hitWindow.hitId);
                }

                CurrentCtx.phaseHitWindows.Clear();
            }

            if (!string.IsNullOrEmpty(CurrentCtx.openClickkkType))
            {
                EntityOwner.LogicManager.viewer.CloseClickkkWindow(CurrentCtx.openClickkkType, isInterrupt);
                CurrentCtx.openClickkkType = null;
            }

            //if(CurrentCtx.phaseBindEffectIds.Count > 0)
            //{
            //    foreach(var id in CurrentCtx.phaseBindEffectIds)
            //    {
            //        EntityOwner.LogicManager.viewer.HideRangeWarnEffect(id);
            //    }

            //    CurrentCtx.phaseBindEffectIds.Clear();
            //}

            if (CurrentCtx.PhaseIntentEffectId != 0)
            {
                EntityOwner.LogicManager.viewer.DestroySceneFxEffect(CurrentCtx.PhaseIntentEffectId);
                CurrentCtx.PhaseIntentEffectId = 0;
            }

            if (!isInterrupt)
            {
                //尝试进行cancel
                //EntityOwner.viewer.TryCancelButtomProgress(CurrentCtx.ShowProgressShowId);
            }
        }

        public GameLogicManager.LogicFightEffectContext GenerateEfffectContextByAbility()
        {
            var sourceInfo = new EffectSourceInfo()
            {
                SrcType = ESourceType.Ability,
                SrcEntityId = this.EntityOwner.Id,
                SrcFactionId = this.EntityOwner.FactionId,
            };
            var ctx = new GameLogicManager.LogicFightEffectContext(EntityOwner.LogicManager, sourceInfo);
            ctx.TargetId = this.CurrentCtx.Target ?.Id ?? 0;
            ctx.TriggerPos = EntityOwner.Pos;

            if(this.CurrentCtx.CastVec1 != null)
            {
                ctx.CastVec1 = this.CurrentCtx.CastVec1;
            }
            else
            {
                ctx.CastVec1 = this.EntityOwner.Pos + this.CurrentCtx.FaceDir;
            }

            ctx.InputVec = this.CurrentCtx.InputVec;

            ctx.TriggerPos = this.CurrentCtx.Position;

            ctx.RunningVariables = this.CurrentCtx.RunningVariables;

            return ctx;
        }


        private void TickIntern(float dt)
        {
            CurrentCtx.AbilityTime += dt;
            CurrentCtx.PhaseElapsed += dt;

            // debug
            if (!string.IsNullOrEmpty(CurrentCtx.DebugSavedAnimTag))
            {
                CurrentCtx.DebugSavedAnimTagTimer += dt;
                if (CurrentCtx.DebugSavedAnimTagTimer > 0.2f)
                {
                    //EntityOwner.viewer.ShowFakeFxEffect(CurrentCtx.DebugSavedAnimTag, EntityOwner.Pos);
                    CurrentCtx.DebugSavedAnimTagTimer = 0;
                }
            }

            // 执行定时事件（相对当前阶段时间）
            for (int i = 0; i < CurrentCtx._scheduled.Count; ++i)
            {
                var s = CurrentCtx._scheduled[i];
                while (s.Left > 0 && CurrentCtx.PhaseElapsed >= s.FireTime)
                {
                    //var executor = GetExecutor(s.Source.Effect);
                    //executor?.Apply(s.Source.Effect, CurrentCtx);
                    var effectCtx = GenerateEfffectContextByAbility();
                    EntityOwner.LogicManager.HandleLogicFightEffect(s.Source.Effect, effectCtx);
                    s.Left--;
                    s.FireTime += s.NextInterval > 0 ? s.NextInterval : float.MaxValue;
                }
            }

            // 检查是否是引导
            var phase = CurrentCtx.AbilityConfig.Phases[CurrentCtx.PhaseIndex];
            if(phase != null) 
            {
                if(CurrentCtx.PhaseIntentEffectId != 0)
                {
                    EntityOwner.LogicManager.viewer.UpdateRangeWarnEffect(CurrentCtx.PhaseIntentEffectId, EntityOwner.Pos, EntityOwner.FinalLook);
                }
            }

            bool phaseFinish = false;
            if(CurrentCtx.PhaseMarkSkip)
            {
                phaseFinish = true;
            }
            // 检查非holding阶段
            else if(!phase.HoldingPhase)
            {
                if(CurrentCtx.PhaseElapsed >= CurrentCtx.PhaseDuration)
                {
                    phaseFinish = true;
                }
            }
            else if(LogicTime.time - CurrentCtx.LastPhaseHoldTime > 0.5f)
            {
                phaseFinish = true;
            }

            // 阶段结束
            if (phaseFinish)
            {
                ExitPhase(CurrentCtx.PhaseIndex);
                var next = CurrentCtx.PhaseIndex + 1;
                if (next < CurrentCtx.AbilityConfig.Phases.Count)
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
                var effectCtx = GenerateEfffectContextByAbility();
                EntityOwner.LogicManager.HandleLogicFightEffect(e, effectCtx);
            }

            //_cooldownEnd = Time.time + CurrentCtx.AbilityConfig.Cooldown;

            CleanupPhase();



            _running = false;
            Debug.Log($"Ability {CurrentCtx.AbilityConfig.Id} complete");
            CurrentCtx = null;

            EventOnAbilityEnd?.Invoke(abName, 0);
        }

        public void Cancel()
        {
            if (!_running) return;
            var abName = CurrentCtx.AbilityConfig.Id;
            foreach (var e in CurrentCtx.AbilityConfig.OnCancelEffects)
            {
                var effectCtx = GenerateEfffectContextByAbility();
                EntityOwner.LogicManager.HandleLogicFightEffect(e, effectCtx);
            }

            var phase = CurrentCtx.AbilityConfig.Phases[CurrentCtx.PhaseIndex];

            //尝试进行cancel
            if (phase != null && phase.WithProgress && CurrentCtx.ShowProgressShowId != 0)
            {
                EntityOwner.viewer.TryCancelButtomProgress(CurrentCtx.ShowProgressShowId);
            }

            CleanupPhase();

            _running = false;
            Debug.Log($"Ability {CurrentCtx.AbilityConfig.Id} Cancel");
            CurrentCtx = null;

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

        public void ApplyUseWeaponHitBox(string weaponName, float openTime, List<MapFightEffectCfg> hitCfgs)
        {
            // 统一为hitwindow处理
            var win = CreateHitWindow(weaponName, openTime, hitCfgs);
            CurrentCtx.phaseHitWindows.Add(win.hitId, win);
        }


        public AbilityHitWindow CreateHitWindow(string weaponName, float openTime, List<MapFightEffectCfg> hitCfgs)
        {
            long hitId = ++HitWiindowIdCounter;

            var hitWin = new AbilityHitWindow()
            {
                hitId = hitId,
                weaponName = weaponName,
                openTime = Time.realtimeSinceStartup,
                durationTime = openTime,
                OnHitEffects = hitCfgs,
            };
            EventOnApplyUseWeapon?.Invoke(hitId, weaponName, openTime);

            return hitWin;
        }

        public void CloseHitWindow(string weaponName, long hitId)
        {
            EventOnCloseHitWindow?.Invoke(weaponName, hitId);
        }

        public void OnUseWeaponHitCallback(long hitId, long hitEntityId)
        {
            if (CurrentCtx == null)
            {
                Debug.LogError($"OnUseWeaponHitCallback hit not found {hitId}");
                return;
            }
            CurrentCtx.phaseHitWindows.TryGetValue(hitId, out var window);
            //  todo 多次命中
            if (!window.HitRecord.Contains(hitEntityId))
            {
                window.HitRecord.Add(hitEntityId);
                //Debug.Log("OnWeaponHitCallback " + "hittttttttttttttttttttttttttttttttttttttttttttttttttttttt " + hitEntityId);

                if (window.OnHitEffects != null)
                {
                    var hitEntity = EntityOwner.LogicManager.AreaManager.GetLogicEntiy(hitEntityId);
                    //MainGameManager.Instance.gameLogicManager.logicEntityDict.TryGetValue(hitEntityId, out var hitEntity);
                    if (hitEntity != null && !hitEntity.MarkDestroyed)
                    {
                        var srcInfo = new EffectSourceInfo()
                        {
                            SrcType = ESourceType.Ability,
                            SrcEntityId = EntityOwner.Id,
                        };

                        foreach (var hitEffect in window.OnHitEffects)
                        {
                            GameLogicManager.LogicFightEffectContext newCtx = new(EntityOwner.LogicManager, srcInfo);

                            newCtx.TargetId = hitEntity.Id;
                            newCtx.TriggerPos = EntityOwner.Pos;
                            //newCtx.CastVec1 = hitEntity.Pos - EntityOwner.Pos;
                            newCtx.CastVec1 =  EntityOwner.FinalLook;

                            EntityOwner.LogicManager.HandleLogicFightEffect(hitEffect, newCtx);
                        }

                        MainGameManager.Instance.StartHitStop(0.04f);
                    }
                    else
                    {
                        Debug.Log($"OnWeaponHitCallback hit target not found or dead {hitEntityId}");
                    }
                }
            }
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
