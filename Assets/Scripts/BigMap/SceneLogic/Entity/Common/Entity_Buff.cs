using Map.Entity;
using Map.Logic;
using Map.Logic.Events;
using My;
using My.Map;
using My.Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Experimental.GraphView;
using UnityEngine;



namespace My.Map.Entity
{
    public enum ETriggerType
    {
        Default,
        Tick,
        OnSkillUsed,
        OnHit,
        OnDie,
        FinalDmgReduced, // 累计最终减伤
        NearCaster, // 接近施法者（由 NearCasterWatch Duration 主动触发）

        PlayerHVoice = 100,
    }


    [Serializable]
    public class BuffTriggerRuleConfig
    {
        public bool ShowSpecific = false;

        public ETriggerType TriggerType;
        public int TriggerParam1;
        public int TriggerParam2;
        public int TriggerParam3;

        [SerializeReference]
        public List<MapFightEffectCfg> OutputFightEffects;

        public bool RemoveOnTrigger;

        public int NeedCount = 1;
        public float TriggerInterval = 0;
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

    public enum EBuffLayerStackMode
    {
        Classic,
        IndependentStack,
    }


    
    public enum EBuffDurationType
    {
        Invalid,
        AnimOverride,
        HitEffect, // ParamStr:效果名  ParamFloat1:单次时间

        // RelativeAttr: ParamStr1=源属性A, ParamStr2=目标属性B上的独立加成, ParamFloat1=比例(结果=floor(A*比例*层数))
        RelativeAttr,

        // SteerInput: ParamStr1=EBuffMoveSteerMode, ParamFloat1=speedRate, ParamFloat2=directionChangeInterval(Random 等)
        SteerInput,

        // NearCasterWatch: ParamFloat1=触发半径, ParamFloat2=检测间隔(0=每Tick)
        NearCasterWatch,
    }

    public enum EBuffMoveSteerMode
    {
        AwayFromCaster,
        TowardCaster,
        Random,
        FixedDirection,
    }

    [Serializable]
    public class BuffDurationEffet
    {
        public EBuffDurationType DurationType;

        public string ParamStr1;
        public string ParamStr2;
        public float ParamFloat1;
        public float ParamFloat2;

        public bool CommonFlag1;
        public bool CommonFlag2;
    }

    // Buff 持续时间侧效果：与 Lifetime 并行，按 DurationType 多态处理持续期逻辑。
    public abstract class BuffDurationInstanceBase
    {
        public abstract void OnBuffConfigureChanged(BuffInstance inst);
        public abstract void OnDetached(BuffInstance inst);
        public abstract void OnTick(BuffInstance inst, float dt);
    }

