using System;
using System.Collections.Generic;
using Config.Unit;
using My.Config;
using My.Map.Fight;
using Map.Logic.Events;
using My.Map.Entity;
using My.Map.Fight;
using My.Map.Logic;
using My.Map.Unit;
using My.Player.Bag;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static My.Map.Fight.FightStruct;
using My.Player;
using Unity.VisualScripting;
using static My.UI.FishingMiniGamePanel;
using static UnityEngine.RuleTile.TilingRuleOutput;
using My.Map.Ground;
using My;
using HighlightPlus;


namespace My.Map
{
    public struct UnitHitInfo
    {
        public Vector2? HitDir;
        public Vector2? HitPoint;

        public UnitHitInfo(Vector2? hitDir, Vector2? hitPoint = null)
        {
            HitDir = hitDir;
            HitPoint = hitPoint;
        }
    }

    public class UnitMoveBehaveInfo
    {
        public enum EMoveBehaveType
        {
            NoMove,
            Patrol,
            MovePath,
            Hunting,
            InPatrolGroup,
            /// <summary>
            /// 前往 MoveToDespawnTarget 逻辑坐标后销毁实体（动态压力守卫退场 / 路人离场可复用）
            /// </summary>
            MoveToThenDespawn,
            MoveToPoint,
            /// <summary>
            /// 在 MoveToTarget 为圆心、WanderRadius 为半径的范围内闲逛
            /// </summary>
            WanderAroundPoint,
            /// <summary>
            /// 沿 PathLoopPoints 世界坐标点列循环走动（NamedPath 解析结果）
            /// </summary>
            PathLoop,
        }

        public EMoveBehaveType MoveBehaveMode 
        { 
            get; 
            set; 
        }

        public long FollowPatrolId;
        public Vector2 PatrolGroupRelativePos;
        public bool DisappearOnArrive;
        public string MovePath = null;

        /// <summary>
        /// MoveToThenDespawn 目标点（逻辑坐标）
        /// </summary>
        public Vector2 MoveToDespawnTarget;
        public Vector2 MoveToTarget;

        /// <summary>
        /// WanderAroundPoint 半径（逻辑单位）
        /// </summary>
        public float WanderRadius;

        /// <summary>
        /// 到达/站定后的可选朝向
        /// </summary>
        public bool HasFaceDir;
        public Vector2 FaceDir;

        /// <summary>
        /// PathLoop 世界坐标点列
        /// </summary>
        public List<Vector2> PathLoopPoints = new();

        // 路网巡逻实例数据（来自 LogicEntityRecord4Npc / 导出）
        public string PatrolPortalNetworkId = string.Empty;

        public List<string> PatrolCycleNodeIds = new();

        public UnitMoveBehaveInfo Clone()
        {
            var copy = new UnitMoveBehaveInfo
            {
                MoveBehaveMode = MoveBehaveMode,
                FollowPatrolId = FollowPatrolId,
                PatrolGroupRelativePos = PatrolGroupRelativePos,
                DisappearOnArrive = DisappearOnArrive,
                MovePath = MovePath,
                MoveToDespawnTarget = MoveToDespawnTarget,
                MoveToTarget = MoveToTarget,
                WanderRadius = WanderRadius,
                HasFaceDir = HasFaceDir,
                FaceDir = FaceDir,
                PatrolPortalNetworkId = PatrolPortalNetworkId ?? string.Empty,
            };
            if (PathLoopPoints != null && PathLoopPoints.Count > 0)
            {
                copy.PathLoopPoints.AddRange(PathLoopPoints);
            }

            if (PatrolCycleNodeIds != null && PatrolCycleNodeIds.Count > 0)
            {
                copy.PatrolCycleNodeIds.AddRange(PatrolCycleNodeIds);
            }

            return copy;
        }
    }

