using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Unit;
using My.Map.Fight;
using My.Map.Logic;
using My.Map.Scene;
using My.Player;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static My.GameLogicManager;
using static My.Map.Entity.EntitySkillComboGraph;
using static My.Map.Entity.MapEntitySkillManager;
using static My.Map.Fight.FightStruct;
using static UnityEngine.Rendering.VolumeComponent;


namespace My.Map
{

    

    public class PlayerLogicEntity : BaseUnitLogicEntity, IAttractSource
    {

        /// <summary>
        /// 带得到
        /// </summary>
        public bool IsQueenMode;
        public bool IsPendingGc; // 是否等待触发gc

        bool _miniGcLowArmed = true;
        bool _miniGcHighArmed = true;


        public bool IsFaQing = false; // 是否发情中
        public float LastFaQingTimer; // 进入发情时间

        public bool DisguiseIfPossible; // 希望伪装自身
        public bool IsExposed { get; set; } = false; // 暴露状态 只有
        public float LastExposeTimer; // 进入暴露时间

        public bool IsZhaZhiMode => CheckHasState(AttrIdConsts.PlayerZhaZhiMode);

        // 沿路径铺设粉雾地面格（参数仍读 TbMapAreaEffect player_pink_mist_trail）
        Vector2 _pinkMistLastTrailPos;
        bool _pinkMistTrailPosInited;

        // 特殊蹲伏
        public bool IsSpecialCrouchStance { get; private set; }

        PlayerHostileDamageBurstTracker _hostileDamageBurstTracker;

        // 敌意短时受伤次数监控（滑动窗口）
        public PlayerHostileDamageBurstTracker HostileDamageBurstTracker => _hostileDamageBurstTracker;

        private bool _dirst_clothes { get; set; }

        public void CheckCanSwitchCrouchStance()
        {

        }

        public void SetSpecialCrouchStance(bool value)
        {
            if (IsSpecialCrouchStance == value)
            {
                return;
            }

            IsSpecialCrouchStance = value;
            if (value)
            {
                LogicManager.globalBuffManager.AddBuff(Id, "player_crouch_stance");
            }
            else
            {
                LogicManager.globalBuffManager.RemoveAllBuffById(Id, "player_crouch_stance");
            }

            RequestAnimLayerRefresh(0);
        }

        // ---------- 搬运 NPC 尸体/昏迷单位（权威状态在逻辑层，供 AI/规则/交互读取）----------

        public const float CarryPutDownSearchRadius = 2.2f;
        public const float CarryPutDownClearanceRadius = 0.28f;

        public long CarriedNpcEntityId { get; private set; }

        public bool IsCarryingNpcBody => CarriedNpcEntityId != 0;

        // 逻辑层中止搬运时通知表现层（HUD、Locomotion 等），避免仅清 Buff 而 UI 仍显示搬运中
        public event Action EventOnCarryNpcBodyAborted;

        public bool TryBeginCarryNpcBody(NpcUnitLogicEntity npc)
        {
            if (npc == null || npc.MarkDestroyed)
            {
                return false;
            }

            if (!npc.IsDead && !npc.MarkUnsensored)
            {
                return false;
            }

            if (IsCarryingNpcBody)
            {
                return false;
            }

            SetSpecialCrouchStance(false);

            var gbm = LogicManager.globalBuffManager;
            gbm.AddBuff(npc.Id, "give_hide");
            gbm.AddBuff(Id, "player_carry_slow");
            gbm.AddBuff(Id, "player_carry_ov_idle");
            gbm.AddBuff(Id, "player_carry_ov_move");
            gbm.AddBuff(Id, "player_carry_ov_walk");

            CarriedNpcEntityId = npc.Id;
            return true;
        }

        // 尝试在附近放下搬运单位；failedNoEmptySpot 表示仍保持搬运状态
        public bool TryPutDownCarriedNpcBody(out bool failedNoEmptySpot)
        {
            failedNoEmptySpot = false;
            if (!IsCarryingNpcBody)
            {
                return false;
            }

            var npc = LogicManager.AreaManager.GetLogicEntiy(CarriedNpcEntityId) as NpcUnitLogicEntity;
            if (npc == null || npc.MarkDestroyed)
            {
                AbortCarryNpcBodyClearPlayerOnly();
                return false;
            }

            if (!MapWorldEmptySpotUtil.TryFindEmptySpotNear(
                    Pos + UnityEngine.Random.insideUnitCircle.normalized * 0.3f,
                    CarryPutDownSearchRadius,
                    CarryPutDownClearanceRadius,
                    CarriedNpcEntityId,
                    Id,
                    out var spot))
            {
                failedNoEmptySpot = true;
                return false;
            }

            LogicManager.globalBuffManager.RemoveAllBuffById(npc.Id, "give_hide");
            npc.TeleportTo(spot);
            CarriedNpcEntityId = 0;
            RemovePlayerCarryBodyBuffs();
            return true;
        }

        // NPC 已销毁等异常：仅清除玩家侧搬运状态（不尝试改 NPC）
        public void AbortCarryNpcBodyClearPlayerOnly()
        {
            if (!IsCarryingNpcBody)
            {
                return;
            }

            CarriedNpcEntityId = 0;
            RemovePlayerCarryBodyBuffs();
            EventOnCarryNpcBodyAborted?.Invoke();
        }

        private void RemovePlayerCarryBodyBuffs()
        {
            var gbm = LogicManager.globalBuffManager;
            gbm.RemoveAllBuffById(Id, "player_carry_slow");
            gbm.RemoveAllBuffById(Id, "player_carry_ov_idle");
            gbm.RemoveAllBuffById(Id, "player_carry_ov_move");
            gbm.RemoveAllBuffById(Id, "player_carry_ov_walk");
        }

        public long? gcCuaseId;
        public bool isSelfGc;
        public float? gcCauseParam = 0;


        public bool IsRetreating;
        public float RetreatingStartTime;
        public static float RetreatDuration = 5.0f;

        public event Action<long> EventOnAttachmentUpdate;
        public event Action EventOnRequestAimHelper;

        public event Action EventOnFaQingStateChange;
        public event Action<bool> EventOnExposeStateChange;

        private float _highFreqStateTimer;

