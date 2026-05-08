using Config.Unit;
using Config;
using UnityEngine;
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

        private bool hShieldBroken = false;
        private float _lastHModeTimer;

        public event Action EventOnHModeChange;

        public bool IsFaQing { get; private set; }


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
        }

        protected override void OnLocalSwitchesMutated()
        {
            base.OnLocalSwitchesMutated();
            var key = NpcRecord.CharacterKey;
            if (string.IsNullOrEmpty(key) || LogicManager?.worldPersistState == null)
            {
                return;
            }

            LogicManager.worldPersistState.NpcCharacters.ReplaceRuntimeLocalSwitches(key, EntityLocalSwitches);
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

        public override string WorldMapLandmarkLabel =>
            NpcConfig != null && !string.IsNullOrEmpty(NpcConfig.Name) ? NpcConfig.Name : CfgId;

        protected override void InitAbility()
        {
            base.InitAbility();

            if (NpcConfig != null)
            {
                ablilityManager.ReconcileRegisteredSkills(NpcConfig.SkillList);
            }

            ablilityManager.RegisterSkill("default_h_attack");

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

            if (NpcRecord.Unsensored)
            {
                LogicManager.globalBuffManager.RequestAddBuff(Id, "unsensored");
            }



            if (NpcConfig.NotTarget)
            {
                LogicManager.globalBuffManager.RequestAddBuff(this.Id, "not_fight_target");
            }

            //InteractComp = new(this);

            //InteractComp.RefreshInteractInfo();
            //InteractComp.RefreshInteractInfo(NpcConfig.InteractList);

        }

        protected override void RegisterSpecAttrs()
        {
            var attrCfg = CfgMgr.Cfgs.TbUnitNpcAttr.GetOrDefault(NpcConfig.AttrTemplateId);

            if(attrCfg != null)
            {
                moveSpeed = attrCfg.MoveSpeed;
            }

            // H 模式相关资源注册
            attributeStore.RegisterResource(AttrIdConsts.NPCHVal, null, 100_000, 0);
            attributeStore.RegisterResource(AttrIdConsts.UnitHShield, null, 120_000, 120_000);
            attributeStore.RegisterResource(AttrIdConsts.DeepZhaChance, null, 999, 3);
            attributeStore.RegisterResource(AttrIdConsts.NPCSJProgress, null, 100_000, 0);
        }

        public override bool IsOmniVision()
        {
            if (AIBrain == null) return false;

            if (AIBrain.CurrentState == AIBrain.StateCombat) return true;
            if (AIBrain.CurrentState == AIBrain.StateFlee) return true;
            return false;
        }

        protected virtual void InitAiBrain()
        {
            AIBrain = AIBrainFactory.CreateAIBrain(this);
        }

        public override void OnUnitDie(int reason, ResourceDeltaIntent lastIntent = null)
        {
            base.OnUnitDie(reason, lastIntent);

            // 初始化掉落容器
            dropBagContainer = new(this.LogicManager, NpcConfig.DefeatDropId, 12);


            if (lastIntent != null && lastIntent.srcEntityId != null)
            {
                var srcEntity = LogicManager.GetLogicEntity(lastIntent.srcEntityId.Value);

                var diff = srcEntity.Pos - this.Pos;
                var impluse = -(diff.normalized);

                ApplyKnockBack(impluse, 0.5f);
            }
        }

        private float _checkAlertTimer;


        protected override void TickActivateState(float dt)
        {
            base.TickActivateState(dt);

            if(!IsAttaching)
            {
                AIBrain?.Tick(dt);

                TickAttractState();

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

            if(!IsFaQing)
            {
                var hValMax = GetHValMax();
                var hVal = GetAttr(AttrIdConsts.NPCHVal);
                if (hVal >= hValMax * 0.6f)
                {
                    IsFaQing = true;
                }
            }
            else
            {
                var hValMax = GetHValMax();
                var hVal = GetAttr(AttrIdConsts.NPCHVal);
                if (hVal < hValMax * 0.3f)
                {
                    IsFaQing = false;
                }
            }


            if(CheckCanNpcBlurt())
            {
                var blurtValue = GetAttr(AttrIdConsts.NPCSJProgress);
                var blurMax = GetBlurtMax();

                if(blurtValue >= blurMax)
                {
                    OnNpcBlurt();
                }
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
            if (VisionSystem.IsTargetVisible(LogicManager.playerLogicEntity.Id))
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

        public override bool IsInHMode()
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


        protected virtual void OnNpcBlurt()
        {
            ForceSetResource(AttrIdConsts.NPCSJProgress, 0); // 清空射精条

            LogicManager.viewer.ShowFakeFxEffect("npc_h_burst", this.Pos);

            var player = LogicManager.playerLogicEntity;
            var sjPlus1 = player.GetAttr(AttrIdConsts.PlayerSJAmount_Fixed);
            var sjPlus2 = player.GetAttr(AttrIdConsts.PlayerSJAmount_Precent);

            var npcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(CfgId);
            var attr = CfgMgr.Cfgs.TbUnitNpcAttr.GetOrDefault(npcCfg?.AttrTemplateId ?? 0);

            float sjAmount = attr?.BaseBlurtAmount ?? 1.0f;
            float dmgPerAmount = attr?.BaseBlurtDmg ?? 1.0f;

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
        public const string NpcDialogHubId = "npc_generic_entry";

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
    }
}

