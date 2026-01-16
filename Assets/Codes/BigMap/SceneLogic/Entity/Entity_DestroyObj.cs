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

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void InitAttribute()
        {
            attributeStore.RegisterNumeric(AttrIdConsts.HP_MAX, initialBase: cacheConfig.HitCount);
            attributeStore.RegisterResource(AttrIdConsts.HP, maxAttrId: AttrIdConsts.HP_MAX, initialCurrent:cacheConfig.HitCount);

            attributeStore.Commit();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (!MarkDestroyed)
            {
                attributeStore.Commit();
            }
        }

        /// <summary>
        /// …À∫¶
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
            DoEntityDestroyed("destroyobj");
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
        }

        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            switch (attrId)
            {
                case AttrIdConsts.HP:
                    {
                        OnDestroyObjOnHit(intent.srcEntityId);

                        if (before > 0 && after <= 0/* && intent.deltaFlags > 0*/)
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

        }
    }
}

