using Config.Unit;
using Config;
using UnityEngine;
using My;
using My.Map.Logic;
using My.Map.Entity;
using System.Collections.Generic;
using static My.Map.Entity.EntitySkillComboGraph;
using Map.Logic.Events;
using static UnityEngine.RuleTile.TilingRuleOutput;
using My.Map.Entity.AI;
using System;
using static My.Map.Fight.FightStruct;
using static UnityEditor.PlayerSettings;
using My.Map.Unit;
using UnityEditor;
using cfg.demo;
using My.Config;
using static My.UI.FishingMiniGamePanel;


namespace My.Map
{
    public partial class NpcUnitLogicEntity : BaseUnitLogicEntity, IEntityInteractable
    {
        public UnitNpc NpcConfig 
        { 
            get 
            { 
                if(cacheUnitNpcCfg == null)
                {
                    cacheUnitNpcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(CfgId);
                }
                return cacheUnitNpcCfg;
            } 
        }

        private UnitNpc cacheUnitNpcCfg;

        public AIBrainV2? AIBrain;

        private float _lastHModeTimer;

        public event Action EventOnHModeChange;

        public bool IsFaQing { get { return NpcDesirePhase >= 2; } }
        public int NpcDesirePhase { get; private set; } // npc独特欲望阶段  

        protected bool hShieldBroken;


        //public EntityInteractComp InteractComp;

        public string GetRuntimeVariable(string paramName)
        {
            return string.Empty;
        }

        public override bool IsInCombat
        {
            get
            {
                if(AIBrain == null)
                {
                    return false;
                }
                if(AIBrain.CurrentState == null)
                {
                    return false;
                }
                return AIBrain.CurrentState == AIBrain.StateCombat;
            }
        }

        public LogicEntityRecord4Npc NpcRecord
        {
            get
            {
                return (LogicEntityRecord4Npc)BindingRecord;
            }
        }

        public bool HasAttachedDesireCrystal =>
            !string.IsNullOrEmpty(NpcRecord.AttachedDesireCrystalTypeId);

        // 运行时掷出的精型，对应图鉴 match_tag
        public string JingyuanMatchTag =>
            My.Config.JingYuanTypeCatalog.GetMatchTag(NpcRecord?.RolledJingyuanTypeId);

        // 喷射落地掉落物 id，由精型推导
        public string BlurtDropItemId =>
            My.Config.JingYuanCodexCatalog.GetPickupItemIdByMatchTag(JingyuanMatchTag);

        public NpcUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var npcRecord = (LogicEntityRecord4Npc)bindingRecord;

            Debug.Log($"NpcUnitLogicEntity init {instId} {npcRecord.MoveBehaveType}");
            this.MoveBehaveInfo = new();
            this.MoveBehaveInfo.MoveBehaveMode = npcRecord.MoveBehaveType;
            this.MoveBehaveInfo.FollowPatrolId = npcRecord.PatrolFollowId;
            this.MoveBehaveInfo.PatrolGroupRelativePos = npcRecord.PatrolGroupRelativePos;
            this.MoveBehaveInfo.DisappearOnArrive = npcRecord.DisappearOnArrive;
            this.MoveBehaveInfo.MovePath = npcRecord.MovePath;
            this.MoveBehaveInfo.PatrolPortalNetworkId = npcRecord.PatrolPortalNetworkId ?? string.Empty;
            this.MoveBehaveInfo.PatrolCycleNodeIds.Clear();
            if (npcRecord.PatrolCycleNodeIds != null && npcRecord.PatrolCycleNodeIds.Count > 0)
            {
                this.MoveBehaveInfo.PatrolCycleNodeIds.AddRange(npcRecord.PatrolCycleNodeIds);
            }

            this.MoveBehaveInfo.MoveToDespawnTarget = npcRecord.MoveToDespawnTarget;
        }

        protected override void RefreshEntityRecordInfo(LogicEntityRecord input)
        {
            base.RefreshEntityRecordInfo(input);
            if (input is LogicEntityRecord4Npc nr && MoveBehaveInfo != null
                && MoveBehaveInfo.MoveBehaveMode == UnitMoveBehaveInfo.EMoveBehaveType.MoveToThenDespawn)
            {
                nr.MoveToDespawnTarget = MoveBehaveInfo.MoveToDespawnTarget;
            }
        }

