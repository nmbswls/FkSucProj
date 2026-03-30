
using System.Collections;
using System.Collections.Generic;
using Config;
using Map.Logic.Events;
using My.Map.Logic;
using My.Player.Bag;
using My.Quest;
using My.Saving;
using UnityEngine;

namespace My.Player
{

    public interface IPlayerSystem
    {
        void InitSystem(GameLogicManager ctx, SaveData savingData);

        void Tick(float dt);
    }

    public class PlayerSystemManager
    {
        public GameLogicManager logicManager { get;private set; }

        public long ItemInstanceIdCounter = 100;
        public PlayerInventoryModel inventoryModel;

        public string[] QuickSlotItemSet = new string[10];

        public string SavedBornPoint = "initial";
        //public string SavedReviveMap = "initial";

        /// <summary>
        /// 养成
        /// </summary>
        public PlayerProgressionSystem ProgressionSystem { get; private set; }

        public PlayerQuestSystem QuestSystem { get; private set; }

        public DialogTriggerSystem DialogTriggerSystem { get; private set; }

        /// <summary>
        /// 
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
            "player_small_staggering",

            "default_push",
            "player_normal_defend",
            "crazy_fire",

            "player_dark_dance",

            "player_ziwei",
            "player_push_surround",
            "player_trace_bullet_01",
        };

        public string[] NormalSkillSlots = new string[8];
        public string[] HumanSkillSlots = new string[8];
        public string[] FaQingSkillSlots = new string[8];

        public class InnerListener : IMapLogicEventHandler
        {
            private PlayerSystemManager systemManager;
            public InnerListener(PlayerSystemManager systemManager)
            {
                this.systemManager = systemManager;
            }

            public void Handle(in IMapLogicEvent evt)
            {
                systemManager.OnLogicEvent(evt);
            }
        }

        private InnerListener innerListener;

        public PlayerSystemManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;

            VariableDict["fix_teleport"] = true;
            VariableDict["a1"] = true;

            ProgressionSystem = new();
            QuestSystem = new();
            DialogTriggerSystem = new();

            QuickSlotItemSet[0] = "feidao";


            NormalSkillSlots[0] = "queen_attack";
            HumanSkillSlots[0] = "default_push";

            NormalSkillSlots[2] = "default_dash";
            NormalSkillSlots[2] = "default_dash";

            NormalSkillSlots[3] = "spawn_attract";
            NormalSkillSlots[4] = "queen_pull_all";

            HumanSkillSlots[3] = "player_small_staggering";
            HumanSkillSlots[4] = "player_dark_dance";
            HumanSkillSlots[5] = "player_push_surround";
            HumanSkillSlots[6] = "player_trace_bullet_01";
            

            FaQingSkillSlots[0] = "player_ziwei";


            innerListener = new(this);
            logicManager.LogicEventBus.Subscribe(EMapLogicEventType.Common, innerListener);
            logicManager.LogicEventBus.Subscribe(EMapLogicEventType.UnitDie, innerListener);
        }

        public void InitPlayerData(SaveData savingData)
        {
            InitBagInfo();

            ProgressionSystem.InitSystem(logicManager, savingData);
            QuestSystem.InitSystem(logicManager, savingData);
            DialogTriggerSystem.InitSystem(logicManager, savingData);
        }

        public void InitBagInfo()
        {
            inventoryModel = new(this);

            var mainBag = inventoryModel.MainBag;
            mainBag.NormalSlots[0] = FakeItemDatabase.CreateItemStack("banana", 2);
            mainBag.NormalSlots[1] = FakeItemDatabase.CreateItemStack("qiezi", 3);
            mainBag.NormalSlots[2] = FakeItemDatabase.CreateItemStack("bangbangtang", 3);
            mainBag.NormalSlots[6] = FakeItemDatabase.CreateItemStack("chanzi", 1);

            mainBag.NormalSlots[12] = FakeItemDatabase.CreateItemStack("evil_scroll_01", 5);
            
            //inventoryModel.NormalSlots[1] = new ItemStack() { ItemID = "qiezi", Count = 3 };
            //inventoryModel.NormalSlots[2] = new ItemStack() { ItemID = "bangbangtang", Count = 3 };

            //inventoryModel.NormalSlots[6] = new ItemStack() { ItemID = "chanzi", Count = 1 };
        }


        public void Tick(float dt)
        {
            inventoryModel.Tick(dt);

            ProgressionSystem.Tick(dt);
            QuestSystem.Tick(dt);
            DialogTriggerSystem.Tick(dt);
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

        /// <summary>
        /// 获取当前状态的技能组
        /// </summary>
        /// <returns></returns>
        public string[] GetSkillSlotsByState()
        {
            var player = logicManager.playerLogicEntity;
            string[] showSkills = null;
            // 启用技能组
            if (player.IsFaQing)
            {
                showSkills = FaQingSkillSlots;
            }
            else
            {
                if (player.IsExposed)
                {
                    showSkills = NormalSkillSlots;
                }
                else
                {
                    showSkills = HumanSkillSlots;
                }
            }
            return showSkills;
        }

        /// <summary>
        /// 获取当前状态的技能组
        /// </summary>
        /// <returns></returns>
        public string[] GetItemSlotsByState()
        {
            var player = logicManager.playerLogicEntity;
            return QuickSlotItemSet;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        public void OnLogicEvent(IMapLogicEvent evt)
        {
            //QuestSystem.OnLogicEvent(evt);

            if(evt is MLEUnitDie deadEvent)
            {
                if(deadEvent.LastIntent == null)
                {
                    return;
                }

                if(deadEvent.LastIntent.srcEntityId != logicManager.playerLogicEntity.Id)
                {
                    return;
                }

                logicManager.AreaManager.Repo.Records.TryGetValue(deadEvent.EntityId, out var logicRecord);
                if(logicRecord == null)
                {
                    return;
                }

                string cfgId = logicRecord.CfgId;
                if(logicRecord.EntityType == Map.EEntityType.Npc)
                {
                    var e = new PlayerKillUnitEvent();
                    e.UnitType = Map.EEntityType.Npc;
                    e.KilledCfgId = cfgId;

                    PlayerEventBus.Publish(e);
                }
                
            }
        }
    }
}

