using Config.Unit;
using Config;
using UnityEngine;
using Map.Logic.Events;
using System;
using Map.Entity.AI;
using System.Collections.Generic;
using My.Map.Entity.AI;
using My.Map.Entity;
using My.Map.Logic;
using static My.Map.Entity.MapEntityAbilityController;
using static My.Map.BaseUnitLogicEntity;
using static UnityEngine.Rendering.VolumeComponent;
using static My.Map.EntityCombatStateComp;


namespace My.Map
{
    
    public abstract partial class BaseUnitLogicEntity : LogicEntityBase, IThrowLauncher, IThrowTarget, IWithEnmity,
        INoticeRecordComp, IUnitWithBattle
    {
        public MapEntityAbilityController abilityController;
        public float viewRadius = 8f;
        public float fovDegrees = 90f;


        public LogicEntityRecord4UnitBase UnitBaseRecord 
        { 
            get 
            { 
                return (LogicEntityRecord4UnitBase)BindingRecord; 
            } 
        }

        public ECombatState CombatState
        { 
            get 
            {
                return combatStateComp?.CombatState ?? ECombatState.NotCombat;
            } 
        }

        public enum EMoveBehaveType
        {
            NoMove,
            Patrol,
            Spawn,
            Hunting,
            InPatrolGroup,
        }

        public EMoveBehaveType MoveBehaveMode;

        public long FollowPatrolId;
        public Vector2 PatrolGroupRelativePos;
        public Vector2? LastInterruptMovePos;


        public EntityMotorComp entityMotorComp;
        public EntityCombatStateComp combatStateComp;

        //public bool IsInBattle; // 既没有战斗 也没有h attract
        public bool IsHMode;

        public int BindRoomId;

        public AbsMapUnitConfig unitCfg;


        public event Action EventOnHpChanged;

        /// <summary>
        /// 吸引源信息
        /// </summary>
        public class AttractInfo
        {
            public float AttractPower;
            public Vector2 Pos;
            public IAttractSource? AttractSource;
            public float LastTriggerTime;
        }

        public AttractInfo? attractInfo;
        public void ApplyAttracted(Vector2 pos, float power, IAttractSource? attractSrc)
        {

            if(attractInfo != null && attractInfo.AttractPower > power && LogicTime.time - attractInfo.LastTriggerTime < 5.0f)
            {
                Debug.Log("");
                return;
            }

            attractInfo = new();
            attractInfo.Pos = pos;
            attractInfo.AttractPower = power;
            attractInfo.LastTriggerTime = LogicTime.time;
            attractInfo.AttractSource = attractSrc;
        }


        public bool CheckAttractState()
        {
            if(attractInfo == null)
            {
                return false;
            }

            if(LogicTime.time - attractInfo.LastTriggerTime > 5.0f)
            {
                return false;
            }

            return true;
        }

        public UnitEnmityComp EnmityComp;
        public UnitNoticeRecordComp NoticeRecordComp;

        public BaseUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var unitRecord = (LogicEntityRecord4UnitBase)bindingRecord;
            this.MoveBehaveMode = unitRecord.MoveBehaveType;
            this.FollowPatrolId = unitRecord.PatrolFollowId;
            this.PatrolGroupRelativePos = unitRecord.PatrolGroupRelativePos;


            this.FaceDir = bindingRecord.FaceDir;
        }

        public override void Initialize()
        {
            base.Initialize();

            // get meta info
            InitAbilityController();

            InitAiBrain();

            // 优先应用覆盖值
            if(UnitBaseRecord.FactionId != EFactionId.None)
            {
                this.FactionId = UnitBaseRecord.FactionId;
            }
            else
            {
                if (unitCfg != null)
                {
                    this.FactionId = unitCfg.DefaultFactionId;
                }
            }
            
            
            if(Type != EEntityType.Player)
            {
                NoticeRecordComp = new();
                NoticeRecordComp.Initialize(this);

                EnmityComp = new();
                EnmityComp.Initialize(this);

                combatStateComp = new(this);
            }

            entityMotorComp = new(this, LogicManager.navProvider);

            InitFacing();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            // 计时、条件检查、冷却等
            abilityController?.Tick(dt);

            {
                if (dashIntent != null)
                {
                    dashIntent.dashTimeLeft -= dt;
                    if (dashIntent.dashTimeLeft <= 0f)
                    {
                        ClearDashIntent();
                    }
                }

            }

            UpdateKnockback(dt);

            AIBrain?.Tick(dt);


            attributeStore.Commit();

            EnmityComp?.Tick(dt);

            //if(EnmityComp != null)
            //{
            //    // 见到
            //    if(EnmityComp.IsEnmityState && LogicTime.time - PlayerNoticeInfo.LastSeeTime < 1f)
            //    {
            //        IsInBattle = true;
            //    }
            //}

            //if(IsInBattle)
            //{

            //}
            combatStateComp?.Tick(dt);

            UpdateHMode();

            NoticeRecordComp?.TryUpdateNoticeList();

            entityMotorComp?.Tick(dt);

            UpdateFaceDir();
        }

