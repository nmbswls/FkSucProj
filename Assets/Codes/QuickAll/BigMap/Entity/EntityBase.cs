using Config;
using Map.Logic.Events;
using My.Map.Entity;
using My.Map.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace My.Map
{
    public enum EEntityType
    {
        None,
        Player,
        InteractPoint,
        Npc,
        LootPoint,
        Monster,
        AreaEffect,
        DestroyObj,

        PatrolGroup,
    }

    public interface IEntityBuffOwner : IEntityAttributeOwner
    {
        Dictionary<long, BuffInstance> BuffContainer { get; }

        Vector2 Pos { get; }


        List<string> AnimOverrideList { get; }

        List<ILogicEntity> FindEntityInRange(Vector2 pos, float radius);

        GlobalBuffManager BuffManager { get; }

        bool CheckHasBuff(string buffId);
    }

    public interface IEntityAttributeOwner
    {
        long Id { get; }
        long GetAttr(string attrId);

        void ApplyResourceChange(string resourceId, long delta, bool isDamage, SourceKey? source, Dictionary<string, long> extraAttrs = null);

        long CalculateResourceCostAmount(ResourceDeltaIntent intent);
        /// <summary>
        /// 增加modifier
        /// </summary>
        /// <param name="m"></param>
        Modifier AddAttrModifier(SourceKey source, string attrId, long val);

        void ExpireModifierBySource(SourceKey sk);

        void UpdateAttrModifier(Modifier m);


    }

    public interface ILogicEntity : IEntityBuffOwner, IEntityAttributeOwner
    {
        GameLogicManager LogicManager { get; }

        long Id { get; }

        string CfgId { get; }
        EEntityType Type { get; }

        EFactionId FactionId { get; }
        bool IsActive { get; }
        Vector2 Pos { get; }

        // 生命周期钩子
        void OnSpawn(LogicEntityRecord data);    // 从记录创建完整实例
        void OnDespawn(out LogicEntityRecord? snapshot); // 输出快照，供下次重建
        void OnWake();   // 从Sleep进入Active，开启AI、感知、昂贵系统
        void OnSleep();  // 从Active降级为Sleep，关闭昂贵系统，保留轻量逻辑
        void Tick(float dt);

        void OnEnterAOI();

        void OnExitAOI();

        event Action<Vector2, Vector2> EventOnEntityMove;

        bool MarkDead { get; set; }
    }

    public static class LogicEvents
    {
        public const string AOI_ENTER = "aoi_enter";
        public const string AOI_EXIT = "aoi_exit";
        public const string STATE_CHANGED = "state_changed";
    }

    public abstract class LogicEntityBase : ILogicEntity, IEntityBuffOwner, IEntityAttributeOwner
    {

        public GlobalBuffManager BuffManager
        {
            get { return LogicManager.globalBuffManager; }
        }

        public GameLogicManager LogicManager { get; protected set; }

        public LogicEntityRecord BindingRecord { get; protected set; }

        public long Id { get; protected set; }

        public string CfgId { get; protected set; }
        public abstract EEntityType Type { get; }

        public EFactionId FactionId { get; set; }

        public bool IsActive { get; protected set; } = true;

        public bool MarkDead { get; set; }

        public event Action<Vector2, Vector2> EventOnEntityMove;

        public Vector2 Pos { get; protected set; } = Vector2.zero;
        public Vector2 FaceDir { get; set; } = Vector2.zero;

        public ISceneAbilityViewer? viewer; // 表现层接口

        public string BelongRoomId { get; set; } = string.Empty;

        public LogicEntityBase(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
        {
            this.LogicManager = logicManager;
            this.Id = instId;
            this.CfgId = cfgId;
            this.Pos = orgPos;
            this.FaceDir = bindingRecord.FaceDir;
            this.BelongRoomId = bindingRecord.BelongRoomId;

            BindingRecord = bindingRecord;
        }

        protected AttributeStore attributeStore;

        public virtual void Initialize()
        {
            attributeStore = new(this);

            attributeStore.EvOnStatusAttrChanged += OnStatusAttriChanged;
            attributeStore.EvOnResourceAttrChanged += OnResourceAttriChanged;

            InitAttribute();
        }

        protected virtual void InitAttribute()
        {
            //// 数值类
            //attributeStore.RegisterNumeric("Attack", initialBase: 100);
            //attributeStore.RegisterNumeric("Strength", initialBase: 10);
            //attributeStore.RegisterNumeric("HP.Max", initialBase: 1000);
            //attributeStore.RegisterNumeric("RegenRate.HP", initialBase: 5);

            //// 资源类
            //attributeStore.RegisterResource("HP", "HP.Max", 100);

            //attributeStore.Commit();
        }


        /// <summary>
        /// 对外属性接口
        /// </summary>
        /// <param name="attrId"></param>
        /// <returns></returns>
        public long GetAttr(string attrId)
        {
            return attributeStore.GetAttr(attrId);
        }

        public bool CheckHasState(string attrId)
        {
            return attributeStore.CheckHasState(attrId);
        }
        public void ApplyResourceChange(string resourceId, long delta, bool isDamage, SourceKey? source, Dictionary<string, long> extraAttrs = null)
        {
            attributeStore.ApplyResourceChange(resourceId, delta, isDamage, source, extraAttrs);
        }


        public virtual void OnStatusAttriChanged(string attrId, bool isOn)
        {

        }
        
        public virtual long CalculateResourceCostAmount(ResourceDeltaIntent intent)
        {
            return intent.delta;
        }


        /// <summary>
        /// 属性变化回调
        /// </summary>
        /// <param name="attrId"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="intent"></param>

        public virtual void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            // 4.3 死亡判断窗口：仅在含伤害时检查
            switch (attrId)
            {
                case AttrIdConsts.HP:
                    {
                        if(intent.deltaFlags > 0)
                        {
                            if (intent.srcKey != null && intent.srcKey.Value.entityId != 0)
                            {
                                var dmg = -intent.finalDelta;
                                var entity = LogicManager.GetLogicEntity(intent.srcKey.Value.entityId);
                                var xixue = entity.GetAttr(AttrIdConsts.DamageXiXue);

                                if(intent.extraAttrs != null)
                                {
                                    intent.extraAttrs.TryGetValue(AttrIdConsts.DamageXiXue, out var extraVal);
                                    xixue += extraVal;
                                }

                                if(xixue > 0)
                                {
                                    Debug.Log("吸血 回血 OnResourceAttriChanged");
                                    var xixueVal = (long)(dmg * (double)(xixue / 10000));
                                    entity.ApplyResourceChange(AttrIdConsts.HP, xixueVal, false, new SourceKey() { type = SourceType.Mechanism, entityId = entity.Id});
                                }
                            }
                        }

                        if (before > 0 && after <= 0/* && intent.deltaFlags > 0*/)
                        {
                            OnEntityDie(intent);
                            break;
                        }
                    }
                    break;
            }
        }

        public Modifier AddAttrModifier(SourceKey source, string attrId, long val)
        {
            return attributeStore.AddModifier(source, attrId, val);
        }

        public void ExpireModifierBySource(SourceKey sk)
        {
            attributeStore.ExpireBySource(sk);
        }


        public void UpdateAttrModifier(Modifier m)
        {
            attributeStore.UpdateModifier(m);
        }


        public virtual void OnEntityDie(ResourceDeltaIntent lastIntent)
        {
            LogicManager.AreaManager.RequestEntityDie(this.Id);

            Debug.Log("Unit Entity OnEntityDie dead " + Id);

            LogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
            {
                Ctx = new()
                {
                    SourceEntity = this,
                    HappenPos = this.Pos,
                },
                Name = "Death",
                Param3 = this.Id,
                //Param4 = src != null ? src.Id : 0,
            });

            EventOnDeath?.Invoke();
        }

        public event Action EventOnDeath;

        public void OnEnterAOI()
        {
        }


        public void OnExitAOI()
        {
        }


        public virtual void Tick(float dt) { }

        protected void NotifyStateChanged(object payload)
        {
        }

        public virtual bool Movable()
        {
            return false;
        }

        public void OnSpawn(LogicEntityRecord data)
        {
        }

        public void OnDespawn(out LogicEntityRecord? snapshot)
        {
            snapshot = null;
        }

        public void OnWake()
        {
        }

        public void OnSleep()
        {
        }

        public void SetPosition(Vector2 pos)
        {
            this.Pos = pos;
            // callback 形式
            LogicManager.AreaManager.UpdatePosition(this.Id, pos);
        }


        public List<string> AnimOverrideList { get; protected set; } = new();

        public void AnimOverrideUpdate(string animOverride)
        {
            AnimOverrideList.Add(animOverride);
        }

        public List<ILogicEntity> FindEntityInRange(Vector2 pos, float radius)
        {
            var l = new List<long>();
            LogicManager.AreaManager.UnitGridIndex.Query(pos, radius, l);

            var ret = new List<ILogicEntity>();
            foreach (var id in l)
            {
                var entity = LogicManager.AreaManager.GetLogicEntiy(id);
                ret.Add(entity);
            }
            return ret;
        }

        public bool CheckHasBuff(string buffId)
        {
            foreach(var buff in BuffContainer)
            {
                if(buff.Value.BuffId == buffId) return true;
            }
            return false;
        }

        public virtual void OnMapLogicEvent(in IMapLogicEvent evt)
        {
        }

        public Dictionary<long, BuffInstance> BuffContainer { get; protected set; } = new();
    }

}

