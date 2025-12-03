using My.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static My.GameLogicManager;

namespace My.Map.Entity
{
    public class InterruptUtils
    {
        public static EAbilityInterruptMask SourceToMask(MapEntityAbilityExecutor.InterruptSource s)
        {
            return s switch
            {
                MapEntityAbilityExecutor.InterruptSource.Hit => EAbilityInterruptMask.Hit,
                MapEntityAbilityExecutor.InterruptSource.Stun => EAbilityInterruptMask.Stun,
                MapEntityAbilityExecutor.InterruptSource.KnockUp => EAbilityInterruptMask.KnockUp,
                MapEntityAbilityExecutor.InterruptSource.InputCancel => EAbilityInterruptMask.InputCancel,
                _ => EAbilityInterruptMask.System
            };
        }
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

    public class MapEntityAbilityExecutor
    {
        public BaseUnitLogicEntity EntityOwner { get; set; }

        public long HitWiindowIdCounter = 10000;

        public class AbilityRunningContext
        {
            public ILogicEntity Actor;         // 施动者
            public ILogicEntity Target;        // 目标对象（如门或敌人），可为空
            public Vector2 FaceDir;           // 面朝方向
            public Vector2? CastVec1;           // 面朝方向
            public Vector2? Position;         // 施放位置（如脚下或点击点）
            public Dictionary<string, object> UserData = new();
            // 时间推进
            public float AbilityTime;        // 自开始起累计时间

            public MapAbilitySpecConfig AbilityConfig;
            public int PhaseIndex;
            public float PhaseElapsed;       // 当前阶段已用时
            public float PhaseDuration;      // 当前阶段时间
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

            if(phase.CanInputInterrupt)
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

        public enum InterruptSource { InputCancel, Hit, Stun, KnockUp, System }

        public struct InterruptRequest
        {
            public InterruptSource source;
            public int priority;     // 来源优先级（例如：Stun=100, Hit=50, InputCancel=30）
            public object payload;   // 可选：时长、方向、效果ID等
        }


        public MapEntityAbilityExecutor(BaseUnitLogicEntity owner)
        {
            this.EntityOwner = owner;
        }

        
        

        public virtual bool TryUseAbility(string abilityName, Vector2? castDir = null, ILogicEntity target = null, Dictionary<string, string> overrideParams = null, Dictionary<string, string> phaseOverrideAnims = null, string? groupOwnerName = null)
        {
            var config = AbilityLibrary.GetAbilityConfig(abilityName);
            if (config == null)
            {
                return false;
            }

            return TryStart(config, castVec1: castDir, target: target, runningOverrides: overrideParams, phaseOverrideAnims: phaseOverrideAnims, groupOwnerName: groupOwnerName);
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
        protected bool TryStart(MapAbilitySpecConfig abilityConf, Vector2? castVec1 = null, ILogicEntity target = null, Dictionary<string, string> runningOverrides = null, Dictionary<string, string> phaseOverrideAnims = null, string? groupOwnerName = null)
        {
            // 检查可打断
            if (!IsActionable())
            {
                return false;
            }

            EntityOwner.TryInterrupt(new InterruptRequest()
            {
                source = InterruptSource.InputCancel,
            });

            if (IsRunning)
            {
                return false;
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
                CastVec1 = castVec1,
                FaceDir = EntityOwner.DesiredFaceDir,
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

            if(abilityConf.MaxStepDistance > 0)
            {
                EntityOwner.StartDash(EntityOwner.DesiredFaceDir, 0.1f, abilityConf.MaxStepDistance / 0.1f, null);
            }

            Debug.Log($"entity {EntityOwner.Id} TryStart {abilityConf.Id}");

            return true;
        }


        private void EnterPhase(int index)
        {
            CurrentCtx.PhaseIndex = index;
            CurrentCtx.PhaseElapsed = 0f;
            var phase = CurrentCtx.AbilityConfig.Phases[index];

            // 锁动作
            var phaseDurRaw = CurrentCtx.GetVariatyRawVal(phase.DurationValue);
            var phaseDur = 0f;
            if (!float.TryParse(phaseDurRaw, out phaseDur))
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
                    var instId = EntityOwner.BuffManager.AddBuff(EntityOwner.Id, buffId);
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
                if(phase.PreviewIntent.IsCircle)
                {
                    var eId = EntityOwner.LogicManager.viewer.ShowRangeWarnEffect(1, phase.PreviewIntent.RangeRadius, 0, EntityOwner.Pos, EntityOwner.FaceDir, phaseDur);
                    CurrentCtx.PhaseIntentEffectId = eId;
                }
                else
                {
                    var eId = EntityOwner.LogicManager.viewer.ShowRangeWarnEffect(2, phase.PreviewIntent.RangeWidth, phase.PreviewIntent.RangeLen, EntityOwner.Pos, EntityOwner.FaceDir, phaseDur);
                    CurrentCtx.PhaseIntentEffectId = eId;
                }
            }

            if(phase.CanInputInterrupt)
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
            if(phase != null)
            {
                if(req.source != InterruptSource.InputCancel)
                {
                    if (EntityOwner.GetAttr(AttrIdConsts.StatUnstoppable) > 0)
                    {
                        // 不可被该来源打断 跳出
                        return false;
                    }
                }
                else
                {
                    if(!phase.CanInputInterrupt)
                    {
                        Debug.Log("TryInterrupt not work");
                        return false;
                    }
                }
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
                    EntityOwner.viewer.ShowFakeFxEffect(CurrentCtx.DebugSavedAnimTag, EntityOwner.Pos);
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
                    EntityOwner.LogicManager.viewer.UpdateRangeWarnEffect(CurrentCtx.PhaseIntentEffectId, EntityOwner.Pos, EntityOwner.FaceDir);
                }
            }

            // 阶段结束
            if (CurrentCtx.PhaseMarkSkip || CurrentCtx.PhaseElapsed >= CurrentCtx.PhaseDuration)
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

        private MapAbilityPhase GetCurrentPhase()
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
            long hitId = ++HitWiindowIdCounter;

            var hitWin = new AbilityHitWindow()
            {
                hitId = hitId,
                weaponName = weaponName,
                openTime = Time.realtimeSinceStartup,
                durationTime = openTime,
                OnHitEffects = hitCfgs,
            };
            CurrentCtx.phaseHitWindows.Add(hitId, hitWin);

            EventOnApplyUseWeapon?.Invoke(hitId, weaponName, openTime);
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
                Debug.Log("OnWeaponHitCallback " + "hittttttttttttttttttttttttttttttttttttttttttttttttttttttt " + hitEntityId);

                if (window.OnHitEffects != null)
                {
                    var hitEntity = EntityOwner.LogicManager.AreaManager.GetLogicEntiy(hitEntityId);
                    //MainGameManager.Instance.gameLogicManager.logicEntityDict.TryGetValue(hitEntityId, out var hitEntity);
                    if (hitEntity != null)
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
                            newCtx.TriggerPos = hitEntity.Pos;
                            newCtx.CastVec1 = hitEntity.Pos;


                            EntityOwner.LogicManager.HandleLogicFightEffect(hitEffect, newCtx);
                        }
                    }
                    else
                    {
                        Debug.Log($"OnWeaponHitCallback hit target not found {hitEntityId}");
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
