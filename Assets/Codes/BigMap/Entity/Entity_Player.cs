using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using My.Map.Entity;
using My.Map.Fight;
using My.Map.Logic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static My.GameLogicManager;
using static My.Map.Entity.EntitySkillComboGraph;
using static My.Map.Entity.MapEntitySkillManager;
using static My.Map.Fight.FightStruct;


namespace My.Map
{
    public class PlayerLogicEntity : BaseUnitLogicEntity, IAttractSource
    {

        public bool IsQueenMode;


        public bool isPendingGc; // 是否等待触发gc
        public long? gcCuaseId;
        public bool isSelfGc;


        public event Action<long> EventOnAttachmentUpdate;

        public PlayerLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {

        }
        

        public override EEntityType Type => EEntityType.Player;

        public bool IsEnabled { get; private set; } = true;

        protected override void InitAiBrain()
        {
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            //扣减值
            TickResourceCost();

            TickRefreshSpiritMonster();

            //TickMoveNoiseEffect(now, dt);
            TickAddAuraHVal(dt);

            TickWatchedInfo();

            TickGc();
            TickPlayerGcYishang();
        }


        public override void Initialize()
        {
            base.Initialize();

            this.ControlledFacing = true;
        }

        protected override void InitAttribute()
        {
            moveSpeed = 2.0f;
            // 数值类
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerGcThreshold, initialBase: 10000);
            attributeStore.RegisterNumeric("HP.Max", initialBase: 1000000);
            attributeStore.RegisterNumeric("RegenRate.HP", initialBase: 5);

            RegisterCommonStates();

            attributeStore.RegisterResource(AttrIdConsts.HP, AttrIdConsts.HP_MAX, null, 1000000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerClothes, null, 100000, 100000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerSan, null, 100, 100);
            attributeStore.RegisterResource(AttrIdConsts.PlayerPleasure, null, 100000, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerKnockDown, null, 100, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerHunger, null, 100, 100);
            attributeStore.RegisterResource(AttrIdConsts.PlayerNaiLi, null, 100, 100);

            // 资源类
            attributeStore.RegisterResource(AttrIdConsts.UnitEnterHVal, null, 0);
            attributeStore.RegisterResource(AttrIdConsts.DeepZhaChance, null, 3);

            attributeStore.Commit();
        }

        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            base.OnResourceAttriChanged(attrId, before, after, intent);

