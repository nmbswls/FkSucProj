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
using static My.Map.Entity.MapEntityAbilityExecutor;
using static UnityEngine.Rendering.VolumeComponent;
using static My.Map.EntityCombatStateComp;
using static My.GameLogicManager;
using UnityEditor.Experimental.GraphView;
using static My.Map.Fight.FightStruct;
using static UnityEngine.GraphicsBuffer;
using My.Player.Bag;


namespace My.Map
{

    public class UnitMoveBehaveInfo
    {
        public enum EMoveBehaveType
        {
            NoMove,
            Patrol,
            MovePath,
            Hunting,
            InPatrolGroup,
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

    }

    public abstract partial class BaseUnitLogicEntity : LogicEntityBase, IThrowLauncher, IThrowTarget, IWithEnmity,
        IUnitVisibility, IUnitWithBattle
    {
        public MapEntitySkillManager ablilityManager;
        public MapEntityAbilityExecutor abilityController;
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

        public UnitMoveBehaveInfo MoveBehaveInfo;
        //public Vector2? LastInterruptPos;

        public EntityMotorComp entityMotorComp;
        public EntityCombatStateComp combatStateComp;

        //public bool IsInBattle; // 既没有战斗 也没有h attract
        public bool IsHMode;
        public bool IsDead = false;
        public bool IsAttaching = false;

        public int BindRoomId;

        public AbsMapUnitConfig unitCfg;

        public string EmnityConfId;

        /// <summary>
        /// event
        /// </summary>
        public event Action<long> EventOnHit;
        public event Action<long> EventOnEnmityBehave;
        public event Action<long> EventOnDie;
        public event Action<long> EventOnConvertAttach;

        public UnitEnmityComp EnmityComp;
        public UnitVisibilityComp VisibilityComp;

        private float externalDecay = 30f;          // 外力自然衰减（每秒）
        public BaseUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var unitRecord = (LogicEntityRecord4UnitBase)bindingRecord;
            Debug.Log($"BaseUnitLogicEntity init {instId} {unitRecord.MoveBehaveType}");
            this.MoveBehaveInfo = new();
            this.MoveBehaveInfo.MoveBehaveMode = unitRecord.MoveBehaveType;
            this.MoveBehaveInfo.FollowPatrolId = unitRecord.PatrolFollowId;
            this.MoveBehaveInfo.PatrolGroupRelativePos = unitRecord.PatrolGroupRelativePos;
            this.MoveBehaveInfo.DisappearOnArrive = unitRecord.DisappearOnArrive;
            this.MoveBehaveInfo.MovePath = unitRecord.MovePath;

            this.FaceDir = bindingRecord.FaceDir;


            this.EmnityConfId = unitRecord.EnmityConfId;

        }

        public override void Initialize()
        {
            base.Initialize();

            // get meta info
            InitAbility();

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
                VisibilityComp = new();
                VisibilityComp.Initialize(this);

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

            if(IsActive && !IsDead)
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
        }

