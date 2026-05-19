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

        EncounterCtrl,
        DynamicSpawner,

        Teleporter,
        SimpleBlock,

        MobNpc = 100,
        FacilityRuin,
        HomeFacility,

        SavePoint,

        /// <summary>
        /// ????????????????UniqName ???
        /// </summary>
        FishingSpot,

        Trap,
    }

    public interface IEntityBuffOwner : IEntityAttributeOwner
    {
        Dictionary<long, BuffInstance> BuffContainer { get; }

        Vector2 Pos { get; }

        IEnumerable<ILogicEntity> FindEntityInRange(Vector2 pos, float radius);

        GlobalBuffManager BuffManager { get; }

        bool CheckHasBuff(string buffId);

        void RegisterBuff(BuffInstance buffInst);
        void RegisterBuffDirect(BuffInstance buffInst);
        void UnregisterBuff(BuffInstance buffInst);
    }

    public interface IEntityAttributeOwner
    {
        long Id { get; }
        long GetAttr(string attrId);
        bool CheckHasState(string attrId);

        void ApplyResourceChange(string resourceId, long delta, bool isEnmity, EDmgFlag flags, long? srcEntityId, Dictionary<string, long> extraAttrs = null, EDmgCategory dmgCat = EDmgCategory.None, Vector2? srcPos = null, Vector2? hitDir = null);

        long CalculateResourceCostAmount(string attrId, ResourceDeltaIntent intent);
        /// <summary>
        /// 添�?��??�修饰�?��?modifier�???
        /// </summary>
        /// <param name="m"></param>
        Modifier AddAttrModifier(ModSourceKey source, string attrId, long val);

        void ExpireModifierBySource(ModSourceKey sk);

        void UpdateAttrModifier(Modifier m);
    }

    public interface IAttributeStoreEnv
    {
        long CalculateResourceCostAmount(string attrId, ResourceDeltaIntent intent);
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

        float OffsetZ { get; }


        long LifeBindEntityId { get; }

        // ???��?��??�??��?��??管�??�驱??
        void OnSpawn(LogicEntityRecord data);    // ???��?�注?? Record
        void OnDespawn(ref LogicEntityRecord? snapshot); // ???��?�可????�???
        void OnWake();   // �? Sleep ????Active ?��???�???恢�? AI�?
        void OnSleep();  // �? Active �???Sleep�?仍�??�保?? Loaded�?
        void Tick(float dt);

        void OnEnterAOI();

        void OnExitAOI();

        event Action<long, Vector2, Vector2> EventOnEntityMove;
        event Action<long> EventOnDestroyed;

        bool MarkDestroyed { get;}

        bool MarkDespawn { get; }

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

    

    public abstract partial class LogicEntityBase : ILogicEntity, IEntityBuffOwner, IEntityAttributeOwner, IWithMotor
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
        public bool IsDirty { get; set; } // ?��?记�???�??��?? Record

        public bool MarkDestroyed { get; set; }

        public event Action<long, Vector2, Vector2> EventOnEntityMove;
        public event Action<long> EventOnDestroyed;
        
        public Vector2 Pos { get; protected set; } = Vector2.zero;

        public float OffsetZ { get; protected set; }

        public ISceneAbilityViewer? viewer; // ?��?�表?��?�???

        public string BelongRoomId { get; set; } = string.Empty;

        public string SrcUniqName { get; set; } = string.Empty;

        /// <summary>
        /// ���ͼ��M���ر����ͣ�Ĭ�ϲ���ʾ
        /// </summary>
        public virtual WorldMapLandmarkKind WorldMapLandmark => WorldMapLandmarkKind.None;

        /// <summary>
        /// ���ͼ����İ�
        /// </summary>
        public virtual string WorldMapLandmarkLabel => string.Empty;

        public float LifeTime;



        #region �?�?�????��?��??�?�?

        /// <summary>
        /// todo�????��?�?�?�??��??�?补�??
        /// </summary>
        public long LifeBindEntityId { get; set; }
        public bool MarkDespawn { get; set; }
        public bool MarkSleep { get; set; }

        #endregion


        #region �?�????��???

        protected HashSet<string> EntityLocalSwitches = new();
        public virtual bool CheckLocalSwitch(string switchName)
        {
            return EntityLocalSwitches.Contains(switchName);
        }

        public virtual void SetLocalSwitch(string switchName, bool isOn)
        {
            if(isOn)
            {
                EntityLocalSwitches.Add(switchName);
            }
            else
            {
                EntityLocalSwitches.Remove(switchName);
            }

            OnLocalSwitchesMutated();
        }

        // 具名 NPC 等在 LocalSwitch 变更时同步到 WorldNpcCharacterPersistRegistry，勿挂到 Record 存盘周期
        protected virtual void OnLocalSwitchesMutated()
        {
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

            if(!string.IsNullOrEmpty(bindingRecord.SrcUniqName))
            {
                this.SrcUniqName = bindingRecord.SrcUniqName;
            }

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
            // ??�?
            var posNow = this.Pos;
            SetPosition(pos);
            EventOnEntityMove?.Invoke(this.Id, posNow, pos);
        }


        protected virtual void InitAttribute()
        {
            //// 示�?�??��?�???
            //attributeStore.RegisterNumeric("Attack", initialBase: 100);
            //attributeStore.RegisterNumeric("Strength", initialBase: 10);
            //attributeStore.RegisterNumeric("HP.Max", initialBase: 1000);
            //attributeStore.RegisterNumeric("RegenRate.HP", initialBase: 5);

            //// 示�?�??��?�???
            //attributeStore.RegisterResource("HP", "HP.Max", 100);

            //attributeStore.Commit();
        }

        protected virtual bool IsMovable()
        {
            return false;
        }

        /// <summary>
        /// 读�?�??��?��?��??
        /// </summary>
        /// <param name="attrId"></param>
        /// <returns></returns>
        public long GetAttr(string attrId)
        {
            return attributeStore.GetAttr(attrId);
        }

        public void DebugGmEnumerateAllAttributes(System.Action<string, long> emit)
        {
            attributeStore?.DebugEnumerateAllAttributes(emit);
        }

        public bool CheckHasState(string attrId)
        {
            return attributeStore.CheckHasState(attrId);
        }
        public void ApplyResourceChange(string resourceId, long delta, bool isEnmity, EDmgFlag flags, long? srcEntityId, Dictionary<string, long> extraAttrs = null, EDmgCategory dmgCat = EDmgCategory.None, Vector2? srcPos = null, Vector2? hitDir = null)
        {
            attributeStore.ApplyResourceChange(resourceId, delta, isEnmity, flags, srcEntityId, extraAttrs, dmgCategory: dmgCat, srcPos: srcPos, hitDir: hitDir);
        }

        /// <summary>
        /// 强�?�设置�?源�??��?��??
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
        /// �?源�??��?????�???
        /// </summary>
        /// <param name="attrId"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="intent"></param>

        public virtual void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
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
        /// �??��?�???毁并请�??��??管�??��???��??
        /// </summary>
        /// <param name="reason"></param>
        public virtual void DoEntityDestroyed(string reason)
        {
            if (MarkDestroyed)
            {
                Debug.Log("DoEntityDestroyed already mark dead");
                return;
            }

            // �??��?��?�?已�??�?
            MarkDestroyed = true;

            // ???��??听�??
            EventOnDestroyed?.Invoke(this.Id);

            LogicManager.AreaManager.RequestEntityDestroy(this.Id, reason);
        }

        /// <summary>
        /// ?��??死亡流�?�?已注??�???
        /// ???��?�?�?死亡�?件�?延�???毁�??
        /// 已�??DoEntityDestroyed / RequestEntityDestroy �??�代??
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

        //    // ?��??�?�?记�?�亡?��??        //    MarkDestroyed = true;
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

        // ?��?档�?��?? Area �??��?�???�??��?��???? Record�?不�?�?�? OnDespawn ??�??��??�???
        public virtual void SyncRecordForPersistence()
        {
            if (LogicManager?.AreaManager?.Repo?.Records.TryGetValue(Id, out var rec) != true)
            {
                return;
            }

            RefreshEntityRecordInfo(rec);
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
            // 位置�??��?��?�? AOI
            if(this.Id != 0)
            {
                LogicManager.AreaManager.UpdatePosition(this.Id, pos);
            }
        }



        public IEnumerable<ILogicEntity> FindEntityInRange(Vector2 pos, float radius)
        {
            return LogicManager.FindEntityInRange(pos, radius);
        }

        public bool CheckHasBuff(string buffId)
        {
            foreach(var buff in BuffContainer)
            {
                if(buff.Value.BuffId == buffId) return true;
            }
            return false;
        }

        public BuffInstance? FindBuffById(string buffId)
        {
            foreach (var buff in BuffContainer)
            {
                if (buff.Value.BuffId == buffId) return buff.Value;
            }
            return null;
        }

        public void RegisterBuff(BuffInstance buffInst)
        {
            BuffContainer.Add(buffInst.InstanceId, buffInst);
            EventOnBuffRegister?.Invoke(buffInst);
            RequestAnimLayerRefreshIfAnimOverrideBuff(buffInst.Def);
        }

        public void RegisterBuffDirect(BuffInstance buffInst)
        {
            BuffContainer[buffInst.InstanceId] = buffInst;
            RequestAnimLayerRefreshIfAnimOverrideBuff(buffInst.Def);
        }

        public void UnregisterBuff(BuffInstance buffInst)
        {
            var def = buffInst.Def;
            BuffContainer.Remove(buffInst.InstanceId);
            EventOnBuffUnregister?.Invoke(buffInst.InstanceId);
            RequestAnimLayerRefreshIfAnimOverrideBuff(def);
        }

        public virtual void OnMapLogicEvent(IMapLogicEvent evt)
        {
        }

        

        public Dictionary<long, BuffInstance> BuffContainer { get; protected set; } = new();
        public event Action<BuffInstance> EventOnBuffRegister;
        public event Action<long> EventOnBuffUnregister;

        
    }

}

