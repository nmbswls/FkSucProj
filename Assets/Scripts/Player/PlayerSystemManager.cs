
using System.Collections.Generic;
using cfg.demo;
using Map.Logic.Events;
using My.Config;
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

        public string[] QuickSlotItemSet = new string[10];

        public string SavedBornPoint = "initial";
        //public string SavedReviveMap = "initial";

        public int Level { get; set; } = 0;

        /// <summary>
        /// 玩家进度（养成等）子系统
        /// </summary>
        public PlayerProgressionSystem ProgressionSystem { get; private set; }

        public PlayerQuestSystem QuestSystem { get; private set; }

        public DialogTriggerSystem DialogTriggerSystem { get; private set; }

        public PlayerFuncOpenSystem FuncOpenSystem { get; private set; }

        public PlayerInventorySystem InventorySystem { get; private set; }

        // RPG Maker 式全局开关（存 PlayerData.GlobalSwitchMap），与地图点位状态语义分离
        public Dictionary<string, bool> GlobalSwitchMap = new();

        public List<string> PlayerSkillList = new() 
        {
            "queen_attack",
            "queen_attack_heavy",
            "queen_dash",
            "queen_shoot",
            "fix_clothes",
            "spawn_attract",
            "queen_pull_all",
            "player_mortar_acquire_01",

            "player_enter_queen",
            "player_quit_queen",

            "queen_dash_down",


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
            "player_mortar_acquire_01",
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

            //VariableDict["fix_teleport"] = true;
            //VariableDict["a1"] = true;

            ProgressionSystem = new();
            QuestSystem = new();
            DialogTriggerSystem = new();
            FuncOpenSystem = new();
            InventorySystem = new();

            QuickSlotItemSet[0] = "feidao";


            NormalSkillSlots[0] = "queen_attack";
            HumanSkillSlots[0] = "default_push";

            NormalSkillSlots[1] = "queen_attack_heavy";

            NormalSkillSlots[2] = "queen_dash";
            HumanSkillSlots[2] = "queen_dash";

            NormalSkillSlots[3] = "queen_dash_down";
            NormalSkillSlots[4] = "queen_pull_all";
            NormalSkillSlots[5] = "player_mortar_acquire_01";

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
            if (savingData != null)
            {
                SaveData.EnsureHydrated(savingData);
            }

            GlobalSwitchMap.Clear();
            if (savingData?.PlayerData?.GlobalSwitchMap != null)
            {
                foreach (var kv in savingData.PlayerData.GlobalSwitchMap)
                {
                    GlobalSwitchMap[kv.Key] = kv.Value;
                }
            }

            Level = savingData.PlayerData.Level;

            ProgressionSystem.InitSystem(logicManager, savingData);
            QuestSystem.InitSystem(logicManager, savingData);
            DialogTriggerSystem.InitSystem(logicManager, savingData);
            FuncOpenSystem.InitSystem(logicManager, savingData);
            InventorySystem.InitSystem(logicManager, savingData);
        }

        public void ApplyRuntimeToSaveData(SaveData data)
        {
            if (data.PlayerData == null)
            {
                data.PlayerData = new PlayerData();
            }

            data.PlayerData.GlobalSwitchMap.Clear();
            foreach (var kv in GlobalSwitchMap)
            {
                data.PlayerData.GlobalSwitchMap[kv.Key] = kv.Value;
            }

            InventorySystem?.WriteWarehouseToSave(data);
        }

        public bool CheckHasParam(string id)
        {
            GlobalSwitchMap.TryGetValue(id, out var val);
            return val;
        }

        public void SetVariable(string id)
        {
            GlobalSwitchMap[id] = true;

            logicManager.LogicEventBus.Publish(new MLEVariableChangeEvent()
            {
                Name = id,
                AfterVal = 1,
            });
        }

        public void InitBagInfo()
        {

            if (CfgMgr.Cfgs == null)
            {
                Debug.LogError("InitBagInfo: CfgMgr.Cfgs is null. Call CfgMgr.LoadGameConfigs before InitPlayerData / OnGameLogicInit.");
                return;
            }

            var mainBag = InventorySystem.MainBag;
            mainBag.NormalSlots[0] = ItemCatalog.CreateItemStack("banana", 2);
            mainBag.NormalSlots[1] = ItemCatalog.CreateItemStack("qiezi", 3);
            mainBag.NormalSlots[2] = ItemCatalog.CreateItemStack("bangbangtang", 3);
            mainBag.NormalSlots[6] = ItemCatalog.CreateItemStack("chanzi", 1);

            mainBag.NormalSlots[12] = ItemCatalog.CreateItemStack("evil_scroll_01", 5);
            
            //inventoryModel.NormalSlots[1] = new ItemStack() { ItemID = "qiezi", Count = 3 };
            //inventoryModel.NormalSlots[2] = new ItemStack() { ItemID = "bangbangtang", Count = 3 };

            //inventoryModel.NormalSlots[6] = new ItemStack() { ItemID = "chanzi", Count = 1 };
        }


        public void Tick(float dt)
        {
            InventorySystem.Tick(dt);

            ProgressionSystem.Tick(dt);
            QuestSystem.Tick(dt);
            DialogTriggerSystem.Tick(dt);
            FuncOpenSystem.Tick(dt);
        }

        public bool CheckHaveItem(string itemId, long count)
        {
            return InventorySystem.CheckHaveItem(itemId, count);
        }

        public long CostItem(string itemId, long count)
        {
            return InventorySystem.CostItem(itemId, count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool CanGainItems(string itemId, long count)
        {
            var itemConf = ItemCatalog.GetItemDef(itemId);
            if (itemConf == null)
            {
                return false;
            }

            if(itemConf.ItemType == EItemType.Currency)
            {
                return true;
            }

            if(InventorySystem.CanGainItems(itemId, count))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试将道具发放到指定背包，返回实际入包数量
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public long TryGiveItem(string itemId, long count, int bagId)
        {
            return InventorySystem.GiveItem(itemId, count, bagId);
        }

        /// <summary>
        /// 根据玩家当前状态返回应显示的技能栏槽位列表
        /// </summary>
        /// <returns></returns>
        public string[] GetSkillSlotsByState()
        {
            var player = logicManager.playerLogicEntity;
            string[] showSkills = null;
            // 发情 / 暴露 / 常态使用不同技能配置
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
        /// 根据状态返回快捷道具栏槽位（当前实现与状态无关，预留扩展）
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

