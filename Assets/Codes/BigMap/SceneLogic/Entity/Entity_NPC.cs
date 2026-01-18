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


namespace My.Map
{
    public partial class NpcUnitLogicEntity : BaseUnitLogicEntity, IEntityInteractable
    {
        public MapNpcConfig cacheCfg;

        public UnitEnmityComp EnmityComp;
        public MapUnitAIBrain? AIBrain;

        public NpcCombatStateComp combatStateComp;

        private bool hShieldBroken = false;
        private float _lastHModeTimer;

        public event Action EventOnHModeChange;

        public EntityInteractComp InteractComp;

        public string GetRuntimeVariable(string paramName)
        {
            return string.Empty;
        }

        public override NpcCombatStateComp.ECombatState CombatState
        {
            get
            {
                return combatStateComp.CombatState;
            }
        }

        public LogicEntityRecord4Npc NpcRecord
        {
            get
            {
                return (LogicEntityRecord4Npc)BindingRecord;
            }
        }

        public NpcUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheCfg = MapNpcConfigLoader.Get(CfgId);
            this.unitCfg = cacheCfg;

            var npcRecord = (LogicEntityRecord4Npc)bindingRecord;

            Debug.Log($"NpcUnitLogicEntity init {instId} {npcRecord.MoveBehaveType}");
            this.MoveBehaveInfo = new();
            this.MoveBehaveInfo.MoveBehaveMode = npcRecord.MoveBehaveType;
            this.MoveBehaveInfo.FollowPatrolId = npcRecord.PatrolFollowId;
            this.MoveBehaveInfo.PatrolGroupRelativePos = npcRecord.PatrolGroupRelativePos;
            this.MoveBehaveInfo.DisappearOnArrive = npcRecord.DisappearOnArrive;
            this.MoveBehaveInfo.MovePath = npcRecord.MovePath;
        }

        public override EEntityType Type => EEntityType.Npc;

        protected override void InitAbility()
        {
            base.InitAbility();

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

            EnmityComp = new();
            EnmityComp.Initialize(this);

            combatStateComp = new(this);

            InitAiBrain();

            if (NpcRecord.Unsensored)
            {
                LogicManager.globalBuffManager.RequestAddBuff(Id, "unsensored");
            }


            InteractComp = new(this);
            InteractComp.RefreshInteractInfo(cacheCfg.InteractList);
        }

        protected override void InitAttribute()
        {
            var cacheCfg = MapNpcConfigLoader.Get(CfgId);
            moveSpeed = cacheCfg.MoveSpeed;

            base.InitAttribute();

        }

        protected virtual void InitAiBrain()
        {
            AIBrain = new();
            //var cacheCfg = MapMonsterConfigLoader.Get(CfgId);
            AIBrain.InitilaizeAll(this, LogicManager.visionSenser, Pos);
        }

        public override void OnUnitDie(int reason, ResourceDeltaIntent lastIntent = null)
        {
            base.OnUnitDie(reason, lastIntent);

            // 初始化掉落包
            dropBagContainer = new(this.LogicManager, unitCfg.DefaultDropId, 12);


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
                EnmityComp?.Tick(dt);
                combatStateComp?.Tick(dt);

                TickHMode();

                CheckSeeEvil();
            }

            InteractComp?.TickInteract(dt);
            //TickGaze();
        }




        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            base.OnResourceAttriChanged(attrId, before, after, intent);

            switch (attrId)
            {
                case AttrIdConsts.UnitHVal:
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


        /// <summary>
        /// 检查h模式
        /// </summary>
        protected void TickHMode()
        {
            bool currHmode = IsInHMode();

            // 五分钟后恢复意志
            if (hShieldBroken && LogicTime.time - _lastHModeTimer > 5 * 60)
            {
                ForceSetResource(AttrIdConsts.UnitHShield, 10000);
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

            // h模式下 检查射精
            if(currHmode)
            {
                var hValMax = GetHValMax();
                var hVal = GetAttr(AttrIdConsts.UnitHVal);
                if (hVal >= hValMax)
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
            if (VisibilityComp.IsTargetVisible(LogicManager.playerLogicEntity.Id))
            {
                if (LogicManager.playerLogicEntity.IsQueenMode)
                {
                    seeEvil = true;
                    
                }
            }

            if (seeEvil)
            {
                if(!CheckHasState(AttrIdConsts.ImmuneEvilShock))
                {
                    LogicManager.globalBuffManager.AddBuff(this.Id, "immune_evil_shock", overrideDuration: 20.0f);
                    LogicManager.globalBuffManager.AddBuff(this.Id, "evil_shock", overrideDuration: 3.0f);
                }
            }
        }


        /// <summary>
        /// 检查事件 
        /// </summary>
        /// <param name="evt"></param>
        public override void OnMapLogicEvent(IMapLogicEvent evt)
        {
            if (EnmityComp != null)
            {
                EnmityComp.OnMapLogicEvent(evt);
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
                        LogicManager.viewer.ShowFakeFxEffect("目击", Pos);

                        if (AIBrain != null)
                        {
                            AIBrain.blackboard.AttractTrigger = true;
                            AIBrain.blackboard.AttractPos = Vector2.zero;
                            AIBrain.blackboard.AttractSrcId = LogicManager.playerLogicEntity?.Id ?? 0;
                            AIBrain.blackboard.AttractLevel = 1;
                        }
                    }
                    break;
            }
        }

        public override bool CheckIsEmnity()
        {
            return EnmityComp.CheckIsEmnity();
        }

        public override bool CheckIsEmnityFaction(EFactionId factionId)
        {
            return EnmityComp.CheckIsEmnityFaction(factionId);
        }

        public bool IsInHMode()
        {
            if(cacheCfg.AlwaysHMode)
            {
                return true;
            }

            return hShieldBroken;
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
                
                case AttrIdConsts.UnitHVal:
                    {

                        // 对于累计h值 检查扣盾
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
                default:
                    {
                        return base.CalculateResourceCostAmount(attrId, intent);
                    }
            }

            return delta;
        }

        protected virtual long GetHValMax()
        {
            return 10000;
        }

        protected virtual void OnNpcBlurt()
        {
            ForceSetResource(AttrIdConsts.UnitHVal, 0);

            LogicManager.viewer.ShowFakeFxEffect("射!", this.Pos);

            // 伤害
            ApplyResourceChange(AttrIdConsts.HP, -80000, false, Fight.FightStruct.EDmgFlag.None, this.Id);

            TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.System,
            });

            abilityController.TryUseAbility("unit_h_mode_sj");
        }


        protected override void UnitOnHit(long delta, long? srcEntityId, Vector2? hitDir)
        {
            base.UnitOnHit(delta, srcEntityId, hitDir);

            if(srcEntityId != null)
            {
                combatStateComp.OnTakeDamage(srcEntityId.Value, Math.Abs(delta));
            }
        }

        public void DoAnimation(string animName)
        {
            AddAnimLayer(animName);
        }

        /// <summary>
        /// 检查是否可以被处决
        /// </summary>
        /// <returns></returns>
        public bool CheckCanExecute()
        {
            if(cacheCfg.ImmuneExecute)
            {
                return false;
            }

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

            // 低血量也可以斩杀
            var currHp = attributeStore.GetAttr(AttrIdConsts.HP);
            var maxHp = attributeStore.GetAttr(AttrIdConsts.HP_MAX);

            float rate = currHp * 1.0f / maxHp;
            if(rate < 0.2f)
            {
                return true;
            }

            return false;
        }
    }
}

