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


namespace My.Map
{
    
    public class UnitNoticeInfo
    {
        public ILogicEntity TargetId { get; set; }
        public float LastSeeTime { get; set; }

        public bool HideOnFace { get; set; }
    }

    public abstract class BaseUnitLogicEntity : LogicEntityBase, IThrowLauncher, IThrowTarget, IWithEnmity
    {
        public MapEntityAbilityController abilityController;
        public float viewRadius = 8f;
        public float fovDegrees = 90f;
        public LogicEntityRecord4UnitBase UnitBaseRecord { get { return (LogicEntityRecord4UnitBase)BindingRecord; } }

        public enum EUnitEnmityMode
        {
            Never,
            LowClothes,
            Always,
        }
        public EUnitEnmityMode EnmityMode;

        public enum EUnitMoveActMode
        {
            NoMove,
            Patrol,
            Spawn,
            Hunting,
            PatrolFollow,
        }

        public EUnitMoveActMode MoveActMode;
        public long FollowPatrolId;
        public Vector2 PatrolGroupRelativePos;

        public bool IsInBattle; // 既没有战斗 也没有h attract
        public bool IsHMode;

        public int BindRoomId;

        public AbsMapUnitConfig unitCfg;


        public event Action EventOnHpChanged;

        /// <summary>
        /// 吸引源信息
        /// </summary>
        public class AttractInfo
        {
            public long SrcId;
            public Vector2 Pos;
            public float LastTriggerTime;
        }

        public Dictionary<long, AttractInfo> attractInfos = new();
        public void GetAttracted(IAttractSource attractSrc)
        {
            if(attractInfos.TryGetValue(attractSrc.Id, out var info))
            {
                info = new()
                {
                    SrcId = attractSrc.Id,
                };
                attractInfos[attractSrc.Id] = info;
            }

            info.Pos = attractSrc.Pos;
            info.LastTriggerTime = LogicTime.time;
        }

        public UnitNoticeInfo PlayerNoticeInfo = new();
        public UnitEnmityComp EnmityComp;

        public BaseUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var unitRecord = (LogicEntityRecord4UnitBase)bindingRecord;
            this.MoveActMode = unitRecord.ActMode;
            this.FollowPatrolId = unitRecord.PatrolFollowId;
            this.PatrolGroupRelativePos = unitRecord.PatrolGroupRelativePos;

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
            
            EnmityComp = new();
            EnmityComp.Initialize(this);
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

                if (knockBackIntent != null)
                {
                    knockBackIntent.knockbackTimeLeft -= dt;
                    if (knockBackIntent.knockbackTimeLeft <= 0f)
                        ClearKnockbackIntent();
                }
            }

            AIBrain?.Tick(dt);


            if (attributeStore.GetAttr(AttrIdConsts.LockFace) == 0)
            {
                if (LogicTime.time - PlayerNoticeInfo.LastSeeTime < 5.0f)
                {
                    var diff = LogicManager.playerLogicEntity.Pos - this.Pos;
                    if (diff.magnitude > 1e-2)
                    {
                        FaceDir = diff;
                    }
                }
                else if (targetMoveIntent != null)
                {
                    if(targetMoveIntent.MoveType == TargettedMoveIntent.ETargettedMoveType.FollowEntity)
                    {
                        var diff = targetMoveIntent.FollowEntity.Pos - this.Pos;
                        if (diff.magnitude > 1e-2)
                        {
                            FaceDir = diff.normalized;
                        }
                    }
                    else if(targetMoveIntent.MoveType == TargettedMoveIntent.ETargettedMoveType.FixPoint)
                    {
                        var diff = targetMoveIntent.FixedMoveTarget - this.Pos;
                        if (diff.magnitude > 1e-2)
                        {
                            FaceDir = diff.normalized;
                        }
                    }
                }
            }

            attributeStore.Commit();

            UpdateNoticeInfo();

            EnmityComp?.Tick(dt);

            if(EnmityComp != null)
            {
                // 见到
                if(EnmityComp.IsEnmityState && LogicTime.time - PlayerNoticeInfo.LastSeeTime < 1f)
                {
                    IsInBattle = true;
                }
            }

            if(IsInBattle)
            {

            }

            UpdateHMode();
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

        protected virtual void UpdateNoticeInfo()
        {
            if (LogicManager.visionSenser.CanSee(Pos, FaceDir, LogicManager.playerLogicEntity.Pos, 5.0f, 60f))
            {
                // 玩家处于视野内
                PlayerNoticeInfo.LastSeeTime = LogicTime.time;
                //PlayerNoticeInfo.HideOnFace = 
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
                LogicManager.CreateNewEntityRecord(rec);
            }
        }


        #region 移动控制


        /// <summary>
        /// 包含方向与速度的当前有效位移 由输入和受控移动共同决定
        /// </summary>
        public Vector2 activeMoveVec;

        public Vector2 externalVel;

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
            public float knockbackTimeLeft;

            public float knockbackMinEndSpeed;

            public float knockbackDuration;
            public float knockDuration;
            public Vector2 knockDir;
        }
        public KnockBackIntent? knockBackIntent;

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

        private void ClearKnockbackIntent()
        {
            knockBackIntent = null;
            externalVel = Vector2.zero;
        }


        public void CreateKnockBackIntent(Vector2 dir, float power)
        {
            KnockBackIntent intent = new();

            intent.knockbackTimeLeft = 0.3f;
            intent.knockbackMinEndSpeed = 0.1f;
            intent.knockbackDuration = 0.3f;
            intent.knockDuration = 0.3f;
            intent.knockDir = dir;

            externalVel = intent.knockDir.normalized * intent.knockDuration;
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


        public TargettedMoveIntent? targetMoveIntent;

        public void StartTargettedMove(TargettedMoveIntent.ETargettedMoveType moveType, ILogicEntity? followedEntity, Vector2 fixedPoint, float arriveDistance, bool clearNav = false, TargettedMoveIntent.ESpeedType speedType = TargettedMoveIntent.ESpeedType.Normal)
        {
            if (targetMoveIntent == null)
            {
                targetMoveIntent = new();
            }

            targetMoveIntent.MoveType = moveType;
            targetMoveIntent.SpeedType = speedType;

            targetMoveIntent.FollowEntity = followedEntity;
            targetMoveIntent.FixedMoveTarget = fixedPoint;

            targetMoveIntent.ArriveDistance = arriveDistance;
            
            targetMoveIntent.NeedRecalculatePath = true;

            // 是否清理速度
            if(!clearNav)
            {
                targetMoveIntent.targettedDesireDir = Vector2.zero;
            }
        }

        public void StopTargetteMove()
        {
            targetMoveIntent = null;
        }
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
        public override void OnMapLogicEvent(in IMapLogicEvent evt)
        {
            if(EnmityComp != null)
            {
                EnmityComp.OnMapLogicEvent(evt);
            }
        }

        public bool CheckIsEmnity()
        {
            return EnmityComp.CheckIsEmnity();
        }

        #endregion


        #region interrrupt

        public void TryInterrupt(InterruptRequest req)
        {
            abilityController.TryInterrupt(req);

            LogicManager.globalThrowManager.TryInterruptThrowByLauncher(this, req);
        }

        #endregion
    }

}