        private float _magicClothesMoveWearDistanceAccum;
        private bool _magicClothesMoveWearSampleInit;

        private Vector2 _magicClothesLastWearSamplePos;

        public int DesireLevel { get; private set; }
        public int SanCorruptLevel { get; private set; }
        public PlayerLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {

        }

        public override EEntityType Type => EEntityType.Player;

        public override bool IsInCombat
        {
            get
            {
                return LogicManager != null && !LogicManager.GameSession.IsPeaceful;
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

            TickCarriedNpcBodyConsistency();

            TickPlayerStateLowFreq();

            TickPlayerStateHighFreq();

            TickAttachingObj(dt);

            TickRetreating();

            TickPlayerPinkMistTrail();
        }

        private void TickCarriedNpcBodyConsistency()
        {
            if (!IsCarryingNpcBody)
            {
                return;
            }

            var npc = LogicManager.AreaManager.GetLogicEntiy(CarriedNpcEntityId) as NpcUnitLogicEntity;
            if (npc == null || npc.MarkDestroyed)
            {
                AbortCarryNpcBodyClearPlayerOnly();
            }
        }


        public override void Initialize()
        {
            base.Initialize();

            DefaultControlledByVelocity = false;

            _hostileDamageBurstTracker?.Dispose();
            _hostileDamageBurstTracker = new PlayerHostileDamageBurstTracker(this, 5f, 5, 5_000);

            RefreshProgressionYCAttrs();

            // 与地图加载后的 PostNewAreaLoaded 使用同一套同步逻辑，避免 Initialize 后短时间状态不一致
            LogicManager.RefreshPlayerMagicClothesAndExposeForCurrentMode();

            _hostileDamageBurstTracker.EventOnQuickDamagedBurst += HandleQuickDamagedBurst;

            LogicManager.globalBuffManager.AddBuff(this.Id, "desire_level_charm", 0);
            LogicManager.globalBuffManager.AddBuff(this.Id, "desire_level_damage_resist", 0);

            _pinkMistTrailPosInited = false;
        }

        protected override void RegisterSpecAttrs()
        {
            attributeStore.RegisterNumeric(AttrIdConsts.HP_MAX, 250_000);
            attributeStore.RegisterResource(AttrIdConsts.HP, AttrIdConsts.HP_MAX, null, 250_000);

            attributeStore.RegisterNumeric(AttrIdConsts.PhysicalPower, 20_000);

            // 数值类
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerGcThreshold, initialBase: 100_000);

            attributeStore.RegisterNumeric(AttrIdConsts.Clothes_ExposeRate, initialBase: 10000);
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerCharm_Inner, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerCharm_Static, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerCharm_Scaled, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerCharm, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerSpellPower, initialBase: 0);

            attributeStore.RegisterNumeric(AttrIdConsts.Arm_Inner, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Arm_Base, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Arm_White, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Arm_White_Percent, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Arm_Extra_1, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Arm_Green, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.Arm_Final, initialBase: 0);

            attributeStore.RegisterNumeric(AttrIdConsts.PhysicalResist, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.PhysicalResistArmRate, initialBase: 0);

            attributeStore.RegisterNumeric(AttrIdConsts.Final_Fix_DR_All, LogicManager.playerDataManager.ProgressionSystem.GetFinalAttribute((int)EYCAttribute.FixDmgReduceFinal));
            

            attributeStore.RegisterNumeric(AttrIdConsts.Basic_HungerCost, initialBase: 10);
            attributeStore.RegisterNumeric(AttrIdConsts.Basic_PleasureAdd, initialBase: 0);

            attributeStore.RegisterNumeric(AttrIdConsts.PlayerZhaZhiMode, initialBase: 0);
            attributeStore.RegisterNumeric(AttrIdConsts.PlayerUnlockYuhuo, initialBase: 0);

            attributeStore.RegisterResource(AttrIdConsts.PlayerClothes, null, 100_000, 100_000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerSanity, null, 100_000, 100_000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerPleasure, null, 100_000, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerKnockDown, null, 100_000, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerHunger, null, 100_000, 100_000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerNaiLi, null, 100_000, 100_000);
            attributeStore.RegisterResource(AttrIdConsts.PlayerEstrusProgrss, null, 100_000, 0);

            attributeStore.RegisterResource(AttrIdConsts.PlayerOriginPower, null, 1000_000, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerJingYu, null, 1000_000, 0);

        }

        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            base.OnResourceAttriChanged(attrId, before, after, intent);

