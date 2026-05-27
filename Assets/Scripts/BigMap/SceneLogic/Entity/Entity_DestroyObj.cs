using Config.Unit;
using Config;
using UnityEngine;
using My.Map.Logic;
using static My.Map.Fight.FightStruct;
using Map.Logic.Events;
using System;


namespace My.Map.Entity
{
    public class DestroyObjLogicEntity : LogicEntityBase
    {

        public MapDestoryObjConfig cacheConfig;
        public DestroyObjLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheConfig = MapDestoryObjCfgtLoader.Get(cfgId);
        }

        public override EEntityType Type => EEntityType.DestroyObj;

        public event Action<long> EventOnHit;
        public event Action<long> EventOnBrack;

        private float _brackTimer;

        public override void Initialize()
        {
            base.Initialize();

            _brackTimer = 0;
        }

        protected override void InitAttribute()
        {
            attributeStore.RegisterNumeric(AttrIdConsts.HP_MAX, initialBase: cacheConfig.HitCount);
            attributeStore.RegisterResource(AttrIdConsts.HP, maxAttrId: AttrIdConsts.HP_MAX, initialCurrent:cacheConfig.HitCount);

            attributeStore.Commit();
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            if(_brackTimer != 0 && LogicTime.time > _brackTimer && !MarkDestroyed)
            {
                DoEntityDestroyed("brack_recycle");
            }
        }

        /// <summary>
        /// 伤害
        /// </summary>
        /// <param name="attrId"></param>
        /// <param name="intent"></param>
        /// <returns></returns>
        public override long CalculateResourceCostAmount(string attrId, ResourceDeltaIntent intent)
        {
            switch(attrId)
            {
                case AttrIdConsts.HP:
                    {
                        if(intent.delta < 0)
                        {
                            return -1;
                        }
                    }
                    break;
            }
            return intent.delta;
        }


        private void OnDestroyObjOnHit(long? hitEntityId)
        {
            EventOnHit?.Invoke(this.Id);
        }

        private void OnDestroyObjBrack()
        {
            if(_brackTimer != 0)
            {
                return;
            }

            _brackTimer = LogicTime.time + 3.0f;

            //DoEntityDestroyed("destroyobj");
            CreateDrop();

            if(cacheConfig.HasOwner && cacheConfig.IsPrecious)
            {
                LogicManager.LogicEventBus.Publish(new MLEObjWithOwnerDestroyedEvent()
                {
                    Ctx = new()
                    {
                        HappenPos = Pos,
                        SourceEntity = LogicManager.playerLogicEntity,

                        IsMapLocal = true,
                        InterestRange = 5.0f,
                    },
                    EntityId = this.Id,
                    ObjCfgId = CfgId,
                    Pos = Pos,
                });
            }

            EventOnBrack?.Invoke(this.Id);
        }

        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            switch (attrId)
            {
                case AttrIdConsts.HP:
                    {
                        OnDestroyObjOnHit(intent.srcEntityId);

                        if (after <= 0/* && intent.deltaFlags > 0*/)
                        {
                            OnDestroyObjBrack();
                            break;
                        }
                    }
                    break;
            }
        }

        public void CreateDrop()
        {
            int dropId = cacheConfig.DropBundleId;
            var dropItems = DropUtils.GetBundleDropItems(dropId);
            if (dropItems != null && dropItems.Count > 0)
            {
                foreach(var dropOne in dropItems)
                {
                    LogicManager.globalDropCollection.CreateDrop(dropOne.Item1, dropOne.Item2, this.Pos + UnityEngine.Random.insideUnitCircle * 0.3f, false, this.Pos);
                }
            }
        }

        public override void OnDespawn(ref LogicEntityRecord snapshot)
        {
            base.OnDespawn(ref snapshot);
        }

        protected override void RefreshEntityRecordInfo(LogicEntityRecord input)
        {
            base.RefreshEntityRecordInfo(input);

            if(_brackTimer > 0)
            {
                MarkDestroyed = true;
            }

            //input.Id = this.Id;
            //input.EntityType = this.en
        }
    }
}

