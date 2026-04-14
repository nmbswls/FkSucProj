using cfg.demo;
using Config.Unit;
using My.Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using My.Player.Bag;
using Map.Logic.Events;
using My.Map.Logic;
using My.UI;
using System.Linq;
using My.Map.Entity;
using My.Map.Fight;
using Config;


namespace My.Map
{
    public class LootPointLogicEntity : LogicEntityBase, ILootableObj
    {
        public MapLootPointConfig cacheConfig;

        public int MaxSlots = 12;

        public LootPointLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheConfig = MapLootPointConfigLoader.Get(cfgId);

            var realRec = (LogicEntityRecord4LootPoint)bindingRecord;
            if(!realRec.ItemInitialized)
            {
                int dropId = realRec.DynamicDropId;
                if (dropId == 0)
                {
                    dropId = cacheConfig.DefaultDropId;
                }
                lootContainer = new(logicManager, MaxSlots);
                lootContainer.InitByDropId(dropId);
            }
            else
            {
                lootContainer = new(logicManager, MaxSlots);
                lootContainer.InitByItems(realRec.InnerItems);
            }

        }

        public override EEntityType Type => EEntityType.LootPoint;


        public bool IsLocked = false;

        public class LootContainer : IItemContainer
        {
            public GameLogicManager logicManager;
            private bool LootInialized = false;
            private List<ItemStack> containItems = new List<ItemStack>();
            public int MaxSlots;

            public Dictionary<int, float> ItemSearchProgress = new();

            public LootContainer(GameLogicManager logicManager, int maxSlots)
            {
                this.logicManager = logicManager;
                this.MaxSlots = maxSlots;
            }

            public void InitByDropId(int dropId)
            {
                if (LootInialized)
                {
                    return;
                }

                LootInialized = true;

                for (int i = 0; i < MaxSlots; i++)
                {
                    containItems.Add(null);
                }

                var items = DropUtils.GetBundleDropItems(dropId);
                for (int i = 0; i < items.Count; i++)
                {
                    containItems[i] = ItemCatalog.CreateItemStack(items[i].Item1, items[i].Item2);

                    //var itemConf = FakeItemDatabase.GetIcon();
                    ItemSearchProgress[i] = 1.5f;
                }
            }

            public void InitByItems(List<ItemStack> items)
            {
                if(LootInialized)
                {
                    return;
                }

                this.LootInialized = true;
                this.containItems.Clear();
                for (int i = 0; i < MaxSlots; i++)
                {
                    containItems.Add(null);
                }

                for(int i=0;i<items.Count;i++)
                {
                    containItems[i] = items[i];
                }
            }

            public List<ItemStack> LootItems
            {
                get
                {
                    return containItems;
                }
            }

            public long GetMaxStack(string itemId)
            {
                return ItemCatalog.GetMaxStackByType(itemId, EContainerType.LootPoint);
            }

            public void SetItemData(int slotIdx, ItemStack item)
            {
                if (slotIdx < 0 || slotIdx >= MaxSlots)
                {
                    return;
                }

                containItems[slotIdx] = item;
            }

            public void SetItemCount(int slotIdx, long count)
            {
                if (slotIdx < 0 || slotIdx >= MaxSlots)
                {
                    return;
                }

                if(containItems[slotIdx] == null)
                {
                    return;
                }

                containItems[slotIdx].Count = count;
            }

            public bool IsSlotIdxValid(int slotIdx)
            {
                if(slotIdx < 0 || slotIdx >= MaxSlots)
                {
                    return false;
                }

                ItemSearchProgress.TryGetValue(slotIdx, out var progress);
                if(progress > 0)
                {
                    return false;
                }

                return true;
            }

            public long GetItemCount(string itemId)
            {
                long ret = 0;
                foreach(var item in containItems)
                {
                    if (item == null) continue;
                    ret += item.Count;
                }
                return ret;
            }

            public ItemStack GetItemByIdx(int slotIdx)
            {
                if (slotIdx < 0 || slotIdx >= MaxSlots)
                {
                    return null;
                }

                return containItems[slotIdx];
            }
        }

        protected LootContainer lootContainer;

        public List<ItemStack> LootItems 
        { 
            get 
            {
                return lootContainer.LootItems; 
            } 
        }

