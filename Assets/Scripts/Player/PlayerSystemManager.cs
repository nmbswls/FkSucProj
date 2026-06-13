
using System;
using System.Collections.Generic;
using cfg.demo;
using Map.Logic.Events;
using My;
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

        void PostInit(PlayerSystemManager owner);

        void Tick(float dt);
    }

    public partial class PlayerSystemManager
    {
        public GameLogicManager logicManager { get;private set; }

        public long ItemInstanceIdCounter = 100;

        public string SavedBornPoint = "initial";

        //public string SavedReviveMap = "initial";

        public int Level { get; set; } = 0;
        public long TotalFallPeopleAmount { get; set; } // 总诱惑人数

        /// <summary>
        /// 玩家进度（养成等）子系统
        /// </summary>
        public PlayerProgressionSystem ProgressionSystem { get; private set; }

        public PlayerQuestSystem QuestSystem { get; private set; }

        public DialogTriggerSystem DialogTriggerSystem { get; private set; }

        public PlayerFuncOpenSystem FuncOpenSystem { get; private set; }

        public PlayerInventorySystem InventorySystem { get; private set; }

        public PlayerSkillSystem SkillSystem { get; private set; }

        public PlayerHumanQuickBarSystem HumanQuickBar { get; private set; }

        public PlayerItemEnchantSystem ItemEnchant { get; private set; }

        public PlayerRuneSystem RuneSystem { get; private set; }

        public PlayerBodyPartSystem BodyPartSystem { get; private set; }

        public PlayerEquipmentManager EquipmentManager { get; private set; }

        public RumorIntelSystem RumorIntel { get; } = new();

        public IReadOnlyList<string> PlayerSkillList => SkillSystem.LearnedSkillIdsView;

        readonly List<string> _registeredSkillIdScratch = new();

        // RPG Maker 式全局开关（存 PlayerData.GlobalSwitchMap），与地图点位状态语义分离
        public Dictionary<string, bool> GlobalSwitchMap = new();

        public PlayerMagicClothesManager MagicClothes { get; private set; }

        public bool HasTempSkill => SkillSystem.HasTempSkill;

        public string GetTempSkillId() => SkillSystem.GetTempSkillId();

        public void GrantTempSkill(string skillId, float durationSec = 0f)
        {
            SkillSystem.GrantTempSkill(skillId, durationSec);
        }

        public void ClearTempSkill()
        {
            SkillSystem.ClearTempSkill();
        }

        public bool ConsumeTempSkillIfMatch(string usedSkillId)
        {
            return SkillSystem.ConsumeTempSkillIfMatch(usedSkillId);
        }

        public bool IsTempSkill(string skillId)
        {
            return SkillSystem.IsTempSkill(skillId);
        }

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
            HumanQuickBar = new PlayerHumanQuickBarSystem(this);
            ItemEnchant = new PlayerItemEnchantSystem(this);
            RuneSystem = new PlayerRuneSystem(this);
            BodyPartSystem = new PlayerBodyPartSystem(this);

            MagicClothes = new PlayerMagicClothesManager(this);

            HumanSkillSlots[0] = "default_push";
            HumanSkillSlots[1] = "force_dash_push_down";

            
            HumanSkillSlots[2] = "queen_dash";

            HumanSkillSlots[3] = "player_summon_ally_turret";
            //HumanSkillSlots[3] = "player_small_staggering";
            HumanSkillSlots[4] = "player_dark_dance";
            //HumanSkillSlots[5] = "player_push_surround";
            HumanSkillSlots[5] = "player_summon_ally_turret";

            HumanSkillSlots[6] = "player_trace_bullet_01";





            FaQingSkillSlots[0] = "player_fq_normal_ziwei";
            FaQingSkillSlots[2] = "player_fq_dash_assult";

            FaQingSkillSlots[3] = "player_fq_crazy_ziwei";
            FaQingSkillSlots[4] = "player_fq_hit_hop";
            FaQingSkillSlots[5] = "player_fq_hit_breast";

            innerListener = new(this);
            logicManager.LogicEventBus.Subscribe(EMapLogicEventType.Common, innerListener);
            logicManager.LogicEventBus.Subscribe(EMapLogicEventType.UnitDie, innerListener);
        }

        public void InitPlayerData(SaveData savingData)
        {
            PreparePlayerSaveContext(savingData);
            InitPlayerSystems(savingData);
            AssemblePlayerSystems();
        }

        void PreparePlayerSaveContext(SaveData savingData)
        {
            if (savingData != null)
            {
                SaveData.EnsureHydrated(savingData);
                SaveData.SyncItemInstanceIdCounterFromSave(savingData);
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
        }

        void InitPlayerSystems(SaveData savingData)
        {
            ProgressionSystem.InitSystem(logicManager, savingData);
            BodyPartSystem.InitSystem(logicManager, savingData);
            QuestSystem.InitSystem(logicManager, savingData);
            DialogTriggerSystem.InitSystem(logicManager, savingData);
            FuncOpenSystem.InitSystem(logicManager, savingData);
            InventorySystem.InitSystem(logicManager, savingData);
            SkillSystem.InitSystem(logicManager, savingData);
            HumanQuickBar.InitSystem(logicManager, savingData);
            ItemEnchant.InitSystem(logicManager, savingData);
            RuneSystem.InitSystem(logicManager, savingData);
            MagicClothes.LoadFromSave(savingData?.PlayerData);

            EquipmentManager = new PlayerEquipmentManager(this);
            EquipmentManager.InitializeFromSave(savingData);
            RumorIntel.InitSystem(logicManager, savingData);
        }

        void AssemblePlayerSystems()
        {
            foreach (var system in EnumeratePlayerSystems())
            {
                system.PostInit(this);
            }

            EquipmentManager.PostInit();
        }

        IEnumerable<IPlayerSystem> EnumeratePlayerSystems()
        {
            yield return ProgressionSystem;
            yield return BodyPartSystem;
            yield return QuestSystem;
            yield return DialogTriggerSystem;
            yield return FuncOpenSystem;
            yield return InventorySystem;
            yield return SkillSystem;
            yield return HumanQuickBar;
            yield return ItemEnchant;
            yield return RuneSystem;
            yield return RumorIntel;
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

            InventorySystem?.WriteMainBagToSave(data);
            InventorySystem?.WriteWarehouseToSave(data);
            SkillSystem?.WriteToSave(data);
            MagicClothes.SaveTo(data.PlayerData);
            ProgressionSystem?.TalentManager?.SaveTo(data.PlayerData);
            EquipmentManager?.SaveTo(data.PlayerData);
            BodyPartSystem?.WriteToSave(data.PlayerData);
            RumorIntel.SaveTo(data.PlayerData);


            HumanQuickBar.WriteToSave(data.PlayerData);
            RuneSystem?.WriteToSave(data.PlayerData);

            data.PlayerData.FuncOpenList ??= new List<EFuncOpenType>();
            data.PlayerData.FuncOpenList.Clear();
            if (FuncOpenSystem?.FuncOpenSet != null)
            {
                foreach (var f in FuncOpenSystem.FuncOpenSet)
                {
                    data.PlayerData.FuncOpenList.Add(f);
                }
            }
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

            SceneAOIManager.Instance?.RequestVisibleChunkRefresh();
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
            mainBag.NormalSlots[8] = ItemCatalog.CreateItemStack(My.Map.Fight.FightEffectConstants.StunTrapItemId, 3);

            //inventoryModel.NormalSlots[1] = new ItemStack() { ItemID = "qiezi", Count = 3 };
            //inventoryModel.NormalSlots[2] = new ItemStack() { ItemID = "bangbangtang", Count = 3 };

            //inventoryModel.NormalSlots[6] = new ItemStack() { ItemID = "chanzi", Count = 1 };
        }


        public bool TrySelectMagicClothesForStealthEntry(string defId)
        {
            if (logicManager == null || logicManager.PlayerHumanMode)
            {
                return false;
            }

            return MagicClothes.TrySelectAndLock(defId, logicManager.playerLogicEntity);
        }

        public bool NamedNpcHasLocalSwitch(string characterKey, string switchName)
        {
            return logicManager?.worldPersistState?.NpcCharacters?.ContainsRuntimeLocalSwitch(characterKey, switchName) ?? false;
        }

        public void SetNamedNpcLocalSwitch(string characterKey, string switchName, bool isOn)
        {
            logicManager?.worldPersistState?.NpcCharacters?.SetRuntimeLocalSwitch(characterKey, switchName, isOn);
        }

        public void Tick(float dt)
        {
            InventorySystem.Tick(dt);

            ProgressionSystem.Tick(dt);
            QuestSystem.Tick(dt);
            DialogTriggerSystem.Tick(dt);
            FuncOpenSystem.Tick(dt);
            RumorIntel.Tick(dt);
            SkillSystem.Tick(dt);
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
        public long GiveItemToPlayer(string itemId, long count)
        {
            return InventorySystem.GiveItemToPlayer(itemId, count);
        }

        public bool IsUsingFaQingSkillBar()
        {
            return logicManager?.playerLogicEntity != null && logicManager.playerLogicEntity.IsFaQing;
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
                bool exposedForGameplay = !logicManager.PlayerHumanMode && player.IsExposed;
                if (exposedForGameplay)
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

        public void CollectRegisteredSkillIdsForEntity(List<string> outIds)
        {
            outIds.Clear();

            foreach (var skillId in SkillSystem.InnateSkillIdsView)
            {
                if (!string.IsNullOrEmpty(skillId) && !outIds.Contains(skillId))
                {
                    outIds.Add(skillId);
                }
            }

            foreach (var e in SkillSystem.grantedActives)
            {
                if (e == null || string.IsNullOrEmpty(e.SkillId) || outIds.Contains(e.SkillId))
                {
                    continue;
                }

                outIds.Add(e.SkillId);
            }

            foreach (var e in SkillSystem.grantedPassives)
            {
                if (e == null || string.IsNullOrEmpty(e.SkillId) || outIds.Contains(e.SkillId))
                {
                    continue;
                }

                outIds.Add(e.SkillId);
            }

            foreach (var skillId in SkillSystem.LearnedSkillIdsView)
            {
                if (string.IsNullOrEmpty(skillId))
                {
                    continue;
                }

                var cfg = SkillLibrary.GetSkillConfig(skillId);
                if (cfg != null && cfg.IsPassive)
                {
                    continue;
                }

                if (!outIds.Contains(skillId))
                {
                    outIds.Add(skillId);
                }
            }

            for (int i = 0; i < SkillSystem.PassiveSkillSlots.Length; i++)
            {
                var id = SkillSystem.PassiveSkillSlots[i];
                if (!string.IsNullOrEmpty(id) && SkillSystem.IsSkillLearned(id) && !outIds.Contains(id))
                {
                    outIds.Add(id);
                }
            }

            if (logicManager != null && logicManager.IsHumanQuickBarAvailable())
            {
                var weaponSkill = HumanQuickBar.GetActiveWeaponSkillId();
                if (!string.IsNullOrEmpty(weaponSkill) && !outIds.Contains(weaponSkill))
                {
                    outIds.Add(weaponSkill);
                }
            }

            if (IsUsingFaQingSkillBar())
            {
                foreach (var id in FaQingSkillSlots)
                {
                    if (!string.IsNullOrEmpty(id) && !outIds.Contains(id))
                    {
                        outIds.Add(id);
                    }
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

            CollectRegisteredSkillIdsForEntity(_registeredSkillIdScratch);
            player.ReconcileSkillsWithLearnedList(_registeredSkillIdScratch);
            ApplyLearnedPassiveBuffLayersToPlayerEntity();
        }

        public void ApplyLearnedPassiveBuffLayersToPlayerEntity()
        {
            var player = logicManager?.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            var applied = new HashSet<string>(StringComparer.Ordinal);

            foreach (var e in SkillSystem.grantedPassives)
            {
                if (e == null || string.IsNullOrEmpty(e.SkillId))
                {
                    continue;
                }

                var skillId = e.SkillId;
                var cfg = SkillLibrary.GetSkillConfig(skillId);
                if (cfg == null || !SkillPassiveBuffUtil.HasPassiveBuffs(cfg))
                {
                    continue;
                }

                int lvl = PlayerSkillSystem.ClampPassiveBuffLayer(skillId, e.Level);
                player.TrySetPassiveSkillBuffLayer(skillId, lvl);
                applied.Add(skillId);
            }

            for (int i = 0; i < SkillSystem.PassiveSkillSlots.Length; i++)
            {
                var skillId = SkillSystem.PassiveSkillSlots[i];
                if (string.IsNullOrEmpty(skillId) || !SkillSystem.IsSkillLearned(skillId))
                {
                    continue;
                }

                if (applied.Contains(skillId))
                {
                    Debug.LogWarning("Passive skill in both grant and slot: " + skillId + ", grant wins.");
                    continue;
                }

                var cfg = SkillLibrary.GetSkillConfig(skillId);
                if (cfg == null || !SkillPassiveBuffUtil.HasPassiveBuffs(cfg))
                {
                    continue;
                }

                int lvl = PlayerSkillSystem.ClampPassiveBuffLayer(skillId, SkillSystem.GetSkillLevel(skillId));
                player.TrySetPassiveSkillBuffLayer(skillId, lvl);
                applied.Add(skillId);
            }

            if (RuneSystem != null)
            {
                var runePassiveScratch = new List<string>();
                RuneSystem.CollectEquippedPassiveSkillIds(applied, runePassiveScratch);
                foreach (var skillId in runePassiveScratch)
                {
                    if (applied.Contains(skillId))
                    {
                        continue;
                    }

                    var cfg = SkillLibrary.GetSkillConfig(skillId);
                    if (cfg == null || !SkillPassiveBuffUtil.HasPassiveBuffs(cfg))
                    {
                        continue;
                    }

                    player.TrySetPassiveSkillBuffLayer(skillId, 1);
                    applied.Add(skillId);
                }
            }
        }

        public bool TryGrantRune(string runeId)
        {
            if (RuneSystem == null || !RuneSystem.TryGrantRune(runeId))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryUnlockRuneUpgrade(string upgradeId)
        {
            if (RuneSystem == null || !RuneSystem.TryUnlockUpgrade(upgradeId))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryEquipRune(cfg.demo.ERuneEquipSlot slot, string runeId)
        {
            if (RuneSystem == null || !RuneSystem.TryEquip(slot, runeId))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryUnequipRune(cfg.demo.ERuneEquipSlot slot)
        {
            if (RuneSystem == null || !RuneSystem.TryUnequip(slot))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryAddSkillLearnedSkill(string skillId, int level = 1)
        {
            if (string.IsNullOrEmpty(skillId) || SkillSystem.IsSkillLearned(skillId))
            {
                return false;
            }

            if (!SkillSystem.TryAddSkillLearned(skillId, level))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool CanLearnSkillFromEntry(int entryId, out string reason)
        {
            reason = null;
            var entry = SkillLearnCatalog.TryGetLearnEntry(entryId);
            if (entry == null)
            {
                reason = "no_entry";
                return false;
            }

            if (SkillSystem.IsSkillLearned(entry.SkillId))
            {
                reason = "already_learned";
                return false;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                reason = "no_player";
                return false;
            }

            if (!glm.CheckCommonCondsAll(entry.LearnConds))
            {
                reason = "cond_fail";
                return false;
            }

            return true;
        }

        public bool TryLearnSkillFromEntry(int entryId, out string reason)
        {
            if (!CanLearnSkillFromEntry(entryId, out reason))
            {
                return false;
            }

            var entry = SkillLearnCatalog.TryGetLearnEntry(entryId);
            if (entry == null)
            {
                reason = "no_entry";
                return false;
            }

            if (!TryConsumeLearnConds(entry.LearnConds, out reason))
            {
                return false;
            }

            if (!TryAddSkillLearnedSkill(entry.SkillId, entry.SkillLevel > 0 ? entry.SkillLevel : 1))
            {
                reason = "add_failed";
                return false;
            }

            reason = null;
            return true;
        }

        // 学习时扣除 learn_conds 中的 OwnItem 消耗（条件已在 CanLearn 中校验过）
        bool TryConsumeLearnConds(System.Collections.Generic.IReadOnlyList<cfg.demo.CommonCheckCond> conds, out string reason)
        {
            reason = null;
            if (conds == null || conds.Count == 0)
            {
                return true;
            }

            foreach (var cond in conds)
            {
                if (cond == null || cond.Type != cfg.demo.ECommonCheckType.OwnItem)
                {
                    continue;
                }

                string itemId = cond.Param5;
                long count = cond.Param1;
                if (string.IsNullOrEmpty(itemId) || count <= 0)
                {
                    continue;
                }

                if (CostItem(itemId, count) < count)
                {
                    reason = "cost_fail";
                    return false;
                }
            }

            return true;
        }

        public bool TryAddLearnedSkill(string skillId) => TryAddSkillLearnedSkill(skillId);

        public bool TryGrantPassiveSkill(string skillId, int level)
        {
            if (!SkillSystem.TryGrantPassive(skillId, level))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryGrantActiveSkill(string skillId, int level)
        {
            if (!SkillSystem.TryGrantActive(skillId, level))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryRevokePassiveSkill(string skillId)
        {
            if (!SkillSystem.TryRevokePassive(skillId))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryRevokeActiveSkill(string skillId)
        {
            if (!SkillSystem.TryRevokeActive(skillId))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryRemoveLearnedSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            if (!SkillSystem.TryRemoveLearned(skillId))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        public bool TryReplaceLearnedSkill(string oldSkillId, string newSkillId)
        {
            if (string.IsNullOrEmpty(newSkillId))
            {
                return false;
            }

            if (!SkillSystem.TryReplaceLearnedSkill(oldSkillId, newSkillId))
            {
                return false;
            }

            SyncLearnedSkillsToPlayerEntity();
            return true;
        }

        // 入口：替换已学技能（列表去重、槽位同步、实体 Unregister/Register 与被动 Buff）
        public bool TryReplaceSkill(string oldSkillId, string newSkillId) =>
            TryReplaceLearnedSkill(oldSkillId, newSkillId);

        // 入口：更新已学技能在本实体上的能力附加参数（不修改 Luban 表内 AbilityExtra）
        public bool TryUpdateLearnedSkillAttachedAttributes(string skillId, IReadOnlyDictionary<string, string> updates)
        {
            if (string.IsNullOrEmpty(skillId) || updates == null || updates.Count == 0)
            {
                return false;
            }

            if (!SkillSystem.IsSkillLearned(skillId))
            {
                return false;
            }

            var player = logicManager?.playerLogicEntity;
            return player != null && player.TryUpdateSkillAttachedAttributes(skillId, updates);
        }

        // 入口：已学技能等级（存档 + 被动 Buff 层同步；主动技能仅存档）
        public bool TrySetLearnedSkillLevel(string skillId, int level)
        {
            if (!SkillSystem.IsSkillLearned(skillId) || level < 1)
            {
                return false;
            }

            int effective = level;
            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg != null && SkillPassiveBuffUtil.HasPassiveBuffs(cfg))
            {
                effective = SkillPassiveBuffUtil.ClampLayerForAllPassiveBuffs(cfg, effective);
            }

            if (!SkillSystem.TrySetSkillLevel(skillId, effective))
            {
                return false;
            }

            if (cfg != null && SkillPassiveBuffUtil.HasPassiveBuffs(cfg)
                && SkillSystem.IsPassiveEquipped(skillId))
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

