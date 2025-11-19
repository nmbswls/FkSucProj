
using System.Collections;
using System.Collections.Generic;
using My.Player.Bag;
using UnityEngine;

namespace My.Player
{
    public class PlayerDataManager
    {
        public GameLogicManager logicManager;
        public PlayerInventoryModel inventoryModel;

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

        public bool CheckHaveItem(string itemId, int count)
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

            return false;
        }

        public int CostItem(string itemId, int count)
        {
            if(count <= 0)
            {
                return 0;
            }

            int leftCount = count;

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
    }
}

