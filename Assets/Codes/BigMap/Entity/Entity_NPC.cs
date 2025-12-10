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


namespace My.Map
{
    public partial class NpcUnitLogicEntity : BaseUnitLogicEntity
    {
        public MapNpcConfig cacheCfg;

        public UnitEnmityComp EnmityComp;
        public MapUnitAIBrain? AIBrain;

        public NpcCombatStateComp combatStateComp;

        public bool IsHMode;
        private float _lastHModeTimer;

        public event Action EventOnHModeChange;

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



            this.EmnityConfId = npcRecord.EnmityConfId;
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
            dropBagContainer = new(this.LogicManager, unitCfg.DropId, 12);


            if (lastIntent != null && lastIntent.srcEntityId != null)
            {
                var srcEntity = LogicManager.GetLogicEntity(lastIntent.srcEntityId.Value);

                var diff = srcEntity.Pos - this.Pos;
                var impluse = -(diff.normalized);

                ApplyKnockBack(impluse, 0.5f);
            }
        }

        protected override void TickActivateState(float dt)
        {
            base.TickActivateState(dt);

            if(!IsAttaching)
            {
                AIBrain?.Tick(dt);
                TickAttractState();
                EnmityComp?.Tick(dt);
                combatStateComp?.Tick(dt);

                // 这个一定是属于npc的 其他unit不应该有 
                UpdateHMode();

                TickHMode();
            }

            //TickGaze();
        }

        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            base.OnResourceAttriChanged(attrId, before, after, intent);

            // 4.3 死亡判断窗口：仅在含伤害时检查
            switch (attrId)
            {
                case AttrIdConsts.UnitHVal:
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
            if(!IsHMode)
            {
                var hVal = GetAttr(AttrIdConsts.UnitHVal);
                if(hVal >= 10000)
                {
                    IsHMode = true;
                    _lastHModeTimer = LogicTime.time;

                    EventOnHModeChange?.Invoke();

                    ForceSetResource(AttrIdConsts.UnitHVal, 0);
                }
            }
            else
            {
                if(LogicTime.time - _lastHModeTimer > 5 * 60)
                {
                    IsHMode = false;
                    EventOnHModeChange?.Invoke();
                    return;
                }

                var hVal = GetAttr(AttrIdConsts.UnitHVal);
                if (hVal >= 10000)
                {
                    ForceSetResource(AttrIdConsts.UnitHVal, 0);

                    LogicManager.viewer.ShowFakeFxEffect("射!", this.Pos);

                    // 伤害
                    ApplyResourceChange(AttrIdConsts.HP, -80000, false, Fight.FightStruct.EDmgFlag.None, this.Id);
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
        }

        public override bool CheckIsEmnity()
        {
            return EnmityComp.CheckIsEmnity();
        }

        public override bool CheckIsEmnityFaction(EFactionId factionId)
        {
            return EnmityComp.CheckIsEmnityFaction(factionId);
        }

        public void UpdateHMode()
        {
            if(cacheCfg.AlwaysHMode)
            {
                IsHMode = true;
            }
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
    }
}