        protected virtual void TickActivateState(float dt)
        {
            AIBrain?.Tick(dt);

            EnmityComp?.Tick(dt);

            combatStateComp?.Tick(dt);

            UpdateHMode();

            VisibilityComp?.TryUpdateNoticeList();

            entityMotorComp?.Tick(dt);

            TickAttractState();

            TickGaze();

            UpdateFaceDir();
        }
        public override void OnEnterAOI()
        {
            if(MoveBehaveInfo.MoveBehaveMode == UnitMoveBehaveInfo.EMoveBehaveType.InPatrolGroup)
            {
                var groupEntity = LogicManager.GetLogicEntity(MoveBehaveInfo.FollowPatrolId);
                this.SetPosition(groupEntity.Pos + MoveBehaveInfo.PatrolGroupRelativePos);
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


        public virtual void OnUnitDie(int reason, ResourceDeltaIntent lastIntent = null)
        {
            this.IsDead = true;

            EventOnDie?.Invoke(this.Id);

            LogicManager.LogicEventBus.Publish(new MLEUnitDeadEvent()
            {
                Ctx = new()
                {
                    HappenPos = Pos,
                    SourceEntity = LogicManager.playerLogicEntity,
                },
                EntityId = this.Id,
                Pos = Pos,
            });
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

            public bool EndOnCollideWall;

            public List<MapFightEffectCfg> OnHitUnitEffects;

            public float timeLeft = 0f;

            public Action<int>? onMoveEnd;
            public bool WithEffect = false;
        }
        public ControlledMoveCtx? controlledMoveCtx;

        // 冲锋
        public void StartDash(Vector2 dashDir, float dashTime, float speed, List<MapFightEffectCfg> onEndEffects, bool withEffect = false)
        {
            ApplyControlledMove(ControlledMoveCtx.EType.Dash, dashDir, dashTime, speed, onEndEffects: onEndEffects);
            controlledMoveCtx.WithEffect = withEffect;
        }

        public void ApplyKnockBack(Vector2 dir, float power, Action<int>? onKnockEnd = null)
        {
            if (CheckHasState(AttrIdConsts.ImmuneKnock))
            {
                return;
            }

            TryInterrupt(new InterruptRequest()
            {
                source = InterruptSource.KnockUp,
                priority = 10,
            });

            ApplyControlledMove(ControlledMoveCtx.EType.Knock, dir, 0.25f, originSpeed: power, impulsePower: power, minEndSpeed: 0.1f);

            //ClearDashIntent(0);
            //ClearKnockbackInfo(2);

            //KnockBackIntent intent = new();

            //intent.knockbackMinEndSpeed = 0.1f;
            //intent.knockbackPower = power;
            //intent.knockDir = dir;

            //externalVel = intent.knockDir.normalized * power;

            //knockTimer = 0.15f;
            //knockVel = intent.knockDir * power; // 初速大

            ////onNewKnockBackIntent?.Invoke(intent);
            //knockBackIntent = intent;
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
        public void ApplyControlledMove(ControlledMoveCtx.EType type, Vector2 dir, float duration, float? originSpeed = null, float? impulsePower = null, List<MapFightEffectCfg> onEndEffects = null, float minEndSpeed = 0)
        {
            if(controlledMoveCtx != null)
            {
                if(controlledMoveCtx.onMoveEnd != null)
                {
                    controlledMoveCtx.onMoveEnd?.Invoke(99);
                }
            }

            ControlledMoveCtx ctx = new();

            ctx.Type = type;
            ctx.Duration = duration;
            ctx.MinEndSpeed = minEndSpeed;
            ctx.MoveDir = dir;
            ctx.OriginSpeed = originSpeed ?? 0;

            ctx.OnHitUnitEffects = onEndEffects;

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

            controlledMoveCtx.timeLeft -= dt;
            if (controlledMoveCtx.timeLeft <= 0f)
            {
                EndControlledMove(1);
                return;
            }

            if (controlledMoveCtx.Type == ControlledMoveCtx.EType.Dash)
            {
                
            }
            else if(controlledMoveCtx.Type == ControlledMoveCtx.EType.Knock)
            {
                // 指数衰减：v(t+dt) = v(t) * e^{-lambda * dt}
                // lambda 越大，减速越快；配合最小末速钳制
                float lambda = 8f; // 可调 6~16
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
                float lambda = 8f; // 可调 6~16
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

            //controlledMoveCtx?.onCollide?.Invoke();

            controlledMoveCtx = null;
            // 平滑收尾：保留少量速度并快速衰减
            externalVel *= 0.1f;

            Debug.Log("end ClearControlledMove");
        }
        #endregion


        protected virtual void InitAbility()
        {
            var comboGraph = GenerateComboGraph();
            ablilityManager = new MapEntitySkillManager(this, comboGraph);

            if(unitCfg != null)
            {
                foreach(var skillId in unitCfg.SkillList)
                {
                    ablilityManager.RegisterSkill(skillId);
                }
            }

            abilityController = new MapEntityAbilityExecutor(this);

            ablilityManager.Executor = abilityController;

            abilityController.EventOnInputCancelPhaseStart += () =>
            {
                if(AIBrain != null)
                {
                    AIBrain.TriggerUpdateImmediately();
                }
            };
        }

        protected virtual EntitySkillComboGraph GenerateComboGraph()
        {
            return null;
        }


        protected override void InitAttribute()
        {
            // 数值类
            attributeStore.RegisterNumeric("Attack", initialBase: 100);
            attributeStore.RegisterNumeric("Strength", initialBase: 10);
            attributeStore.RegisterNumeric("HP.Max", initialBase: 100000);
            attributeStore.RegisterNumeric("RegenRate.HP", initialBase: 5);

            // 资源类
            attributeStore.RegisterResource(AttrIdConsts.HP, AttrIdConsts.HP_MAX, null, 100000);

            RegisterCommonStates();


            // 资源类
            attributeStore.RegisterResource(AttrIdConsts.UnitEnterHVal, null, 10000, 0);
            attributeStore.RegisterResource(AttrIdConsts.DeepZhaChance, null, 999, 3);

            attributeStore.Commit();
        }

        protected void RegisterCommonStates()
        {
            attributeStore.RegisterNumeric(AttrIdConsts.Unmovable, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.LockFace, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.ForbidSkillOp, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.NoSelect, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Ghost, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Invisible, initialBase: 0);

            attributeStore.RegisterNumeric(AttrIdConsts.ImmuneKnock, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Stun, initialBase: 0);
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
            return VisibilityComp.IsTargetVisible (targetId);
        }

        #endregion


        #region a

        #endregion

        public override void OnDespawn(out LogicEntityRecord? snapshot)
        {
            snapshot = BindingRecord;

            //
            // 当死亡状态下的unit被回收时，执行destroy 且如果有掠夺品 需要创建新掠夺物
            if(IsDead)
            {
                DoEntityDestroyed("dead_despawn");

                bool hasDrop = false;
                List<ItemStack> items = new();
                if (dropBagContainer != null)
                {
                    foreach(var slot in dropBagContainer.InnerItems)
                    {
                        if(slot != null && slot.Count > 0)
                        {
                            hasDrop = true;
                            items.Add(slot);
                        }
                    }
                }

                if(hasDrop)
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
                                var entity = LogicManager.GetLogicEntity(intent.srcEntityId.Value);
                                var xixue = entity.GetAttr(AttrIdConsts.DamageXiXue);

                                if (intent.extraAttrs != null)
                                {
                                    intent.extraAttrs.TryGetValue(AttrIdConsts.DamageXiXue, out var extraVal);
                                    xixue += extraVal;
                                }

                                if (xixue > 0)
                                {
                                    Debug.Log("吸血 回血 OnResourceAttriChanged");
                                    var xixueVal = (long)(dmg * (double)(xixue / 10000));
                                    entity.ApplyResourceChange(AttrIdConsts.HP, xixueVal, false, EDmgFlag.Xixue, srcEntityId: Id);
                                }
                            }

                            EventOnHit?.Invoke(this.Id);
                            {
                                foreach(var b in BuffContainer.Values)
                                {
                                    b.DoBuffTrigger(ETriggerType.OnHit);
                                }
                            }
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
            }
        }

        public class UnitBagContainer : IItemContainer
        {
            public GameLogicManager logicManager;
            private bool Inialized = false;
            private List<ItemStack> containItems = new List<ItemStack>();
            public string DropId;
            public int MaxSlots;

            public Dictionary<int, float> ItemSearchProgress = new();

            public UnitBagContainer(GameLogicManager logicManager, string dropId, int maxSlots)
            {
                this.logicManager = logicManager;
                this.DropId = dropId;
                this.MaxSlots = maxSlots;
            }

            public List<ItemStack> InnerItems
            {
                get
                {

                    if (!Inialized)
                    {
                        Inialized = true;

                        for (int i = 0; i < MaxSlots; i++)
                        {
                            containItems.Add(null);
                        }

                        var items = logicManager.DropTable.GetBundleDropItems(DropId);
                        for (int i = 0; i < items.Count; i++)
                        {
                            containItems[i] = FakeItemDatabase.CreateItemStack(items[i].Item1, items[i].Item2);

                            //var itemConf = FakeItemDatabase.GetIcon();
                            ItemSearchProgress[i] = 1.5f;
                        }
                    }
                    return containItems;

                }
            }

            public long GetMaxStack(string itemId)
            {
                return FakeItemDatabase.GetMaxStackByType(itemId, EContainerType.LootPoint);
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
        /// 转换成attach
        /// </summary>
        public virtual void ConvertToAttachment()
        {
            if(IsAttaching || IsDead)
            {
                return;
            }

            IsAttaching = true;
            EventOnConvertAttach?.Invoke(this.Id);

            // 设置attach
            LogicManager.playerLogicEntity.AtttachingUnits.Add(this.Id);
        }

        /// <summary>
        /// 强制死亡
        /// </summary>
        public void ForceDie()
        {
            //  
            this.attributeStore.SetResource(AttrIdConsts.HP, 0);
            OnUnitDie(99);
        }
    }

}