        public override bool CheckLocalSwitch(string switchName)
        {
            var key = NpcRecord.CharacterKey;
            if (!string.IsNullOrEmpty(key) && LogicManager?.playerDataManager != null)
            {
                return LogicManager.playerDataManager.NamedNpcHasLocalSwitch(key, switchName);
            }

            return base.CheckLocalSwitch(switchName);
        }

        public override void SetLocalSwitch(string switchName, bool isOn)
        {
            var key = NpcRecord.CharacterKey;
            if (!string.IsNullOrEmpty(key) && LogicManager?.playerDataManager != null)
            {
                LogicManager.playerDataManager.SetNamedNpcLocalSwitch(key, switchName, isOn);
                return;
            }

            base.SetLocalSwitch(switchName, isOn);
        }

        public override EEntityType Type => EEntityType.Npc;

        public override WorldMapLandmarkKind WorldMapLandmark
        {
            get
            {
                if (WorldMapRuntime.IsNpcBossLandmark(CfgId))
                    return WorldMapLandmarkKind.MajorBoss;
                return base.WorldMapLandmark;
            }
        }

        public override string WorldMapLandmarkLabel
        {
            get
            {
                var key = NpcRecord?.CharacterKey;
                if (!string.IsNullOrEmpty(key))
                {
                    return NpcCharacterInfoUtil.GetDisplayName(key, NpcConfig?.Name ?? CfgId);
                }

                return NpcConfig != null && !string.IsNullOrEmpty(NpcConfig.Name) ? NpcConfig.Name : CfgId;
            }
        }

        protected override void InitAbility()
        {
            base.InitAbility();

            if (NpcConfig != null)
            {
                // 合并普通技能与H行为技能后一次性 Reconcile，避免两次调用互相覆盖
                var allSkills = new List<string>();
                if (NpcConfig.SkillList != null)
                {
                    allSkills.AddRange(NpcConfig.SkillList);
                }
                if (NpcConfig.HBehaveList != null)
                {
                    allSkills.AddRange(NpcConfig.HBehaveList);
                }
                ablilityManager.ReconcileRegisteredSkills(allSkills);
            }

            // 通用H普攻：固定注册，不依赖配表
            ablilityManager.RegisterSkill("default_h_attack");

            // 人类种族空手兜底普攻
            if (NpcConfig != null && NpcConfig.RaceId == "human")
            {
                ablilityManager.RegisterSkill("human_unarmed_attack");
            }

            abilityController.EventOnInputCancelPhaseStart += () =>
            {
                if (AIBrain != null)
                {
                    AIBrain.TriggerUpdateImmediately();
                }
            };
        }


        protected override EntitySkillComboGraph GenerateComboGraph()
        {
            EntitySkillComboGraph graph = new();
            {
                var node = new ComboNode()
                {
                    NodeId = 100,
                    AbilityId = "guard_attack_01",
                    deriveWindows = new List<DeriveWindow>()
                    {
                        new DeriveWindow()
                        {
                            id = "1",
                            window = new TimeWindow(0.27f, 0.57f),
                        }
                    }
                };
                graph.ComboNodes.Add(node);

            }
            {
                var node = new ComboNode()
                {
                    NodeId = 101,
                    AbilityId = "guard_attack_02",
                };
                graph.ComboNodes.Add(node);

            }

            {
                var trans = new EntitySkillComboGraph.Transition()
                {
                    fromNodeId = 0,
                    toNodeId = 100,
                    triggerInput = new InputPattern()
                    {
                        SkillId = "guard_attack"
                    },
                };

                graph.Transitions.Add(trans);
            }
            {
                var trans = new EntitySkillComboGraph.Transition()
                {
                    fromNodeId = 100,
                    toNodeId = 101,
                    triggerInput = new InputPattern()
                    {
                        SkillId = "guard_attack"
                    },
                    windowId = "1",
                };

                graph.Transitions.Add(trans);
            }

            graph.BuildGraph();
            return graph;
        }

