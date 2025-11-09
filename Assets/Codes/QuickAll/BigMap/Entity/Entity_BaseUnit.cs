using Config.Unit;
using Config;
using Map.Logic.Chunk;
using UnityEngine;
using Map.Logic.Events;
using System;
using Map.Entity.AI;
using Map.Entity.Attr;
using System.Collections.Generic;
using Map.Entity.Throw;


namespace Map.Entity
{
    
    public abstract class BaseUnitLogicEntity : LogicEntityBase, IThrowLauncher, IThrowTarget
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

        public int BindRoomId;

        public AbsMapUnitConfig unitCfg;


        public event Action EventOnHpChanged;

        


        public BaseUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            this.CampId = bindingRecord.CampId;
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
        }

        public override void Tick(float now, float dt)
        {
            base.Tick(now, dt);
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

            //AIBrain?.Tick(now, dt);


            if (attributeStore.GetAttr(AttrIdConsts.LockFace) == 0 && targettedMoveIntent != null && targettedMoveIntent.targettedDesireDir != null)
            {
                if (targettedMoveIntent.targettedDesireDir.magnitude > 1e-2)
                {
                    FaceDir = targettedMoveIntent.targettedDesireDir;
                }
            }

            attributeStore.Commit();
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
            public Vector2 MoveTarget;
            public float lastUpdateNavTime;
            public bool NeedRecalculatePath;
            public Vector2 targettedDesireDir;
            public float StopDistance = 1f; // 停止距离
        }

        public TargettedMoveIntent? targettedMoveIntent;

        public void StartTargettedMove(Vector2 moveToPos, float stopDistance)
        {
            if (targettedMoveIntent != null && moveToPos == targettedMoveIntent.MoveTarget)
            {
                return;
            }

            if (targettedMoveIntent == null)
            {
                targettedMoveIntent = new();
            }

            targettedMoveIntent.StopDistance = stopDistance;
            targettedMoveIntent.MoveTarget = moveToPos;
            targettedMoveIntent.NeedRecalculatePath = true;
        }


        public void StopTargetteMove()
        {
            targettedMoveIntent = null;
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

            //attributeStore.RegisterNumeric(AttrIdConsts.Unmovable, initialBase: 0);
            //attributeStore.RegisterNumeric(AttrIdConsts.LockFace, initialBase: 0);
            // 资源类
            attributeStore.RegisterResource(AttrIdConsts.HP, AttrIdConsts.HP_MAX, 100);

            attributeStore.Commit();
        }

        /// <summary>
        /// hit回调
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="src"></param>
        public void OnHit(long damage, ILogicEntity? src, int damageFlag = 0)
        {
            SourceKey? srcKey = src == null ? null : new SourceKey() { type = SourceType.AbilityActive };
            attributeStore.ApplyResourceChange(AttrIdConsts.HP, -damage, true, srcKey);
            //attributeStore.CostResource(AttrIdConsts.HP, damage);
            //if(attributeStore.GetAttr(AttrIdConsts.HP) <= 0)
            //{
            //    // 濒死
            //    this.MarkDestroy = true;

            //    Debug.Log("Unit Entity OnHit dead " + Id);

            //    LogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
            //    {
            //        Name = "Death",
            //        Param3 = this.Id,
            //        Param4 = src != null ? src.Id : 0,
            //    });


            //    EventOnDeath?.Invoke();
            //    return;
            //}

            Debug.Log("Unit Entity OnHit left hp " + attributeStore.GetAttr(AttrIdConsts.HP) + " unit " + Id);
            LogicManager.LogicEventBus.Publish(new MLEUnitOnHit()
            {
                //SrcKey = src
                OnHitId = this.Id,
                Damage = damage,
                Flags = damageFlag,
            });


            //EventOnHpChanged?.Invoke();
        }


        public MapUnitAIBrain? AIBrain;


        protected virtual void InitAiBrain()
        {
            AIBrain = new();
            //var cacheCfg = MapMonsterConfigLoader.Get(CfgId);
            AIBrain.Initilaize(this, LogicManager.visionSenser, Pos);

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
    }

}

