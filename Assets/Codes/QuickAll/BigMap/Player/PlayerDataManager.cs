
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
            inventoryModel = new(60);

            inventoryModel.Slots[0] = new ItemStack() { ItemID = "banana", Count = 2 };
            inventoryModel.Slots[1] = new ItemStack() { ItemID = "qiezi", Count = 3 };
            inventoryModel.Slots[2] = new ItemStack() { ItemID = "bangbangtang", Count = 3 };

            inventoryModel.Slots[6] = new ItemStack() { ItemID = "chanzi", Count = 1 };
        }

        public bool CheckHaveItem(string itemId, int count)
        {
            long totalNum = 0;
            foreach(var slot in  inventoryModel.Slots)
            {
                if (slot == null) continue;
                if(slot.ItemID != itemId) { continue; }

                totalNum += slot.Count;
                if(totalNum > count)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CostItem(string itemId, int count)
        {
            if(count <= 0)
            {
                return false;
            }
            foreach (var slot in inventoryModel.Slots)
            {
                if (slot == null) continue;
                if (slot.ItemID != itemId) { continue; }

                if(slot.Count > count)
                {
                    slot.Count -= count;
                    count = 0;
                }
                else
                {
                    count -= slot.Count;
                    slot.Count = 0;
                }

                if(count <= 0)
                {
                    break;
                }
            }

            for (int i=0;i<inventoryModel.Slots.Count;i++)
            {
                if (inventoryModel.Slots[i].Count <= 0)
                {
                    inventoryModel.Slots[i] = null;
                }
            }
             return true;
        }
    }
}