        public override void Initialize()
        {
            base.Initialize();

            InitAiBrain();

            if (NpcRecord.SpawnWithImmediateInvestigation && AIBrain != null && LogicManager.playerLogicEntity != null)
            {
                AIBrain.SuspiciousPos = LogicManager.playerLogicEntity.Pos;
                AIBrain.ChangeState(AIBrain.StateSearch);
            }

            if (NpcRecord.Unsensored)
            {
                LogicManager.globalBuffManager.RequestAddBuff(Id, "unsensored");
            }



            if (NpcConfig.NotTarget)
            {
                LogicManager.globalBuffManager.RequestAddBuff(this.Id, "not_fight_target");
            }

            // move_style：马达/贴地方式（0/3 等 → IgnoreGround）；AI 移动看 MoveBehaveType / 配表 idle_move_behave
            if(NpcConfig.MoveStyle != 1)
            {
                MotorSystem.IgnoreGround = true;
            }

            ApplySpawnFaceDir();
            if (NpcConfig.LockInitialFace)
            {
                LogicManager.globalBuffManager.RequestAddBuff(Id, "lock_face");
            }

            //InteractComp.RefreshInteractInfo();
            //InteractComp.RefreshInteractInfo(NpcConfig.InteractList);

        }

        void ApplySpawnFaceDir()
        {
            var faceDir = BindingRecord != null ? BindingRecord.FaceDir : Vector2.right;
            if (faceDir.sqrMagnitude <= 1e-8f)
            {
                return;
            }

            ForceSetFaceTarget(faceDir.normalized, true);
        }

        protected override void RegisterSpecAttrs()
        {
            var attrCfg = CfgMgr.Cfgs.TbUnitNpcAttr.GetOrDefault(NpcConfig.AttrTemplateId);

            if(attrCfg != null)
            {
                moveSpeed = attrCfg.MoveSpeed;
            }

            var initHp = (long)(attrCfg.Hp * 1000);
            attributeStore.RegisterNumeric(AttrIdConsts.HP_MAX, initialBase: initHp);
            attributeStore.RegisterResource(AttrIdConsts.HP, AttrIdConsts.HP_MAX, null, initHp);

            // H 模式相关资源注册
            attributeStore.RegisterResource(AttrIdConsts.UnitKnockDown, null, 100_000, 0); 

            attributeStore.RegisterResource(AttrIdConsts.NPCHVal, null, 100_000, 0);
            attributeStore.RegisterResource(AttrIdConsts.UnitHShield, null, 120_000, 120_000);
            attributeStore.RegisterResource(AttrIdConsts.DeepZhaChance, null, 999, 1);
            attributeStore.RegisterResource(AttrIdConsts.NPCSJProgress, null, 100_000, 0);

            var bodyPower = attrCfg.PhysicalPower;
            attributeStore.RegisterNumeric(AttrIdConsts.PhysicalPower, (long)(bodyPower * 1000));
            var hPower = attrCfg.HPower;
            attributeStore.RegisterNumeric(AttrIdConsts.HPower, (long)(hPower * 1000));

            attributeStore.RegisterNumeric(AttrIdConsts.Will, 10_000);
            attributeStore.RegisterNumeric(AttrIdConsts.DesireDensityAmplify, 0);
            attributeStore.RegisterNumeric(AttrIdConsts.NPCSJProgress_GainRate, 0);

            if (NpcConfig != null)
            {
                int vk = NpcConfig.VisionConeKind;
                BaseVisionConeKind = (VisionConeKind)Mathf.Clamp(vk, 0, (int)VisionConeKind.Omniscient);
                if (NpcConfig.VisionRange > 0f)
                {
                    viewRadius = NpcConfig.VisionRange;
                }
                if (NpcConfig.VisionFovDeg > 0f)
                {
                    fovDegrees = NpcConfig.VisionFovDeg;
                }
            }

        }

        public override VisionConeKind GetEffectiveVisionConeKind()
        {
            var kind = BaseVisionConeKind;
            if (kind == VisionConeKind.Normal && AIBrain != null)
            {
                var s = AIBrain.CurrentState;
                if (s == AIBrain.StateCombat || s == AIBrain.StateFlee)
                {
                    return VisionConeKind.Alert;
                }
            }
            return kind;
        }

        protected virtual void InitAiBrain()
        {
            AIBrain = AIBrainFactory.CreateAIBrain(this);
        }