            // 4.3 死亡判断窗口：仅在含伤害时检查
            switch (attrId)
            {
                case AttrIdConsts.PlayerEstrusProgrss:
                    {
                        // 溢出部分转换为高潮条
                        if (before < after && after > 100_000)
                        {
                            long overflow = after - 100_000;
                            long toPleasure = (long)(overflow * 0.2);
                            attributeStore.ApplyResourceChange(AttrIdConsts.PlayerPleasure, toPleasure, intent.isEnmity, EDmgFlag.None, intent.srcEntityId);
                        }
                    }

                    break;

                case AttrIdConsts.PlayerPleasure:
                    {
                        if (IsPendingGc)
                        {
                            break;
                        }

                        HandlePlayerPleasureMiniGc(before, after);

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
                                gcCuaseId = intent.srcEntityId;

                                //gcCauseParam
                            }
                            break;
                        }
                    }
                    break;

                case AttrIdConsts.PlayerClothes:
                    _dirst_clothes = true;
                    break;

                case AttrIdConsts.PlayerKnockDown:
                    {
                        var knockDownMax = GetResourceMax(AttrIdConsts.PlayerKnockDown);
                        if (before < knockDownMax && after >= knockDownMax)
                        {
                            HandlePlayerKnockDownFull();
                        }
                    }
                    break;
            }
        }

        void HandlePlayerKnockDownFull()
        {
            if (!LogicManager.globalBuffManager.CheckHasBuff(Id, "player_knocked_down"))
            {
                LogicManager.globalBuffManager.RequestAddBuff(Id, "player_knocked_down");
            }

            ForceSetResource(AttrIdConsts.PlayerKnockDown, 0);
        }


        public float applyHValTimer;

        // 与 PlayerSkillList 对齐技能运行时；失去/替换技能时需调用以解绑旧被动 Buff
        public void ReconcileSkillsWithLearnedList(IReadOnlyDictionary<string, int> skills)
        {
            ablilityManager?.ReconcileRegisteredSkills(skills);
        }

        public void ReconcileSkillsWithLearnedList(IReadOnlyCollection<string> skillIds)
        {
            var dict = new Dictionary<string, int>(StringComparer.Ordinal);
            if (skillIds != null)
            {
                foreach (var id in skillIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        dict[id] = 1;
                    }
                }
            }

            ReconcileSkillsWithLearnedList(dict);
        }

        // 更新已注册技能的能力覆盖字典（仅本实体，不改表）
        public bool TryUpdateSkillAttachedAttributes(string skillId, IReadOnlyDictionary<string, string> updates)
        {
            return ablilityManager != null && ablilityManager.TryMergeSkillAbilityExtraVariables(skillId, updates);
        }

        public bool TrySetPassiveSkillBuffLayer(string skillId, int layer)
        {
            return ablilityManager != null && ablilityManager.TrySetPassiveSkillBuffLayer(skillId, layer);
        }

        // 仅本实体运行时替换技能 id（NPC 等无「已学列表」时用）；玩家优先走 PlayerSystemManager.TryReplaceSkill
        public bool TryReplaceRegisteredSkillOnEntity(string oldSkillId, string newSkillId)
        {
            return ablilityManager != null && ablilityManager.TryReplaceRegisteredSkill(oldSkillId, newSkillId);
        }

        protected override void InitAbility()
        {
            base.InitAbility();

            LogicManager.playerDataManager.SyncLearnedSkillsToPlayerEntity();

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

                    var surrounds = LogicManager.visionSenser.OverlapCircleAllEntity(
                        Pos,
                        abilityConf.AttractRange,
                        filterParam,
                        MapLogicPosition.ResolveAttackHitHeight(this));

                    foreach (var surround in surrounds)
                    {
                        var unit = surround as NpcUnitLogicEntity;
                        if (unit != null)
                        {
                            unit.OnReceiveStimulus(new StimulusEvent(this.Pos, 25, 100, EStimulusType.Evil_Ability, this.Id));
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

            //TickResourceChange(1.0f);

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

            // 刷新衣物提供属性
            if(_dirst_clothes)
            {
                // 刷新衣装产生的基础属性影响
                RefreshClothesRelateYCAttrs();
                _dirst_clothes = false;
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
            RefreshPlayerSanCorruptLevel();

            TickPlayerGcYishang();

            // 检查玩家衣着
            TickPlayerClothesBroken();

            TickMagicClothesMoveWear(0.2f);

            // 检查是否进入高潮
            TickGc();

            TickFaQing();
        }

        /// <summary>
        /// 检查玩家状态变化
        /// </summary>
        protected override void TickResourceChange(float interval)
        {
            var baseGc = attributeStore.GetAttr(AttrIdConsts.Basic_PleasureAdd);
            ApplyResourceChange(AttrIdConsts.PlayerPleasure, baseGc, false, EDmgFlag.None, null);

            var baseHungerCost = attributeStore.GetAttr(AttrIdConsts.Basic_HungerCost);
            ApplyResourceChange(AttrIdConsts.PlayerHunger, -baseHungerCost, false, EDmgFlag.None, null);

            var hunger = GetAttr(AttrIdConsts.PlayerHunger);
            if (hunger <= 0)
            {
                ApplyResourceChange(AttrIdConsts.HP, -500, false, EDmgFlag.Loss, null);
                LogicManager.viewer.ShowFakeFxEffect("饿", this.Pos);
            }
            else if(hunger >= 90000)
            {
                ApplyResourceChange(AttrIdConsts.HP, 100, false, EDmgFlag.None, null);
            }


            var jingyuVal = GetAttr(AttrIdConsts.PlayerJingYu);
            int jingyuLevel = PlayerGamePlayRule.GetJingYuLevel(jingyuVal);
            var jingyuCfg = CfgMgr.Cfgs.TbPlayerJingYuLevel.GetOrDefault(jingyuLevel);

            if (!IsFaQing)
            {
                // 将发情缓慢提升到标准线
                int basicEstrus = GetBasicEstrusByDesireLevel();
                long curEstrus = GetAttr(AttrIdConsts.PlayerEstrusProgrss);
                if (curEstrus < basicEstrus * 1000)
                {
                    ApplyResourceChange(AttrIdConsts.PlayerEstrusProgrss, 100, false, EDmgFlag.None, null);
                }

                if(jingyuCfg != null && jingyuCfg.EstrusUp > 0)
                {
                    ApplyResourceChange(AttrIdConsts.PlayerEstrusProgrss, (long)(jingyuCfg.EstrusUp * interval), false, EDmgFlag.None, null);
                }
            }
            else
            {
                // 每秒降低1点
                ApplyResourceChange(AttrIdConsts.PlayerEstrusProgrss, -1000, false, EDmgFlag.None, null);
            }

            TickBeingGazedInfo();


            if (jingyuLevel > 0)
            {
                if(jingyuCfg != null)
                {
                    if(jingyuCfg.CostPerSec > 0)
                    {
                        ApplyResourceChange(AttrIdConsts.PlayerJingYu, -(long)(jingyuCfg.CostPerSec * interval * 1000), false, EDmgFlag.None, null);
                    }

                    if(jingyuCfg.HpPerSec > 0)
                    {
                        ApplyResourceChange(AttrIdConsts.HP, (long)(jingyuCfg.HpPerSec * interval * 1000), false, EDmgFlag.None, null);
                    }

                    if(jingyuCfg.HungerPerSec > 0)
                    {
                        ApplyResourceChange(AttrIdConsts.PlayerHunger, (long)(jingyuCfg.HungerPerSec * interval * 1000), false, EDmgFlag.None, null);

                    }
                }

            }

            // 身上有jingyu时 需要累计发情进度
            if(jingyuLevel > 0)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void RefreshPlayerDesireLevel()
        {
            // 发情中暂时锁定欲望等级为最高
            // 等待自然褪去
            if(IsFaQing)
            {
                return;
            }

            int nowDesireLevel = 0;
            nowDesireLevel = 0;
            var cfgs = CfgMgr.Cfgs.TbPlayerDesireLevel.DataList;

            var estrusVal = GetAttr(AttrIdConsts.PlayerEstrusProgrss);

            for (int i = cfgs.Count - 1; i >= 0; i--)
            {
                int desireLine = cfgs[i].DesireLine;

                if (estrusVal >= desireLine * 1000)
                {
                    nowDesireLevel = cfgs[i].Level;
                    break;
                }
            }

            if(nowDesireLevel != DesireLevel)
            {
                var uLevelCfg = CfgMgr.Cfgs.TbPlayerDesireLevel.GetOrDefault(DesireLevel);

                {
                    int layer = 0;
                    if (uLevelCfg != null)
                    {
                        layer = (int)(uLevelCfg.ExtraCharm * 1000);
                    }

                    var buff = FindBuffById("desire_level_charm");

                    if (buff != null)
                    {
                        buff.SetBuffLayerDirect(layer);
                    }
                }


                {
                    int layer = 0;
                    if (uLevelCfg != null)
                    {
                        layer = uLevelCfg.ExtraDamageReduce;
                    }

                    var buff = FindBuffById("desire_level_damage_resist");

                    if (buff != null)
                    {
                        buff.SetBuffLayerDirect(layer);
                    }
                }

                this.DesireLevel = nowDesireLevel;
            }
        }

        private void RefreshPlayerSanCorruptLevel()
        {
            SanCorruptLevel = 0;
            var cfgs = CfgMgr.Cfgs.TbPlayerSanCorruptLevel.DataList;

            var sanVal = GetAttr(AttrIdConsts.PlayerSanity);

            for (int i = 0; i < cfgs.Count; i++)
            {
                int sanLine = cfgs[i].SanLine;
                if (sanVal >= sanLine * 1000)
                {
                    SanCorruptLevel = cfgs[i].Level;
                    return;
                }
            }

            SanCorruptLevel = cfgs[cfgs.Count - 1].Level;
        }

        /// <summary>
        /// 获取快感基准
        /// </summary>
        /// <returns></returns>
        private int GetBasicEstrusByDesireLevel()
        {
            var desireCfg = CfgMgr.Cfgs.TbPlayerSanCorruptLevel.GetOrDefault(SanCorruptLevel);
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
        private void TickPlayerClothesBroken()
        {
            // 人类形态不维护暴露 / 伪装衣装逻辑
            if (LogicManager.PlayerHumanMode)
            {
                return;
            }

            // 对于非伪装地图 不检查
            // todo 是否支持强行伪装

            do
            {
                if (MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapOverlayCfg != null && MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapOverlayCfg.IsCivilArea)
                {
                    DisguiseIfPossible = true;
                    break;
                }

                DisguiseIfPossible = false;

            }
            while (false);

            var clothes = GetAttr(AttrIdConsts.PlayerClothes);

            // 检查不需要伪装的状态
            if (!DisguiseIfPossible)
            {
                if(!IsExposed)
                {
                    EnterExposeState(clothes > 0 ? false : true);
                }
                return;
            }

            if(!IsExposed)
            {

                if (clothes <= 0)
                {
                    EnterExposeState(true);
                }
            }
        }


        const string PinkMistTrailCfgId = "player_pink_mist_trail";
        const float PinkMistTrailMinSpacing = 0.45f;
        const float PinkMistTrailTeleportResetSqr = 25f;

        // 移动超过间距时在脚下铺设粉雾地面格
        void TickPlayerPinkMistTrail()
        {
            if (MarkDestroyed || !IsActive)
            {
                return;
            }

            var table = CfgMgr.Cfgs?.TbMapAreaEffect;
            if (table == null)
            {
                return;
            }

            if (DesireLevel == 0)
            {
                return;
            }

            var row = table.GetOrDefault(PinkMistTrailCfgId);
            if (row == null)
            {
                return;
            }

            if (!_pinkMistTrailPosInited)
            {
                _pinkMistLastTrailPos = Pos;
                _pinkMistTrailPosInited = true;
                return;
            }

            var delta = Pos - _pinkMistLastTrailPos;
            var sq = delta.sqrMagnitude;
            if (sq < PinkMistTrailMinSpacing * PinkMistTrailMinSpacing)
            {
                return;
            }

            // 大位移视为传送/切场景，不连成线段
            if (sq > PinkMistTrailTeleportResetSqr)
            {
                _pinkMistLastTrailPos = Pos;
                return;
            }

            _pinkMistLastTrailPos = Pos;

            float life = row.DefaultLifetime > 0f ? row.DefaultLifetime : 8f;
            float radius = row.ShapeRadius > 0f ? row.ShapeRadius : 0.9f;
            LogicManager.GroundMistManager.AddElementCircle(Pos, radius, EGroundMistType.PinkMist, life);
        }

        void HandlePlayerPleasureMiniGc(long before, long after)
        {
            UpdateMiniGcRearm(after);

            if (after <= before)
            {
                return;
            }

            if (_miniGcLowArmed
                && PlayerGamePlayRule.CrossedMiniGcThreshold(before, after, PlayerGamePlayRule.MiniGcThresholdLow))
            {
                _miniGcLowArmed = false;
                TriggerMiniGc();
            }

            if (_miniGcHighArmed
                && PlayerGamePlayRule.CrossedMiniGcThreshold(before, after, PlayerGamePlayRule.MiniGcThresholdHigh))
            {
                _miniGcHighArmed = false;
                TriggerMiniGc();
            }
        }

        void UpdateMiniGcRearm(long pleasure)
        {
            if (!_miniGcLowArmed
                && PlayerGamePlayRule.ShouldRearmMiniGcThreshold(pleasure, PlayerGamePlayRule.MiniGcThresholdLow))
            {
                _miniGcLowArmed = true;
            }

            if (!_miniGcHighArmed
                && PlayerGamePlayRule.ShouldRearmMiniGcThreshold(pleasure, PlayerGamePlayRule.MiniGcThresholdHigh))
            {
                _miniGcHighArmed = true;
            }
        }

        void TriggerMiniGc()
        {
            TryInterrupt(new InterruptRequest
            {
                source = EInterruptSource.Stun,
                priority = 80,
            });

            LogicManager.globalBuffManager.RequestAddBuff(
                Id,
                "force_stun",
                overrideDuration: PlayerGamePlayRule.MiniGcStunDuration);
            LogicManager.globalBuffManager.RequestAddBuff(
                Id,
                "player_mini_gc_debuff",
                overrideDuration: PlayerGamePlayRule.MiniGcSlowDuration);

            LogicManager.GroundLiquidManager.AddElementCircle(
                Pos,
                PlayerGamePlayRule.MiniGcLiquidRadius,
                EGroundLiquidType.GcLiquid,
                PlayerGamePlayRule.MiniGcLiquidDuration);
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

            //var gcLiquidEntity = new LogicEntityRecord();
            //gcLiquidEntity.Id = GameLogicManager.LogicEntityIdInst++;
            //gcLiquidEntity.EntityType = EEntityType.AreaEffect;
            //gcLiquidEntity.CfgId = "ground_gc_liquid";
            //gcLiquidEntity.LifeTime = 20.0f;
            //gcLiquidEntity.Position = this.Pos;
            //gcLiquidEntity.FactionId = this.FactionId;
            LogicManager.GroundLiquidManager.AddElementCircle(this.Pos, 1.0f, EGroundLiquidType.GcLiquid, 20f);

            //LogicManager.AddNewEntityRecord(gcLiquidEntity);

            // 添加自身debuff
            LogicManager.globalBuffManager.RequestAddBuff(this.Id, "gc_self_debuff");

            LogicManager.globalBuffManager.RequestAddBuff(this.Id, "gc_self_yishang", layer: 100);

            if(!isSelfGc)
            {
                ApplyResourceChange(AttrIdConsts.PlayerSanity, -10_000, false, FightStruct.EDmgFlag.None, this.Id);
            }
            
            ForceSetResource(AttrIdConsts.PlayerPleasure, 0);
            _miniGcLowArmed = true;
            _miniGcHighArmed = true;

            // 尝试结束发情
            if(IsFaQing)
            {
                //var randVal = UnityEngine.Random.Range(0, 10000);
                LogicManager.globalBuffManager.RemoveAllBuffById(Id, "player_faqing");
                IsFaQing = false;
                Debug.Log("player leave faqing");

                EventOnFaQingStateChange?.Invoke();
            }

            LogicManager.viewer.ShowGcCloseupWindow("gc", 1.0f);
        }

        /// <summary>
        /// 检查是否进入发情状态
        /// </summary>
        private void TickFaQing()
        {
            
            if (IsFaQing)
            {
                bool canLeave = false;
                long curEstrus = GetAttr(AttrIdConsts.PlayerEstrusProgrss);
                do
                {
                    // 下降到冷静线就可以推出了
                    if(curEstrus < 20_000)
                    {
                        canLeave = true;
                        break;
                    }
                }
                while (false);

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
                //case AttrIdConsts.PlayerEstrusProgrss:
                //    {

                //    }
                //    break;
                case AttrIdConsts.PlayerPleasure:
                    {
                        if(delta > 0)
                        {
                            var sensi = this.GetAttr(AttrIdConsts.PlayerSensitivity);
                            long sensitiveBonus = PlayerGamePlayRule.CalculateSensitiveBonus(sensi);
                            delta = (long)(delta * (10000 + sensitiveBonus) * 0.0001);
                        }
                        return delta;
                    }
                    break;

                default:
                    {
                        return base.CalculateResourceCostAmount(attrId, intent);
                    }
            }
        }

        

        /// <summary>
        /// 检查向周围传播hval
        /// </summary>
        protected void TickApplyAuraHVal()
        {
            // 人类形态不触发
            if(LogicManager.PlayerHumanMode)
            {
                return;
            }

            if(DesireLevel == 0)
            {
                return;
            }

            var desireCfg = CfgMgr.Cfgs.TbPlayerDesireLevel.Get(DesireLevel);
            float auraRange = desireCfg.AuraMaxRange;
            // 
            var candidates = LogicManager.visionSenser.OverlapCircleAllEntity(
                Pos,
                3.0f,
                new EntityFilterParam()
                {
                    FilterParamLists = new() { EEntityType.Npc },
                    CampFilterType = ECampFilterType.NotSelf,
                    SelfCampId = EFactionId.Player,
                },
                MapLogicPosition.ResolveAttackHitHeight(this));

            foreach (var candidate in candidates)
            {
                if(candidate is not BaseUnitLogicEntity unit)
                {
                    continue;
                }

                var willProtect = PlayerGamePlayRule.CalculateUnitWIillProtectParam(this.GetUnitLevel(), unit.GetUnitLevel(), this.GetFinalCharm(), unit.GetAttr(AttrIdConsts.Will));

                var dist = (candidate.Pos - this.Pos).magnitude;
                var addPerSec = PlayerGamePlayRule.CalculatePlayerDesireAuraEffect(DesireLevel, dist, willProtect);

                if(addPerSec <= 0)
                {
                    continue;
                }
                var effect = new MapAbilityEffectAddResourceCfg()
                {
                    ResourceId = AttrIdConsts.NPCHVal,
                    AddValue = addPerSec,
                    IsEnmity = true,
                };
                Debug.Log($"TickApplyAuraHVal apply to {unit.Id} val:{addPerSec}");
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

        public class PlayerGazedRec
        {
            public long SrcEntityId;
            public float LastTriggerTime;
            public int Power;
        }

        private Dictionary<long, PlayerGazedRec> BeingGazedTrack = new();

        /// <summary>
        /// tick 被注视效果
        /// </summary>
        private void TickBeingGazedInfo()
        {

            if(LogicManager.PlayerHumanMode)
            {
                return;
            }

            
            //int exposeLevel = PlayerGamePlayRule.GetClothesRawOverRate10000ForGameplay(LogicManager);
            int exposeLevel = PlayerGamePlayRule.CalculateClothesExposeLevel(LogicManager);

            foreach (var key in BeingGazedTrack.Keys.ToList())
            {
                if (BeingGazedTrack[key].LastTriggerTime + 3f < LogicTime.time)
                {
                    BeingGazedTrack.Remove(key);
                    continue;
                }

                var gazingEntity = LogicManager.GetLogicEntity(key);

                if(gazingEntity == null || gazingEntity.MarkDestroyed)
                {
                    BeingGazedTrack.Remove(key);
                    continue;
                }

                if(gazingEntity is not BaseUnitLogicEntity unit)
                {
                    BeingGazedTrack.Remove(key);
                    continue;
                }

                if(unit.IsDead || unit.IsAttaching || unit.MarkUnsensored)
                {
                    BeingGazedTrack.Remove(key);
                    continue;
                }

                // 更新视线强度
                int attractLevel = PlayerGamePlayRule.CalculateUnitAttractedLevel(LogicManager, unit.GetAttr(AttrIdConsts.Will));
                if(attractLevel > 0)
                {
                    BeingGazedTrack[key].Power = 1;
                }
                else
                {
                    BeingGazedTrack[key].Power = 0;
                }

                // 只要吸引力等级超过0 就执行attract
                if(attractLevel > 0)
                {
                    if (unit is NpcUnitLogicEntity npcEntity && npcEntity.NpcConfig.IgnoreAttractLevel < attractLevel)
                    {
                        npcEntity.OnReceiveStimulus(new StimulusEvent(this.Pos, 99, 100, EStimulusType.Player_Attract, this.Id));
                        //npcEntity.ApplyAttracted(ENpcAttractSrcType.Player, 99, this.Pos, this.Id);
                    }
                }
            }

            int totalGazePower = 0;
            foreach(var gaze in BeingGazedTrack.Values)
            {
                totalGazePower += gaze.Power;
            }

            if(IsInBusyZone)
            {
                totalGazePower += 5;
            }

            if(totalGazePower > 0)
            {
                long addRate = PlayerGamePlayRule.GetPleasuAddByGazePower(this.GetUnitLevel(), totalGazePower);

                var exposeCfg = CfgMgr.Cfgs.TbPlayerClothesExposeInfo.GetOrDefault(exposeLevel);
                addRate = (long)(addRate * (1 - exposeCfg.GazeResist));
                if (addRate > 0)
                {
                    Debug.Log($"remove sanity by gaze {addRate}");
                    ApplyResourceChange(AttrIdConsts.PlayerSanity, -addRate, false, EDmgFlag.None, null);
                }
            }

            
        }


        public override void OnGazeEnter(long srcId)
        {
            if(!BeingGazedTrack.TryGetValue(srcId, out var info))
            {
                info = new() { SrcEntityId = srcId };
                BeingGazedTrack[srcId] = info;
            }

            BeingGazedTrack[srcId].LastTriggerTime = LogicTime.time;
        }

        public override void OnGazeLeave(long srcId)
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
            AggroSystem = new UnitAggroSystem(this, EAggroMode.Player);
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
                if (AggroSystem != null && AggroSystem.HasHostile)
                {
                    return AggroSystem.CurrentTargetId;
                }

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
            if (IsZhaZhiMode)
            {
                LogicManager.globalBuffManager.RemoveAllBuffById(Id, "player_zhazhi");
            }
            else
            {
                LogicManager.globalBuffManager.AddBuff(Id, "player_zhazhi");
            }
        }

        public override string  GetAnimOverride(string rawAnimName)
        {
            var changedAnim = base.GetAnimOverride(rawAnimName);
            if(changedAnim != rawAnimName)
            {
                return changedAnim;
            }

            if(IsSpecialCrouchStance)
            {
                if(rawAnimName == "move")
                {
                    return "move_c";
                }
                if (rawAnimName == "idle")
                {
                    return "idle_c";
                }
            }

            return rawAnimName;
        }

        public override bool CanActiveUseSkill()
        {
            if(!base.CanActiveUseSkill())
            {
                return false;
            }

            if(IsCarryingNpcBody)
            {
                return false;
            }

            return true;
        }


        // 魔力衣装：由 PlayerMagicClothesManager 调整 PlayerClothes 固定上限并刷新关联属性
        public void ApplyMagicClothesRuntime(long maxClothes)
        {
            if (LogicManager.PlayerHumanMode)
            {
                return;
            }

            attributeStore.SetResourceFixMax(AttrIdConsts.PlayerClothes, maxClothes);
            RefreshClothesRelateYCAttrs();
        }

        // 人类形态：解除暴露态并重置衣装固定上限为默认（魔力衣装数值不生效）
        public void ApplyHumanModeShieldingState()
        {
            const long defaultClothesMax = 100_000;

            if (IsExposed)
            {
                long cur = GetAttr(AttrIdConsts.PlayerClothes);
                ExitExposeState(cur);
            }

            attributeStore.SetResourceFixMax(AttrIdConsts.PlayerClothes, defaultClothesMax);
            RefreshClothesRelateYCAttrs();
        }

        

        private void TickMagicClothesMoveWear(float interval)
        {
            if (LogicManager.PlayerHumanMode)
            {
                return;
            }

            var mgr = LogicManager.playerDataManager.MagicClothes;
            if (!mgr.ShouldApplyMoveWear(this))
            {
                _magicClothesMoveWearSampleInit = false;
                _magicClothesMoveWearDistanceAccum = 0f;
                return;
            }

            var def = mgr.GetActiveDef();
            if (def == null || def.MoveWearDistancePerCheck <= 0.01f)
            {
                return;
            }

            if (!_magicClothesMoveWearSampleInit)
            {
                _magicClothesLastWearSamplePos = Pos;
                _magicClothesMoveWearSampleInit = true;
                return;
            }

            // 仅用实际位移累加：有按键但顶墙不动时 Pos 不变，不会增加磨损距离（与 ShouldApplyMoveWear 的输入条件配合）
            float moved = Vector2.Distance(Pos, _magicClothesLastWearSamplePos);
            const float moveWearDisplacementEpsilon = 0.002f;
            if (moved < moveWearDisplacementEpsilon)
            {
                moved = 0f;
            }

            float maxStep = GetCurrSpeed() * interval * 2f;
            if (moved > maxStep)
            {
                moved = maxStep;
            }

            _magicClothesLastWearSamplePos = Pos;
            _magicClothesMoveWearDistanceAccum += moved;

            bool lossed = false;
            while (_magicClothesMoveWearDistanceAccum >= def.MoveWearDistancePerCheck)
            {
                _magicClothesMoveWearDistanceAccum -= def.MoveWearDistancePerCheck;
                int chance = mgr.GetMoveWearEffectiveChancePermille(def);
                if (chance <= 0)
                {
                    continue;
                }

                int roll = UnityEngine.Random.Range(0, 1000);
                if (roll >= chance)
                {
                    continue;
                }

                long loss = mgr.ComputeMoveWearLoss(def);
                if (loss > 0)
                {
                    ApplyResourceChange(AttrIdConsts.PlayerClothes, -loss, false, EDmgFlag.None, null);
                    lossed = true;
                }
            }

            if(lossed)
            {
                LogicManager.viewer.ShowFakeFxEffect("撕裂", Pos);
            }

        }

        /// <summary>
        /// 从养成系统同步魅力/护甲叶子属性（与衣装覆盖率无关）
        /// </summary>
        public void RefreshProgressionYCAttrs()
        {
            var progression = LogicManager.playerDataManager.ProgressionSystem;
            attributeStore.RefreshAttrBaseNum(AttrIdConsts.PlayerCharm_Inner, progression.GetFinalAttribute((int)EYCAttribute.InnerCharm));
            attributeStore.RefreshAttrBaseNum(AttrIdConsts.PlayerCharm_Static, progression.GetFinalAttribute((int)EYCAttribute.StaticCharm));
            attributeStore.RefreshAttrBaseNum(AttrIdConsts.Arm_Inner, progression.GetFinalAttribute((int)EYCAttribute.InnerArm));
            attributeStore.RefreshAttrBaseNum(AttrIdConsts.Arm_Base, progression.GetFinalAttribute((int)EYCAttribute.StaticArm));
            attributeStore.Commit();
        }

        /// <summary>
        /// 根据当前衣装状态刷新衣装覆盖率及相关表现（属性图仅更新 Clothes_ExposeRate）
        /// </summary>
        public void RefreshClothesRelateYCAttrs()
        {
            long clothes = GetAttr(AttrIdConsts.PlayerClothes);
            int applyRate = 10000;
            if (!LogicManager.PlayerHumanMode && !IsExposed)
            {
                long rawOverRate = PlayerGamePlayRule.GetClothesRawOverRate10000ForGameplay(LogicManager);
                //int exposeLevel = PlayerGamePlayRule.CalculateClothesExposeLevel(LogicManager);
                applyRate = PlayerGamePlayRule.CalculateBreakClothesInnerRate(clothes, rawOverRate);
            }

            var clothesCharmBuff = FindBuffById("player_expose_charm");
            int buffLayer = (applyRate - 1) / 10_000 + 1;
            if (clothesCharmBuff == null)
            {
                LogicManager.globalBuffManager.AddBuff(this.Id, "player_expose_charm", buffLayer);
            }
            else
            {
                clothesCharmBuff.SetBuffLayerDirect(buffLayer);
            }

            attributeStore.RefreshAttrBaseNum(AttrIdConsts.Clothes_ExposeRate, applyRate, forceDirty: true);
            attributeStore.Commit();
        }

        // 技能蓄力主动进入暴露态（Z 键）；仅在可伪装区域且尚未暴露时生效
        public bool TryEnterExposeFromSkill()
        {
            if (LogicManager.PlayerHumanMode)
            {
                return false;
            }

            if (IsExposed)
            {
                return false;
            }

            if (!DisguiseIfPossible)
            {
                return false;
            }

            var clothes = GetAttr(AttrIdConsts.PlayerClothes);
            EnterExposeState(clothes > 0 ? false : true);
            return true;
        }

        // 技能引导完成时退出暴露态（Z 键 / fix_clothes）
        public bool TryExitExposeFromSkill(long restoreValue)
        {
            if (LogicManager.PlayerHumanMode)
            {
                return false;
            }

            if (!IsExposed)
            {
                return false;
            }

            if (!DisguiseIfPossible)
            {
                return false;
            }

            ExitExposeState(restoreValue);
            return true;
        }

        private void EnterExposeState(bool isBroken)
        {
            IsExposed = true;
            LogicManager.globalBuffManager.AddBuff(this.Id, "player_clothes_expose"); // todo 这里
            EventOnExposeStateChange?.Invoke(isBroken);
        }

        public void ExitExposeState(long clothesValue)
        {
            IsExposed = false;
            LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "player_clothes_expose"); // todo 这里

            attributeStore.SetResource(AttrIdConsts.PlayerClothes, clothesValue);
            EventOnExposeStateChange?.Invoke(false);
        }

        protected override void OnDamageBeforeFinalReduce(long dmg, ResourceDeltaIntent intent)
        {
            base.OnDamageBeforeFinalReduce(dmg, intent);

            if(!intent.deltaFlags.HasFlag(EDmgFlag.Loss))
            {
                // 获取原始h系数
                var hParam = intent.extraAttrs?.GetValueOrDefault(AttrIdConsts.HImpulse_Pipeline) ?? 0;

                // 根据伤害计算h冲击力
                long hImpulse = DamagePipeline.CalculateDmgBonusedHImpulse(hParam, dmg, GetUnitLevel());

                Debug.Log("OnDamageBeforeFinalReduce dmg impulse h " + hImpulse + " dmg " + dmg);

                ApplyHImpulseDirectly(hImpulse, intent);
            }
        }

        /// <summary>
        /// 直接应用一个h冲击
        /// </summary>
        /// <param name="hImpulse"></param>
        /// <param name="intent"></param>
        public void ApplyHImpulseDirectly(long hImpulse, ResourceDeltaIntent intent = null)
        {
            // 根据h冲击力分配高潮与发情
            (var climax, var estrus) = DamagePipeline.DistributeClimaxAndEstrusFromHImpulse(hImpulse, new LiveEntityFightAttrProvider(this));
            Debug.Log("OnDamageBeforeFinalReduce impulse h " + hImpulse + " " + climax + " " + estrus);

            // 叠加高潮条（快乐条
            if (climax > 0)
            {
                attributeStore.ApplyResourceChange(AttrIdConsts.PlayerPleasure, climax, intent?.isEnmity ?? false, EDmgFlag.None, intent?.srcEntityId ?? 0);
            }

            if (estrus > 0)
            {
                attributeStore.ApplyResourceChange(AttrIdConsts.PlayerEstrusProgrss, estrus, intent?.isEnmity ?? false, EDmgFlag.None, intent?.srcEntityId ?? 0);
            }
        }

        protected override long CalculateUnitHpChange(string attrId, ResourceDeltaIntent intent)
        {
            long delta = base.CalculateUnitHpChange(attrId, intent);

            if(delta < 0)
            {
                var maxHp = GetAttr(AttrIdConsts.HP_MAX);
                if(Math.Abs(delta) > maxHp * 0.15f)
                {
                    LogicManager.viewer.ShowMapSpeachBubble(this.Id, "哎呦", 1.0f);

                    LogicManager.viewer.ShowSceneFxEffect("h_voice_vfx", Pos, Vector2.right);

                    foreach (var b in BuffContainer.Values)
                    {
                        b.DoBuffTrigger(ETriggerType.PlayerHVoice);
                    }
                }
            }
            return delta;
        }

        public override int GetUnitLevel()
        {
            return LogicManager.playerDataManager.Level;
        }

        public override void InitVisionSystem()
        {

        }

        public bool IsInBusyZone = false;


        /// <summary>
        /// 直接吸收
        /// </summary>
        /// <param name="absorbVal"></param>
        public void OnAbsorbBlurtDirectly(float absorbVal)
        {
            long hungerBaseRate = 2000;
            var hungerVal = (long)(absorbVal * (hungerBaseRate * 0.0001) * 10000);
            Debug.Log("直接吸取 hunger " + hungerVal);

            if(hungerVal > 0)
            {
                ApplyResourceChange(AttrIdConsts.PlayerHunger, +hungerVal, false, EDmgFlag.None, null);
                ApplyResourceChange(AttrIdConsts.HP, +hungerVal, false, EDmgFlag.None, null);
            }

            float toJingYuanRate = 0.8f; // 默认情况下 直接榨取80%吸收为精元
            toJingYuanRate -= GetAttr(AttrIdConsts.PlayerJingYuRate) * 0.0001f;

            if (toJingYuanRate < 0f) toJingYuanRate = 0f;
            // 计算减成
            if (toJingYuanRate > 1.0f) toJingYuanRate = 1.0f;

            var toJingYuan = toJingYuanRate * absorbVal;
            var toJingYu = absorbVal - toJingYuan;

            if((int)(toJingYuan * 1000 / 1000) > 0)
            {
                LogicManager.playerDataManager.InventorySystem.GiveItemToPlayer("jingyuan", (int)(toJingYuan * 1000 / 1000));
            }

            if(toJingYu * 1000 > 0)
            {
                ApplyResourceChange(AttrIdConsts.PlayerJingYu, (long)(toJingYu * 1000), false, EDmgFlag.None, null);
            }
        }


        private float _lastTriggerHVoiceBlurtTime = 0;
        private const float TriggerHVoiceInterval = 2.0f;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tracker"></param>
        private void HandleQuickDamagedBurst()
        {
            if(_lastTriggerHVoiceBlurtTime != 0 && LogicTime.time - _lastTriggerHVoiceBlurtTime < TriggerHVoiceInterval)
            {
                return;
            }

            _lastTriggerHVoiceBlurtTime = LogicTime.time;

            LogicManager.viewer.ShowMapSpeachBubble(this.Id, "嗯哼", 1.0f);

            LogicManager.viewer.ShowSceneFxEffect("h_voice_vfx", Pos, Vector2.right);

            foreach (var b in BuffContainer.Values)
            {
                b.DoBuffTrigger(ETriggerType.PlayerHVoice);
            }
        }
    }

    // 滑动窗口内敌意 HP 损失次数；单次损失不低于 minDamagePerHit 才计数；达标抛事件并清空窗口
    public sealed class PlayerHostileDamageBurstTracker : IDisposable
    {
        readonly PlayerLogicEntity _player;
        readonly Queue<float> _hitLogicTimes = new();
        readonly float _windowSeconds;
        readonly int _hitCountThreshold;
        readonly long _minDamagePerHit;

        bool _disposed;

        public PlayerHostileDamageBurstTracker(PlayerLogicEntity player, float windowSeconds, int hitCountThreshold, long minDamagePerHit)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _windowSeconds = Math.Max(0.05f, windowSeconds);
            _hitCountThreshold = Math.Max(1, hitCountThreshold);
            _minDamagePerHit = Math.Max(0L, minDamagePerHit);

            _player.EventOnHpChanged += OnHpChanged;
            _player.EventOnDestroyed += OnPlayerDestroyed;
        }

        // 滑动窗口内敌意受伤计数达标并清空窗口后触发
        public event Action EventOnQuickDamagedBurst;

        public int DebugQueuedHitCount => _hitLogicTimes.Count;

        void OnPlayerDestroyed(long _)
        {
            Dispose();
        }

        void OnHpChanged(long entityId, long? srcEntityId, long finalDelta)
        {
            if (_disposed || entityId != _player.Id)
            {
                return;
            }

            // UnitOnHpChanged 仅在敌意意图下触发；finalDelta 受伤为负
            if (finalDelta >= 0)
            {
                return;
            }

            long loss = Math.Abs(finalDelta);
            if (loss < _minDamagePerHit)
            {
                return;
            }

            float now = LogicTime.time;
            PruneOlderThan(now - _windowSeconds);

            _hitLogicTimes.Enqueue(now);

            PruneOlderThan(now - _windowSeconds);

            if (_hitLogicTimes.Count >= _hitCountThreshold)
            {
                EventOnQuickDamagedBurst?.Invoke();
                _hitLogicTimes.Clear();
            }
        }

        void PruneOlderThan(float cutoff)
        {
            while (_hitLogicTimes.Count > 0 && _hitLogicTimes.Peek() < cutoff)
            {
                _hitLogicTimes.Dequeue();
            }
        }

        public void Dispose()
        {
            if (_disposed || _player == null)
            {
                return;
            }

            _disposed = true;
            _player.EventOnHpChanged -= OnHpChanged;
            _player.EventOnDestroyed -= OnPlayerDestroyed;
            _hitLogicTimes.Clear();
        }

    }
}