    // SteerInput 类 Buff 施加前免疫判定（拒绝整 buff，避免仅挡位移仍挂 Fear/Lured/ForbidSkillOp）
    internal static class SteerBuffApplyGate
    {
        internal static bool IsRejected(IEntityBuffOwner target, BuffDefinition def)
        {
            if (def == null)
            {
                return false;
            }

            foreach (var eff in def.ResolveDurationEffects())
            {
                if (eff == null || eff.DurationType != EBuffDurationType.SteerInput)
                {
                    continue;
                }

                if (target is not IEntityAttributeOwner attrOwner)
                {
                    continue;
                }

                if (attrOwner.CheckHasState(AttrIdConsts.ImmuneSteerInput))
                {
                    return true;
                }

                if (!TryParseSteerMode(eff.ParamStr1, out var mode))
                {
                    continue;
                }

                if (mode switch
                {
                    EBuffMoveSteerMode.AwayFromCaster => attrOwner.CheckHasState(AttrIdConsts.ImmuneFear),
                    EBuffMoveSteerMode.TowardCaster => attrOwner.CheckHasState(AttrIdConsts.ImmuneLured),
                    _ => false,
                })
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryParseSteerMode(string raw, out EBuffMoveSteerMode mode)
        {
            mode = EBuffMoveSteerMode.AwayFromCaster;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            return System.Enum.TryParse(raw, true, out mode);
        }
    }

    internal static class BuffDurationInstanceFactory
    {
        internal const string RelativeAttrModAbilitySlot = "__BuffDuration_RelativeAttr";

        public static BuffDurationInstanceBase Create(BuffDurationEffet eff)
        {
            if (eff == null)
            {
                return null;
            }

            switch (eff.DurationType)
            {
                case EBuffDurationType.RelativeAttr:
                    return new BuffDurationRelativeAttrInstance(eff);
                case EBuffDurationType.SteerInput:
                    return new BuffDurationSteerInputInstance(eff);
                case EBuffDurationType.NearCasterWatch:
                    return new BuffDurationNearCasterWatchInstance(eff);
                default:
                    return null;
            }
        }
    }

    // RelativeAttr：依据持有者属性 A 按比例折算为对属性 B 的独立 Modifier，Tick 检测 A 变化并刷新。
    internal sealed class BuffDurationRelativeAttrInstance : BuffDurationInstanceBase
    {
        private readonly string _srcAttrId;
        private readonly string _dstAttrId;
        private readonly float _ratio;
        private Modifier _linkedMod;
        private long _lastSrcSnapshot;

        public BuffDurationRelativeAttrInstance(BuffDurationEffet cfg)
        {
            _srcAttrId = cfg.ParamStr1;
            _dstAttrId = cfg.ParamStr2;
            _ratio = cfg.ParamFloat1;
        }

        public override void OnBuffConfigureChanged(BuffInstance inst)
        {
            if (!IsConfigValid(inst))
            {
                return;
            }

            var owner = inst.BuffOwner;
            long srcNow = owner.GetAttr(_srcAttrId);
            _lastSrcSnapshot = srcNow;
            ApplyMod(inst, owner, ComputeModValue(srcNow, inst.GetModifierScaleLayer()));
        }

        public override void OnDetached(BuffInstance inst)
        {
            if (_linkedMod != null && inst.BuffOwner != null)
            {
                inst.BuffOwner.ExpireModifierBySource(_linkedMod.source);
            }

            _linkedMod = null;
            _lastSrcSnapshot = 0;
        }

        public override void OnTick(BuffInstance inst, float dt)
        {
            if (!IsConfigValid(inst))
            {
                return;
            }

            var owner = inst.BuffOwner;
            long srcNow = owner.GetAttr(_srcAttrId);
            if (srcNow == _lastSrcSnapshot && _linkedMod != null)
            {
                return;
            }

            _lastSrcSnapshot = srcNow;
            ApplyMod(inst, owner, ComputeModValue(srcNow, inst.GetModifierScaleLayer()));
        }

        private bool IsConfigValid(BuffInstance inst)
        {
            if (string.IsNullOrEmpty(_srcAttrId) || string.IsNullOrEmpty(_dstAttrId))
            {
                return false;
            }

            return inst.BuffOwner != null;
        }

        private static ModSourceKey MakeSourceKey(BuffInstance inst)
        {
            return new ModSourceKey()
            {
                entityId = inst.CasterId,
                buffId = inst.InstanceId,
                abilityName = BuffDurationInstanceFactory.RelativeAttrModAbilitySlot,
            };
        }

        private long ComputeModValue(long srcAttrValue, int layer)
        {
            double v = srcAttrValue * _ratio;
            if (layer != 1)
            {
                v *= layer;
            }

            if (v > long.MaxValue)
            {
                return long.MaxValue;
            }

            if (v < long.MinValue)
            {
                return long.MinValue;
            }

            return (long)v;
        }

        private void ApplyMod(BuffInstance inst, IEntityBuffOwner owner, long modVal)
        {
            if (_linkedMod == null)
            {
                _linkedMod = owner.AddAttrModifier(MakeSourceKey(inst), _dstAttrId, modVal);
                return;
            }

            if (_linkedMod.value == modVal)
            {
                return;
            }

            _linkedMod.value = modVal;
            owner.UpdateAttrModifier(_linkedMod);
        }
    }


    [Serializable]
    public class BuffDefinition
    {
        public string BuffId;

        public string Desc;

        public string Icon = "fallback";
        public EBuffLayerOverrideType LayerOverrideType;
        public int MaxStackLayer;
        public EBuffLayerStackMode LayerStackMode = EBuffLayerStackMode.Classic;

        public EBuffTurnOverrideType TurnOverrideType;

        public float DefaultDuration;

        public bool IsAura;
        public float AuraRange;
        public string AuraBuffId;

        public string EffectId;
        public int HeadHintPriority = 0;

        public float ZOffsetOverride;

        public bool IsHidden;

        // 首次施加时立刻清空 VisionSystem 目击累积
        public bool FlushWitnessOnApply;

        [Serializable]
        public class OneModPair
        {
            public string ModifierAttrId;
            public long ModifierValue;
        }

        public List<OneModPair> ModifierAttrs = new();

        public List<OneModPair> PotencyCopyAttrs = new();
        public List<OneModPair> PotencyCalcRates = new();
        public long PotencyBase;

        [SerializeReference]
        public List<MapFightEffectCfg> OnAttachEffects = null;
        [SerializeReference]
        public List<MapFightEffectCfg> OnDetachEffects = null;

        public BuffDurationEffet DurationEffect;

        public List<BuffDurationEffet> DurationEffects = new();

        public List<BuffTriggerRuleConfig> TriggerList = new();

        // 兼容旧单 DurationEffect 字段
        public IReadOnlyList<BuffDurationEffet> ResolveDurationEffects()
        {
            if (DurationEffects != null && DurationEffects.Count > 0)
            {
                return DurationEffects;
            }

            if (DurationEffect != null)
            {
                return new[] { DurationEffect };
            }

            return Array.Empty<BuffDurationEffet>();
        }

        public bool HasAnimOverrideDuration()
        {
            foreach (var eff in ResolveDurationEffects())
            {
                if (eff != null && eff.DurationType == EBuffDurationType.AnimOverride)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // IndependentStack：每层独立剩余时间，可选记录 Caster/SrcBuff
    public sealed class BuffLayerStackEntry
    {
        public float RemainingLifetime;
        public long CasterId;
        public long SrcBuffId;

        public BuffLayerStackEntry(float remainingLifetime, long casterId, long srcBuffId)
        {
            RemainingLifetime = remainingLifetime;
            CasterId = casterId;
            SrcBuffId = srcBuffId;
        }
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

        public void Clear()
        {
            _buffs.Clear();
            _frameEvents.Clear();
            _addRequests.Clear();
            _removeRequests.Clear();
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
                if (!buffInst.UsesIndependentStack && buffInst.Lifetime != -1)
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
                    buff.BuffOwner.UnregisterBuff(buff);
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
            buffId = BuffCastRemap.ResolveBuffId(logicManager, casterId, buffId);
            var targetEntity = logicManager.AreaManager.GetLogicEntiy(entityId);
            if (targetEntity == null)
            {
                Debug.Log($"RemoveAllBuffById not found {entityId} ");
                return 0;
            }
            var instance = AddBuffInternal(targetEntity, buffId, layer, overrideDuration, casterId, srcBuffId);
            return instance != null ? instance.InstanceId : 0;
        }

        public void RehydrateBuffFromPersist(IEntityBuffOwner owner, My.Saving.BuffPersistData data)
        {
            if (owner == null || data == null || string.IsNullOrEmpty(data.BuffId))
            {
                return;
            }

            long? caster = data.CasterEntityId != 0 ? data.CasterEntityId : (long?)null;
            long? srcBuff = data.SrcBuffId != 0 ? data.SrcBuffId : (long?)null;
            var def = BuffLibrary.GetBuffDefinition(data.BuffId);

            if (def.LayerStackMode == EBuffLayerStackMode.IndependentStack)
            {
                if (data.StackLayers != null && data.StackLayers.Count > 0)
                {
                    var inst = new BuffInstance(owner, ++BuffInstIdCounter, data.BuffId, 0, -1, casterId: caster, srcBuffId: srcBuff);
                    inst.LoadIndependentStackFromPersist(data.StackLayers);
                    inst.OnBuffAddOrUpdate(true);
                    _buffs[inst.InstanceId] = inst;
                    owner.RegisterBuffDirect(inst);
                    return;
                }

                var legacyStack = new BuffInstance(owner, ++BuffInstIdCounter, data.BuffId, data.Layer, data.RemainingLifetime, casterId: caster, srcBuffId: srcBuff);
                legacyStack.OnBuffAddOrUpdate(true);
                _buffs[legacyStack.InstanceId] = legacyStack;
                owner.RegisterBuffDirect(legacyStack);
                return;
            }

            var classic = new BuffInstance(owner, ++BuffInstIdCounter, data.BuffId, data.Layer, data.RemainingLifetime, casterId: caster, srcBuffId: srcBuff);
            classic.RestoreCachedPotencyFromPersist(data.CachedPotencyAttrs);
            classic.OnBuffAddOrUpdate(true);
            _buffs[classic.InstanceId] = classic;
            owner.RegisterBuffDirect(classic);
        }

        // 外部接口：请求添加 Buff（可在效果中调用）
        public void RequestAddBuff(long entityId, string buffId, int layer = 1, float overrideDuration = -1, long? casterId = null, long? srcBuffId = null)
        {
            buffId = BuffCastRemap.ResolveBuffId(logicManager, casterId, buffId);
            if (buffId == "unit_knockfly"
                && logicManager.AreaManager.GetLogicEntiy(entityId, false) is IEntityAttributeOwner target
                && !target.CheckHasState(AttrIdConsts.UnitStagger))
            {
                buffId = "unit_stagger";
            }

            _addRequests.Add((entityId, buffId, layer, overrideDuration, casterId, srcBuffId));
        }

        public void RequestRemoveBuff(ILogicEntity targetEntity, long buffInstId)
        {
            _removeRequests.Add((0, buffInstId));
        }

        // subtractLayer：0 整实例移除（Classic）或按规则摘层（Independent 见实现）；>0 减层
        public void RemoveAllBuffById(long entityId, string buffId, int subtractLayer = 0, long? casterId = null, long? srcBuffId = null)
        {
            var targetEntity = logicManager.AreaManager.GetLogicEntiy(entityId, false);
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

                if (buffInst.UsesIndependentStack)
                {
                    bool hasFilter = casterId != null || srcBuffId != null;
                    if (subtractLayer <= 0 && !hasFilter)
                    {
                        RequestRemoveBuff(targetEntity, buffInst.InstanceId);
                    }
                    else if (subtractLayer <= 0 && hasFilter)
                    {
                        buffInst.RemoveIndependentStackEntries(0, casterId, srcBuffId, removeAllMatchingFilter: true);
                    }
                    else
                    {
                        buffInst.RemoveIndependentStackEntries(subtractLayer, casterId, srcBuffId, removeAllMatchingFilter: false);
                    }
                    continue;
                }

                if(casterId != null && casterId != buffInst.CasterId)
                {
                    continue;
                }

                if (srcBuffId != null && buffInst.SrcBuffId != srcBuffId.Value)
                {
                    continue;
                }

                if (subtractLayer > 0)
                {
                    int newLayer = buffInst.Layer - subtractLayer;
                    if (newLayer <= 0)
                    {
                        RequestRemoveBuff(targetEntity, buffInst.InstanceId);
                    }
                    else
                    {
                        buffInst.SetBuffLayerDirect(newLayer);
                    }
                }
                else
                {
                    RequestRemoveBuff(targetEntity, buffInst.InstanceId);
                }
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

        private static BuffInstance FindMergeTarget(Dictionary<long, BuffInstance> table, IEntityBuffOwner target, BuffDefinition def, string buffId)
        {
            if (def.LayerOverrideType == EBuffLayerOverrideType.Duplicate
                && def.LayerStackMode != EBuffLayerStackMode.IndependentStack)
            {
                return null;
            }

            foreach (var b in table.Values)
            {
                if (b.BuffOwner == target && b.BuffId == buffId)
                {
                    return b;
                }
            }

            return null;
        }

        private static float ComputeIndependentPerEntryDuration(EBuffTurnOverrideType turn, float incoming, float refLife)
        {
            switch (turn)
            {
                case EBuffTurnOverrideType.NoOp:
                case EBuffTurnOverrideType.Replace:
                    return incoming;
                case EBuffTurnOverrideType.MaxTurn:
                    if (refLife < 0)
                    {
                        return -1;
                    }

                    return Math.Max(incoming, refLife);
                case EBuffTurnOverrideType.AddTurn:
                    if (refLife < 0)
                    {
                        return -1;
                    }

                    return refLife + incoming;
                default:
                    return incoming;
            }
        }

        protected BuffInstance AddBuffInternal(ILogicEntity target, string buffId, int layer, float overrideDuration, long? casterId, long? srcBuffId)
        {
            var buffDef = BuffLibrary.GetBuffDefinition(buffId);
            if (target is IEntityBuffOwner buffOwner && SteerBuffApplyGate.IsRejected(buffOwner, buffDef))
            {
                return null;
            }

            float duration = buffDef.DefaultDuration;
            if (overrideDuration > 0)
            {
                duration = overrideDuration;
            }

            var existing = FindMergeTarget(_buffs, target, buffDef, buffId);

            if (buffDef.LayerStackMode == EBuffLayerStackMode.IndependentStack)
            {
                if (existing == null)
                {
                    var inst = new BuffInstance(target, ++BuffInstIdCounter, buffId, layer, duration, casterId: casterId, srcBuffId: srcBuffId);
                    inst.OnBuffAddOrUpdate(true);

                    _buffs.Add(inst.InstanceId, inst);
                    target.RegisterBuff(inst);

                    var ev = new MLEApplyBuff()
                    {
                        Ctx = new MapLogicEventContext { CorrelationId = Guid.NewGuid() },
                        CasterId = casterId ?? 0,
                        TargetId = target.Id,
                        BuffId = buffId,
                        Layer = inst.Layer,
                    };
                    logicManager.LogicEventBus.Publish(ev);
                    return inst;
                }

                float refLife = existing.GetStackTurnReferenceLifetime();
                float perEntry = ComputeIndependentPerEntryDuration(buffDef.TurnOverrideType, duration, refLife);

                switch (buffDef.LayerOverrideType)
                {
                    case EBuffLayerOverrideType.NoOp:
                        break;
                    case EBuffLayerOverrideType.Replace:
                        existing.ReplaceIndependentStack(layer, perEntry, casterId, srcBuffId);
                        break;
                    case EBuffLayerOverrideType.AddLayer:
                    case EBuffLayerOverrideType.Duplicate:
                        existing.PushIndependentLayers(layer, perEntry, casterId, srcBuffId);
                        break;
                    default:
                        Debug.LogError("Buff Override Error");
                        break;
                }

                return existing;
            }

            // Classic
            bool needCreate = false;
            if (existing != null)
            {
                var layerOverrideType = buffDef.LayerOverrideType;
                switch (layerOverrideType)
                {
                    case EBuffLayerOverrideType.NoOp:
                        {

                        }
                        break;
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

                if (!needCreate)
                {
                    if (BuffPotencyUtil.UsesPotency(buffDef)
                        && !BuffPotencyUtil.TryCommitPotency(existing, logicManager, casterId, out var rejectedWeak)
                        && rejectedWeak)
                    {
                        return existing;
                    }

                    var turnOverrideType = buffDef.TurnOverrideType;
                    switch (turnOverrideType)
                    {
                        case EBuffTurnOverrideType.NoOp:
                            {

                            }
                            break;
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
            }
            else
            {
                needCreate = true;
            }

            if (needCreate)
            {
                existing = new BuffInstance(target, ++BuffInstIdCounter, buffId, layer, lifeTIme: duration, casterId:casterId, srcBuffId:srcBuffId);
                BuffPotencyUtil.TryCommitPotency(existing, logicManager, casterId, out _);
                existing.OnBuffAddOrUpdate(true);

                _buffs.Add(existing.InstanceId, existing);
                target.RegisterBuff(existing);

                var ev = new MLEApplyBuff()
                {
                    Ctx = new MapLogicEventContext { CorrelationId = Guid.NewGuid() },
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

        public bool TryGetBuffInstance(long instId, out BuffInstance inst)
        {
            return _buffs.TryGetValue(instId, out inst);
        }
    }

    public class BuffInstance
    {
        public long InstanceId;
        public string BuffId;

        private int _classicLayer;
        private List<BuffLayerStackEntry> _independentStack;

        public int Layer
        {
            get
            {
                if (Def != null && Def.LayerStackMode == EBuffLayerStackMode.IndependentStack && _independentStack != null)
                {
                    return _independentStack.Count;
                }

                return _classicLayer;
            }
            set
            {
                if (Def != null && Def.LayerStackMode == EBuffLayerStackMode.IndependentStack && _independentStack != null)
                {
                    Debug.LogWarning($"Set Layer ignored for independent-stack buff {BuffId}");
                    return;
                }

                _classicLayer = value;
            }
        }

        public float Lifetime;

        public long CasterId;
        public long SrcBuffId; // 如果是光环等才有绑定关系
        public Dictionary<string, long> CachedPotencyAttrs;
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

        public bool UsesIndependentStack => Def != null && Def.LayerStackMode == EBuffLayerStackMode.IndependentStack;

        private List<Modifier?> registeredModifiers;

        private List<TriggerRuntimeStruct> triggerRuntimes;

        public class TriggerRuntimeStruct
        {
            public float lastTriggerTime;
            public BuffTriggerRuleConfig config;

            public int Counter;
        }

        public class AuraRuntimeInfo
        {
            public float lastAuraTick;

            public List<long> AffectedEntites = new();
        }

        public AuraRuntimeInfo? auraRuntimeInfo = null;

        private readonly List<BuffDurationInstanceBase> _durationLogics = new();

        public BuffInstance(IEntityBuffOwner owner, long instId, string buffId, int layer, float lifeTIme = -1, long? casterId = null, long? srcBuffId = null)
        {
            InstanceId = instId;
            BuffId = buffId;
            BuffOwner = owner;

            Def = BuffLibrary.GetBuffDefinition(buffId);
            CasterId = casterId ?? 0;
            SrcBuffId = srcBuffId ?? 0;

            foreach (var eff in Def.ResolveDurationEffects())
            {
                var logic = BuffDurationInstanceFactory.Create(eff);
                if (logic != null)
                {
                    _durationLogics.Add(logic);
                }
            }

            if (Def.LayerStackMode == EBuffLayerStackMode.IndependentStack)
            {
                Lifetime = -1;
                _independentStack = new List<BuffLayerStackEntry>();
                for (int i = 0; i < layer; i++)
                {
                    _independentStack.Add(new BuffLayerStackEntry(lifeTIme, CasterId, SrcBuffId));
                }

                _classicLayer = 0;
            }
            else
            {
                Lifetime = lifeTIme;
                _classicLayer = layer;
                _independentStack = null;
            }

            foreach (var trigger in Def.TriggerList)
            {
                if (triggerRuntimes == null)
                {
                    triggerRuntimes = new();
                }
                triggerRuntimes.Add(new TriggerRuntimeStruct()
                {
                    lastTriggerTime = 0,
                    config = trigger,
                });
            }

            if (Def.IsAura)
            {
                auraRuntimeInfo = new();
            }
        }

        public int GetModifierScaleLayer()
        {
            int n = Layer;
            if (Def.LayerStackMode == EBuffLayerStackMode.IndependentStack && Def.MaxStackLayer > 0)
            {
                return Math.Min(n, Def.MaxStackLayer);
            }

            return n;
        }

        public List<BuffLayerPersistEntry> ExportStackLayersForPersist()
        {
            var list = new List<BuffLayerPersistEntry>(_independentStack?.Count ?? 0);
            if (_independentStack == null)
            {
                return list;
            }

            foreach (var e in _independentStack)
            {
                list.Add(new BuffLayerPersistEntry
                {
                    RemainingLifetime = e.RemainingLifetime,
                    CasterEntityId = e.CasterId,
                    SrcBuffId = e.SrcBuffId,
                });
            }

            return list;
        }

        public void LoadIndependentStackFromPersist(IList<BuffLayerPersistEntry> rows)
        {
            if (_independentStack == null)
            {
                _independentStack = new List<BuffLayerStackEntry>();
            }
            else
            {
                _independentStack.Clear();
            }

            if (rows == null)
            {
                return;
            }

            foreach (var row in rows)
            {
                _independentStack.Add(new BuffLayerStackEntry(row.RemainingLifetime, row.CasterEntityId, row.SrcBuffId));
            }
        }

        public float GetStackTurnReferenceLifetime()
        {
            if (_independentStack == null || _independentStack.Count == 0)
            {
                return 0;
            }

            foreach (var e in _independentStack)
            {
                if (e.RemainingLifetime < 0)
                {
                    return -1;
                }
            }

            float max = 0;
            foreach (var e in _independentStack)
            {
                max = Math.Max(max, e.RemainingLifetime);
            }

            return max;
        }

        public void PushIndependentLayers(int count, float perEntryDur, long? casterId, long? srcBuffId)
        {
            if (!UsesIndependentStack || _independentStack == null || count <= 0)
            {
                return;
            }

            long c = casterId ?? 0;
            long s = srcBuffId ?? 0;
            for (int i = 0; i < count; i++)
            {
                _independentStack.Add(new BuffLayerStackEntry(perEntryDur, c, s));
            }

            OnBuffAddOrUpdate(false);
        }

        public void ReplaceIndependentStack(int count, float perEntryDur, long? casterId, long? srcBuffId)
        {
            if (!UsesIndependentStack || _independentStack == null)
            {
                return;
            }

            _independentStack.Clear();
            if (count <= 0)
            {
                MarkedForRemove = true;
                return;
            }

            PushIndependentLayers(count, perEntryDur, casterId, srcBuffId);
        }

        public void RemoveIndependentStackEntries(int subtractLayer, long? casterId, long? srcBuffId, bool removeAllMatchingFilter)
        {
            if (!UsesIndependentStack || _independentStack == null)
            {
                return;
            }

            int before = _independentStack.Count;
            if (removeAllMatchingFilter && subtractLayer <= 0 && (casterId != null || srcBuffId != null))
            {
                _independentStack.RemoveAll(e => MatchesStackEntryFilter(e, casterId, srcBuffId));
            }
            else if (subtractLayer > 0)
            {
                int removed = 0;
                if (casterId != null || srcBuffId != null)
                {
                    for (int i = _independentStack.Count - 1; i >= 0 && removed < subtractLayer; i--)
                    {
                        if (MatchesStackEntryFilter(_independentStack[i], casterId, srcBuffId))
                        {
                            _independentStack.RemoveAt(i);
                            removed++;
                        }
                    }
                }
                else
                {
                    while (removed < subtractLayer && _independentStack.Count > 0)
                    {
                        _independentStack.RemoveAt(_independentStack.Count - 1);
                        removed++;
                    }
                }
            }

            if (_independentStack.Count == 0)
            {
                MarkedForRemove = true;
            }
            else if (_independentStack.Count != before)
            {
                OnBuffAddOrUpdate(false);
            }
        }

        private static bool MatchesStackEntryFilter(BuffLayerStackEntry e, long? casterId, long? srcBuffId)
        {
            if (casterId != null && e.CasterId != casterId.Value)
            {
                return false;
            }

            if (srcBuffId != null && e.SrcBuffId != srcBuffId.Value)
            {
                return false;
            }

            return true;
        }

        private void TickIndependentStack(float dt)
        {
            if (!UsesIndependentStack || _independentStack == null || _independentStack.Count == 0)
            {
                return;
            }

            bool changed = false;
            for (int i = _independentStack.Count - 1; i >= 0; i--)
            {
                var e = _independentStack[i];
                if (e.RemainingLifetime < 0)
                {
                    continue;
                }

                e.RemainingLifetime -= dt;
                if (e.RemainingLifetime < 0)
                {
                    _independentStack.RemoveAt(i);
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            if (_independentStack.Count == 0)
            {
                MarkedForRemove = true;
            }
            else
            {
                OnBuffAddOrUpdate(false);
            }
        }

        /// <summary>
        /// 当buff添加或改变是 
        /// </summary>
        public void OnBuffAddOrUpdate(bool isAdd)
        {
            int modLayer = GetModifierScaleLayer();
            if (registeredModifiers == null)
            {
                registeredModifiers = new();
                foreach (var oneAttr in Def.ModifierAttrs)
                {
                    var srcKey = new ModSourceKey()
                    {
                        entityId = CasterId,
                        buffId = InstanceId,
                    };
                    var modifier = BuffOwner.AddAttrModifier(srcKey, oneAttr.ModifierAttrId, oneAttr.ModifierValue * modLayer);
                    registeredModifiers.Add(modifier);
                }
            }
            else
            {
                for (int i = 0; i < Def.ModifierAttrs.Count; i++)
                {
                    registeredModifiers[i].value = Def.ModifierAttrs[i].ModifierValue * modLayer;
                    BuffOwner.UpdateAttrModifier(registeredModifiers[i]);
                }
            }

            // 新实例：RegisterBuff 之后会统一 Notify；此处仅处理已存在实例的层数/持续时间变更
            if (!isAdd && BuffOwner is LogicEntityBase leOwner)
            {
                leOwner.NotifyAnimLayerRefreshIfAnimOverrideBuff(Def);
            }

            foreach (var logic in _durationLogics)
            {
                logic?.OnBuffConfigureChanged(this);
            }

            if (isAdd && Def.FlushWitnessOnApply && BuffOwner is BaseUnitLogicEntity unit)
            {
                unit.VisionSystem?.FlushWitnessState();
                unit.AggroSystem?.OnVisionUpdate();
            }
        }


        public void SetBuffLayerDirect(int layer)
        {
            if (UsesIndependentStack && _independentStack != null)
            {
                while (_independentStack.Count > layer)
                {
                    _independentStack.RemoveAt(_independentStack.Count - 1);
                }

                while (_independentStack.Count < layer)
                {
                    _independentStack.Add(new BuffLayerStackEntry(-1, CasterId, SrcBuffId));
                }

                if (layer <= 0)
                {
                    MarkedForRemove = true;
                }
                else
                {
                    OnBuffAddOrUpdate(false);
                }

                return;
            }

            _classicLayer = layer;
            OnBuffAddOrUpdate(false);
        }


        public Dictionary<string, long> GetAttributeByLayer()
        {
            return new();
        }

        public void Tick(float dt)
        {
            TickIndependentStack(dt);
            if (triggerRuntimes != null)
            {
                foreach (var triggerInfo in triggerRuntimes)
                {
                    if (triggerInfo.config.TriggerType != ETriggerType.Tick)
                    {
                        continue;
                    }

                    if (triggerInfo.lastTriggerTime == 0)
                    {
                        triggerInfo.lastTriggerTime = LogicTime.time;
                        continue;
                    }

                    if (LogicTime.time - triggerInfo.lastTriggerTime < triggerInfo.config.TriggerParam1 * 0.001f)
                    {
                        continue;
                    }

                    triggerInfo.lastTriggerTime = triggerInfo.lastTriggerTime + triggerInfo.config.TriggerParam1 * 0.001f;

                    if(triggerInfo.config.OutputFightEffects != null)
                    {
                        foreach(var e in  triggerInfo.config.OutputFightEffects)
                        {
                            HandleBuffTriggerEffect(e, ETriggerType.Tick);
                        }
                    }
                }
            }

            if (Def.IsAura)
            {
                TickAuraEffect();
            }

            foreach (var logic in _durationLogics)
            {
                logic?.OnTick(this, dt);
            }
        }

        public void DoBuffTrigger(ETriggerType triggerType, int val = 1)
        {
            if (triggerRuntimes == null)
            {
                return;
            }

            foreach (var t in triggerRuntimes)
            {
                if(t.config.TriggerType != triggerType)
                {
                    continue;
                }
                // check
                switch (triggerType)
                {
                    case ETriggerType.OnHit:
                        {

                        }
                        break;
                }

                if (t.config.TriggerInterval != 0 && LogicTime.time - t.lastTriggerTime < t.config.TriggerInterval)
                {
                    continue;
                }

                t.lastTriggerTime = LogicTime.time;
                t.Counter += val;

                if (t.config.NeedCount > 0 && t.Counter < t.config.NeedCount)
                {
                    continue;
                }

                t.Counter = 0;
                Debug.Log("buff trigger " + BuffId);

                if (t.config.OutputFightEffects != null)
                {
                    foreach (var fightEffect in t.config.OutputFightEffects)
                    {
                        HandleBuffTriggerEffect(fightEffect, triggerType);
                    }
                }

                // 移除自身
                if(t.config.RemoveOnTrigger)
                {
                    this.MarkedForRemove = true;
                }
            }
        }
        

        /// <summary>
        /// 处理触发效果
        /// </summary>
        /// <param name="triggerRuntime"></param>
        protected void HandleBuffTriggerEffect(MapFightEffectCfg fightEffect, ETriggerType triggerType)
        {
            switch (fightEffect)
            {
                // buff触发器中 
                case MapAbilityEffectAddResourceCfg:
                case MapAbilityEffectCostResourceCfg:
                case MapFightEffectApplyDamageCfg:
                case MapFightEffectResourcePercentDamageCfg:
                case MapAbilityEffectCastSkillCfg:
                case MapFightEffectQueueModeCfg:
                case MapFightEffectBroadcastAttractCfg:
                case MapFightEffectTriggerAlert:
                case MapFightEffectCauseNoise:
                case MapFightEffectWantedIncidentBroadcastCfg:
                case MapFightEffectEasyEffect:
                case MapFightEffectCreateAreaEffectCfg:
                case MapAbilityEffectHitBoxCfg:
                case MapFightEffectShowEffect:
                case MapFightEffectShowCloseupWindowCfg:
                    {
                        long srcEntity = CasterId;

                        var srcInfo = new GameLogicManager.EffectSourceInfo()
                        {
                            SrcType = GameLogicManager.ESourceType.BuffEffect,
                            SrcEntityId = srcEntity,
                            SrcBuffId = InstanceId,
                        };
                        var ctx = new GameLogicManager.LogicFightEffectContext(BuffOwner.BuffManager.logicManager, GameLogicManager.EFightCtxType.Buff, srcInfo);

                        ctx.TriggerPos = BuffOwner.Pos;
                        ctx.TargetId = BuffOwner.Id;

                        FillTriggerCacheAttrVal(ctx);

                        Debug.Log($"HandleBuffTriggerEffect handle trigger effect {fightEffect.GetType()}");
                        BuffOwner.BuffManager.logicManager.HandleLogicFightEffect(fightEffect, ctx);
                    }
                    break;
            }
        }

        private HashSet<long> _cacheFrameAffected = new();

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
            _cacheFrameAffected.Clear();
            foreach (var one in BuffOwner.FindEntityInRange(BuffOwner.Pos, Def.AuraRange))
            {
                _cacheFrameAffected.Add(one.Id);
            }
            foreach (var affectedId in auraRuntimeInfo.AffectedEntites.ToList())
            {
                // 当帧不再受光环里
                if (!_cacheFrameAffected.Contains(affectedId))
                {
                    // 移除光环效果
                    BuffOwner.BuffManager.RemoveAllBuffById(affectedId, Def.AuraBuffId, casterId:this.BuffOwner.Id, srcBuffId: this.InstanceId);
                    auraRuntimeInfo.AffectedEntites.Remove(affectedId);
                }
            }

            foreach (var currAffectId in _cacheFrameAffected)
            {
                var exist = auraRuntimeInfo.AffectedEntites.Find((item) => item == currAffectId);
                if (exist == -1)
                {
                    // 移除光环效果
                    BuffOwner.BuffManager.RequestAddBuff(currAffectId, Def.AuraBuffId, 1, casterId: this.BuffOwner.Id, srcBuffId: this.InstanceId);
                    auraRuntimeInfo.AffectedEntites.Add(currAffectId);
                }
            }
        }



        /// <summary>
        /// buff移除
        /// </summary>
        public void OnBuffRemove()
        {

            if(Def.OnDetachEffects != null)
            {
                foreach(var fightEffect in  Def.OnDetachEffects)
                {
                    HandleBuffTriggerEffect(fightEffect, ETriggerType.Default);
                }
            }

            foreach (var logic in _durationLogics)
            {
                logic?.OnDetached(this);
            }

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
        }
        void FillTriggerCacheAttrVal(GameLogicManager.LogicFightEffectContext ctx)
        {
            if (CachedPotencyAttrs != null)
            {
                foreach (var kv in CachedPotencyAttrs)
                {
                    ctx.CacheAttrVal[kv.Key] = kv.Value;
                }
            }

        }

        public void RestoreCachedPotencyFromPersist(List<AttrKvPair> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                CachedPotencyAttrs = null;
                return;
            }

            CachedPotencyAttrs = new Dictionary<string, long>(rows.Count);
            foreach (var pair in rows)
            {
                if (string.IsNullOrEmpty(pair.AttrId))
                {
                    continue;
                }

                CachedPotencyAttrs[pair.AttrId] = pair.Val;
            }
        }

        public List<AttrKvPair> ExportCachedPotencyForPersist()
        {
            if (CachedPotencyAttrs == null || CachedPotencyAttrs.Count == 0)
            {
                return null;
            }

            var list = new List<AttrKvPair>(CachedPotencyAttrs.Count);
            foreach (var kv in CachedPotencyAttrs)
            {
                list.Add(new AttrKvPair { AttrId = kv.Key, Val = kv.Value });
            }

            return list;
        }

        public float LastTriggerTime;
        public bool CanTrigger(float now) => (now - LastTriggerTime) >= 0;
    }

    public static class BuffPotencyUtil
    {
        public static bool UsesPotency(BuffDefinition def)
        {
            if (def == null)
            {
                return false;
            }

            return (def.PotencyCopyAttrs != null && def.PotencyCopyAttrs.Count > 0)
                || (def.PotencyCalcRates != null && def.PotencyCalcRates.Count > 0)
                || def.PotencyBase != 0;
        }

        public static long ComputePotencyScore(BuffDefinition def, Dictionary<string, long> attrs)
        {
            if (def == null)
            {
                return 0;
            }

            long score = def.PotencyBase;
            if (def.PotencyCalcRates == null || attrs == null)
            {
                return score;
            }

            foreach (var pair in def.PotencyCalcRates)
            {
                if (string.IsNullOrEmpty(pair.ModifierAttrId))
                {
                    continue;
                }

                if (attrs.TryGetValue(pair.ModifierAttrId, out var val))
                {
                    score += val * pair.ModifierValue / 10000;
                }
            }

            return score;
        }

        public static bool TryCommitPotency(BuffInstance inst, GameLogicManager mgr, long? casterId, out bool rejectedWeak)
        {
            rejectedWeak = false;
            if (inst?.Def == null || !UsesPotency(inst.Def))
            {
                return true;
            }

            long oldScore = ComputePotencyScore(inst.Def, inst.CachedPotencyAttrs);

            var preview = new Dictionary<string, long>();
            ILogicEntity caster = null;
            if (casterId != null && mgr != null)
            {
                caster = mgr.GetLogicEntity(casterId.Value, false);
            }

            if (inst.Def.PotencyCopyAttrs != null)
            {
                foreach (var pair in inst.Def.PotencyCopyAttrs)
                {
                    if (string.IsNullOrEmpty(pair.ModifierAttrId))
                    {
                        continue;
                    }

                    preview[pair.ModifierAttrId] = caster?.GetAttr(pair.ModifierAttrId) ?? 0;
                }
            }

            long newScore = ComputePotencyScore(inst.Def, preview);

            if (oldScore > 0 && newScore < oldScore)
            {
                rejectedWeak = true;
                return false;
            }

            inst.CachedPotencyAttrs = preview.Count > 0 ? preview : null;
            return true;
        }
    }
}



