using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using My.Config;
using My.Map.Entity;
using My.Map.Fight;
using My.Map.Logic;
using My.Player;
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

        /// <summary>
        /// 带得到
        /// </summary>
        public bool IsQueenMode;
        public bool IsPendingGc; // 是否等待触发gc


        public bool IsFaQing = false; // 是否发情中
        public float LastFaQingTimer; // 进入发情时间

        
        public bool IsExposed = false; // 暴露状态
        public float LastExposeTimer; // 进入暴露时间

        public bool IsZhaZhiMode = false;


        public long? gcCuaseId;
        public bool isSelfGc;

        public bool IsRetreating;
        public float RetreatingStartTime;
        public static float RetreatDuration = 5.0f;

        public event Action<long> EventOnAttachmentUpdate;
        public event Action EventOnRequestAimHelper;

        public event Action EventOnFaQingStateChange;
        public event Action EventOnExposeStateChange;

        private float _lowFreqStateTimer;
        private float _highFreqStateTimer;

        public int DesireLevel { get; private set; }

        public PlayerLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {

        }

        public override EEntityType Type => EEntityType.Player;

        public override bool IsInCombat
        {
            get
            {
                return false;
            }
        }

        public int AttractLevel 
        { 
            get 
            {
                var clothesVal = GetAttr(AttrIdConsts.PlayerClothes);
                if (clothesVal > 80000)
                {
                    return 0;
                }

                if (clothesVal > 50000)
                {
                    return 1;
                }
                if (clothesVal > 20000)
                {
                    return 2;
                }
                return 3;
            } 
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            TickPlayerStateLowFreq();

            TickPlayerStateHighFreq();

            TickAttachingObj(dt);

            TickRetreating();
        }


        public override void Initialize()
        {
            base.Initialize();

            DefaultControlledByVelocity = false;

            if (!MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapCfg.DefaultDisguise)
            {
                IsExposed = true;
            }
        }

        protected override void RegisterSpecAttrs()
        {
            // 数值类
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerGcThreshold, initialBase: 100_000);
            attributeStore.RegisterNumeric(AttrIdConsts.Charmed, LogicManager.playerDataManager.ProgressionSystem.GetFinalAttribute((int)EYCAttribute.Charm));

            attributeStore.RegisterNumeric(AttrIdConsts.Basic_HungerCost, initialBase: 10);
            attributeStore.RegisterNumeric(AttrIdConsts.Basic_PleasureAdd, initialBase: 0);

            attributeStore.RegisterResource(AttrIdConsts.PlayerClothes, null, 100_000, 100_000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerSanity, null, 100_000, 100_000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerPleasure, null, 100_000, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerKnockDown, null, 100_000, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerHunger, null, 100_000, 100_000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerNaiLi, null, 100_000, 100_000);
            //attributeStore.RegisterResource(AttrIdConsts.PlayerFaQingVal, null, 100_000, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerOriginPower, null, 1000_000, 300_000);
            

            // 资源类
            attributeStore.RegisterResource(AttrIdConsts.UnitHVal, null, 0);
            attributeStore.RegisterResource(AttrIdConsts.DeepZhaChance, null, 3);

        }

        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            base.OnResourceAttriChanged(attrId, before, after, intent);

            // 4.3 死亡判断窗口：仅在含伤害时检查
            switch (attrId)
            {
                case AttrIdConsts.PlayerPleasure:
                    {
                        if(IsPendingGc)
                        {
                            break;
                        }
                        var gcThreshold = attributeStore.GetAttr(AttrIdConsts.PlayerGcThreshold);
                        if (before < gcThreshold && after >= gcThreshold)
                        {
                            IsPendingGc = true;
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
                        FilterParamLists = new() { EEntityType.Npc },
                    };

                    var surrounds = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, abilityConf.AttractRange, filterParam);

                    foreach (var surround in surrounds)
                    {
                        var unit = surround as NpcUnitLogicEntity;
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
                        // 增加一种技能结束前后的特殊窗口 防止一些被打断也出现窗口
                        new DeriveWindow()
                        {
                            id = "1",
                            window = new TimeWindow(0.2f, 0.35f),
                        }
                    }
                };
                graph.ComboNodes.Add(node);

            }

            {
                var node = new ComboNode()
                {
                    NodeId = 201,
                    AbilityId = "queen_dash_attack_01",
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
                    windowId = "1",
                };

                graph.Transitions.Add(trans);
            }

            graph.BuildGraph();
            return graph;
        }

        

        /// <summary>
        /// 低频进行player相关逻辑
        /// </summary>
        private void TickPlayerStateLowFreq()
        {
            if (LogicTime.time < _lowFreqStateTimer + 1.0f)
            {
                return;
            }

            _lowFreqStateTimer += 1.0f;

            TickResourceChange(1.0f);

            TickRefreshSpiritMonster();

            TickApplyAuraHVal();

            {
                var hunger = GetAttr(AttrIdConsts.PlayerHunger);
                if (hunger <= 0)
                {
                    if (!LogicManager.globalBuffManager.CheckHasBuff(this.Id, "player_hungry"))
                    {
                        LogicManager.globalBuffManager.AddBuff(this.Id, "player_hungry");
                    }
                }
                else
                {
                    if (LogicManager.globalBuffManager.CheckHasBuff(this.Id, "player_hungry"))
                    {
                        LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "player_hungry");
                    }
                }
            }
        }


        private void TickPlayerStateHighFreq()
        {
            if (LogicTime.time < _highFreqStateTimer + 0.2f)
            {
                return;
            }

            _highFreqStateTimer += 0.2f;

            

            RefreshPlayerDesireLevel();

            TickPlayerGcYishang();

            TickBeingGazedInfo();

            // 检查玩家衣着暴露
            TickPlayerExpose();

            // 检查是否进入高潮
            TickGc();

            TickFaQing();
        }

        /// <summary>
        /// 检查玩家状态变化
        /// </summary>
        private void TickResourceChange(float interval)
        {
            var baseGc = attributeStore.GetAttr(AttrIdConsts.Basic_PleasureAdd);
            ApplyResourceChange(AttrIdConsts.PlayerPleasure, baseGc, false, EDmgFlag.None, null);

            var baseHungerCost = attributeStore.GetAttr(AttrIdConsts.Basic_HungerCost);
            ApplyResourceChange(AttrIdConsts.PlayerHunger, -baseHungerCost, false, EDmgFlag.None, null);

            var hunger = GetAttr(AttrIdConsts.PlayerHunger);
            if (hunger <= 0)
            {
                ApplyResourceChange(AttrIdConsts.HP, -500, false, EDmgFlag.None, null);
                LogicManager.viewer.ShowFakeFxEffect("饿", this.Pos);
            }
            else if(hunger >= 90000)
            {
                ApplyResourceChange(AttrIdConsts.HP, 100, false, EDmgFlag.None, null);
            }

            // 将发情缓慢提升到标准线
            int basicEstrus = GetBasicEstrusByDesireLevel();
            long curEstrus = GetAttr(AttrIdConsts.PlayerEstrusProgrss);

            // 发情较低时缓慢上升
            if (curEstrus < basicEstrus * 1000) 
            {
                ApplyResourceChange(AttrIdConsts.PlayerEstrusProgrss, 100, false, EDmgFlag.None, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void RefreshPlayerDesireLevel()
        {
            DesireLevel = 0;
            var cfgs = CfgMgr.Cfgs.TbPlayerDesireLevel.DataList;

            var sanity = GetAttr(AttrIdConsts.PlayerSanity);

            for (int i = 0; i < cfgs.Count; i++)
            {
                int sanLine = cfgs[i].SanLine;
                if (sanity >= sanLine * 1000)
                {
                    DesireLevel = cfgs[i].Level;
                    return;
                }
            }

            DesireLevel = cfgs[cfgs.Count - 1].Level;
        }

        /// <summary>
        /// 获取快感基准
        /// </summary>
        /// <returns></returns>
        private int GetBasicEstrusByDesireLevel()
        {
            var desireCfg = CfgMgr.Cfgs.TbPlayerDesireLevel.GetOrDefault(DesireLevel);
            if(desireCfg == null)
            {
                return 0;
            }
            return desireCfg.BasicEstrus;
        }

        /// <summary>
        /// 更新身上的高潮易伤
        /// </summary>
        private void TickPlayerGcYishang()
        {
            BuffInstance buffInst = null;
            foreach (var buff in BuffContainer)
            {
                if (buff.Value.BuffId == "gc_self_yishang")
                {
                    buffInst = buff.Value;
                }
            }

            if (buffInst != null)
            {
                if (buffInst.Layer > 0)
                {
                    buffInst.Layer -= (int)(Math.Ceiling(LogicTime.time * 20));
                    buffInst.Layer = Math.Max(0, buffInst.Layer);

                    buffInst.OnBuffAddOrUpdate(false);
                }

                if (buffInst.Layer <= 0)
                {
                    LogicManager.globalBuffManager.RequestRemoveBuff(this, buffInst.InstanceId);
                }
            }
        }

        /// <summary>
        /// 检查是否进入暴露状态
        /// </summary>
        private void TickPlayerExpose()
        {
            if(!MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapCfg.DefaultDisguise)
            {
                return;
            }

            var clothes = GetAttr(AttrIdConsts.PlayerClothes);

            if(IsExposed)
            {
                


                if (clothes > 0)
                {
                    IsExposed = false;
                    LogicManager.globalBuffManager.AddBuff(this.Id, "player_clothes_expose");

                    EventOnExposeStateChange?.Invoke();
                }
            }
            else
            {
                if (clothes <= 0)
                {
                    IsExposed = true;
                    LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "player_clothes_expose");

                    EventOnExposeStateChange?.Invoke();
                }
            }
        }


        /// <summary>
        /// 检查高潮状态
        /// </summary>
        private void TickGc()
        {
            if(!IsPendingGc)
            {
                return;
            }
            IsPendingGc = false;

            var gcLiquidEntity = new LogicEntityRecord();
            gcLiquidEntity.Id = GameLogicManager.LogicEntityIdInst++;
            gcLiquidEntity.EntityType = EEntityType.AreaEffect;
            gcLiquidEntity.CfgId = "ground_gc_liquid";
            gcLiquidEntity.LifeTime = 20.0f;
            gcLiquidEntity.Position = this.Pos;
            gcLiquidEntity.FactionId = this.FactionId;

            LogicManager.AddNewEntityRecord(gcLiquidEntity);

            // 添加自身debuff
            LogicManager.globalBuffManager.RequestAddBuff(this.Id, "gc_self_debuff");

            LogicManager.globalBuffManager.RequestAddBuff(this.Id, "gc_self_yishang", layer: 100);

            // 非自慰需要扣san
            if(GetAttr(AttrIdConsts.PlayerSanity) > 60_000)
            {
                ApplyResourceChange(AttrIdConsts.PlayerSanity, -10000, false, FightStruct.EDmgFlag.None, this.Id);
            }
            

            ForceSetResource(AttrIdConsts.PlayerPleasure, 0);

            LogicManager.viewer.ShowPauseCloseupWindow("gc", 1.0f);
        }

        /// <summary>
        /// 检查是否进入发情状态
        /// </summary>
        private void TickFaQing()
        {
            
            if (IsFaQing)
            {
                bool canLeave = false;

                if (LogicTime.time - LastFaQingTimer > 60.0f)
                {
                    canLeave = true;
                }

                // 发情状态下，有以下情况会进行脱离：
                //     1.高潮脱离 走另一条路线
                //     2.靠理智强行（待定）
                //     3.时间脱离

                if (canLeave) 
                {
                    LogicManager.globalBuffManager.RemoveAllBuffById(Id, "player_faqing");
                    IsFaQing = false;
                    Debug.Log("player leave faqing");

                    EventOnFaQingStateChange?.Invoke();
                }
            }
            // 检查进入发情
            else
            {

                bool checkEnter = false;

                //var faqingVal = GetAttr(AttrIdConsts.PlayerFaQingVal);
                //if(faqingVal >= 100_000)
                //{
                //    checkEnter = true;
                //}

                if(checkEnter)
                {
                    LogicManager.globalBuffManager.RequestAddBuff(Id, "player_faqing");
                    IsFaQing = true;
                    Debug.Log("player enter faqing");

                    EventOnFaQingStateChange?.Invoke();

                    //attributeStore.SetResource(AttrIdConsts.PlayerFaQingVal, 0);
                }
            }
        }


        public override void OnStatusAttriChanged(string attrId, bool isOn)
        {
            base.OnStatusAttriChanged(attrId, isOn);
            switch (attrId)
            {
                case AttrIdConsts.HideView:
                    {
                        
                    }
                    break;
                case AttrIdConsts.PlayerClothes:
                    {

                    }
                    break;
            }
        }


        public override long CalculateResourceCostAmount(string attrId, ResourceDeltaIntent intent)
        {
            long delta = intent.delta;
            switch (attrId)
            {
                //case AttrIdConsts.PlayerFaQingVal:
                //    {
                //        if(IsFaQing)
                //        {
                //            return 0;
                //        }
                //        return delta;
                //    }
                //    break;
                default:
                    {
                        return base.CalculateResourceCostAmount(attrId, intent);
                    }
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
        /// 检查向周围传播hval
        /// </summary>
        protected void TickApplyAuraHVal()
        {
            float auraRange = 3.0f;
            // 
            var units = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, 3.0f, new EntityFilterParam()
            {
                FilterParamLists = new() { EEntityType.Npc },
                CampFilterType = ECampFilterType.NotSelf,
                SelfCampId = EFactionId.Player,
            });

            var effect = new MapAbilityEffectAddResourceCfg()
            {
                ResourceId = AttrIdConsts.UnitHVal,
                AddValue = 50,
                IsEnmity = true,
            };

            foreach (var unit in units)
            {
                var sourceInfo = new EffectSourceInfo()
                {
                    SrcType = ESourceType.Mechanism,
                    SrcEntityId = this.Id,
                };
                GameLogicManager.LogicFightEffectContext ctx = new(LogicManager, EFightCtxType.None, sourceInfo)
                {
                    TargetId = unit.Id,
                };
                LogicManager.HandleLogicFightEffect(effect, ctx);
            }
        }

        #region 移动声音等


        #endregion

        #region watch

        private Dictionary<long, float> BeingGazedTrack = new();

        /// <summary>
        /// tick 被注视效果
        /// </summary>
        private void TickBeingGazedInfo()
        { 

            foreach(var key in BeingGazedTrack.Keys.ToList())
            {
                if (BeingGazedTrack[key] + 2.0f < LogicTime.time)
                {
                    BeingGazedTrack.Remove(key);
                }
            }

            if(WillBeGazed())
            {
                if (BeingGazedTrack.Count > 1)
                {
                    //attributeStore.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 100, false, EDmgFlag.None, null);
                }
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

        public void OnGazeEnter(long srcId)
        {
            BeingGazedTrack[srcId] = LogicTime.time;
        }

        public void OnGazeLeave(long srcId)
        {
            BeingGazedTrack.Remove(srcId);
        }

        #endregion

        public override void OnUnitDie(int reason, ResourceDeltaIntent lastIntent = null)
        {
            base.OnUnitDie(reason, lastIntent);
        }



        public class AttachingObjInfo
        {
            public int Id;
            public string AttachId;
            public long? SrcEntityId;
            public float AttachDuration;

            public float leftDuration;
            public float LeftHp;

            public long BuffInstId;
        }

        public List<AttachingObjInfo> AtttachingObjList = new();


        /// <summary>
        /// 打attach
        /// </summary>
        public void HitAttachObjs()
        {
            foreach (var obj in AtttachingObjList)
            {
                obj.LeftHp -= 1;
            }
        }

        public void AddAttachingObjInfo(string attachId, long? srcEntityId)
        {
            int id = 1;
            if(AtttachingObjList.Count > 0)
            {
                id = AtttachingObjList.Select(item => item.Id).Max() + 1;
            }


            var cfg = MapPlayerAttachObjCfgLoader.Get(attachId);
            if(cfg == null)
            {
                Debug.LogError("No attach found {attachId}");
                return;
            }

            var obj = new AttachingObjInfo();
            obj.Id = id;
            obj.AttachId = attachId;
            obj.SrcEntityId = srcEntityId;
            obj.AttachDuration = cfg.AutoDropTime;
            obj.LeftHp = cfg.HitCount;

            AtttachingObjList.Add(obj);

            if(!string.IsNullOrEmpty(cfg.AttachMainBuff))
            {
                long bid = LogicManager.globalBuffManager.AddBuff(this.Id, cfg.AttachMainBuff);
                obj.BuffInstId = bid;
            }

            // 通知上层改变view
            EventOnAttachmentUpdate?.Invoke(0);
        }

        /// <summary>
        /// 执行撤退
        /// </summary>
        private void TickRetreating()
        {
            if(!IsRetreating)
            {
                return;
            }

            if (LogicTime.time - RetreatingStartTime > RetreatDuration)
            {
                IsRetreating = false;

                LogicManager.OnBigMapRetreatSuccess();
            }
        }

        public void TryStartRetreating()
        {
            if (IsRetreating)
            {
                return;
            }

            IsRetreating = true;
            RetreatingStartTime = LogicTime.time;
        }
        

        /// <summary>
        /// 检查身上的attack物体
        /// </summary>
        private void TickAttachingObj(float dt)
        {
            foreach(var obj in AtttachingObjList)
            {
                if(obj.SrcEntityId != null)
                {
                    var entity = LogicManager.GetLogicEntity(obj.SrcEntityId.Value) as BaseUnitLogicEntity;
                    if (entity != null)
                    {
                        entity.TeleportTo(this.Pos);
                    }
                }

                obj.leftDuration -= dt;
            }

            bool changed = false;
            for(int i = AtttachingObjList.Count - 1; i>=0;i--)
            {
                bool removed = false;
                if ((AtttachingObjList[i].AttachDuration > 0 && AtttachingObjList[i].leftDuration <= 0) || AtttachingObjList[i].LeftHp <= 0)
                {
                    removed = true;
                }


                if (removed)
                {
                    OnAttachRemoved(AtttachingObjList[i]);

                    AtttachingObjList.RemoveAt(i);

                    changed = true;
                }
            }

            if(changed)
            {
                EventOnAttachmentUpdate?.Invoke(0);
            }
        }


        private void OnAttachRemoved(AttachingObjInfo obj)
        {
            if (obj.SrcEntityId != null)
            {
                var entity = LogicManager.GetLogicEntity(obj.SrcEntityId.Value) as BaseUnitLogicEntity;
                if (entity != null)
                {
                    entity.RestoreFromAttach();
                }
            }

            if(obj.BuffInstId != 0)
            {
                LogicManager.globalBuffManager.RequestRemoveBuff(this, obj.BuffInstId);
            }
        }


        protected override float GetBaseMoveSpeed()
        {
            if(IsQueenMode)
            {
                return 1.2f;
            }
            else
            {
                return 2.5f;
            }
        }

        public override void InitAggroSystem()
        {
        }

        public override void InitEnmitySystem()
        {
        }

        private long SupportTargetId;
        public void UpdateSupportTargetId(long supportTargetId)
        {
            this.SupportTargetId = supportTargetId;
        }

        public void RequestAimHelper()
        {
            EventOnRequestAimHelper?.Invoke();
        }

        public override long CurrentTargetId
        {
            get
            {
                RequestAimHelper();
                return SupportTargetId;
            }
        }


        public override void ProcessHit(long? srcEntityId, Vector2? hitDir)
        {
            base.ProcessHit(srcEntityId, hitDir);

            if (IsInStealth())
            {
                EndStealth();
            }
        }

        public void SwitchZhaZHiMode()
        {
            if(IsZhaZhiMode)
            {
                IsZhaZhiMode = !IsZhaZhiMode;

                LogicManager.globalBuffManager.AddBuff(this.Id, "player_zhazhi");
            }
            else
            {
                IsZhaZhiMode = !IsZhaZhiMode;

                LogicManager.globalBuffManager.AddBuff(this.Id, "player_zhazhi");
            }
        }
    }
}





