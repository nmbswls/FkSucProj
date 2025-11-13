using My.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.XR;
using UnityEngine.UIElements;
using static My.Map.Entity.MapEntityAbilityController;
using static Unity.VisualScripting.Member;
using static UnityEditor.Progress;
using static UnityEngine.Rendering.VolumeComponent;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Entity
{
    public class InterruptUtils
    {
        public static EAbilityInterruptMask SourceToMask(MapEntityAbilityController.InterruptSource s)
        {
            return s switch
            {
                MapEntityAbilityController.InterruptSource.Hit => EAbilityInterruptMask.Hit,
                MapEntityAbilityController.InterruptSource.Stun => EAbilityInterruptMask.Stun,
                MapEntityAbilityController.InterruptSource.KnockUp => EAbilityInterruptMask.KnockUp,
                MapEntityAbilityController.InterruptSource.InputCancel => EAbilityInterruptMask.InputCancel,
                _ => EAbilityInterruptMask.System
            };
        }
    }


    public class AbilityHitWindow
    {
        public long hitId;
        public float openTime;
        public float durationTime;
        public List<long> HitRecord = new();

        public int HitParam0;
        public int HitParam1;

        public List<MapFightEffectCfg> OnHitEffects; // 原始数据 还是生成hitwindow专用数据放入？

        // 来源是weapon 还是 技能？
    }

    public class MapEntityAbilityController
    {
        public BaseUnitLogicEntity EntityOwner { get; set; }

        public long HitWiindowIdCounter = 10000;

        public class AbilityRunningContext
        {
            public ILogicEntity Actor;         // 施动者
            public ILogicEntity Target;        // 目标对象（如门或敌人），可为空
            public Vector2? FaceDir;           // 面朝方向
            public Vector2? CastDir;           // 面朝方向
            public Vector2? Position;         // 施放位置（如脚下或点击点）
            public Dictionary<string, object> UserData = new();
            // 时间推进
            public float AbilityTime;        // 自开始起累计时间

            public MapAbilitySpecConfig AbilityConfig;
            public int PhaseIndex;
            public float PhaseElapsed;       // 当前阶段已用时
            public float PhaseDuration;      // 当前阶段时间

            //public List<Modifier> PhaseModifiers = new();
            public List<long> PhaseBindBuffs = new();
            public Dictionary<long, AbilityHitWindow> phaseHitWindows = new();

            public string? openClickkkType;
            public float? openClickkkDuration;

            public ISceneAbilityViewer? viewer; // 表现层接口

            public string DebugSavedAnimTag;
            public float DebugSavedAnimTagTimer;

            public SourceKey? SrcKey = null;
            public List<ScheduledEvent> _scheduled = new();

            public long ShowProgressShowId = 0;

            // 变量集合
            public Dictionary<string, string> RunningVariables = new();
            public Dictionary<string, string> PhaseOverrideAnims = null;

            public Dictionary<string, long> RunningStorage = new();


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


        private Dictionary<string, float> _sharedCooldown = new();



        public event Action<long, string, float> EventOnApplyUseWeapon;
        public event Action<long> EventOnCloseHitWindow;


        public class AbilityState
        {
            public string AbilityName;
            public float lastUseTime;
            public float cooldown;

            public MapAbilitySpecConfig cacheConfig;
        }

        public Dictionary<string, AbilityState> AbilityStateInfos = new();


        public bool IsRunning { get { return _running; } }


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


        public MapEntityAbilityController(BaseUnitLogicEntity owner)
        {
            this.EntityOwner = owner;
        }

        /// <summary>
        /// 获取当前准备好的主动技能
        /// </summary>
        /// <returns></returns>
        public bool CheckAnyReadyAbility()
        {
            foreach (var abName in AbilityStateInfos.Keys)
            {
                if (!IsAbilityReady(abName))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前准备好的主动技能
        /// </summary>
        /// <returns></returns>
        public List<AbilityState> GetAllReadyAbilities()
        {
            List<AbilityState> ret = new();
            foreach (var abName in AbilityStateInfos.Keys)
            {
                if(!IsAbilityReady(abName))
                {
                    continue;
                }

                ret.Add(AbilityStateInfos[abName]);
            }

            return ret;
        }

        /// <summary>
        /// 获取当前准备好的主动技能
        /// </summary>
        /// <returns></returns>
        public bool IsAbilityReady(string abilityName)
        {

            AbilityStateInfos.TryGetValue(abilityName, out var abState);
            if(abState == null)
            {
                return false;
            }

            if (abState.cacheConfig.IsPassive)
            {
                return false;
            }

            if (abState.cooldown > 0)
            {
                return false;
            }

            if (abState.cacheConfig.TypeTag == AbilityTypeTag.Combat)
            {
                if (EntityOwner.IsHMode)
                {
                    return false;
                }
            }
            else if (abState.cacheConfig.TypeTag == AbilityTypeTag.HMode)
            {
                if (!EntityOwner.IsHMode)
                {
                    return false;
                }
            }

            return true;
        }


        public bool RegisterAbility(MapAbilitySpecConfig abilityCfg)
        {
            if (AbilityStateInfos.TryGetValue(abilityCfg.Id, out var state))
            {
                Debug.Log($"RegisterAbility duplicate {abilityCfg.Id}");
                return false;
            }
            var newState = new AbilityState()
            {
                AbilityName = abilityCfg.Id,
                cacheConfig = abilityCfg
            };

            AbilityStateInfos[newState.AbilityName] = newState;
            return true;
        }

        public virtual void TryUseAbility(string abilityName, Vector2? castDir = null, ILogicEntity target = null, Dictionary<string, string> overrideParams = null, Dictionary<string, string> phaseOverrideAnims = null)
        {
            AbilityStateInfos.TryGetValue(abilityName, out var abilityState);
            if (abilityState == null || abilityState.cacheConfig == null)
            {
                return;
            }

            if(!IsAbilityReady(abilityName))
            {
                return;
            }

            TryStart(abilityState, castDir: castDir, target: target, runningOverrides: overrideParams, phaseOverrideAnims: phaseOverrideAnims);
        }

        public void Tick(float dt)
        {
            foreach(var abState in AbilityStateInfos.Values)
            {
                if(abState.cooldown > 0)
                {
                    abState.cooldown -= dt;
                }
            }

            if (!_running) return;
            TickIntern(dt);
        }




        protected bool TryStart(AbilityState abState, Vector2? castDir = null, ILogicEntity target = null, Dictionary<string, string> runningOverrides = null, Dictionary<string, string> phaseOverrideAnims = null)
        {
            if (_running) return false;

            CurrentCtx = new AbilityRunningContext
            {
                Actor = EntityOwner,
                Target = target,
                AbilityTime = 0f,
                PhaseIndex = 0,
                PhaseElapsed = 0f,
                AbilityConfig = abState.cacheConfig,
                viewer = EntityOwner.viewer,
                RunningVariables = runningOverrides,
                CastDir = castDir,
                Position = EntityOwner.Pos,

                PhaseOverrideAnims = phaseOverrideAnims,
            };
            foreach (var e in abState.cacheConfig.OnStartEffects)
            {
                var effectCtx = GenerateEfffectContextByAbility();
                EntityOwner.LogicManager.HandleLogicFightEffect(e, effectCtx);
            }
            EnterPhase(0);
            _running = true;

            if(abState.cacheConfig.CoolDown > 0)
            {
                abState.cooldown = abState.cacheConfig.CoolDown;
            }
            abState.lastUseTime = LogicTime.time;

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

            if (CurrentCtx.PhaseIndex >= CurrentCtx.AbilityConfig.Phases.Count)
            {
                Debug.Log("TryInterrupt satate wrror phase index >= count");
                return false;
            }

            var phase = CurrentCtx.AbilityConfig.Phases[CurrentCtx.PhaseIndex];
            if (phase == null) return false;

            if (EntityOwner.GetAttr(AttrIdConsts.StatUnstoppable) > 0)
            {
                Debug.Log("TryInterrupt unstoppable");
                return false;
            }

            // 不可被该来源打断 跳出
            if (!phase.InterruptMask.HasFlag(InterruptUtils.SourceToMask(req.source)))
                return false;

            // 执行打断
            CleanupPhase();
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

            CurrentCtx.PhaseBindBuffs.Clear();

            if (CurrentCtx.phaseHitWindows.Count > 0)
            {
                foreach (var hitWindow in CurrentCtx.phaseHitWindows.Values)
                {
                    EventOnCloseHitWindow?.Invoke(hitWindow.hitId);
                }

                CurrentCtx.phaseHitWindows.Clear();
            }

            if (!string.IsNullOrEmpty(CurrentCtx.openClickkkType))
            {
                EntityOwner.LogicManager.viewer.CloseClickkkWindow(CurrentCtx.openClickkkType, isInterrupt);
                CurrentCtx.openClickkkType = null;
            }
        }

        public GameLogicManager.LogicFightEffectContext GenerateEfffectContextByAbility()
        {
            var ctx = new GameLogicManager.LogicFightEffectContext(EntityOwner.LogicManager, new SourceKey()
            {
                entityId = EntityOwner.Id,
                type = SourceType.AbilityActive,
                sourceId = this.CurrentCtx.AbilityConfig.Id,
            });
            ctx.Actor = this.CurrentCtx.Actor;
            ctx.Target = this.CurrentCtx.Target;
            ctx.FaceDir = this.CurrentCtx.FaceDir;
            ctx.CastDir = this.CurrentCtx.CastDir;
            ctx.Position = this.CurrentCtx.Position;

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


            // 阶段结束
            if (CurrentCtx.PhaseElapsed >= CurrentCtx.PhaseDuration)
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
        }

        public void Cancel()
        {
            if (!_running) return;
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
        }

        private void ReleaseLocks()
        {
            //var mover = GetComponent<CharacterMover>();
            //if (mover) mover.LockMovement = false;
        }

        public void ApplyUseWeaponHitBox(string weaponName, float openTime, List<MapFightEffectCfg> hitCfgs)
        {
            // 统一为hitwindow处理
            long hitId = ++HitWiindowIdCounter;

            var hitWin = new AbilityHitWindow()
            {
                hitId = hitId,
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
                        foreach (var hitEffect in window.OnHitEffects)
                        {
                            GameLogicManager.LogicFightEffectContext newCtx = new(EntityOwner.LogicManager, new SourceKey() { entityId = EntityOwner.Id, type = SourceType.AbilityActive });

                            newCtx.Actor = EntityOwner;
                            newCtx.CastDir = hitEntity.Pos - EntityOwner.Pos;
                            newCtx.Target = hitEntity;
                            newCtx.Position = hitEntity.Pos;

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
