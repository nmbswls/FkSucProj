
using System.Collections;
using System.Collections.Generic;
using Config;
using Map.Logic.Events;
using My.Player.Bag;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public class PlayerDataManager
    {
        public GameLogicManager logicManager { get;private set; }

        public long ItemInstanceIdCounter = 100;
        public PlayerInventoryModel inventoryModel;

        /// <summary>
        /// 养成
        /// </summary>
        public PlayerProgressionSystem ProgressionSystem { get; private set; }
        /// <summary>
        /// 游戏变量表
        /// </summary>
        public Dictionary<string, bool> VariableDict = new();

        public List<string> PlayerSkillList = new() 
        {
            "queen_attack",
            "default_dash",
            "queen_shoot",
            "fix_clothes",
            "spawn_attract",
            "queen_pull_all",

            "player_enter_queen",
            "player_quit_queen",


            "h_mode_execute",

            "queen_counter",

            "default_push",
            "player_normal_defend",
            "crazy_fire",

            "player_dark_dance"
        };

        public PlayerDataManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;

            VariableDict["fix_teleport"] = true;
            VariableDict["a1"] = true;

            ProgressionSystem = new(logicManager);
        }
        public void InitPlayerData(SaveData savingData)
        {
            InitBagInfo();

            ProgressionSystem.InitializeSystem(savingData);
        }

        public void InitBagInfo()
        {
            inventoryModel = new(this);

            var mainBag = inventoryModel.MainBag;
            mainBag.NormalSlots[0] = FakeItemDatabase.CreateItemStack("banana", 2);
            mainBag.NormalSlots[1] = FakeItemDatabase.CreateItemStack("qiezi", 3);
            mainBag.NormalSlots[2] = FakeItemDatabase.CreateItemStack("bangbangtang", 3);
            mainBag.NormalSlots[6] = FakeItemDatabase.CreateItemStack("chanzi", 1);

            //inventoryModel.NormalSlots[1] = new ItemStack() { ItemID = "qiezi", Count = 3 };
            //inventoryModel.NormalSlots[2] = new ItemStack() { ItemID = "bangbangtang", Count = 3 };

            //inventoryModel.NormalSlots[6] = new ItemStack() { ItemID = "chanzi", Count = 1 };
        }

        public void Tick(float dt)
        {
            inventoryModel.Tick(dt);
        }

        public bool CheckHasParam(string id)
        {
            VariableDict.TryGetValue(id, out var val);
            return val;
        }

        public void SetVariable(string id)
        {
            VariableDict[id] = true;

            // 变量事件
            logicManager.LogicEventBus.Publish(new MLEVariableChangeEvent()
            {
                Name = id,
                AfterVal = 1,
            });
        }
        public bool CheckHaveItem(string itemId, long count)
        {
            return inventoryModel.CheckHaveItem(itemId, count);
        }

        public long CostItem(string itemId, long count)
        {
            return inventoryModel.CostItem(itemId, count);
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

        /// <summary>
        /// 返回值为实际添加数量
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public long TryGiveItem(string itemId, long count, int bagId)
        {
            return inventoryModel.GiveItem(itemId, count, bagId);
        }
    }
}