        public override void OnUnitDie(int reason, ResourceDeltaIntent lastIntent = null)
        {
            base.OnUnitDie(reason, lastIntent);

            var items = new List<(string, int)>();
            if (MarkUnsensored)
            {
                int baseDropId = NpcConfig.DefeatDropId;
                if (baseDropId > 0)
                {
                    items.AddRange(DropUtils.GetBundleDropItems(baseDropId));
                }

                long finalDensity = DesireDensityUtil.GetFinalDensity(this);
                items.AddRange(MindFragment.MindFragmentDropResolver.Roll(this, finalDensity));
            }
            else
            {
                int dropId = NpcConfig.FallbackDropId > 0 ? NpcConfig.FallbackDropId : NpcConfig.DefeatDropId;
                if (dropId > 0)
                {
                    items.AddRange(DropUtils.GetBundleDropItems(dropId));
                }
            }

            dropBagContainer = new(this.LogicManager, 12, items);

            if (lastIntent != null && lastIntent.srcPos != null)
            {
                var diff = lastIntent.srcPos.Value - Pos;
                if (diff.sqrMagnitude > 1e-8f)
                {
                    ApplyKnockBack(-diff.normalized, 0.5f);
                }
            }
            else if (lastIntent != null && lastIntent.HitDir != null)
            {
                ApplyKnockBack(-lastIntent.HitDir.Value, 0.5f);
            }

            // 当且仅当伤害非致命时 才执行欲望结晶掉落
            if(lastIntent != null && lastIntent.deltaFlags.HasFlag(EDmgFlag.Nonlethal))
            {
                LogicManager.TryCreateDesireCrystalFromNpc(this);
            }
        }

        private float _checkAlertTimer;

        protected override void TickResourceChange(float interval)
        {
            var hValUp = GetAttr(AttrIdConsts.NPCHVal_Basic_Up);
            if(hValUp > 0)
            {
                var addVal = (long)Math.Ceiling(hValUp * interval);
                if(addVal > 0)
                {
                    ApplyResourceChange(AttrIdConsts.NPCHVal, addVal, false, EDmgFlag.None, null);
                }
            }

            var sjValUp = GetAttr(AttrIdConsts.NPCSJProgress_Basic_Up);
            if (sjValUp > 0)
            {
                var addVal = (long)Math.Ceiling(sjValUp * interval);
                if (addVal > 0)
                {
                    ApplyResourceChange(AttrIdConsts.NPCSJProgress, addVal, false, EDmgFlag.None, null);
                }
            }

            TickPlayerMist();
        }

        protected override void TickActivateState(float dt)
        {
            base.TickActivateState(dt);

            if(!IsAttaching)
            {
                TickStimulusAttract();

                TickPlayerMist();

                if (!AiBrainSuspended)
                {
                    AIBrain?.Tick(dt);
                }

                TickHRelateProperties();

                CheckSeeEvil();
            }

            //InteractComp?.TickInteract(dt);
            //TickGaze();
        }




        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            base.OnResourceAttriChanged(attrId, before, after, intent);

