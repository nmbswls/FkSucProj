using Config.Unit;
using Config;
using UnityEngine;
using My.Map.Logic;
using My.Player.Bag;


namespace My.Map.Entity
{
    public class GatherPointLogicEntity : LogicEntityBase
    {

        public int LeftCount = 0;
        public float LastRefreshTime = 0;

        public GatherPointConfig cacheConfig;

        public GatherPointLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheConfig = GatherPointCfgtLoader.Get(cfgId);

            LeftCount = cacheConfig.MaxCount;
            LastRefreshTime = 0;
        }

        public override EEntityType Type => EEntityType.GatherPoint;

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

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            if(LeftCount < cacheConfig.MaxCount)
            {
                if (LogicTime.time > LastRefreshTime)
                {
                    LeftCount += 1;
                    LastRefreshTime = LogicTime.time;
                }
            }
        }


        /// <summary>
        /// 执行拾取
        /// </summary>
        public void DoGather()
        {
            Debug.Log($"DoGather {Id} ");

            if(LeftCount <= 0)
            {
                return;
            }

            bool beforeMax = false;
            if(LeftCount >= cacheConfig.MaxCount)
            {
                beforeMax = true;
            }
            LeftCount -= 1;
            if(beforeMax && LeftCount < cacheConfig.MaxCount)
            {
                LastRefreshTime = LogicTime.time;
            }

            var dropId = cacheConfig.DropBundleId;
            
            var items = DropUtils.GetBundleDropItems(dropId);

            foreach (var item in items)
            {
                string itemId = item.Item1;
                int cnt = item.Item2;

                long put = 0;
                if(!cacheConfig.GatherOnGround)
                {
                    put = LogicManager.playerDataManager.InventorySystem.MainBag.TryGiveItem(itemId, cnt);
                }
                if(put < cnt)
                {
                    Debug.Log("bag full create drop");
                    LogicManager.globalDropCollection.CreateDrop(item.Item1, cnt - put, Pos + UnityEngine.Random.insideUnitCircle, false, Pos);
                }
            }

        }
    }
}