        public event Action<LootPointLogicEntity> EventOnLootPointUnlock;
        public event Action<LootPointLogicEntity> EventOnLootPointUsed;
        public event Action<int> EnOnUnrealed;

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
                LogicManager.viewer.ShowFakeFxEffect("???", Pos);
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
                    LogicManager.viewer.ShowFakeFxEffect("????", Pos);
                    return;
                }

                foreach (var oneInfo in cacheConfig.UnlockItemCost)
                {
                    var ret = LogicManager.playerDataManager.CostItem(oneInfo.ItemId, oneInfo.Count);
                }
            }

            this.IsLocked = false;
            LogicManager.viewer.ShowFakeFxEffect("????", Pos);
            EventOnLootPointUnlock?.Invoke(this);
        }

        public void TryUseLootPoint()
        {
            if (IsLocked)
            {
                LogicManager.viewer.ShowFakeFxEffect("????", Pos);
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
                    LogicManager.viewer.ShowFakeFxEffect("????????", Pos);
                    return;
                }
            }

            LogicManager.viewer.ShowFakeFxEffect("???", Pos);
            EventOnLootPointUsed?.Invoke(this);

            // ??????
            if(this.FactionId != Entity.EFactionId.None)
            {
                LogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
                {
                    Ctx = new()
                    {
                        HappenPos = Pos,
                        SourceEntity = LogicManager.playerLogicEntity,
                    },
                    Name = "Loot",
                    Param3 = (long)this.FactionId, // ????????
                    Param5 = this.BelongRoomId, // ????????
                });
            }
            
        }

        public bool CanLoot()
        {
            if(cacheConfig.LootRequiment == null)
            {
                return true;
            }

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
                LogicManager.viewer.ShowFakeFxEffect("????????", Pos);
                return false;
            }

            return true;
        }

        public void TickUnReveal(float dt)
        {
            if(lootContainer.ItemSearchProgress.Count == 0)
            {
                return;
            }

            var minKey = lootContainer.ItemSearchProgress.Keys.Min();
            lootContainer.ItemSearchProgress[minKey] -= dt;

            if(lootContainer.ItemSearchProgress[minKey] <= 0)
            {
                lootContainer.ItemSearchProgress.Remove(minKey);
                EnOnUnrealed?.Invoke(minKey);

                // ???
                var itemStack = lootContainer.LootItems[minKey];
                var conf = ItemCatalog.GetItemDef(itemStack.ItemID);
                if (conf == null)
                {
                    return;
                }

                switch(conf.RevealEffectType)
                {
                    case EItemRevealEffectType.AddGcVal:
                        {
                            int v = conf.RevealP1 != 0 ? (int)conf.RevealP1 : 5000;
                            LogicManager.playerLogicEntity.ApplyResourceChange(AttrIdConsts.PlayerPleasure, v, false, FightStruct.EDmgFlag.None, null);
                        }
                        break;
                    case EItemRevealEffectType.CostClothes:
                        {
                            int v = conf.RevealP1 != 0 ? (int)conf.RevealP1 : 3000;
                            LogicManager.playerLogicEntity.ApplyResourceChange(AttrIdConsts.PlayerClothes, -v, false, FightStruct.EDmgFlag.None, null);
                        }
                        break;
                }
            }


        }


        public int GetCurrUnrealed()
        {
            if (lootContainer.ItemSearchProgress.Count == 0)
            {
                return -1;
            }
            var minKey = lootContainer.ItemSearchProgress.Keys.Min();
            return minKey;
        }

        public bool IsRevealed(int itemIdx)
        {
            lootContainer.ItemSearchProgress.TryGetValue(itemIdx, out var result);
            return result <= 0;
        }

        public void RemoveFromIndex(int index, int count)
        {
            if (index < 0 || index >= lootContainer.LootItems.Count) return;
            var s = lootContainer.LootItems[index];
            if (s == null) return;
            s.RemoveFromStack(count);
            if (s.Count <= 0) lootContainer.LootItems[index] = null;
        }

        public EContainerType GetContainerType()
        {
            return EContainerType.LootPoint;
        }

        public IItemContainer GetLootItemContainer()
        {
            return lootContainer;
        }
    }

    public struct InteractPointState
    {
        public Vector2 Position;
        public bool IsEnabled;
    }

}