            switch (attrId)
            {
                case AttrIdConsts.NPCHVal:
                    {
                        _lastHModeTimer = LogicTime.time;

                        var hValMax = GetHValMax();
                        // 溢出部分转换为高潮条
                        if (before < after && after > hValMax)
                        {
                            long overflow = after - hValMax;
                            long toBlurt = (long)(overflow * 0.2f);
                            attributeStore.ApplyResourceChange(AttrIdConsts.NPCSJProgress, toBlurt, intent.isEnmity, EDmgFlag.None, intent.srcEntityId);
                        }
                    }
                    break;
                case AttrIdConsts.NPCSJProgress:
                    {
                        _lastHModeTimer = LogicTime.time;
                    }
                    break;
                case AttrIdConsts.UnitHShield:
                    {
                        _lastHModeTimer = LogicTime.time;
                    }
                    break;
            }
        }


        // H 模式与护盾相关 Tick
        protected void TickHRelateProperties()
        {
            //bool currHmode = IsInHMode();
            TickNpcHDesirePhase();

            // 盾破后超过五分钟恢复部分护盾
            if (hShieldBroken && LogicTime.time - _lastHModeTimer > 5 * 60)
            {
                var hShieldMax = GetAttr(AttrIdConsts.Basic_ExtraDmg);
                ForceSetResource(AttrIdConsts.UnitHShield, hShieldMax);
            }

            if(!hShieldBroken)
            {
                var hVal = GetAttr(AttrIdConsts.UnitHShield);
                if (hVal <= 0)
                {
                    hShieldBroken = true;
                    _lastHModeTimer = LogicTime.time;
                }
            }

            // 

            //if(!IsFaQing)
            //{
            //    var hValMax = GetHValMax();
            //    var hVal = GetAttr(AttrIdConsts.NPCHVal);
            //    if (hVal >= hValMax * 0.6f)
            //    {
            //        IsFaQing = true;
            //    }
            //}
            //else
            //{
            //    var hValMax = GetHValMax();
            //    var hVal = GetAttr(AttrIdConsts.NPCHVal);
            //    if (hVal < hValMax * 0.3f)
            //    {
            //        IsFaQing = false;
            //    }
            //}

            var blurtValue = GetAttr(AttrIdConsts.NPCSJProgress);
            var blurMax = GetBlurtMax();

            if (blurtValue >= blurMax)
            {
                var fcked = CheckHasState(AttrIdConsts.NpcFcked);

                if(fcked)
                {
                    var player = LogicManager.playerLogicEntity;
                    var sjPlus1 = player.GetAttr(AttrIdConsts.PlayerSJAmount_Fixed);
                    var sjPlus2 = player.GetAttr(AttrIdConsts.PlayerSJAmount_Precent);

                    var npcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(CfgId);
                    var attr = CfgMgr.Cfgs.TbUnitNpcAttr.GetOrDefault(npcCfg?.AttrTemplateId ?? 0);

                    float sjAmount = attr?.BaseBlurtAmount ?? 1.0f;
                    float dmgPerAmount = attr?.BaseBlurtDmg ?? 1.0f;

                    if (CheckHasBuff("status_stiff"))
                    {
                        sjAmount *= 2.5f;
                    }

                    sjAmount = sjAmount * (10000 + sjPlus2) / 10000f + sjPlus1 * 0.001f;

                    OnNpcBlurt(sjAmount, dmgPerAmount);

                    player.OnAbsorbBlurtDirectly(sjAmount, this);

                    LogicManager.viewer.ShowFakeFxEffect("内射", this.Pos);

                }
                else
                {
                    if (CheckCanNpcBlurt())
                    {
                        TryUseBlurtSkill();
                    }
                }
            }
        }

        /// <summary>
        /// 欲望phase
        /// </summary>
        protected void TickNpcHDesirePhase()
        {
            var hVal = GetAttr(AttrIdConsts.NPCHVal);
            var hMax = GetAttr(AttrIdConsts.NPCHVal_Max);

            int desireLevel = PlayerGamePlayRule.GetNpcHDesirePhase(hVal, hMax);
            if(desireLevel != NpcDesirePhase)
            {
                return;
            }

            NpcDesirePhase = desireLevel;
        }


        /// <summary>
        /// 实施h impulse
        /// </summary>
        /// <param name="rawHPulse"></param>
        public void ApplyNpcHImpulse(long rawHPulse)
        {
            (var hValRate, var sjRate) = PlayerGamePlayRule.GetDesirePhaseSplitRate(NpcDesirePhase);

            var hVal = (long)(rawHPulse * hValRate);
            var sjVal = (long)(rawHPulse * sjRate);

            if(hVal > 0)
            {
                ApplyResourceChange(AttrIdConsts.NPCHVal, hVal, false, EDmgFlag.None, null);
            }

            if (sjVal > 0)
            {
                ApplyResourceChange(AttrIdConsts.NPCSJProgress, sjVal, false, EDmgFlag.None, null);
            }
        }

        public void CheckSeeEvil()
        {
            if (LogicTime.time - _checkAlertTimer < 0.5f)
            {
                return;
            }

            _checkAlertTimer = LogicTime.time;

            if (isEvilAlerting) return;

            bool seeEvil = false;
            if (VisionSystem.IsTargetWitnessed(LogicManager.playerLogicEntity.Id))
            {
                if (LogicManager.playerLogicEntity.IsExposed && !LogicManager.PlayerHumanMode)
                {
                    seeEvil = true;
                }
            }

            if (seeEvil)
            {
                if(!CheckHasState(AttrIdConsts.ImmuneEvilShock))
                {
                    LogicManager.globalBuffManager.RequestAddBuff(this.Id, "immune_evil_shock", overrideDuration: 20.0f);
                    LogicManager.globalBuffManager.RequestAddBuff(this.Id, "evil_shock", overrideDuration: 3.0f);
                }
            }
        }
        // 地图逻辑事件
        public override void OnMapLogicEvent(IMapLogicEvent evt)
        {
            if (EnmitySystem != null)
            {
                EnmitySystem.OnMapLogicEvent(evt);
            }

            switch(evt)
            {
                case MLEObjWithOwnerDestroyedEvent objDestroyedEv:
                    {
                        var diff = objDestroyedEv.Pos - this.Pos;
                        if(diff.magnitude > 5.0f)
                        {
                            break;
                        }
                        var angle = Vector2.Angle(diff, CurrentLook);
                        if (angle > 120 * 0.5f)
                        {
                            break;
                        }

                        Debug.Log("npc on event interest");
                        LogicManager.viewer.ShowFakeFxEffect("npc_interest_fx", Pos);
                    }
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool IsInHBehaveMode()
        {
            if(NpcConfig.AlwaysHMode)
            {
                return true;
            }

            return IsFaQing;
        }

        protected override void OnConvertToAttachment()
        {
            base.OnConvertToAttachment();

            AIBrain.ResetBrain();
        }

        protected override void OnRestoreFromAttach()
        {
            base.OnRestoreFromAttach();
        }

        public override long CalculateResourceCostAmount(string attrId, ResourceDeltaIntent intent)
        {

            var delta = intent.delta;

            switch (attrId)
            {
                
                case AttrIdConsts.NPCHVal:
                    {
                        // 正值伤害先扣 H 盾
                        if(delta > 0)
                        {
                            var shieldVal = attributeStore.GetAttr(AttrIdConsts.UnitHShield);
                            if (shieldVal > delta)
                            {
                                ApplyResourceChange(AttrIdConsts.UnitHShield, -delta, intent.isEnmity, intent.deltaFlags, intent.srcEntityId);
                                Debug.Log($"CalculateResourceCostAmount cost hshield {delta}");
                                delta = 0;
                            }
                            else
                            {
                                ApplyResourceChange(AttrIdConsts.UnitHShield, -shieldVal, intent.isEnmity, intent.deltaFlags, intent.srcEntityId);
                                Debug.Log($"CalculateResourceCostAmount cost hshield {shieldVal}");
                                delta = delta - shieldVal;
                            }
                        }
                    }
                    break;

                case AttrIdConsts.NPCSJProgress:
                    {
                        if (delta > 0)
                        {
                            long rate = GetAttr(AttrIdConsts.NPCSJProgress_GainRate);
                            delta = (long)(delta * (10000 + rate) / 10000f);
                        }
                    }
                    break;
                default:
                    {
                        return base.CalculateResourceCostAmount(attrId, intent);
                    }
            }

            return delta;
        }

        protected virtual long GetBlurtMax()
        {
            return 100_000;
        }

        protected virtual long GetHValMax()
        {
            return 100_000;
        }

        /// <summary>
        /// 检查单位是否可喷射
        /// </summary>
        /// <returns></returns>
        private bool CheckCanNpcBlurt()
        {
            if(!abilityController.IsActionable())
            {
                return false;
            }
            return true;
        }


        public void OnNpcBlurt(float sjAmount, float sjDamage)
        {
            ForceSetResource(AttrIdConsts.NPCSJProgress, 0); // 清空射精条
            var totalDamage = (long)(sjAmount * sjDamage * 1000);
            // 自身hp大伤害
            ApplyResourceChange(AttrIdConsts.HP, -totalDamage, false, Fight.FightStruct.EDmgFlag.None, LogicManager.playerLogicEntity.Id);
        }

        // debuff 小射精：不清空射精条
        public void OnNpcMiniBlurt(float sjAmount, float sjDamage, long? srcEntityId)
        {
            var totalDamage = (long)(sjAmount * sjDamage * 1000);
            if (totalDamage <= 0)
            {
                return;
            }

            ApplyResourceChange(AttrIdConsts.HP, -totalDamage, false, Fight.FightStruct.EDmgFlag.None, srcEntityId);
        }

        protected void TryUseBlurtSkill()
        {

            var player = LogicManager.playerLogicEntity;
            var sjPlus1 = player.GetAttr(AttrIdConsts.PlayerSJAmount_Fixed);
            var sjPlus2 = player.GetAttr(AttrIdConsts.PlayerSJAmount_Precent);

            var npcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(CfgId);
            var attr = CfgMgr.Cfgs.TbUnitNpcAttr.GetOrDefault(npcCfg?.AttrTemplateId ?? 0);

            float sjAmount = attr?.BaseBlurtAmount ?? 1.0f;
            float dmgPerAmount = attr?.BaseBlurtDmg ?? 1.0f;

            if (CheckHasBuff("status_stiff"))
            {
                sjAmount *= 2.5f;
            }

            sjAmount = sjAmount * (10000 + sjPlus2) / 10000f + sjPlus1 * 0.001f;

            TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.System,
            });

            abilityController.TryUseAbility("unit_h_mode_sj", overrideParams: new Dictionary<string, string>()
            {
                ["SJ_Amount"] = sjAmount.ToString(),
                ["SJ_Damage"] = dmgPerAmount.ToString(),
            });
        }

        protected override void UnitOnHpChanged(long finalDelta, long? srcEntityId, Vector2? hitDir, bool isEnmity, EDmgFlag deltaFlags)
        {
            base.UnitOnHpChanged(finalDelta, srcEntityId, hitDir, isEnmity, deltaFlags);

            if(srcEntityId != null)
            {
                AggroSystem?.OnTakeDamage(srcEntityId.Value, Math.Abs(finalDelta));
            }
        }

        public void DoAnimation(string animName)
        {
            PlayerAnim(animName);
        }
        // 是否可被处决
        public bool CheckCanExecute()
        {
            //if(NpcConfig.ImmuneExecute)
            //{
            //    return false;
            //}

            if(IsDead)
            {
                return false;
            }

            if(MarkDestroyed)
            {
                return false;
            }

            if(hShieldBroken)
            {
                return true;
            }

            // 低血量也可处决
            var currHp = attributeStore.GetAttr(AttrIdConsts.HP);
            var maxHp = attributeStore.GetAttr(AttrIdConsts.HP_MAX);

            float rate = currHp * 1.0f / maxHp;
            if(rate < 0.2f)
            {
                return true;
            }

            return false;
        }

        public override string GetEnmityCfgId()
        {
            return NpcConfig.EmnityCfgId;
        }

        // 全局 NPC 交谈统一入口对话 id；存在 TbDialogMetaInfo 时由入口生成菜单与分支

        public bool HasDialogEntry()
        {
            if(!string.IsNullOrEmpty(GetCurrentDialogId()))
            {
                return true;
            }

            if(LogicManager.shopDataManager.TryGetShopDefByNpcId(CfgId, out var shopInfo))
            {
                return true;
            }

            return false;
        }


        public string GetCurrentDialogId()
        {
            string fallback = NpcConfig != null ? NpcConfig.PeaceDialogId : string.Empty;
            if (CfgMgr.Cfgs == null || LogicManager == null || string.IsNullOrEmpty(CfgId))
            {
                return fallback;
            }

            int bestPriority = int.MinValue;
            string bestDialogId = null;

            foreach (var row in CfgMgr.Cfgs.TbNpcDialogInfo.DataList)
            {
                if (row.NpcId != CfgId)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(row.NeedNpcVal) && !CheckLocalSwitch(row.NeedNpcVal))
                {
                    continue;
                }

                if (!LogicManager.CheckCommonCondsAll(row.ShowCond))
                {
                    continue;
                }

                if (row.Priority > bestPriority)
                {
                    bestPriority = row.Priority;
                    bestDialogId = row.DialogId;
                }
            }

            return string.IsNullOrEmpty(bestDialogId) ? fallback : bestDialogId;
        }

        public override int GetUnitLevel()
        {
            var attrCfg = CfgMgr.Cfgs.TbUnitNpcAttr.GetOrDefault(NpcConfig.AttrTemplateId);
            if (attrCfg == null) return 1;
            return attrCfg.Level;
        }

        public override bool CanUnsensored()
        {
            if(NpcConfig.NoUnsensored)
            {
                return false;
            }

            return true;
        }


        public override bool IsEnmityWith(BaseUnitLogicEntity otherUnit)
        {
            // 
            if(otherUnit is PlayerLogicEntity && this.IsFaQing)
            {
                return true;
            }

            return EnmitySystem.IsEnmityWith(otherUnit);
        }

        public override bool IsNoAggro()
        {
            return NpcConfig.NoAggro;
        }
    }
}

