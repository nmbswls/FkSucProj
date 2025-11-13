using Map.Entity;
using Map.Logic;
using Map.Logic.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static GameLogicManager;



namespace My.Map.Entity
{
    public enum TriggerType
    {
        Tick,
        OnSkillUsed,
    }

    public class BuffTriggerRuleConfig
    {
        public TriggerType TriggerType;
        public int TriggerParam1;
        public int TriggerParam2;
        public int TriggerParam3;

        public List<MapFightEffectCfg> OutputFightEffects;
    }

    public enum EBuffEffectType
    {
        None,
        CostResource,
        AddBuff,
        RemoveBuff,

        ShowFx,
    }
    //public class BuffEffectCfg
    //{
    //    public EBuffEffectType EffectType;
    //    public int Param0;
    //    public int Param1;
    //    public int Param2;
    //    public int Param3;

    //    public List<AttrKvPair> ExtraAttrs;
    //}

    public enum EBuffLayerOverrideType
    {
        NoOp = 0,
        Replace = 1,
        AddLayer = 2,
        Duplicate = 3,
    }

    public enum EBuffTurnOverrideType
    {
        NoOp = 0,
        Replace = 1,
        MaxTurn = 2,
        AddTurn = 3,
    }


    public static class BuffLibrary
    {
        public static Dictionary<string, BuffDefinition> _library;
        public static BuffDefinition GetBuffDefinition(string buffId)
        {
            if(_library == null)
            {
                _library = new();

                _library["lock_move"] = new BuffDefinition()
                {
                    BuffId = "lock_move",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 } },
                    DefaultDuration = -1,
                };
                _library["lock_face"] = new BuffDefinition()
                {
                    BuffId = "lock_face",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 } },
                    DefaultDuration = -1,
                };

                _library["beizha"] = new BuffDefinition()
                {
                    BuffId = "beizha",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    ModifierAttrs = new() { 
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidOp, ModifierValue = 1 }
                    },
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.AnimOverride,
                        ParamStr = "test",
                    },
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = TriggerType.Tick,
                            TriggerParam1 = 200, // 每0.2秒一次
                            //OutputEffects = new()
                            //{
                            //    new BuffEffectCfg()
                            //    {
                            //        EffectType = EBuffEffectType.ShowFx,
                            //    },
                            //},
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectCostResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.HP,
                                    CostValue = 5,
                                    Flags = 1,
                                    ExtraAttrInfos = new List<AttrKvPair>(){new(){ AttrId  = AttrIdConsts.DamageXiXue, Val = 2000} }
                                }
                            }
                        }
                    },
                    DefaultDuration = -1,
                };

                _library["give_hide_aura"] = new BuffDefinition()
                {
                    BuffId = "give_hide_aura",
                    DefaultDuration = -1,
                    AuraRange = 1.0f,
                    IsAura = true,
                    AuraBuffId = "give_hide",
                };

                _library["give_hide"] = new BuffDefinition()
                {
                    BuffId = "give_hide",
                    DefaultDuration = -1,
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,

                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.HidingMask, ModifierValue = 1 } ,
                    },
                };

                _library["hide_marked"] = new BuffDefinition()
                {
                    BuffId = "hide_marked",
                    DefaultDuration = -1,
                    //ModifierAttrs = new() {
                    //    new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.HidingMask, ModifierValue = 1 } ,
                    //},
                };

                _library["unsensored"] = new BuffDefinition()
                {
                    BuffId = "unsensored",
                    DefaultDuration = -1,
                };

                _library["be_fcked"] = new BuffDefinition()
                {
                    BuffId = "be_fcked",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                    },
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.AnimOverride,
                        ParamStr = "test",
                    },
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = TriggerType.Tick,
                            TriggerParam1 = 200, // 每0.2秒一次
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.PlayerKnockDown,
                                    AddValue = 2,
                                    Flags = 1,
                                }
                            }
                        }
                    },
                    DefaultDuration = -1,
                };

                _library["jian_su_self"] = new BuffDefinition()
                {
                    BuffId = "jian_su_self",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.JianSu, ModifierValue = 8000 } },
                    DefaultDuration = -1,
                };

                _library["throwing"] = new BuffDefinition()
                {
                    BuffId = "throwing",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() 
                    { 
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidOp, ModifierValue = 1 }
                    },
                    DefaultDuration = -1,
                };

            }

            _library.TryGetValue(buffId, out BuffDefinition def);
            return def;
        }
    }

    public enum EBuffDurationType
    {
        Invalid,
        AnimOverride,
    }
    public class BuffDurationEffet
    {
        public EBuffDurationType DurationType;

        public string ParamStr;
    }


    [Serializable]
    public class BuffDefinition
    {
        public string BuffId;

        public EBuffLayerOverrideType LayerOverrideType;
        public int MaxStackLayer;

        public EBuffTurnOverrideType TurnOverrideType;

        public float DefaultDuration;

        public bool IsAura;
        public float AuraRange;
        public string AuraBuffId;   

        [Serializable]
        public class OneModPair
        {
            public string ModifierAttrId;
            public long ModifierValue;
        }

        public List<OneModPair> ModifierAttrs = new();

        public List<MapFightEffectCfg> OnAttachEffects = null;
        public List<MapFightEffectCfg> OnDetachEffects = null;

        public BuffDurationEffet DurationEffect;

        public List<BuffTriggerRuleConfig> TriggerList = new();
    }


    public static class BuffTriggerCheckSystem
    {
        public static bool Matches(IMapLogicEvent evt, BuffInstance buffInst, BuffTriggerRuleConfig rule)
        {
            //if (evt.Type != rule.TriggerType) return false;
            // 根据 _param 解析条件，例如:
            // tag=Vulnerable;source=Player;value>=5
            //return ParamMatch(evt);
            return true;
        }

        /// <summary>
        /// 检查触发参数是否一致
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        //private static bool ParamMatch(BuffTriggerEvent evt)
        //{
        //    return false;
        //}
    }


    // 简易可队列化事件总线
    public class BuffEventBus
    {
        private readonly Queue<IMapLogicEvent> _queue = new Queue<IMapLogicEvent>(64);

        public void Enqueue(IMapLogicEvent ev) => _queue.Enqueue(ev);

        public void EnqueueRange(IEnumerable<IMapLogicEvent> events)
        {
            foreach (var e in events) _queue.Enqueue(e);
        }

        // 将队列内容一次性倒出（供 BuffManager 当前帧消费）
        public List<IMapLogicEvent> Drain()
        {
            var list = new List<IMapLogicEvent>(_queue.Count);
            while (_queue.Count > 0)
                list.Add(_queue.Dequeue());
            return list;
        }
    }



    public class GlobalBuffManager
    {
        public GameLogicManager logicManager;
        public int MaxTriggerDepthPerFrame = 6;

        public static long BuffInstIdCounter = 1000;

        public BuffEventBus BuffEventBus = new();

        private Dictionary<long, BuffInstance> _buffs = new();

        private readonly List<IMapLogicEvent> _frameEvents = new();


        // 请求队列（避免评估阶段直接改表）
        private readonly List<(long target, string buffId, int layer, float overrideDuration, long? casterId, long? srcBuffId)> _addRequests = new();
        private readonly List<(long target, long buffInstId)> _removeRequests = new();

        public GlobalBuffManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;
        }
        public void Tick(float dt)
        {
            TickLifetime(dt);
        }

        //public void ExecuteBuffTriggerEffect(BuffInstance buffInst, BuffEffectCfg cfg)
        //{
        //    switch(cfg.EffectType)
        //    {
        //        case EBuffEffectType.ShowFx:
        //            {
        //                logicManager.viewer.ShowFakeFxEffect("effect", buffInst.BuffOwner.Pos);
        //            }
        //            break;
        //    }
        //}


        private void HandleOnLogicEvent(IMapLogicEvent ev)
        {
            switch (ev)
            {
                case MLEApplyBuff evApplyBuff:
                    {
                        var targetEntity = logicManager.GetLogicEntity(evApplyBuff.TargetId) as BaseUnitLogicEntity;
                        if(targetEntity != null)
                        {
                            foreach(var buff in targetEntity.BuffContainer.Values)
                            {
                                if (!buff.CanTrigger(LogicTime.time)) continue;

                                foreach (var rule in buff.Def.TriggerList)
                                {
                                    // 检查触发
                                    if (!BuffTriggerCheckSystem.Matches(ev, buff, rule))
                                    {
                                        continue;
                                    }

                                    // 执行
                                    // OnTrigger()

                                    buff.LastTriggerTime = LogicTime.time;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void TickLifetime(float dt)
        {
            // 1) 收集外部事件（其它系统可直接向 EventBus.Enqueue）
            _frameEvents.AddRange(BuffEventBus.Drain());

            // 2) 推进定时器，产生 Tick/Expire 事件
            foreach (var buffInst in _buffs.Values)
            {
                buffInst.Tick(dt);
                if(buffInst.Lifetime != -1)
                {
                    buffInst.Lifetime -= dt;

                    if (buffInst.Lifetime < 0 && !buffInst.MarkedForRemove)
                    {
                        buffInst.MarkedForRemove = true; // 标记过期，清理阶段移除
                    }
                }
            }

            int consumed = 0;
            foreach (var ev in _frameEvents)
            {
                if (consumed >= MaxTriggerDepthPerFrame) break;

                HandleOnLogicEvent(ev);
                
                consumed++;
            }

            // 6) 处理Add/Remove请求（合并/堆叠/刷新）
            FlushBuffAddRemoveRequests();

            // 7) 清理
            List<long> toRemove = new();
            foreach(var buff in  _buffs.Values)
            {
                if (buff.MarkedForRemove)
                {
                    buff.OnBuffRemove();
                    toRemove.Add(buff.InstanceId);
                }
            }

            foreach(var removed in toRemove)
            {
                _buffs.Remove(removed);
            }

            _frameEvents.Clear();
        }

        public long AddBuff(long entityId, string buffId, int layer = 1, float overrideDuration = -1, long? casterId = null, long? srcBuffId = null)
        {
            var targetEntity = logicManager.AreaManager.GetLogicEntiy(entityId);
            if (targetEntity == null)
            {
                Debug.Log($"RemoveAllBuffById not found {entityId} ");
                return 0;
            }
            var instance = AddBuffInternal(targetEntity, buffId, layer, overrideDuration, casterId, srcBuffId);
            return instance.InstanceId;
        }

        // 外部接口：请求添加 Buff（可在效果中调用）
        public void RequestAddBuff(long entityId, string buffId, int layer = 1, float overrideDuration = -1, long? casterId = null, long? srcBuffId = null)
        {
            _addRequests.Add((entityId, buffId, layer, overrideDuration, casterId, srcBuffId));
        }

        public void RequestRemoveBuff(ILogicEntity targetEntity, long buffInstId)
        {
            _removeRequests.Add((0, buffInstId));
        }

        public void RemoveAllBuffById(long entityId, string buffId, int layer = 1, long? casterId = null, long? srcBuffId = null)
        {
            var targetEntity = logicManager.AreaManager.GetLogicEntiy(entityId);
            if(targetEntity == null)
            {
                Debug.Log($"RemoveAllBuffById not found {entityId} ");
                return;
            }

            foreach (var buffInst in targetEntity.BuffContainer.Values.ToList())
            {
                if(buffInst.BuffId != buffId)
                {
                    continue;
                }

                if(casterId != null && casterId != buffInst.CasterId)
                {
                    continue;
                }

                RequestRemoveBuff(targetEntity, buffInst.InstanceId);
            }
        }

        private void FlushBuffAddRemoveRequests()
        {
            // 先执行移除
            foreach (var r in _removeRequests)
            {
                _buffs.TryGetValue(r.buffInstId, out var buffInst);
                if(buffInst != null)
                {
                    buffInst.MarkedForRemove = true;
                }
            }
            _removeRequests.Clear();

            // 合并同目标同Buff的多次 Add
            foreach (var addReq in _addRequests)
            {
                var targetEntity = logicManager.AreaManager.GetLogicEntiy(addReq.target);
                if (targetEntity == null)
                {
                    Debug.Log($"RemoveAllBuffById not found {addReq.target} ");
                    continue;
                }
                AddBuffInternal(targetEntity, addReq.buffId, addReq.layer, addReq.overrideDuration, addReq.casterId, addReq.srcBuffId);
            }
            _addRequests.Clear();
        }

        protected BuffInstance AddBuffInternal(ILogicEntity target, string buffId, int layer, float overrideDuration, long? casterId, long? srcBuffId)
        {
            var buffDef = BuffLibrary.GetBuffDefinition(buffId);

            float duration = buffDef.DefaultDuration;
            if (overrideDuration > 0)
            {
                duration = overrideDuration;
            }
            bool needCreate = false;
            var existing = _buffs.FirstOrDefault(b => b.Value.BuffOwner == target && b.Value.Def.BuffId == buffId).Value;
            if (existing != null)
            {
                var layerOverrideType = buffDef.LayerOverrideType;
                switch (layerOverrideType)
                {
                    case EBuffLayerOverrideType.NoOp:
                        {

                        }
                        break;
                    // 重置
                    case EBuffLayerOverrideType.Replace:
                        {
                            existing.Layer = layer;
                            break;
                        }
                    case EBuffLayerOverrideType.AddLayer:
                        {
                            int maxLayer = buffDef.MaxStackLayer;
                            existing.Layer += layer;
                            if(maxLayer > 0)
                            {
                                existing.Layer = Math.Min(maxLayer, existing.Layer);
                            }
                            existing.Lifetime = duration;
                            break;
                        }
                    case EBuffLayerOverrideType.Duplicate:
                        {
                            needCreate = true;

                        }
                        break;
                    default:
                        {
                            Debug.LogError("Buff Override Error");
                            break;
                        }
                }

                var turnOverrideType = buffDef.TurnOverrideType;
                switch (turnOverrideType)
                {
                    case EBuffTurnOverrideType.NoOp:
                        {

                        }
                        break;
                    // 重置
                    case EBuffTurnOverrideType.Replace:
                        {
                            existing.Lifetime = duration;
                            break;
                        }
                    case EBuffTurnOverrideType.MaxTurn:
                        {
                            existing.Lifetime = Math.Max(duration, existing.Lifetime);
                            break;
                        }
                    case EBuffTurnOverrideType.AddTurn:
                        {
                            existing.Lifetime += duration;
                            break;
                        }

                    default:
                        {
                            Debug.LogError("Buff Override Error");
                            break;
                        }
                }
                existing.OnBuffAddOrUpdate(false);
            }
            else
            {
                needCreate = true;
            }

            if (needCreate)
            {
                existing = new BuffInstance(target, ++BuffInstIdCounter, buffId, layer, lifeTIme: duration, casterId:casterId, srcBuffId:srcBuffId);
                existing.OnBuffAddOrUpdate(true);
                _buffs.Add(existing.InstanceId, existing);
                var ev = new MLEApplyBuff()
                {
                    Ctx = new MapLogicEventContext { CorrelationId = Guid.NewGuid(), Reliable = true },
                    CasterId = casterId ?? 0,
                    TargetId = target.Id,
                    BuffId = buffId,
                    Layer = layer,
                };
                logicManager.LogicEventBus.Publish(ev);
            }
            return existing;
        }

        //// 提供外部直接施加接口（立即排队）
        //public void AddBuffImmediate(GameObject target, BuffDefinition def, GameObject source, float? overrideDuration = null)
        //{
        //    RequestAddBuff(target, def, source, overrideDuration);
        //    // 可选：马上Flush，但一般等帧末统一处理
        //}

        // 用于外部系统注入事件
        //public void Emit(GameEvent ev) => BuffEventBus.Enqueue(ev);

        private List<MapLogicSubscription> logicSubs = new();

        private MapLogicEventAdapter adapter;
        private List<MapLogicSubscription> subs = new();

        public void InitEventListening()
        {
            if(adapter == null)
            {
                adapter = new((ev) =>
                {
                    BuffEventBus.Enqueue(ev);
                });
            }

            if(logicSubs.Count > 0)
            {
                foreach(var sub in logicSubs)
                {
                    logicManager.LogicEventBus.Unsubscribe(sub);
                }
                logicSubs.Clear();
            }
        }


        //// 调试：列出目标当前 Buff
        //public List<BuffInstance> GetBuffs(GameObject target)
        //{
        //    return _buffs.Where(b => b.Owner == target).ToList();
        //}

        public bool CheckHasBuff(long entityId, string buffId)
        {
            var targetEntity = logicManager.AreaManager.GetLogicEntiy(entityId);
            if (targetEntity == null)
            {
                Debug.Log($"RemoveAllBuffById not found {entityId} ");
                return false;
            }

            foreach(var buff in targetEntity.BuffContainer.Values)
            {
                if(buff.BuffId == buffId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class BuffInstance
    {
        public long InstanceId;
        public string BuffId;
        public int Layer;
        public float Lifetime;

        public long CasterId;
        public long SrcBuffId; // 如果是光环等才有绑定关系
        public IEntityBuffOwner BuffOwner;


        /// <summary>
        /// 对于buff instance来说
        /// entityId 为施法者
        /// 
        /// </summary>
        //public SourceKey? srcKey;

        public BuffDefinition Def;


        public bool MarkedForRemove;

        public float? tickIntervalSec; // null 表示非周期

        private List<Modifier?> registeredModifiers;

        private List<TickTriggerStruct> tickTriggers;

        public class TickTriggerStruct
        {
            public float lastTick;
            public BuffTriggerRuleConfig config;
        }

        public class AuraRuntimeInfo
        {
            public float lastAuraTick;

            public List<long> AffectedEntites = new();
        }

        public AuraRuntimeInfo? auraRuntimeInfo = null;

        public BuffInstance(IEntityBuffOwner owner, long instId, string buffId, int layer, float lifeTIme = -1, long? casterId = null, long? srcBuffId = null)
        {
            this.InstanceId = instId;
            this.BuffId = buffId;
            this.CasterId = casterId??0;
            this.SrcBuffId = srcBuffId ?? 0;
            this.Layer = layer;

            BuffOwner = owner;
            Lifetime = lifeTIme;


            Def = BuffLibrary.GetBuffDefinition(buffId);

            owner.BuffContainer.Add(instId, this);
            foreach (var trigger in Def.TriggerList)
            {
                if (trigger.TriggerType == TriggerType.Tick)
                {
                    if (tickTriggers == null)
                    {
                        tickTriggers = new();
                    }
                    tickTriggers.Add(new TickTriggerStruct()
                    {
                        lastTick = 0,
                        config = trigger,
                    });
                }
            }

            if (Def.IsAura)
            {
                auraRuntimeInfo = new();
            }
        }

        /// <summary>
        /// 当buff添加或改变是 
        /// </summary>
        public void OnBuffAddOrUpdate(bool isAdd)
        {
            if (registeredModifiers == null)
            {
                registeredModifiers = new();
                foreach (var oneAttr in Def.ModifierAttrs)
                {
                    var srcKey = new SourceKey()
                    {
                        type = SourceType.Buff,
                        buffId = InstanceId,
                    };
                    var modifier = BuffOwner.AddAttrModifier(srcKey, oneAttr.ModifierAttrId, oneAttr.ModifierValue * Layer);
                    registeredModifiers.Add(modifier);
                }
            }
            else
            {
                for (int i = 0; i < Def.ModifierAttrs.Count; i++)
                {
                    registeredModifiers[i].value = Def.ModifierAttrs[i].ModifierValue * Layer;
                    BuffOwner.UpdateAttrModifier(registeredModifiers[i]);
                }
            }

            if (isAdd)
            {
                if (Def.DurationEffect != null)
                {
                    if (Def.DurationEffect.DurationType == EBuffDurationType.AnimOverride)
                    {
                        BuffOwner.AnimOverrideList.Add("1");
                    }
                }
            }
        }

        public Dictionary<string, long> GetAttributeByLayer()
        {
            return new();
        }

        public void Tick(float dt)
        {
            if (tickTriggers != null)
            {
                foreach (var triggerInfo in tickTriggers)
                {
                    if (triggerInfo.lastTick == 0)
                    {
                        triggerInfo.lastTick = LogicTime.time;
                        continue;
                    }

                    if (LogicTime.time - triggerInfo.lastTick < triggerInfo.config.TriggerParam1 * 0.001f)
                    {
                        continue;
                    }

                    triggerInfo.lastTick = triggerInfo.lastTick + triggerInfo.config.TriggerParam1 * 0.001f;

                    //if (triggerInfo.config.OutputEffects != null)
                    //{
                    //    foreach (var effect in triggerInfo.config.OutputEffects)
                    //    {
                    //        BuffOwner.BuffManager.ExecuteBuffTriggerEffect(this, effect);
                    //    }
                    //}

                    if(triggerInfo.config.OutputFightEffects != null)
                    {
                        foreach (var fightEffect in triggerInfo.config.OutputFightEffects)
                        {
                            switch(fightEffect)
                            {
                                // buff触发器中 
                                case MapAbilityEffectCostResourceCfg costResourceCfg:
                                    {
                                        long srcEntity = CasterId;

                                        var ctx = new LogicFightEffectContext(BuffOwner.BuffManager.logicManager, new SourceKey()
                                        {
                                            type = SourceType.BuffEffect,
                                            entityId = srcEntity,
                                            buffId = InstanceId,
                                        });

                                        ctx.Target = BuffOwner as ILogicEntity;

                                        BuffOwner.BuffManager.logicManager.HandleLogicFightEffect(fightEffect, ctx);
                                    }
                                    break;
                            }
                            
                        }
                    }
                }
            }

            if (Def.IsAura)
            {
                TickAuraEffect();
            }
        }

        protected void TickAuraEffect()
        {
            if (!Def.IsAura)
            {
                return;
            }

            if (LogicTime.time - auraRuntimeInfo.lastAuraTick < 1.0f)
            {
                return;
            }
            auraRuntimeInfo.lastAuraTick = LogicTime.time;
            var currAffectOnes = BuffOwner.FindEntityInRange(BuffOwner.Pos, Def.AuraRange);
            foreach (var affectedId in auraRuntimeInfo.AffectedEntites.ToList())
            {
                // 当帧不再受光环里
                if (currAffectOnes.Find(item => item.Id == affectedId) == null)
                {
                    // 移除光环效果
                    BuffOwner.BuffManager.RemoveAllBuffById(affectedId, Def.AuraBuffId, casterId:this.BuffOwner.Id, srcBuffId: this.InstanceId);
                    auraRuntimeInfo.AffectedEntites.Remove(affectedId);
                }
            }

            foreach (var currAffectOne in currAffectOnes)
            {
                var exist = auraRuntimeInfo.AffectedEntites.Find((item) => item == currAffectOne.Id);
                if (exist == -1)
                {
                    // 移除光环效果
                    BuffOwner.BuffManager.RequestAddBuff(currAffectOne.Id, Def.AuraBuffId, 1, casterId: this.BuffOwner.Id, srcBuffId: this.InstanceId);
                    auraRuntimeInfo.AffectedEntites.Add(currAffectOne.Id);
                }
            }
        }

        public void OnBuffRemove()
        {
            if (registeredModifiers != null)
            {
                foreach (var mod in registeredModifiers)
                {
                    if (mod != null)
                    {
                        BuffOwner.ExpireModifierBySource(mod.source);
                    }
                }
                registeredModifiers = null;
            }

            // 解除绑定
            if (BuffOwner != null)
            {
                BuffOwner.BuffContainer.Remove(InstanceId);
            }

            if (Def.DurationEffect != null)
            {
                if (Def.DurationEffect.DurationType == EBuffDurationType.AnimOverride)
                {
                    BuffOwner.AnimOverrideList.Remove("1");
                }
            }
        }
        public float LastTriggerTime;
        public bool CanTrigger(float now) => (now - LastTriggerTime) >= 0;
    }
}



