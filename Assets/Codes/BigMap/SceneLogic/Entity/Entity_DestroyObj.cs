using Config.Unit;
using Config;
using UnityEngine;
using My.Map.Logic;


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

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void InitAttribute()
        {
            attributeStore.RegisterNumeric("HP.Max", initialBase: 100);
            attributeStore.RegisterResource("HP", "HP.Max", 100);

            attributeStore.Commit();
        }

        public void DoDrop()
        {

        }
    }
}