    public abstract partial class BaseUnitLogicEntity : LogicEntityBase, IThrowLauncher, IThrowTarget, IWithEnmity,
        IUnitWithVision
    {
        public MapEntitySkillManager ablilityManager;
        public MapEntityAbilityExecutor abilityController;
        public float viewRadius = 5f;
        public float fovDegrees = 150f;

        /// <summary>
        /// 配表或运行时设定的基底视野锥类型；有效类型见 GetEffectiveVisionConeKind。
        /// </summary>
        public VisionConeKind BaseVisionConeKind { get; set; } = VisionConeKind.Normal;

        public bool MarkNoLogic; // 
        public bool MarkUnsensored { get; set; }

        public LogicEntityRecord4UnitBase UnitBaseRecord 
        { 
            get 
            { 
                return (LogicEntityRecord4UnitBase)BindingRecord; 
            } 
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract bool IsInCombat
        {
            get;
        }

        public BuffDamageTriggerContext LastBuffDamageTriggerContext;

        public virtual bool CanUnsensored()
        {
            return true;
        }

        public enum EMacroBehaveAuthority
        {
            None = 0,
            Routine = 1,
            Wanted = 2,
            Other = 3,
        }

        // Record/导出基底意图；宏观机制写微观 Override，不回写 Record
        public UnitMoveBehaveInfo BaseMoveBehaveInfo { get; protected set; }

        UnitMoveBehaveInfo _macroMoveBehave;
        EMacroBehaveAuthority _macroBehaveAuthority = EMacroBehaveAuthority.None;
        string _macroIdleAnim = string.Empty;
        string _macroMoveAnim = string.Empty;

        public bool HasMacroMoveBehave => _macroMoveBehave != null;
        public EMacroBehaveAuthority MacroMoveBehaveAuthority =>
            _macroMoveBehave != null ? _macroBehaveAuthority : EMacroBehaveAuthority.None;
        public EMacroBehaveAuthority MacroLocomotionAuthority =>
            HasMacroLocomotionAnims() ? _macroBehaveAuthority : EMacroBehaveAuthority.None;

        bool HasMacroLocomotionAnims()
        {
            return !string.IsNullOrEmpty(_macroIdleAnim) || !string.IsNullOrEmpty(_macroMoveAnim);
        }

        void SyncMacroBehaveAuthority()
        {
            if (_macroMoveBehave == null && !HasMacroLocomotionAnims())
            {
                _macroBehaveAuthority = EMacroBehaveAuthority.None;
            }
        }

        // 生效意图：宏观 Override 优先，否则 Base
        public UnitMoveBehaveInfo MoveBehaveInfo => _macroMoveBehave ?? BaseMoveBehaveInfo;

        // 宏观机制仲裁优先级（与 Rule.Priority 无关）
        public static int GetMacroPriority(EMacroBehaveAuthority authority)
        {
            return authority switch
            {
                EMacroBehaveAuthority.Wanted => 30,
                EMacroBehaveAuthority.Routine => 20,
                EMacroBehaveAuthority.Other => 10,
                _ => 0,
            };
        }

        EMacroBehaveAuthority GetActiveMacroBehaveAuthority()
        {
            if (_macroMoveBehave != null || HasMacroLocomotionAnims())
            {
                return _macroBehaveAuthority;
            }

            return EMacroBehaveAuthority.None;
        }

        public bool CanApplyMacroBehaveAuthority(EMacroBehaveAuthority authority)
        {
            var active = GetActiveMacroBehaveAuthority();
            if (active == EMacroBehaveAuthority.None)
            {
                return true;
            }

            return GetMacroPriority(authority) >= GetMacroPriority(active);
        }

        void PreemptLowerMacroBehave(EMacroBehaveAuthority incoming)
        {
            var active = GetActiveMacroBehaveAuthority();
            if (active == EMacroBehaveAuthority.None)
            {
                return;
            }

            if (GetMacroPriority(incoming) <= GetMacroPriority(active))
            {
                return;
            }

            if (_macroMoveBehave != null && _macroBehaveAuthority == active)
            {
                _macroMoveBehave = null;
            }

            if (_macroBehaveAuthority == active)
            {
                _macroIdleAnim = string.Empty;
                _macroMoveAnim = string.Empty;
            }

            SyncMacroBehaveAuthority();
        }

        public UnitMoveBehaveInfo TryAllocateMacroMoveBehave(EMacroBehaveAuthority authority)
        {
            if (!CanApplyMacroBehaveAuthority(authority))
            {
                return null;
            }

            PreemptLowerMacroBehave(authority);

            _macroBehaveAuthority = authority;
            _macroMoveBehave = new UnitMoveBehaveInfo();
            return _macroMoveBehave;
        }

        public bool SetMacroLocomotion(EMacroBehaveAuthority authority, string idleAnim, string moveAnim)
        {
            if (authority == EMacroBehaveAuthority.None || !CanApplyMacroBehaveAuthority(authority))
            {
                return false;
            }

            PreemptLowerMacroBehave(authority);

            _macroBehaveAuthority = authority;
            _macroIdleAnim = idleAnim ?? string.Empty;
            _macroMoveAnim = moveAnim ?? string.Empty;
            RequestAnimLayerRefresh(0);
            return true;
        }

        public bool ClearMacroLocomotion(EMacroBehaveAuthority authority)
        {
            if (_macroBehaveAuthority != authority || !HasMacroLocomotionAnims())
            {
                return false;
            }

            _macroIdleAnim = string.Empty;
            _macroMoveAnim = string.Empty;
            SyncMacroBehaveAuthority();
            RequestAnimLayerRefresh(0);
            return true;
        }

        public bool ClearMacroMoveBehave(EMacroBehaveAuthority authority)
        {
            if (_macroMoveBehave == null)
            {
                return true;
            }

            if (_macroBehaveAuthority != authority)
            {
                return false;
            }

            _macroMoveBehave = null;
            SyncMacroBehaveAuthority();
            return true;
        }

        public bool ClearMacroBehave(EMacroBehaveAuthority authority)
        {
            if (_macroBehaveAuthority != authority)
            {
                return false;
            }

            _macroMoveBehave = null;
            _macroIdleAnim = string.Empty;
            _macroMoveAnim = string.Empty;
            _macroBehaveAuthority = EMacroBehaveAuthority.None;
            RequestAnimLayerRefresh(0);
            return true;
        }

        public override string GetAnimOverride(string rawAnimName)
        {
            if (HasMacroLocomotionAnims())
            {
                var idle = _macroIdleAnim;
                var move = _macroMoveAnim;
                var candidate = rawAnimName == "idle" ? idle :
                    rawAnimName == "move" || rawAnimName == "walk" ? move : string.Empty;
                if (!string.IsNullOrEmpty(candidate))
                {
                    return rawAnimName == "walk" && string.IsNullOrEmpty(move) ? rawAnimName :
                        rawAnimName == "idle" ? idle : move;
                }
            }

            return base.GetAnimOverride(rawAnimName);
        }

        public CompFightMeleeSlot MeleeSlotManager;

        //public Vector2? LastInterruptPos;


        public bool IsDead = false;

        public int BindRoomId;

        //public AbsMapUnitConfig unitCfg;

        /// <summary>
        /// event
        /// </summary>
        public event Action<long, long?, UnitHitInfo> EventOnHit;
        public event Action<long> EventOnEnmityBehave;
        public event Action<long> EventOnDie;
        public event Action<long> EventOnAttachStatusChanged;
        public event Action<long> EventOnGhostChange;
        public event Action<long> EventOnInvisibleChange;
        // id, 来源实体, 实际 applied 的 HP 变化量（finalDelta，负数为扣血）
        public event Action<long, long?, long> EventOnHpChanged;


        protected float externalDecay = 30f;          // 外力自然衰减（每秒）
        public BaseUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var unitRecord = (LogicEntityRecord4UnitBase)bindingRecord;
            BaseMoveBehaveInfo = new UnitMoveBehaveInfo();

            MarkUnsensored = unitRecord.Unsensored;
            MarkNoLogic = unitRecord.MarkNoLogic;
            IsAttaching = unitRecord.MarkAttaching;
            IsDead = unitRecord.MarkDefeated;
        }

        public override void Initialize()
        {
            base.Initialize();

            // 优先应用覆盖值
            if (UnitBaseRecord.FactionId != EFactionId.None)
            {
                this.FactionId = UnitBaseRecord.FactionId;
            }
            else
            {
                //if (unitCfg != null)
                //{
                //    this.FactionId = unitCfg.DefaultFactionId;
                //}
            }

            // get meta info
            InitAbility();

            if(Type != EEntityType.Player)
            {
                InitEnmitySystem();
                InitAggroSystem();
            }

            InitVisionSystem();

            InitGazeModule();


            if (MarkNoLogic)
            {
                LogicManager.globalBuffManager.AddBuff(this.Id, "system_no_logic");
            }

            HitWindowRegistry = new(this);

            ResolveLogicYAtPosition(LogicYSetReason.Script);
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            // 计时、条件检查、冷却等

            if (!MarkNoLogic && !IsDead && !MarkUnsensored)
            {
                TickActivateState(dt);
            }

            UpdateControlledMove(dt);

            // 外力自然衰减（除非在Dash中保持常速）
            if (controlledMoveCtx == null)
            {
                externalVel = Vector2.MoveTowards(externalVel, Vector2.zero, externalDecay * dt);
            }

            ablilityManager?.Tick(dt);
            abilityController?.Tick(dt);

            if(!MarkDestroyed)
            {
                attributeStore.Commit();
            }

            if(!MarkDestroyed && !IsAttaching && !IsDead && !CheckHasState(AttrIdConsts.Unmovable))
            {
                if(MotorSystem.FreeMoveInput.magnitude > 0.1f)
                {
                    TryInterrupt(new InterruptRequest()
                    {
                        source = EInterruptSource.Move,
                    });
                }
            }

            TickUnitEvilAlert();

            MeleeSlotManager?.UpdateSlotPositions();

            if(DialogControlled)
            {
                CheckDialogActorBehaviour();
            }

            UpdateUnitOffsetZ();
        }


        

        protected override bool CanTickGroundOverlay()
        {
            if (MarkNoLogic || IsDead || MarkUnsensored)
            {
                return false;
            }
            return true;
        }

        protected virtual void TickResourceChange(float interval)
        {

        }

        protected virtual void TickActivateState(float dt)
        {
            AggroSystem?.Tick(dt);
            UpdateGazeModule();
            VisionSystem?.TryUpdateNoticeList();
            EnmitySystem?.Tick(dt);
        }

        protected override bool IsMovable()
        {
            return true;
        }


        public virtual VisionConeKind GetEffectiveVisionConeKind() => BaseVisionConeKind;

        /// <summary>
        /// 全角上限（普通 180 / 警觉 270 / 全知 360）
        /// </summary>
        public static float MaxFullFovDegreesForKind(VisionConeKind kind) => kind switch
        {
            VisionConeKind.Normal => 180f,
            VisionConeKind.Alert => 270f,
            VisionConeKind.Omniscient => 360f,
            _ => 180f,
        };

        public static float ClampFovDegreesForKind(float rawFov, VisionConeKind kind)
        {
            float cap = MaxFullFovDegreesForKind(kind);
            return Mathf.Clamp(Mathf.Abs(rawFov), 1f, cap);
        }

        /// <summary>
        /// 背身极近距离「接触感知」半径（仍可被射线遮挡）；与纯视觉锥分开。
        /// </summary>
        public virtual float GetVisionContactSenseRadius() => 0.35f;


        public virtual bool IsOmniVision()
        {
            return GetEffectiveVisionConeKind() == VisionConeKind.Omniscient;
        }


        public override void OnEnterAOI()
        {
            if(MoveBehaveInfo.MoveBehaveMode == UnitMoveBehaveInfo.EMoveBehaveType.InPatrolGroup)
            {
                var groupEntity = LogicManager.GetLogicEntity(MoveBehaveInfo.FollowPatrolId);
                this.SetPosition(groupEntity.Pos + MoveBehaveInfo.PatrolGroupRelativePos);
            }
        }


        /// <summary>
        /// 死亡或失去意识
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="lastIntent"></param>
        public virtual void OnUnitDie(int reason, ResourceDeltaIntent lastIntent = null)
        {
            if (lastIntent != null && lastIntent.deltaFlags.HasFlag(EDmgFlag.Nonlethal) && CanUnsensored())
            {
                LogicManager.globalBuffManager.RequestAddBuff(this.Id, "unsensored");
                this.MarkUnsensored = true;
            }
            else
            {
                this.IsDead = true;

                EventOnDie?.Invoke(this.Id);

                LogicManager.LogicEventBus.Publish(new MLEUnitDie()
                {
                    Ctx = new()
                    {

                        HappenPos = Pos,
                        SourceEntity = LogicManager.playerLogicEntity,
                    },
                    EntityId = this.Id,
                    Pos = Pos,
                    LastIntent = lastIntent
                });
            }

            foreach(var b in BuffContainer.Values)
            {
                b.DoBuffTrigger(ETriggerType.OnDie);
            }

            TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.Die,
            });
            
