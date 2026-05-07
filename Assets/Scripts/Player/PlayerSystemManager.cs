
using System;
using System.Collections.Generic;
using cfg.demo;
using Map.Logic.Events;
using My.Config;
using My.Map.Entity;
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

        public PlayerSkillSystem SkillSystem { get; private set; }

        public List<string> PlayerSkillList => SkillSystem.learnedSkillIds;

        public string[] NormalSkillSlots => SkillSystem.NormalSkillSlots;

        // RPG Maker 式全局开关（存 PlayerData.GlobalSwitchMap），与地图点位状态语义分离
        public Dictionary<string, bool> GlobalSwitchMap = new();

        public PlayerMagicClothesManager MagicClothes { get; private set; }

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
            SkillSystem = new PlayerSkillSystem();

            MagicClothes = new PlayerMagicClothesManager(this);

            QuickSlotItemSet[0] = "feidao";

            HumanSkillSlots[0] = "default_push";

            HumanSkillSlots[2] = "queen_dash";

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
            SkillSystem.InitSystem(logicManager, savingData);
            MagicClothes.LoadFromSave(savingData?.PlayerData);
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
            SkillSystem?.WriteToSave(data);
            MagicClothes.SaveTo(data.PlayerData);
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


        public bool TrySelectMagicClothesForStealthEntry(string defId)
        {
            return MagicClothes.TrySelectAndLock(defId, logicManager?.playerLogicEntity);
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
                    showSkills = SkillSystem.NormalSkillSlots;
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

        public void SyncLearnedSkillsToPlayerEntity()
        {
            var player = logicManager?.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            player.ReconcileSkillsWithLearnedList(PlayerSkillList);
            ApplyLearnedPassiveBuffLayersToPlayerEntity();
        }

        public void ApplyLearnedPassiveBuffLayersToPlayerEntity()
        {
            var player = logicManager?.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            foreach (var skillId in SkillSystem.learnedSkillIds)
            {
                if (string.IsNullOrEmpty(skillId))
                {
                    continue;
                }

                var cfg = SkillLibrary.GetSkillConfig(skillId);
                if (cfg == null || !cfg.IsPassive || string.IsNullOrEmpty(cfg.PassiveBuffId))
                {
                    continue;
                }

                int lvl = SkillSystem.GetSkillLevel(skillId);
                player.TrySetPassiveSkillBuffLayer(skillId, lvl);
            }
        }

        public bool TryAddLearnedSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || SkillSystem.IsLearned(skillId))
            {
                return false;
            }

            SkillSystem.learnedSkillIds.Add(skillId);
            SkillSystem.OnSkillLearned(skillId);
            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryRemoveLearnedSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            int removed = SkillSystem.learnedSkillIds.RemoveAll(id =>
                string.Equals(id, skillId, StringComparison.Ordinal));
            if (removed == 0)
            {
                return false;
            }

            SkillSystem.OnSkillForgotten(skillId);
            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryReplaceLearnedSkill(string oldSkillId, string newSkillId)
        {
            if (string.IsNullOrEmpty(newSkillId))
            {
                return false;
            }

            int removed = 0;
            for (int i = SkillSystem.learnedSkillIds.Count - 1; i >= 0; i--)
            {
                if (string.Equals(SkillSystem.learnedSkillIds[i], oldSkillId, StringComparison.Ordinal))
                {
                    SkillSystem.learnedSkillIds.RemoveAt(i);
                    removed++;
                }
            }

            if (removed == 0)
            {
                return false;
            }

            if (!SkillSystem.IsLearned(newSkillId))
            {
                SkillSystem.learnedSkillIds.Add(newSkillId);
            }

            SkillSystem.OnReplaceSkillId(oldSkillId, newSkillId);
            SkillSystem.ReplaceSkillIdInNormalSlots(oldSkillId, newSkillId);
            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        // 入口：替换已学技能（列表去重、槽位同步、实体 Unregister/Register 与被动 Buff）
        public bool TryReplaceSkill(string oldSkillId, string newSkillId) =>
            TryReplaceLearnedSkill(oldSkillId, newSkillId);

        // 入口：更新已学技能在本实体上的能力附加参数（不改全局 SkillLibrary）
        public bool TryUpdateLearnedSkillAttachedAttributes(string skillId, IReadOnlyDictionary<string, string> updates)
        {
            if (string.IsNullOrEmpty(skillId) || updates == null || updates.Count == 0)
            {
                return false;
            }

            if (!SkillSystem.IsLearned(skillId))
            {
                return false;
            }

            var player = logicManager?.playerLogicEntity;
            return player != null && player.TryUpdateSkillAttachedAttributes(skillId, updates);
        }

        // 入口：已学技能等级（存档 + 被动 Buff 层同步；主动技能仅存档）
        public bool TrySetLearnedSkillLevel(string skillId, int level)
        {
            if (!SkillSystem.IsLearned(skillId) || level < 1)
            {
                return false;
            }

            int effective = level;
            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg != null && cfg.IsPassive && !string.IsNullOrEmpty(cfg.PassiveBuffId))
            {
                BuffDefinition def = BuffLibrary.GetBuffDefinition(cfg.PassiveBuffId);
                if (def != null && def.MaxStackLayer > 0)
                {
                    effective = Math.Min(effective, def.MaxStackLayer);
                }
            }

            if (!SkillSystem.TrySetSkillLevel(skillId, effective))
            {
                return false;
            }

            if (cfg != null && cfg.IsPassive && !string.IsNullOrEmpty(cfg.PassiveBuffId))
            {
                var player = logicManager?.playerLogicEntity;
                if (player != null)
                {
                    player.TrySetPassiveSkillBuffLayer(skillId, effective);
                }
            }

            return true;
        }

        // 入口：被动专用；须为被动技能，写入等级并刷新 Buff 层
        public bool TrySetLearnedPassiveSkillBuffLayer(string skillId, int level)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg == null || !cfg.IsPassive)
            {
                return false;
            }

            return TrySetLearnedSkillLevel(skillId, level);
        }
    }
}

