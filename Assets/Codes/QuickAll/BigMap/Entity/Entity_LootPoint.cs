using Config.Unit;
using Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using static UnityEditor.Progress;
using My.Player.Bag;
using My.Map.Logic.Chunk;


namespace My.Map
{
    public class LootPointLogicEntity : LogicEntityBase, ILootableObj
    {
        public MapLootPointConfig cacheConfig;

        public string DropId;
        public LootPointLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheConfig = MapLootPointConfigLoader.Get(cfgId);

            var realRec = (LogicEntityRecord4LootPoint)bindingRecord;
            DropId = realRec.DynamicDropId;
            if(string.IsNullOrEmpty(DropId))
            {
                DropId = cacheConfig.DefaultDropId;
            }
        }

        public override EEntityType Type => EEntityType.LootPoint;


        public bool LootInialized = false;
        public bool IsLocked = false;

        public Dictionary<int, float> ItemSearchProgress = new();

        private List<ItemStack> containItems = new List<ItemStack>();
        public List<ItemStack> LootItems { get {

                if (!LootInialized)
                {
                    LootInialized = true;

                    var items = LogicManager.DropTable.GetBundleDropItems(DropId);
                    foreach (var item in items)
                    {
                        containItems.Add(new ItemStack()
                        {
                            ItemID = item.Item1,
                            Count = item.Item2
                        });
                    }
                }
                return containItems; 
            
            } }

        public event Action<LootPointLogicEntity> EventOnLootPointUnlock;
        public event Action<LootPointLogicEntity> EventOnLootPointUsed;
        public override void Initialize()
        {
            base.Initialize();

            if (cacheConfig.DefaultLocked)
            {
                IsLocked = true;
            }
        }

        public void TryUnlockLootPoint()
        {
            if (!IsLocked)
            {
                LogicManager.viewer.ShowFakeFxEffect("没锁", Pos);
                return;
            }

            if (cacheConfig.UnlockItemCost != null)
            {
                bool enough = true;
                foreach (var oneInfo in cacheConfig.UnlockItemCost)
                {
                    if (!LogicManager.playerDataManager.CheckHaveItem(oneInfo.ItemId, oneInfo.Count))
                    {
                        enough = false;
                    }
                }

                if (!enough)
                {
                    LogicManager.viewer.ShowFakeFxEffect("不够", Pos);
                    return;
                }

                foreach (var oneInfo in cacheConfig.UnlockItemCost)
                {
                    var ret = LogicManager.playerDataManager.CostItem(oneInfo.ItemId, oneInfo.Count);
                }
            }

            this.IsLocked = false;
            LogicManager.viewer.ShowFakeFxEffect("解了", Pos);
            EventOnLootPointUnlock?.Invoke(this);
        }

        public void TryUseLootPoint()
        {
            if (IsLocked)
            {
                LogicManager.viewer.ShowFakeFxEffect("锁了", Pos);
                return;
            }

            

            if (cacheConfig.LootRequiment != null)
            {
                bool match = true;
                switch (cacheConfig.LootRequiment.ReqType)
                {
                    case MapLootPointConfig.ELootReqType.HoldItem:
                        {
                            if (!LogicManager.playerDataManager.CheckHaveItem(cacheConfig.LootRequiment.Param3, cacheConfig.LootRequiment.Param1))
                            {
                                match = false;
                            }
                        }
                        break;
                }


                if (!match)
                {
                    LogicManager.viewer.ShowFakeFxEffect("条件不足", Pos);
                    return;
                }
            }

            LogicManager.viewer.ShowFakeFxEffect("搜！", Pos);
            EventOnLootPointUsed?.Invoke(this);
        }

        public void Add(ItemStack s)
        {
            if (s != null && !s.IsEmpty)
                containItems.Add(new ItemStack(s.ItemID, s.Count));
        }

        public void RemoveFromIndex(int index, int count)
        {
            if (index < 0 || index >= containItems.Count) return;
            var s = containItems[index];
            if (s == null) return;
            s.RemoveFromStack(count);
            if (s.Count <= 0) containItems.RemoveAt(index);
        }

        public void UpdateSearchProgress(int idx, float addTime)
        {

        }
    }

    public struct InteractPointState
    {
        public Vector2 Position;
        public bool IsEnabled;
    }

}