            // 4.3 死亡判断窗口：仅在含伤害时检查
            switch (attrId)
            {
                case AttrIdConsts.PlayerPleasure:
                    {
                        if(isPendingGc)
                        {
                            break;
                        }
                        var gcThreshold = attributeStore.GetAttr(AttrIdConsts.PlayerGcThreshold);
                        if (before < gcThreshold && after >= gcThreshold)
                        {
                            isPendingGc = true;
                            if(intent.deltaFlags.HasFlag(EDmgFlag.ZiWei))
                            {
                                isSelfGc = true;
                            }
                            else
                            {
                                isSelfGc = false;
                            }
                            break;
                        }
                    }
                    break;
            }
        }


        public float applyHValTimer;

        protected override void InitAbility()
        {
            base.InitAbility();

            foreach(var skill in LogicManager.playerDataManager.PlayerSkillList)
            {
                ablilityManager.RegisterSkill(skill);
            }

            abilityController.EventOnUseAbility += (abilityName) =>
            {
                // 检查施加attract
                var abilityConf = AbilityLibrary.GetAbilityConfig(abilityName);
                if (abilityConf == null)
                {
                    return;
                }
                if (abilityConf.CauseAttract)
                {
                    var filterParam = new EntityFilterParam()
                    {
                        FilterParamLists = new() { EEntityType.Monster, EEntityType.Npc },
                    };

                    var surrounds = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, abilityConf.AttractRange, filterParam);

                    foreach (var surround in surrounds)
                    {
                        var unit = surround as BaseUnitLogicEntity;
                        if (unit != null)
                        {
                            unit.ApplyAttracted(Pos, abilityConf.AttractPower, this);
                        }
                    }
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
                    AbilityId = "queen_attack_01",
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
                    AbilityId = "queen_attack_02",
                    deriveWindows = new List<DeriveWindow>()
                    {
                        new DeriveWindow()
                        {
                            id = "1",
                            window = new TimeWindow(0.27f, 0.6f),
                        }
                    }
                };
                graph.ComboNodes.Add(node);

            }
            {
                var node = new ComboNode()
                {
                    NodeId = 102,
                    AbilityId = "queen_attack_03",
                };
                graph.ComboNodes.Add(node);

            }
            {
                var node = new ComboNode()
                {
                    NodeId = 200,
                    AbilityId = "queen_dash",
                    deriveWindows = new List<DeriveWindow>()
                    {
                        new DeriveWindow()
                        {
                            id = "1",
                            window = new TimeWindow(0.4f, 0.55f),
                        }
                    }
                };
                graph.ComboNodes.Add(node);

            }

            {
                var node = new ComboNode()
                {
                    NodeId = 201,
                    AbilityId = "queen_dash_attack",
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
                        SkillId = "queen_attack"
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
                        SkillId = "queen_attack"
                    },
                    windowId = "1",
                };

                graph.Transitions.Add(trans);
            }
            {
                var trans = new EntitySkillComboGraph.Transition()
                {
                    fromNodeId = 101,
                    toNodeId = 102,
                    triggerInput = new InputPattern()
                    {
                        SkillId = "queen_attack"
                    },
                    windowId = "1",
                };

                graph.Transitions.Add(trans);
            }
            {
                var trans = new EntitySkillComboGraph.Transition()
                {
                    fromNodeId = 0,
                    toNodeId = 200,
                    triggerInput = new InputPattern()
                    {
                        SkillId = "queen_dash"
                    },
                };

                graph.Transitions.Add(trans);
            }
            {
                var trans = new EntitySkillComboGraph.Transition()
                {
                    fromNodeId = 200,
                    toNodeId = 201,
                    triggerInput = new InputPattern()
                    {
                        SkillId = "queen_attack"
                    },
                };

                graph.Transitions.Add(trans);
            }

            graph.BuildGraph();
            return graph;
        }
        public void TickResourceCost()
        {
            var baseGc = attributeStore.GetAttr(AttrIdConsts.PlayerHungerCost);

        }

        /// <summary>
        /// 更新身上的高潮易伤
        /// </summary>
        public void TickPlayerGcYishang()
        {
            BuffInstance buffInst = null;
            foreach (var buff in BuffContainer)
            {
                if (buff.Value.BuffId == "gc_self_yishang") 
                {
                    buffInst = buff.Value;
                }
            }

            if(buffInst != null)
            {
                if(buffInst.Layer > 0)
                {
                    buffInst.Layer -= (int)(Math.Ceiling(LogicTime.time * 20));
                    buffInst.Layer = Math.Max(0, buffInst.Layer);

                    buffInst.OnBuffAddOrUpdate(false);
                }

                if(buffInst.Layer <= 0)
                {
                    LogicManager.globalBuffManager.RequestRemoveBuff(this, buffInst.InstanceId);
                }
            }
        }

        public void TickGc()
        {
            if(!isPendingGc)
            {
                return;
            }
            isPendingGc = false;

            var gcLiquidEntity = new LogicEntityRecord();
            gcLiquidEntity.Id = GameLogicManager.LogicEntityIdInst++;
            gcLiquidEntity.EntityType = EEntityType.AreaEffect;
            gcLiquidEntity.CfgId = "ground_gc_liquid";
            gcLiquidEntity.LifeTime = 20.0f;
            gcLiquidEntity.Position = this.Pos;
            gcLiquidEntity.FactionId = this.FactionId;

            LogicManager.AddNewEntityRecord(gcLiquidEntity);

            LogicManager.globalBuffManager.RequestAddBuff(this.Id, "gc_self_debuff");

            LogicManager.globalBuffManager.RequestAddBuff(this.Id, "gc_self_yishang", layer: 100);

            if(!isSelfGc)
            {
                // 非自慰需要扣san
                ApplyResourceChange(AttrIdConsts.PlayerSan, -500, false, FightStruct.EDmgFlag.None, this.Id);
            }

            ForceSetResource(AttrIdConsts.PlayerPleasure, 0);

            LogicManager.viewer.ShowPauseCloseupWindow("gc", 1.0f);
        }

        public override void OnStatusAttriChanged(string attrId, bool isOn)
        {
            base.OnStatusAttriChanged(attrId, isOn);
            switch (attrId)
            {
                case AttrIdConsts.HidingMask:
                    {
                        // 进入隐身时
                        if (isOn)
                        {
                            bool hasWatched = false;
                            var filterParam = new EntityFilterParam()
                            {
                                FilterParamLists = new() { EEntityType.Monster, EEntityType.Npc },
                                CampFilterType = ECampFilterType.NotSelf,
                                SelfCampId = EFactionId.Player,
                            };

                            var surrounds = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, 5, filterParam);
                            if (surrounds != null)
                            {
                                foreach (var one in surrounds)
                                {
                                    if (one is not BaseUnitLogicEntity unit)
                                    {
                                        continue;
                                    }

                                    // 敌对模式
                                    if (unit.Type == EEntityType.Monster)
                                    {
                                        if (!LogicManager.visionSenser.CanSee(unit.Pos, unit.FaceDir, this.Pos, 5f, 60))
                                        {
                                            continue;
                                        }

                                        hasWatched = true;
                                    }
                                }
                            }

                            if (hasWatched)
                            {
                                LogicManager.globalBuffManager.RequestAddBuff(this.Id, "hide_marked", 1);
                            }
                        }
                        else
                        {
                            // 脱战时同样需要清理该标记
                            LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "hide_marked");
                        }
                    }
                    break;
            }
        }


        private float lastRefreshSpiritTime; // 上次更新时间

        /// <summary>
        /// 检查精灵怪物
        /// </summary>
        protected void TickRefreshSpiritMonster()
        {
            
        }

        /// <summary>
        /// 为周围看着自己的打
        /// </summary>
        protected void TickAddAuraHVal(float dt)
        {
            if (LogicTime.time < applyHValTimer)
            {
                return;
            }
            applyHValTimer = LogicTime.time + 0.2f;

            ApplyAuraHVal();
        }


        protected void ApplyAuraHVal()
        {
            float auraRange = 3.0f;
            // 
            var units = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, 3.0f, new EntityFilterParam()
            {
                FilterParamLists = new() { EEntityType.Monster, EEntityType.Npc },
                CampFilterType = ECampFilterType.NotSelf,
                SelfCampId = EFactionId.Player,
            });

            var effect = new MapAbilityEffectAddResourceCfg()
            {
                ResourceId = AttrIdConsts.UnitEnterHVal,
                AddValue = 20,
                IsEnmity = true,
            };

            foreach (var unit in units)
            {
                var sourceInfo = new EffectSourceInfo()
                {
                    SrcType = ESourceType.Mechanism,
                    SrcEntityId = this.Id,
                };
                GameLogicManager.LogicFightEffectContext ctx = new(LogicManager, sourceInfo)
                {
                    TargetId = unit.Id,
                };
                LogicManager.HandleLogicFightEffect(effect, ctx);
            }
        }

        #region 移动声音等


        #endregion

        #region watch

        public Dictionary<long, float> WatchedInfo = new();

        public float _watchTimer = 0;
        public void TickWatchedInfo()
        { 
            if(LogicTime.time < _watchTimer)
            {
                return;
            }

            _watchTimer = LogicTime.time + 1f;

            foreach(var key in WatchedInfo.Keys.ToList())
            {
                if (WatchedInfo[key] + 2.0f < LogicTime.time)
                {
                    WatchedInfo.Remove(key);
                }
            }

            if(WatchedInfo.Count > 1)
            {
                attributeStore.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 100, false, EDmgFlag.None, null);
            }
        }



        /// <summary>
        /// 只有衣装满足条件时 才成为被注视对象
        /// </summary>
        /// <returns></returns>
        public bool WillBeGazed()
        {
            if (GetAttr(AttrIdConsts.PlayerClothes) > 80000)
            {
                return false;
            }

            return true;
        }

        public void UpdateWatchedInfo(long watchId)
        {
            WatchedInfo[watchId] = LogicTime.time;
        }

        #endregion

        public override void OnUnitDie(int reason, ResourceDeltaIntent lastIntent = null)
        {
            base.OnUnitDie(reason, lastIntent);


        }

        public class AttachmentUnitInfo
        {
            public string AttachBuffId;
            public string AttachViewName;
        }

        public List<long> AtttachingUnits = new();
        public void AddAttachmentUnit(long entity, string attachId)
        {
            AtttachingUnits.Add(entity);

            // 通知上层改变view
            EventOnAttachmentUpdate?.Invoke(0);

        }
    }
}





