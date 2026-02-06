using Config;
using Map.Logic.Events;
using My.Map.Entity;
using My.Map.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static My.Map.Fight.FightStruct;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace My.Map
{
    public enum EEntityType
    {
        None,
        Player,
        InteractPoint,
        Npc,
        LootPoint,
        AreaEffect,
        DestroyObj,
        GatherPoint,
        AttractPoint,
        PatrolGroup,
        EventGroup,
        HomePlacement,

        EncounterCtrl,
        DynamicSpawner,

        MobNpc = 100,
        FacilityRuin,
    }

    public interface IEntityBuffOwner : IEntityAttributeOwner
    {
        Dictionary<long, BuffInstance> BuffContainer { get; }

        Vector2 Pos { get; }


        List<string> AnimOverrideList { get; }

        List<ILogicEntity> FindEntityInRange(Vector2 pos, float radius);

        GlobalBuffManager BuffManager { get; }

        bool CheckHasBuff(string buffId);

        void RegisterBuff(BuffInstance buffInst);
        void UnregisterBuff(BuffInstance buffInst);
    }

    public interface IEntityAttributeOwner
    {
        long Id { get; }
        long GetAttr(string attrId);
        bool CheckHasState(string attrId);

        void ApplyResourceChange(string resourceId, long delta, bool isEnmity, EDmgFlag flags, long? srcEntityId, Dictionary<string, long> extraAttrs = null);

        long CalculateResourceCostAmount(string attrId, ResourceDeltaIntent intent);
        /// <summary>
        /// 增加modifier
        /// </summary>
        /// <param name="m"></param>
        Modifier AddAttrModifier(ModSourceKey source, string attrId, long val);

        void ExpireModifierBySource(ModSourceKey sk);

        void UpdateAttrModifier(Modifier m);


    }

    public interface ILogicEntity : IEntityBuffOwner, IEntityAttributeOwner
    {
        GameLogicManager LogicManager { get; }

        long Id { get; }

        string CfgId { get; }
        EEntityType Type { get; }

        EFactionId FactionId { get; }
        bool IsActive { get; set; }
        Vector2 Pos { get; }

        long LifeBindEntityId { get; }

        // 生命周期钩子
        void OnSpawn(LogicEntityRecord data);    // 从记录创建完整实例
        void OnDespawn(ref LogicEntityRecord? snapshot); // 输出快照，供下次重建
        void OnWake();   // 从Sleep进入Active，开启AI、感知、昂贵系统
        void OnSleep();  // 从Active降级为Sleep，关闭昂贵系统，保留轻量逻辑
        void Tick(float dt);

        void OnEnterAOI();

        void OnExitAOI();

        event Action<long, Vector2, Vector2> EventOnEntityMove;
        event Action<long> EventOnDestroyed;

        bool MarkDestroyed { get;}

        void OnMapLogicEvent(IMapLogicEvent ev);

        void TeleportTo(Vector2 pos);

        bool CheckLocalSwitch(string switchName);

        void SetLocalSwitch(string switchName, bool isOn);
    }

    public static class LogicEvents
    {
        public const string AOI_ENTER = "aoi_enter";
        public const string AOI_EXIT = "aoi_exit";
        public const string STATE_CHANGED = "state_changed";
    }

    public interface IWithAnim
    {
        void AddAnimLayer(string animName, int layer = 0, int priorirt = 1);

        void RemoveAnimLayer(string animName);
    }

    public abstract partial class LogicEntityBase : ILogicEntity, IEntityBuffOwner, IEntityAttributeOwner, IWithAnim, IWithMotor
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

        public bool IsActive { get; set; } = true;
        public bool IsDirty { get; set; } // 实体脏标记位 寻找时机写回record

        public bool MarkDestroyed { get; set; }

        public event Action<long, Vector2, Vector2> EventOnEntityMove;
        public event Action<long> EventOnDestroyed;
        public event Action EventOnAnimLayerUpdate;
        public event Action<string, int> EventOnAnimPlay;
        public Vector2 Pos { get; protected set; } = Vector2.zero;

        public ISceneAbilityViewer? viewer; // 表现层接口

        public string BelongRoomId { get; set; } = string.Empty;

        public float LifeTime;



        #region 生命周期

        /// <summary>
        /// todo 拆分到外部维护
        /// </summary>
        public long LifeBindEntityId { get; set; }
        public bool MarkDespawn { get; set; }
        public bool MarkSleep { get; set; }

        #endregion


        #region 通用字段

        protected HashSet<string> EntityLocalSwitches = new();
        public bool CheckLocalSwitch(string switchName)
        {
            return EntityLocalSwitches.Contains(switchName);
        }

        public void SetLocalSwitch(string switchName, bool isOn)
        {
            if(isOn)
            {
                EntityLocalSwitches.Add(switchName);
            }
            else
            {
                EntityLocalSwitches.Remove(switchName);
            }
        }

        #endregion

        public LogicEntityBase(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
        {
            this.LogicManager = logicManager;
            this.Id = instId;
            this.CfgId = cfgId;
            this.Pos = orgPos;
            this.BelongRoomId = bindingRecord.BelongRoomId;
            this.FactionId = bindingRecord.FactionId;

            this.IsActive = bindingRecord.Activated;
            this.LifeTime = bindingRecord.LifeTime;
            this.LifeBindEntityId = bindingRecord.LifeBindEntityId;

            BindingRecord = bindingRecord;

            if (bindingRecord.LocalSwitches != null)
            {
                foreach(var oneSwitch in bindingRecord.LocalSwitches)
                {
                    EntityLocalSwitches.Add(oneSwitch);
                }
            }
        }

        protected AttributeStore attributeStore;

        public virtual void Initialize()
        {
            LoadCfg();
            attributeStore = new(this);

            attributeStore.EvOnStatusAttrChanged += OnStatusAttriChanged;
            attributeStore.EvOnResourceAttrChanged += OnResourceAttriChanged;

            if(IsMovable())
            {
                MotorSystem = new(this, LogicManager.navProvider);
            }
            InitAttribute();
        }

        protected virtual void LoadCfg()
        {

        }

        public void TeleportTo(Vector2 pos)
        {
            // 其他
            var posNow = this.Pos;
            SetPosition(pos);
            EventOnEntityMove?.Invoke(this.Id, posNow, pos);
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

        protected virtual bool IsMovable()
        {
            return false;
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
        public void ApplyResourceChange(string resourceId, long delta, bool isEnmity, EDmgFlag flags, long? srcEntityId, Dictionary<string, long> extraAttrs = null)
        {
            attributeStore.ApplyResourceChange(resourceId, delta, isEnmity, flags, srcEntityId, extraAttrs);
        }

        /// <summary>
        /// 一般由系统机制调用
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="newVal"></param>
        public void ForceSetResource(string resourceId, long newVal)
        {
            attributeStore.SetResource(resourceId, newVal);
        }

        public virtual void OnStatusAttriChanged(string attrId, bool isOn)
        {

        }
        
        public virtual long CalculateResourceCostAmount(string attrId, ResourceDeltaIntent intent)
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
        }

        public Modifier AddAttrModifier(ModSourceKey source, string attrId, long val)
        {
            return attributeStore.AddModifier(source, attrId, val);
        }

        public void ExpireModifierBySource(ModSourceKey sk)
        {
            attributeStore.ExpireBySource(sk);
        }


        public void UpdateAttrModifier(Modifier m)
        {
            attributeStore.UpdateModifier(m);
        }

        /// <summary>
        /// 销毁理由：
        /// </summary>
        /// <param name="reason"></param>
        public virtual void DoEntityDestroyed(string reason)
        {
            if (MarkDestroyed)
            {
                Debug.Log("DoEntityDestroyed already mark dead");
                return;
            }

            // 标记已销毁 
            MarkDestroyed = true;

            // 销毁回调
            EventOnDestroyed?.Invoke(this.Id);

            LogicManager.AreaManager.RequestEntityDestroy(this.Id, reason);
        }

        /// <summary>
        /// 处理entity的死亡逻辑
        ///  可能包括状态清理 各种中断等
        ///  死亡后默认5秒后销毁 但view可能已经隐藏
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="lastIntent"></param>
        //public virtual void OnEntityDie(int reason, ResourceDeltaIntent lastIntent = null)
        //{
        //    if(MarkDestroyed)
        //    {
        //        Debug.Log("OnEntityDie already mark dead");
        //        return;
        //    }

        //    // 标记死亡 
        //    MarkDestroyed = true;
        //    MarkDeadTime = LogicTime.time;

        //    //LogicManager.AreaManager.RequestEntityDestroy(this.Id, 1);

        //    Debug.Log("Unit Entity OnEntityDie dead " + Id);

        //    LogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
        //    {
        //        Ctx = new()
        //        {
        //            SourceEntity = this,
        //            HappenPos = this.Pos,
        //        },
        //        Name = "Death",
        //        Param3 = this.Id,
        //        //Param4 = src != null ? src.Id : 0,
        //    });

        //    EventOnDeath?.Invoke(this.Id);
        //}


        public virtual void OnEnterAOI()
        {
        }


        public virtual void OnExitAOI()
        {
        }


        public void Tick(float dt) 
        {
            OnPreTick(dt);

            OnTick(dt);
        }

        protected virtual void OnPreTick(float dt)
        {

        }

        protected virtual void OnTick(float dt)
        {
            if (!MarkDestroyed)
            {
                attributeStore?.Commit();
            }

            TickLifeTime(dt);

            MotorSystem?.Tick(dt);
        }


        protected virtual void TickLifeTime(float dt)
        {
            if (!MarkDestroyed && LifeTime > 0)
            {
                LifeTime -= dt;
                if (LifeTime <= 0)
                {
                    // 
                    DoEntityDestroyed("lifetime");
                }
            }
        }
        protected void NotifyStateChanged(object payload)
        {
        }

        public virtual bool Movable()
        {
            return false;
        }

        public virtual void OnSpawn(LogicEntityRecord data)
        {
            MarkDespawn = false;
        }

        public virtual void OnDespawn(ref LogicEntityRecord snapshot)
        {
            RefreshEntityRecordInfo(snapshot);
            MarkDespawn = true;
        }

        protected virtual void RefreshEntityRecordInfo(LogicEntityRecord input)
        {
            //input.Id = this.Id;
            //input.EntityType = this.en
            input.Position = this.Pos;

            if(MarkDestroyed)
            {
                input.MarkDestroyed = true;
            }
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
            if(this.Id != 0)
            {
                LogicManager.AreaManager.UpdatePosition(this.Id, pos);
            }
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

        public void RegisterBuff(BuffInstance buffInst)
        {
            BuffContainer.Add(buffInst.InstanceId, buffInst);
            EventOnBuffRegister?.Invoke(buffInst);
        }
        public void UnregisterBuff(BuffInstance buffInst)
        {
            BuffContainer.Remove(buffInst.InstanceId);
            EventOnBuffUnregister?.Invoke(buffInst.InstanceId);
        }

        public virtual void OnMapLogicEvent(IMapLogicEvent evt)
        {
        }

        public struct AnimLayerStruct
        {
            public int Layer;
            public string Name;
            public int Priority;
        }

        public List<AnimLayerStruct> AnimLayers { get; set; } = new();

        public virtual void AddAnimLayer(string animName, int layer = 0, int priorirt = 1)
        {
            foreach(var a in AnimLayers)
            {
                if(a.Name == animName)
                {
                    return;
                }
            }

            AnimLayers.Add(new AnimLayerStruct() {
                Layer = layer,
                Name = animName,
                Priority = priorirt
            });

            EventOnAnimLayerUpdate?.Invoke();
        }

        public virtual void RemoveAnimLayer(string animName)
        {
            AnimLayers.RemoveAll(a => a.Name == animName);

            EventOnAnimLayerUpdate?.Invoke();
        }

        public void PlayerAnim(string animName, int layer = 0)
        {
            EventOnAnimPlay?.Invoke(animName, layer);
        }


        public Dictionary<long, BuffInstance> BuffContainer { get; protected set; } = new();
        public event Action<BuffInstance> EventOnBuffRegister;
        public event Action<long> EventOnBuffUnregister;
    }

}

