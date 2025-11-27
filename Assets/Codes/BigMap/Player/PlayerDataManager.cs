
using System.Collections;
using System.Collections.Generic;
using Config;
using My.Player.Bag;
using UnityEngine;

namespace My.Player
{
    public class PlayerDataManager
    {
        public GameLogicManager logicManager;
        public PlayerInventoryModel inventoryModel;

        public Dictionary<string, long> CurrencyBag = new();

        public PlayerDataManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;
        }
        public void InitPlayer()
        {
            InitBagInfo();
        }

        public void InitBagInfo()
        {
            inventoryModel = new();

            inventoryModel.AddItem(0, 0, "banana",2);
            inventoryModel.AddItem(0, 1, "qiezi", 3);
            inventoryModel.AddItem(0, 2, "bangbangtang", 3);
            inventoryModel.AddItem(0, 6, "chanzi", 2);

            //inventoryModel.NormalSlots[1] = new ItemStack() { ItemID = "qiezi", Count = 3 };
            //inventoryModel.NormalSlots[2] = new ItemStack() { ItemID = "bangbangtang", Count = 3 };

            //inventoryModel.NormalSlots[6] = new ItemStack() { ItemID = "chanzi", Count = 1 };
        }

        public bool CheckHaveItem(string itemId, long count)
        {
            long totalNum = 0;

            for(int bagId = 0; bagId <= 4; bagId++)
            {
                var bag = inventoryModel.GetBagById(bagId);
                if (bag == null)
                {
                    continue;
                }

                var bagCount = bag.GetItemCount(itemId);
                totalNum += bagCount;
                if(totalNum >= count)
                {
                    return true;
                }
            }

            CurrencyBag.TryGetValue(itemId, out var currencyVal);
            totalNum += currencyVal;

            if (totalNum >= count)
            {
                return true;
            }
            return false;
        }

        public long CostItem(string itemId, long count)
        {
            if(count <= 0)
            {
                return 0;
            }

            long leftCount = count;
            var itemConf = FakeItemDatabase.GetItem(itemId);
            if(itemConf.ItemType == FakeItemConf.EItemType.Currency)
            {
                CurrencyBag.TryGetValue(itemId, out var itemVal);
                if(itemVal > leftCount)
                {
                    CurrencyBag[itemId] = itemVal - leftCount;
                    leftCount = 0;
                }
                else
                {
                    CurrencyBag[itemId] = 0;
                    leftCount -= itemVal;
                }
            }

            for (int bagId = 0; bagId <= 4; bagId++)
            {
                var bag = inventoryModel.GetBagById(bagId);
                if (bag == null)
                {
                    continue;
                }

                leftCount = bag.TryCostItem(itemId, leftCount);

                if(leftCount <= 0)
                {
                    break;
                }
            }

            return leftCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool CanGainItems(string itemId, long count)
        {
            var itemConf = FakeItemDatabase.GetItem(itemId);
            if(itemConf.ItemType == FakeItemConf.EItemType.Currency)
            {
                return true;
            }

            if(inventoryModel.CanGainItems(itemId, count))
            {
                return true;
            }
            return false;
        }
    }
}