            LogicManager.LogicEventBus.Publish(new MLEUnitCantAlert()
            {
                Ctx = new()
                {

                    HappenPos = Pos,
                    SourceEntity = LogicManager.playerLogicEntity,
                },
                EntityId = this.Id,
            });
        }


        public virtual void ForceUnitUnsensored(int reason, long srcId)
        {
            //foreach (var b in BuffContainer.Values)
            //{
            //    b.DoBuffTrigger(ETriggerType.OnDie);
            //}
            MarkUnsensored = true;
            LogicManager.globalBuffManager.RequestAddBuff(this.Id, "unsensored");

            TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.Die,
            });

            LogicManager.LogicEventBus.Publish(new MLEUnitCantAlert()
            {
                Ctx = new()
                {

                    HappenPos = Pos,
                    SourceEntity = LogicManager.playerLogicEntity,
                },
                EntityId = this.Id,
            });
        }

        public virtual int GetUnitLevel()
        {
            return 1;
        }

        #region 移动控制


        /// <summary>
        /// 包含方向与速度的当前有效位移 由输入和受控移动共同决定
        /// </summary>
        public Vector2 activeMoveVec;

        public Vector2 externalVel;
        public Vector2 knockVel;              // 当前击退速度(替代 externalVel 或由其驱动)
        public float knockTimer;

        public float accel = 20f;

        
        

        //public class DashIntent
        //{
        //    public float dashTimeLeft = 0f;

        //    public float dashDuration;
        //    public bool dashIFrameActive = false;
        //    public float dashIFrameLeft = 0f;
        //    public Vector2 dashDir;
        //    public float dashSpeed;

        //    public List<MapFightEffectCfg> OnHitUnitCfg;
        //    public Action onCollide;
        //}
        //public DashIntent? dashIntent;

        //public class KnockBackIntent
        //{
        //    public float knockbackMinEndSpeed;
        //    public float knockbackPower;
        //    public Vector2 knockDir;
        //    public Action<int>? onKnockEnd;
        //}
        //public KnockBackIntent? knockBackIntent;

        public class ControlledMoveCtx
        {
            public enum EType 
            { 
                None, 
                Dash, 
                Knock, 
                Step,
                Pull,
            }
            public EType Type;
            public float Duration;
            public float MinEndSpeed;
            public bool DashIFrameActive = false;
            public Vector2 MoveDir;
            public enum EControlMode
            {
                None,
                FixSpeed,
                Impulse,
            }
            public EControlMode ControlMode;
            public float OriginSpeed;
            public float ImpulsePower;
            public float DecayPowerRate = 0;

            public bool EndOnCollideWall;
            public bool EndOnHitUnit;

            // 无武器 hitbox 时，通过近体半径检测单位碰撞后派发
            public List<MapFightEffectCfg> OnHitUnitEffects;
            public float DashHitRadius;
            // 由表现层 Physics2D 扫射写入，逻辑层在下一个 UpdateControlledMove 消费
            public long? PendingContactHitEntityId;

            public float timeLeft = 0f;

            public Action<int>? onMoveEnd;
            public bool WithEffect = false;

            public string? WeaponName;
            public long? HitId;

            #region 技能相关

            public bool IsActiveAbility;
            public bool EndPhaseWhenMoveEnds;
            public string BindAbilityId;
            public int BindAbilityPhaseIdx;

            #endregion
        }
        public ControlledMoveCtx? controlledMoveCtx;

        // 由表现层订阅：bool=是否激活，float=检测半径
        public event Action<bool, float> EventDashContactSensorChanged;

        // 表现层在 Physics2D 扫射命中后调用，将结果写入当前移动上下文
        public void NotifyDashContactHit(long entityId)
        {
            if (controlledMoveCtx != null && controlledMoveCtx.OnHitUnitEffects != null)
            {
                controlledMoveCtx.PendingContactHitEntityId = entityId;
            }
        }

        // 冲锋
        public ControlledMoveCtx StartDash(Vector2 dashDir, float dashTime, float speed, List<MapFightEffectCfg> onHitEffects,
            bool withEffect = false,
            bool withGhost = false,
            string dashWeaponName = "",
            bool stopOnWall = false,
            bool stopOnUnit = false,
            float dashHitRadius = 0.6f)
        {
            ApplyControlledMove(ControlledMoveCtx.EType.Dash, dashDir, dashTime, speed);
            controlledMoveCtx.WithEffect = withEffect;
            controlledMoveCtx.EndOnCollideWall = stopOnWall;

            // 初始化dash绑定武器
            if (!string.IsNullOrEmpty(dashWeaponName))
            {
                var hitId = ApplyUseWeapon(dashWeaponName, "0", dashTime, onHitEffects);
                controlledMoveCtx.WeaponName = dashWeaponName;
                controlledMoveCtx.HitId = hitId;
            }
            else if (onHitEffects != null && onHitEffects.Count > 0)
            {
                // 无武器时，使用近体检测路径
                controlledMoveCtx.OnHitUnitEffects = onHitEffects;
                controlledMoveCtx.DashHitRadius = dashHitRadius > 0f ? dashHitRadius : 0.6f;
                // 通知表现层开启 Physics2D 扫射传感器
                EventDashContactSensorChanged?.Invoke(true, controlledMoveCtx.DashHitRadius);
            }

            controlledMoveCtx.EndOnHitUnit = stopOnUnit;

            return controlledMoveCtx;
        }

        public void ApplyKnockBack(Vector2 dir, float knockDist, float decayRate = 8f, Action<int>? onKnockEnd = null)
        {
            if (CheckHasState(AttrIdConsts.ImmuneKnock))
            {
                return;
            }

            //TryInterrupt(new InterruptRequest()
            //{
            //    source = EInterruptSource.KnockUp,
            //    priority = 10,
            //});

            ApplyControlledMove(ControlledMoveCtx.EType.Knock, dir, originSpeed: knockDist * decayRate, minEndSpeed: 0.1f);
        }

        /// <summary>
        /// 实现受控移动
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dir"></param>
        /// <param name="duration"></param>
        /// <param name="speed"></param>
        /// <param name="onEndEffects"></param>
        /// <param name="minEndSpeed"></param>
        public void ApplyControlledMove(ControlledMoveCtx.EType type, Vector2 dir, float duration = -1, float? originSpeed = null, float decayRate = 8f, List<MapFightEffectCfg> onHitEffects = null, float minEndSpeed = 0)
        {

            EndControlledMove(99);

            ControlledMoveCtx ctx = new();

            ctx.Type = type;
            ctx.Duration = duration;
            ctx.MinEndSpeed = minEndSpeed;
            ctx.MoveDir = dir;
            ctx.OriginSpeed = originSpeed ?? 0;

            ctx.OnHitUnitEffects = onHitEffects;

            ctx.DecayPowerRate = decayRate;

            ctx.timeLeft = ctx.Duration;

            //onNewDashIntent?.Invoke(intent);

            controlledMoveCtx = ctx;

            // 施加初始速度
            externalVel = ctx.MoveDir.normalized * ctx.OriginSpeed;
        }

        private void UpdateControlledMove(float dt)
        {
            if(controlledMoveCtx == null)
            {
                return;
            }

            if(controlledMoveCtx.HitId != null && controlledMoveCtx.EndOnHitUnit)
            {
                HitWindowRegistry.activeHitWindows.TryGetValue(controlledMoveCtx.HitId.Value, out var window);
                if(window != null && window.HitRecord.Count > 0)
                {
                    EndControlledMove(2);
                    return;
                }
            }
            else if (controlledMoveCtx.EndOnHitUnit && controlledMoveCtx.OnHitUnitEffects != null && controlledMoveCtx.OnHitUnitEffects.Count > 0)
            {
                // 由表现层 Physics2D 扫射写入，此处消费
                if (controlledMoveCtx.PendingContactHitEntityId.HasValue)
                {
                    var hitEntityId = controlledMoveCtx.PendingContactHitEntityId.Value;
                    var srcInfo = new GameLogicManager.EffectSourceInfo()
                    {
                        SrcType = GameLogicManager.ESourceType.Ability,
                        SrcEntityId = this.Id,
                    };
                    var effectsToFire = controlledMoveCtx.OnHitUnitEffects;
                    controlledMoveCtx.OnHitUnitEffects = null;
                    foreach (var effect in effectsToFire)
                    {
                        var newCtx = new GameLogicManager.LogicFightEffectContext(LogicManager, GameLogicManager.EFightCtxType.HitBox, srcInfo);
                        newCtx.TargetId = hitEntityId;
                        newCtx.TriggerPos = Pos;
                        newCtx.CastVec1 = FinalLook;
                        LogicManager.HandleLogicFightEffect(effect, newCtx);
                    }
                    EndControlledMove(2);
                    return;
                }
            }

            

            if(controlledMoveCtx.Duration > 0)
            {
                controlledMoveCtx.timeLeft -= dt;
                if (controlledMoveCtx.timeLeft <= 0f)
                {
                    EndControlledMove(1);
                    return;
                }
            }
            

            if (controlledMoveCtx.Type == ControlledMoveCtx.EType.Dash)
            {
                
            }
            else if(controlledMoveCtx.Type == ControlledMoveCtx.EType.Knock)
            {
                // 指数衰减：v(t+dt) = v(t) * e^{-lambda * dt}
                // lambda 越大，减速越快；配合最小末速钳制
                float lambda = controlledMoveCtx.DecayPowerRate; // 可调 6~16
                if (lambda < 1) lambda = 1;



                float damping = Mathf.Exp(-lambda * dt);
                externalVel *= damping;

                // 末端钳制
                if (externalVel.magnitude < controlledMoveCtx.MinEndSpeed)
                {
                    externalVel = Vector2.zero;
                    EndControlledMove(1);
                }
            }
            else if (controlledMoveCtx.Type == ControlledMoveCtx.EType.Pull)
            {
                // 指数衰减：v(t+dt) = v(t) * e^{-lambda * dt}
                // lambda 越大，减速越快；配合最小末速钳制
                float lambda = controlledMoveCtx.DecayPowerRate; // 可调 6~16
                float damping = Mathf.Exp(-lambda * dt);
                externalVel *= damping;

                // 末端钳制
                if (externalVel.magnitude < controlledMoveCtx.MinEndSpeed)
                {
                    externalVel = Vector2.zero;
                    EndControlledMove(1);
                }
            }
        }


        //// 推进（建议放 FixedUpdate 或你的角色控制器速度合成前）
        //private void UpdateKnockback(float dt)
        //{
        //    if (knockBackIntent == null) return;

        //    knockTimer -= dt;
        //    if (knockTimer <= 0f)
        //    {
        //        ClearKnockbackInfo(0);
        //        return;
        //    }

        //    // 指数衰减：v(t+dt) = v(t) * e^{-lambda * dt}
        //    // lambda 越大，减速越快；配合最小末速钳制
        //    float lambda = 16f; // 可调 6~16
        //    float damping = Mathf.Exp(-lambda * dt);
        //    knockVel *= damping;

        //    // 末端钳制
        //    if (knockTimer < 0.08f && knockVel.magnitude < knockBackIntent.knockbackMinEndSpeed)
        //    {
        //        knockVel = Vector2.zero;
        //    }
        //}

        //public event Action<DashIntent> onNewDashIntent;
        //public event Action<KnockBackIntent> onNewKnockBackIntent;

        /// <summary>
        /// 
        /// 1 time over
        /// 2 wall collide
        /// </summary>
        /// <param name="reason"></param>
        public void EndControlledMove(int reason)
        {

            if (reason != 0)
            {
                //
            }

            if(controlledMoveCtx == null)
            {
                return;
            }

            // 如果无武器接触传感器处于激活状态，通知表现层关闭
            if (controlledMoveCtx.OnHitUnitEffects != null)
            {
                EventDashContactSensorChanged?.Invoke(false, 0f);
            }

            if(!string.IsNullOrEmpty(controlledMoveCtx.WeaponName))
            {
                ClearWeapon(controlledMoveCtx.WeaponName);
                //HitWindowRegistry.CloseHitWindow(controlledMoveCtx.HitWindowId.Value);
            }

            controlledMoveCtx.onMoveEnd?.Invoke(reason);

            if (abilityController.CurrentCtx != null && !string.IsNullOrEmpty(controlledMoveCtx.BindAbilityId)
                    && abilityController.CurrentCtx.AbilityConfig.Id == controlledMoveCtx.BindAbilityId
                    && abilityController.CurrentCtx.PhaseIndex == controlledMoveCtx.BindAbilityPhaseIdx)
            {
                if (controlledMoveCtx.EndPhaseWhenMoveEnds)
                {
                    // 尝试next phase
                    Debug.Log($"end ClearControlledMove next phase {controlledMoveCtx.BindAbilityId}:{controlledMoveCtx.BindAbilityPhaseIdx}");
                    abilityController.CurrentCtx.PhaseMarkSkip = true;
                }

                if(controlledMoveCtx.HitId != null && HitWindowRegistry.activeHitWindows.TryGetValue(controlledMoveCtx.HitId.Value, out var window))
                {
                    if(window != null && window.HitRecord.Count > 0)
                    {
                        if (controlledMoveCtx.BindAbilityPhaseIdx < abilityController.CurrentCtx.AbilityConfig.Phases.Count)
                        {
                            Debug.Log($"end controlled move set hit value");
                            var phaseName = abilityController.CurrentCtx.AbilityConfig.Phases[controlledMoveCtx.BindAbilityPhaseIdx].PhaseName;
                            abilityController.CurrentCtx.RunningStorage[$"{phaseName}.FirstHit"] = window.HitRecord[0];
                        }
                    }
                }
                
            }


            controlledMoveCtx = null;
            // 平滑收尾：保留少量速度并快速衰减
            externalVel *= 0.1f;

            Debug.Log($"end ClearControlledMove reason:{reason}");
        }
        #endregion


        protected virtual void InitAbility()
        {
            var comboGraph = GenerateComboGraph();
            ablilityManager = new MapEntitySkillManager(this, comboGraph);

            
            abilityController = new MapEntityAbilityExecutor(this);

            ablilityManager.Executor = abilityController;
        }

        protected virtual EntitySkillComboGraph GenerateComboGraph()
        {
            return null;
        }


        protected override void InitAttribute()
        {
            // 数值类
            RegisterUnitCommonNumeris();
            

            RegisterUnitCommonStates();

            // 资源类
            RegisterSpecAttrs();

            attributeStore.Commit();
        }

        protected void RegisterUnitCommonStates()
        {
            attributeStore.RegisterNumeric(AttrIdConsts.Unmovable, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.LockFace, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.ForbidSkillOp, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.PeaceCombatRestricted, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.NoSelect, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.PerfectDodgeWindow, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.NoInteract, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Ghost, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Invisible, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.SuperArmor, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.ImmumeKaiYou, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.FastTurn, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.NoKiller, initialBase: 0);

            attributeStore.RegisterNumeric(AttrIdConsts.ImmuneKnock, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Stun, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Fear, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.ImmuneFear, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Lured, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.ImmuneLured, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.ImmuneSteerInput, initialBase: 0);

            attributeStore.RegisterNumeric(AttrIdConsts.ImmuneEvilShock, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.ImmuneDamage, initialBase: 0);
        }

        protected void RegisterUnitCommonNumeris()
        {
            attributeStore.RegisterNumeric("Attack", initialBase: 100);
            attributeStore.RegisterNumeric("Strength", initialBase: 10);
            //attributeStore.RegisterNumeric(AttrIdConsts.HP_MAX, initialBase: 100_000);
            attributeStore.RegisterNumeric("RegenRate.HP", initialBase: 5);
            attributeStore.RegisterNumeric(AttrIdConsts.UnitWitnessSpotRate, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.UnitWitnessEscapeRate, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.UnitVisionRangeMul, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.UnitVisionFovMul, initialBase: 0);
        }
        

        protected virtual void RegisterSpecAttrs()
        {

        }

        public void OnThrownInterrupt()
        {
            Debug.Log($"unit thrown interrput {Id}");
        }

        public void OnThrowStart()
        {
            Debug.Log($"unit thrown interrput {Id}");
        }

        public bool CanBeThrow()
        {
            return true;
        }

        public void OnBeingThrowStart()
        {
            Debug.Log($"unit thrown interrput {Id}");

            TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.Stun,
            });
        }

        public void OnBeingThrowInterrupt()
        {
            Debug.Log($"unit thrown interrput {Id}");
        }



        #region IThrowLauncher

        #endregion


        #region throwed
        #endregion

        #region 事件

        /// <summary>
        /// 检查事件 
        /// </summary>
        /// <param name="evt"></param>
        public override void OnMapLogicEvent(IMapLogicEvent evt)
        {
            
        }

        public virtual bool CheckIsEmnity()
        {
            return false;
        }

        public virtual bool CheckIsEmnityFaction(EFactionId factionId)
        {
            return false;
        }

        // 运行时改阵营（交互策反等）；同步 Record 并清理仇恨/脱战
        public void ApplyRuntimeFactionChange(EFactionId newFaction, bool leaveCombat = true)
        {
            FactionId = newFaction;
            if (BindingRecord != null)
            {
                BindingRecord.FactionId = newFaction;
            }

            AggroSystem?.ClearTarget(0f);

            if (!leaveCombat)
            {
                return;
            }

            if (this is NpcUnitLogicEntity npc && npc.AIBrain != null)
            {
                var brain = npc.AIBrain;
                if (brain.CurrentState == brain.StateCombat || brain.CurrentState == brain.StateFlee)
                {
                    brain.ChangeState(brain.StateIdle);
                }
            }
        }

        #endregion


        #region interrrupt

        /// <summary>
        /// 打断
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public void TryInterrupt(InterruptRequest req)
        {
            // 检查超级护甲
            if (CheckHasState(AttrIdConsts.SuperArmor))
            {
                // 免疫所有被动打断
                if (req.source == EInterruptSource.Stun
                || req.source == EInterruptSource.Hit)
                {
                    return;
                }
            }
                
            abilityController.TryInterrupt(req);

            LogicManager.globalThrowManager.TryInterruptThrowByLauncher(this, req);
        }


        #endregion


        #region a

        #endregion

        public override void OnDespawn(ref LogicEntityRecord? snapshot)
        {
            base.OnDespawn(ref snapshot);

            if(snapshot != null)
            {
                var recUnit = (LogicEntityRecord4UnitBase)snapshot;
                recUnit.Unsensored = this.MarkUnsensored;
                recUnit.MarkNoLogic = this.MarkNoLogic;
            }
            //
            // 当死亡状态下的unit被回收时，执行destroy 且如果有掠夺品 需要创建新掠夺物
            if (IsDead)
            {
                DoEntityDestroyed("dead_despawn");

                bool hasDrop = false;
                List<ItemStack> items = new();
                if (dropBagContainer != null)
                {
                    foreach (var slot in dropBagContainer.InnerItems)
                    {
                        if (slot != null && slot.Count > 0)
                        {
                            hasDrop = true;
                            items.Add(slot);
                        }
                    }
                }

                if (hasDrop)
                {
                    Debug.Log("entity remove on die create loot point." + Id);

                    LogicEntityRecord rec = new LogicEntityRecord4LootPoint()
                    {
                        Id = GameLogicManager.LogicEntityIdInst++,
                        EntityType = EEntityType.LootPoint,
                        CfgId = "spoil_small",
                        Position = this.Pos,

                        ItemInitialized = true,
                        InnerItems = items,
                    };
                    LogicManager.AddNewEntityRecord(rec);
                }
            }
        }

        protected override void RefreshEntityRecordInfo(LogicEntityRecord input)
        {
            base.RefreshEntityRecordInfo(input);

            var realInput = input as LogicEntityRecord4UnitBase;
        }

        /// <summary>
        /// 属性变化回调
        /// </summary>
        /// <param name="attrId"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="intent"></param>

        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            // 4.3 死亡判断窗口：仅在含伤害时检查
            switch (attrId)
            {
                case AttrIdConsts.HP:
                    {
                        if (intent.isEnmity)
                        {
                            if (intent.srcEntityId != null && intent.srcEntityId.Value != 0)
                            {
                                var dmg = -intent.finalDelta;
                                var entity = LogicManager.GetLogicEntity(intent.srcEntityId.Value, false);
                                long xixue = intent.extraAttrs?.GetValueOrDefault(AttrIdConsts.XiXue_Pipeline) ?? 0;
                                if (intent.extraAttrs != null)
                                {
                                    intent.extraAttrs.TryGetValue(AttrIdConsts.DamageXiXue, out var extraVal);
                                    xixue += extraVal;
                                }

                                if (xixue > 0 && entity != null)
                                {
                                    Debug.Log("吸血 回血 OnResourceAttriChanged");
                                    var xixueVal = (long)(dmg * (double)(xixue / 10000));
                                    entity.ApplyResourceChange(AttrIdConsts.HP, xixueVal, false, EDmgFlag.Xixue, srcEntityId: Id);
                                }

                                if (entity is BaseUnitLogicEntity srcUnit)
                                {
                                    srcUnit.UnitOnApplyHit();
                                }
                            }

                            UnitOnHpChanged(intent.finalDelta, intent.srcEntityId, intent.HitDir, intent.isEnmity, intent.deltaFlags);

                        }

                        if (before > 0 && after <= 0/* && intent.deltaFlags > 0*/)
                        {
                            OnUnitDie(0, intent);
                            break;
                        }
                    }
                    break;
            }

            if(intent.isEnmity)
            {
                EventOnEnmityBehave?.Invoke(this.Id);

                if(intent.srcEntityId != null)
                {
                    float threatAmount = 111f;
                    if (attrId == AttrIdConsts.HP && intent.finalDelta < 0)
                    {
                        threatAmount = Math.Abs(intent.finalDelta);
                    }

                    AggroSystem?.OnTakeDamage(intent.srcEntityId.Value, threatAmount);

                    var player = LogicManager.playerLogicEntity;
                    if (player != null
                        && intent.srcEntityId.Value == player.Id
                        && this != player)
                    {
                        player.AggroSystem?.OnDealHostileDamage(Id, threatAmount);
                    }
                }
            }
        }


        protected virtual void UnitOnHpChanged(long finalDelta, long? srcEntityId, Vector2? hitDir, bool isEnmity, EDmgFlag deltaFlags)
        {
            if(isEnmity) //不是这样
            {
                // 触发onhit
                foreach (var b in BuffContainer.Values)
                {
                    b.DoBuffTrigger(ETriggerType.OnHit);
                }
            }
            
            EventOnHpChanged?.Invoke(this.Id, srcEntityId, finalDelta);

            if (Math.Abs(finalDelta) > 1)
            {
                // 伤害逻辑
                if (srcEntityId != null)
                {
                    var srcNpc = LogicManager.GetLogicEntity(srcEntityId.Value) as NpcUnitLogicEntity;
                    if (srcNpc != null)
                    {
                        srcNpc.AggroSystem.OnTakeDamage(this.Id, Math.Abs(finalDelta));
                    }
                }
            }
        }

        public bool TryResolvePerfectDodgeAgainstHit(long? srcEntityId, Vector2? hitDir)
        {
            if (!CheckHasState(AttrIdConsts.PerfectDodgeWindow))
            {
                return false;
            }

            foreach (var b in BuffContainer.Values)
            {
                b.DoBuffTrigger(ETriggerType.OnPerfectDodge);
            }

            Debug.LogError("完美闪避啦");

            LogicManager.globalBuffManager.RemoveAllBuffById(Id, "phase_perfect_dodge");
            return true;
        }

        public void ProcessHit(long? srcEntityId, Vector2? hitDir)
        {
            ProcessHit(srcEntityId, new UnitHitInfo(hitDir));
        }

        public virtual void ProcessHit(long? srcEntityId, UnitHitInfo hitInfo)
        {
            if (srcEntityId.HasValue
                && this is NpcUnitLogicEntity
                && LogicManager.GetLogicEntity(srcEntityId.Value, false) is BaseUnitLogicEntity srcUnit
                && FightEffectInterceptors.ShouldBlockHit(srcUnit, this))
            {
                return;
            }

            TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.Hit,
                priority = 1,
            });
            EventOnHit?.Invoke(this.Id, srcEntityId, hitInfo);
        }

        /// <summary>
        /// 单位执行攻击
        /// </summary>
        protected virtual void UnitOnApplyHit()
        {
            //LogicManager.viewer.StartHitStop(0.03f);
        }


        public class UnitBagContainer : IItemContainer
        {
            public GameLogicManager logicManager;
            private bool Inialized = false;
            private List<ItemStack> containItems = new List<ItemStack>();
            public int MaxSlots;

            public Dictionary<int, float> ItemSearchProgress = new();

            public UnitBagContainer(GameLogicManager logicManager, int maxSlots, List<(string, int)> items)
            {
                this.logicManager = logicManager;
                this.MaxSlots = maxSlots;

                for (int i = 0; i < MaxSlots; i++)
                {
                    containItems.Add(null);
                }

                for (int i = 0; i < items.Count; i++)
                {
                    containItems[i] = ItemCatalog.CreateItemStack(items[i].Item1, items[i].Item2);
                }
            }

            public UnitBagContainer(GameLogicManager logicManager, int maxSlots, List<DropUtils.DropReward> items)
            {
                this.logicManager = logicManager;
                this.MaxSlots = maxSlots;
                for (int i = 0; i < MaxSlots; i++) containItems.Add(null);
                for (int i = 0; i < items.Count && i < MaxSlots; i++) containItems[i] = items[i]?.CreateItemStack();
            }

            public List<ItemStack> InnerItems
            {
                get
                {

                    if (!Inialized)
                    {
                        Inialized = true;

                        
                    }
                    return containItems;

                }
            }

            public long GetMaxStack(string itemId)
            {
                return ItemCatalog.GetMaxStackByType(itemId, EContainerType.LootPoint);
            }

            public void SetItemData(int slotIdx, ItemStack item)
            {
                if (slotIdx < 0 || slotIdx >= MaxSlots)
                {
                    return;
                }

                containItems[slotIdx] = item;
            }

            public void SetItemCount(int slotIdx, long count)
            {
                if (slotIdx < 0 || slotIdx >= MaxSlots)
                {
                    return;
                }

                if (containItems[slotIdx] == null)
                {
                    return;
                }

                containItems[slotIdx].Count = count;
            }

            public bool IsSlotIdxValid(int slotIdx)
            {
                if (slotIdx < 0 || slotIdx >= MaxSlots)
                {
                    return false;
                }

                ItemSearchProgress.TryGetValue(slotIdx, out var progress);
                if (progress > 0)
                {
                    return false;
                }

                return true;
            }

            public long GetItemCount(string itemId)
            {
                long ret = 0;
                foreach (var item in containItems)
                {
                    if (item == null) continue;
                    ret += item.Count;
                }
                return ret;
            }

            public ItemStack GetItemByIdx(int slotIdx)
            {
                if (slotIdx < 0 || slotIdx >= MaxSlots)
                {
                    return null;
                }

                return containItems[slotIdx];
            }
        }

        protected UnitBagContainer dropBagContainer = null;


        /// <summary>
        /// 强制死亡
        /// </summary>
        public void ForceDie()
        {
            //  
            this.attributeStore.SetResource(AttrIdConsts.HP, 0);
            OnUnitDie(99);
        }

        public bool IsAttaching = false;


        /// <summary>
        /// 转换成attach
        /// </summary>
        public void ConvertToAttachment()
        {
            if (IsAttaching || IsDead)
            {
                return;
            }

            IsAttaching = true;

            OnConvertToAttachment();

            LogicManager.LogicEventBus.Publish(new MLEUnitCantAlert()
            {
                Ctx = new()
                {
                    HappenPos = Pos,
                    SourceEntity = LogicManager.playerLogicEntity,
                },
                EntityId = this.Id,
            });
        }

        public void RestoreFromAttach()
        {
            if (!IsAttaching || IsDead)
            {
                return;
            }

            IsAttaching = false;

            OnRestoreFromAttach();
        }

        protected virtual void OnConvertToAttachment()
        {
            LogicManager.globalBuffManager.RequestAddBuff(this.Id, "as_attaching");

            MotorSystem.StopMove();

            TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.System,
                priority = 999,
            });


            EventOnAttachStatusChanged?.Invoke(this.Id);
        }


        protected virtual void OnRestoreFromAttach()
        {
            LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "as_attaching");

            MotorSystem.StopMove();

            ApplyKnockBack(UnityEngine.Random.insideUnitCircle, 1.0f);

            EventOnAttachStatusChanged?.Invoke(this.Id);
        }


        public override void OnStatusAttriChanged(string attrId, bool isOn)
        {
            base.OnStatusAttriChanged (attrId, isOn);

            switch (attrId)
            {
                case AttrIdConsts.Stun:
                case AttrIdConsts.ForbidSkillOp:
                    {
                        if(isOn)
                        {
                            TryInterrupt(new InterruptRequest()
                            {
                                source = EInterruptSource.Stun,
                                priority = 10,
                            });
                        }
                    }
                    break;
                case AttrIdConsts.Ghost:
                    {
                        EventOnGhostChange?.Invoke(this.Id);
                    }
                    break;
                case AttrIdConsts.Invisible:
                    {
                        EventOnInvisibleChange?.Invoke(this.Id);
                    }
                    break;
            }
        }

        public override long CalculateResourceCostAmount(string attrId, ResourceDeltaIntent intent)
        {
            long delta = intent.delta;
            switch(attrId)
            {
                case AttrIdConsts.HP:
                    {
                        return CalculateUnitHpChange(attrId, intent);
                    }
                    break;
                default:
                    {
                        return base.CalculateResourceCostAmount(attrId, intent);
                    }
            }
        }

        protected virtual void OnDamageBeforeFinalReduce(long dmg, ResourceDeltaIntent intent)
        {

        }

        protected virtual long CalculateUnitHpChange(string attrId, ResourceDeltaIntent intent)
        {
            long delta = intent.delta;

            if (delta < 0)
            {
                if (CheckHasState(AttrIdConsts.ImmuneDamage))
                {
                    return 0;
                }

                // H / 非 H 均走 DamagePipeline；H 类在管线内读 HStrength_Pipeline，跳过护甲
                if (intent.DmgCategory == EDmgCategory.H)
                {
                    var hResolved = DamagePipeline.ResolveHpDeltaCore(
                        delta,
                        intent.DmgCategory,
                        intent,
                        () => 0,
                        () => 0,
                        () => 0,
                        () => 0);
                    var hDmg = Math.Abs(hResolved);
                    OnDamageBeforeFinalReduce(hDmg, intent);
                    return -hDmg;
                }

                var dmg = DamagePipeline.ResolveHpDeltaCore(
                    delta,
                    intent.DmgCategory,
                    intent,
                    () => attributeStore.GetAttr(AttrIdConsts.Basic_ExtraDmg),
                    GetFinalArm,
                    () => attributeStore.GetAttr(AttrIdConsts.Basic_JianShang),
                    () => attributeStore.GetAttr(AttrIdConsts.NonH_JianShang_Rate));

                dmg = Math.Abs(dmg);

                OnDamageBeforeFinalReduce(dmg, intent);

                long weiyiDrVal = 0;
                if (!intent.deltaFlags.HasFlag(EDmgFlag.Loss))
                {
                    var weiyi_dr = GetAttr(AttrIdConsts.Weiyi_JianShang);
                    weiyiDrVal = Math.Min(weiyi_dr, dmg);
                    dmg -= weiyiDrVal;
                }

                long fixDrVal = 0;
                if (!intent.deltaFlags.HasFlag(EDmgFlag.Loss))
                {
                    var fix_dr = GetAttr(AttrIdConsts.Final_Fix_DR_All);
                    fixDrVal = Math.Min(fix_dr, dmg);
                    dmg -= fixDrVal;
                }
                    
                if(fixDrVal > 0)
                {
                    foreach (var b in BuffContainer.Values)
                    {
                        b.DoBuffTrigger(ETriggerType.FinalDmgReduced, (int)fixDrVal);
                    }
                }

                if (weiyiDrVal > 0 || dmg > 0)
                {
                    LastBuffDamageTriggerContext = new BuffDamageTriggerContext
                    {
                        WeiyiDrVal = weiyiDrVal,
                        HpDamageVal = dmg,
                    };

                    long takenVal = weiyiDrVal + dmg;
                    if (takenVal > int.MaxValue)
                    {
                        takenVal = int.MaxValue;
                    }

                    foreach (var b in BuffContainer.Values)
                    {
                        b.DoBuffTrigger(ETriggerType.OnDamageTaken, (int)takenVal);
                    }

                    LastBuffDamageTriggerContext = default;
                }
                
                if (dmg <= 0)
                {
                    dmg = 0;
                }

                return -dmg;
            }

            return delta;
        }


        public long GetFinalArm()
        {
            return GetAttr(AttrIdConsts.Arm_Final);
            //var armWhite = GetAttr(AttrIdConsts.Arm_White);
            //var armPercent = GetAttr(AttrIdConsts.ArmPercent_White);

            //var armExtra1 = GetAttr(AttrIdConsts.Arm_Extra_1);

            //return (long)(armWhite * (10000 + armPercent) * 0.0001 + armExtra1);
        }

        public long GetFinalHTechnique()
        {
            return GetAttr(AttrIdConsts.HTechnique);
        }

        #region alert

        protected bool isEvilAlerting;
        protected float evilAlertStartTime;
        protected float evilAlertDuration;

        public bool IsEvilAlert { get { return isEvilAlerting; } }
        
        /// <summary>
        /// 开始告警
        /// 告警当且仅当涉及到恶魔真身的逻辑才会调用
        /// 普通的通缉并不会触发evil告警
        /// </summary>
        /// <param name="duration"></param>
        public void StartEvilAlert(float duration)
        {
            if(isEvilAlerting)
            {
                return;
            }

            evilAlertStartTime = LogicTime.time;
            evilAlertDuration = duration;
            isEvilAlerting = true;

            LogicManager.AreaManager.EntityTryRegisterAlert(this);
        }



        protected void TickUnitEvilAlert()
        {
            if (!isEvilAlerting)
            {
                return;
            }

            bool finished = false;
            if(IsAttaching)
            {
                finished = true;
            }

            if(LogicTime.time > evilAlertStartTime + evilAlertDuration)
            {
                finished = true;
            }

            if(IsDead)
            {
                finished = true;
            }

            if(MarkNoLogic)
            {
                finished = true;
            }

            if(finished)
            {
                isEvilAlerting = false;

                evilAlertStartTime = 0;
                evilAlertDuration = 0;

                LogicManager.AreaManager.EntityTryUnregisterAlert(this.Id);
            }
        }


        #endregion


        /// <summary>
        /// 获取感知参数
        /// </summary>
        /// <returns></returns>
        public (float, float) GetViewRangeAndAngle()
        {
            var k = GetEffectiveVisionConeKind();
            float rangeMul = Mathf.Max(0.05f, (10000f + GetAttr(AttrIdConsts.UnitVisionRangeMul)) / 10000f);
            float fovMul = Mathf.Max(0.05f, (10000f + GetAttr(AttrIdConsts.UnitVisionFovMul)) / 10000f);
            return (viewRadius * rangeMul, ClampFovDegreesForKind(fovDegrees * fovMul, k));
        }

        /// <summary>
        /// 
        /// </summary>
        public void UpdateUnitOffsetZ()
        {
            OffsetZ = 0;

            foreach (var buffInst in BuffContainer.Values)
            {
                if (buffInst.Def.ZOffsetOverride > 0)
                {
                    OffsetZ = buffInst.Def.ZOffsetOverride;
                }
            }
        }

        public virtual bool CanActiveUseSkill()
        {
            if(CheckHasState(AttrIdConsts.ForbidSkillOp))
            {
                return false;
            }
            if (CheckHasState(AttrIdConsts.Stun))
            {
                return false;
            }
            if (CheckHasState(AttrIdConsts.Fear))
            {
                return false;
            }
            if (CheckHasState(AttrIdConsts.Lured))
            {
                return false;
            }
            return true;
        }

        public virtual bool IsInHBehaveMode()
        {
            return false;
        }


        protected override void TickStateLowFreq()
        {
            base.TickStateLowFreq();
            TickResourceChange(1.0f);
        }

        protected override void OnLiquidAdd(EGroundLiquidType liquidType)
        {
            switch (liquidType)
            {
                case EGroundLiquidType.GcLiquid:
                    LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "b_ground_gc_liquid");
                    break;
                case EGroundLiquidType.Milk:
                    LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "b_ground_milk_liquid");
                    break;
            }
        }

        protected override void OnLiquidRemove(EGroundLiquidType liquidType)
        {
            switch (liquidType)
            {
                case EGroundLiquidType.GcLiquid:
                    LogicManager.globalBuffManager.AddBuff(this.Id, "b_ground_gc_liquid");
                    break;
                case EGroundLiquidType.Milk:
                    LogicManager.globalBuffManager.AddBuff(this.Id, "b_ground_milk_liquid");
                    break;
            }
        }

        protected override void OnMistAdd(EGroundMistType mistType)
        {
            switch (mistType)
            {
                case EGroundMistType.PinkMist:
                    LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "player_pink_mist");
                    break;
            }
        }

        protected override void OnMistRemove(EGroundMistType mistType)
        {
            switch (mistType)
            {
                case EGroundMistType.PinkMist:
                    LogicManager.globalBuffManager.AddBuff(this.Id, "player_pink_mist");
                    break;
            }
        }

    }


}