        public override void OnEnterAOI()
        {
            if(MoveBehaveMode == EMoveBehaveType.InPatrolGroup)
            {
                var groupEntity = LogicManager.GetLogicEntity(FollowPatrolId);
                this.SetPosition(groupEntity.Pos + PatrolGroupRelativePos);
            }
        }

        protected virtual void UpdateHMode()
        {
            if(Type == EEntityType.Player)
            {
                return;
            }

            if (unitCfg.AlwaysHMode)
            {
                IsHMode = true;
            }
        }


        public override void OnEntityDie(ResourceDeltaIntent lastIntent)
        {
            base.OnEntityDie(lastIntent);

            if(lastIntent.srcKey != null)
            {
                var logicEntity = LogicManager.GetLogicEntity(lastIntent.srcKey.Value.entityId);

                var diff = logicEntity.Pos - this.Pos;
                var impluse = -(diff.normalized);

                CreateKnockBackIntent(impluse, 5f);
            }


            if(!string.IsNullOrEmpty(unitCfg.DropId))
            {
                Debug.Log("entity die generate drop spoil." + Id);

                LogicEntityRecord rec = new LogicEntityRecord4LootPoint()
                {
                    Id = GameLogicManager.LogicEntityIdInst++,
                    EntityType = EEntityType.LootPoint,
                    CfgId = "spoil_small",
                    Position = this.Pos,
                    DynamicDropId = unitCfg.DropId,
                };
                LogicManager.AddNewEntityRecord(rec);
            }
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

        public float moveSpeed = 4.0f;
        public float GetCurrSpeed()
        {
            var jiansu = GetAttr(AttrIdConsts.JianSu);
            if(jiansu > 10000)
            {
                jiansu = 9000;
            }
            return moveSpeed * (10000 - jiansu) * 0.0001f;
        }

        public class DashIntent
        {
            public float dashTimeLeft = 0f;

            public float dashDuration;
            public bool dashIFrameActive = false;
            public float dashIFrameLeft = 0f;
            public Vector2 dashDir;
            public float dashSpeed;

            public Action onCollide;
        }
        public DashIntent? dashIntent;

        public class KnockBackIntent
        {
            public float knockbackMinEndSpeed;
            public float knockbackPower;
            public Vector2 knockDir;
            public Action<int>? onKnockEnd;
        }
        public KnockBackIntent? knockBackIntent;


        // 推进（建议放 FixedUpdate 或你的角色控制器速度合成前）
        private void UpdateKnockback(float dt)
        {
            if (knockBackIntent == null) return;

            knockTimer -= dt;
            if (knockTimer <= 0f)
            {
                ClearKnockbackInfo(0);
                return;
            }

            // 指数衰减：v(t+dt) = v(t) * e^{-lambda * dt}
            // lambda 越大，减速越快；配合最小末速钳制
            float lambda = 16f; // 可调 6~16
            float damping = Mathf.Exp(-lambda * dt);
            knockVel *= damping;

            // 末端钳制
            if (knockTimer < 0.08f && knockVel.magnitude < knockBackIntent.knockbackMinEndSpeed)
            {
                knockVel = Vector2.zero;
            }
        }

        public event Action<DashIntent> onNewDashIntent;
        public event Action<KnockBackIntent> onNewKnockBackIntent;

        public void CreateDashIntent(Vector2 dir, float dashTime, float speed)
        {
            if (dashIntent != null)
            {

            }

            DashIntent intent = new();
            intent.dashDir = dir;
            intent.dashTimeLeft = dashTime;
            intent.dashSpeed = speed;

            intent.dashDuration = dashTime;
            externalVel = intent.dashDir.normalized * intent.dashSpeed;
            //onNewDashIntent?.Invoke(intent);

            dashIntent = intent;
        }


        public void ClearDashIntent()
        {

            dashIntent?.onCollide?.Invoke();

            dashIntent = null;
            // 平滑收尾：保留少量速度并快速衰减
            externalVel *= 0.1f;

            Debug.Log("end ClearDashIntent");
        }

        private void ClearKnockbackInfo(int endReason)
        {
            knockBackIntent?.onKnockEnd?.Invoke(endReason);
            knockBackIntent = null;
            knockVel = Vector2.zero;
            knockTimer = 0;
        }


        public void CreateKnockBackIntent(Vector2 dir, float power, Action<int>? onKnockEnd = null)
        {
            ClearDashIntent();
            ClearKnockbackInfo(2);

            KnockBackIntent intent = new();

            intent.knockbackMinEndSpeed = 0.1f;
            intent.knockbackPower = power;
            intent.knockDir = dir;

            externalVel = intent.knockDir.normalized * power;

            knockTimer = 0.15f;
            knockVel = intent.knockDir * power; // 初速大

            //onNewKnockBackIntent?.Invoke(intent);
            knockBackIntent = intent;
        }



        public class TargettedMoveIntent
        {
            public enum ETargettedMoveType
            {
                FixPoint,
                FollowEntity,
                FollowSomething,
            }

            public ETargettedMoveType MoveType;


            public enum ESpeedType
            {
                Normal = 0,
                Slow,
                Dash,
            }

            public ESpeedType SpeedType;

            public Vector2 FixedMoveTarget;
            public ILogicEntity? FollowEntity;


            public Vector2 targettedDesireDir;
            public float ArriveDistance = 1f;

            public bool NeedRecalculatePath;
        }


        //public TargettedMoveIntent? targetMoveIntent;

        //public void StartTargettedMove(TargettedMoveIntent.ETargettedMoveType moveType, ILogicEntity? followedEntity, Vector2 fixedPoint, float arriveDistance, bool clearNav = false, TargettedMoveIntent.ESpeedType speedType = TargettedMoveIntent.ESpeedType.Normal)
        //{
        //    if (targetMoveIntent == null)
        //    {
        //        targetMoveIntent = new();
        //    }

        //    targetMoveIntent.MoveType = moveType;
        //    targetMoveIntent.SpeedType = speedType;

        //    targetMoveIntent.FollowEntity = followedEntity;
        //    targetMoveIntent.FixedMoveTarget = fixedPoint;

        //    targetMoveIntent.ArriveDistance = arriveDistance;
            
        //    targetMoveIntent.NeedRecalculatePath = true;

        //    // 是否清理速度
        //    if(!clearNav)
        //    {
        //        targetMoveIntent.targettedDesireDir = Vector2.zero;
        //    }
        //}

        //public void StopTargetteMove()
        //{
        //    targetMoveIntent = null;
        //}
        #endregion


        protected abstract void InitAbilityController();


        protected override void InitAttribute()
        {
            // 数值类
            attributeStore.RegisterNumeric("Attack", initialBase: 100);
            attributeStore.RegisterNumeric("Strength", initialBase: 10);
            attributeStore.RegisterNumeric("HP.Max", initialBase: 1000);
            attributeStore.RegisterNumeric("RegenRate.HP", initialBase: 5);

            // 资源类
            attributeStore.RegisterResource(AttrIdConsts.HP, AttrIdConsts.HP_MAX, 100);

            RegisterCommonStates();


            // 资源类
            attributeStore.RegisterResource(AttrIdConsts.UnitEnterHVal, null, 0);
            attributeStore.RegisterResource(AttrIdConsts.DeepZhaChance, null, 3);

            attributeStore.Commit();
        }

        protected void RegisterCommonStates()
        {
            attributeStore.RegisterNumeric(AttrIdConsts.Unmovable, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.LockFace, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.ForbidOp, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.NoSelect, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Ghost, initialBase: 0);
        }


        public MapUnitAIBrain? AIBrain;


        protected virtual void InitAiBrain()
        {
            AIBrain = new();
            //var cacheCfg = MapMonsterConfigLoader.Get(CfgId);
            AIBrain.InitilaizeAll(this, LogicManager.visionSenser, Pos);

            //AIBrain.BrainStateMachine.Reset();

            //// 装备移动状态
            //string initState = string.Empty;

            //switch (UnitBaseRecord.ActMode)
            //{
            //    case EUnitMoveActMode.NoMove:
            //        {
            //            AIBrain.BrainStateMachine.Register(new IdleBrainState(AIBrain));
            //            initState = "Idle";
            //            break;
            //        }
            //    case EUnitMoveActMode.Hunting:
            //        {
            //            AIBrain.BrainStateMachine.Register(new HuntingBrainState(AIBrain));
            //            initState = "Hunting";
            //            break;
            //        }
            //    case EUnitMoveActMode.PatrolFollow:
            //        {
            //            AIBrain.BrainStateMachine.Register(new FollowPatrolGroupBrainState(AIBrain));
            //            initState = "FollowPatrolGroup";
            //            break;
            //        }
            //}

            //// 对于有H模式的单位 赋予状态
            //if (unitCfg.HasHMode)
            //{
            //    AIBrain.BrainStateMachine.Register(new HModeChaseBrainState(AIBrain));
            //}

            //if (!unitCfg.IsPeace)
            //{
            //    var combatState = new CombatChaseBrainState(AIBrain);

            //    switch (unitCfg.AITemplateMode)
            //    {
            //        case "Warrior":
            //            {
            //                var template = Resources.Load<DefaultAIParamTemplate4Warrior>($"AITemplate/{unitCfg.AITemplateName}");

            //                var standOffStrategy = new DistanceControlStrategy(template.KeepDistance * 0.001f);
            //                combatState.RegisterStrategy(standOffStrategy);

            //                var mainFightStrategy = new PrimaryUseSkillStrategy(1f);
            //                combatState.RegisterStrategy(mainFightStrategy);
            //            }
            //            break;
            //        case "Shooter":
            //            {
            //                var template = Resources.Load<DefaultAIParamTemplate4Shooter>($"AITemplate/{unitCfg.AITemplateName}");

            //                var standOffStrategy = new DistanceControlStrategy(template.KeepDistance * 0.001f);
            //                combatState.RegisterStrategy(standOffStrategy);

            //                var mainFightStrategy = new PrimaryUseSkillStrategy(1f);
            //                combatState.RegisterStrategy(mainFightStrategy);
            //            }
            //            break;
            //    }

            //    AIBrain.BrainStateMachine.Register(combatState);
            //}
            //else
            //{
            //    var fleeState = new FleeAwayBrainState(AIBrain, 10, 5f);
            //    AIBrain.BrainStateMachine.Register(fleeState);
            //}

            //AIBrain.BrainStateMachine.Register(new ReturnBrainState(AIBrain));
            //// 你可以注册 Idle/Patrol/Return 等状态，这里示例聚焦 CombatChase。

            //if (!string.IsNullOrEmpty(initState))
            //{
            //    AIBrain.BrainStateMachine.Change(initState);
            //}
            //else
            //{
            //    Debug.LogError("AIBrain.BrainStateMachine no init ");
            //}
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
            if(EnmityComp != null)
            {
                EnmityComp.OnMapLogicEvent(evt);
            }

            if(evt is MLEAttractEvent realEvt)
            {
                ApplyAttracted(realEvt.Pos, realEvt.Power, realEvt.AttractSource);
            }
        }

        public bool CheckIsEmnity()
        {
            return EnmityComp.CheckIsEmnity();
        }

        public bool CheckIsEmnityFaction(EFactionId factionId)
        {
            return EnmityComp.CheckIsEmnityFaction(factionId);
        }

        #endregion


        #region interrrupt

        public void TryInterrupt(InterruptRequest req)
        {
            abilityController.TryInterrupt(req);

            LogicManager.globalThrowManager.TryInterruptThrowByLauncher(this, req);
        }

        public bool IsTargetVisible(long targetId)
        {
            return NoticeRecordComp.IsTargetVisible (targetId);
        }

        #endregion
    }

}

