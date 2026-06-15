using System;
using System.Collections.Generic;
using cfg.demo;
using My;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using Newtonsoft.Json;
using UnityEngine;

namespace My.Saving
{
    [Serializable]
    public class OpenWorldReturnBookmark
    {
        public string MapId;
        public Vector2 Pos;
    }

    [Serializable]
    public class BuffLayerPersistEntry
    {
        public float RemainingLifetime;
        public long CasterEntityId;
        public long SrcBuffId;
    }

    [Serializable]
    public class BuffPersistData
    {
        public string BuffId;
        public int Layer;
        public float RemainingLifetime;
        public long CasterEntityId;
        public long SrcBuffId;
        public List<AttrKvPair> CachedPotencyAttrs;
        // IndependentStack：逐层时长与可选来源；null 时用 Layer/RemainingLifetime 旧字段还原
        public List<BuffLayerPersistEntry> StackLayers;
    }

    [Serializable]
    public class MetaData
    {
        public string SaveTime;
        public string Version;
    }

    [Serializable]
    public class PlayerData
    {
        public int Level;
        public float CurrentHP;
        public float MaxHP;

        public long TotalFallPeopleAmount = 0;

        // 内城：繁荣度、当前人口（与 HomeDataManager 同步）
        public int HomeProsperity;
        public int HomeCurrentPopulation;

        public Dictionary<string, bool> GlobalSwitchMap = new();

        public Dictionary<string, FishingSpotRuntimeSave> FishingSpotByUniqName = new();
        public Dictionary<string, RepairPointRuntimeSave> HomeRuinByUniqName = new();

        // 具名交互点 / 可移除障碍：键为地图刷新项 UniqName，仅存 LocalSwitches
        public Dictionary<string, MapInteractPointPersistData> InteractPointByUniqName = new();

        // 师门/技能学习系统获得的技能（不含自带、授予）
        public List<LearnedSkillEntry> LearnedSkills = new();

        // 天赋/任务等授予的被动、主动（免装配，按条 Level）
        public List<GrantedSkillEntry> GrantedPassives = new();
        public List<GrantedSkillEntry> GrantedActives = new();

        public List<string> NormalSkillSlotOverrides = new();
        public List<string> PassiveSkillSlotOverrides = new();
        public List<QuickSlotBindingPersist> WeaponQuickSlotOverrides = new();
        public List<QuickSlotBindingPersist> ConsumableQuickSlotOverrides = new();
        public int ActiveWeaponSlotIndex = -1;
        public int ActiveConsumableIndex;

        // 重要具名 NPC：键为 CharacterKey；LocalSwitches 由 Registry 在运行时维护（随 SetLocalSwitch 更新），与单图 Record 存盘周期解耦
        public Dictionary<string, NpcCharacterPersistData> NpcCharacterPersistByKey = new();

        // 魔力衣装（潜入选定后锁定）
        public string MagicClothesDefId;
        public bool MagicClothesLockedForStealth;

        // 天赋节点等级（Level==0 可省略；对应 TbTalentNode + TbTalentNodeLevel）
        public List<TalentNodeLevelPersist> TalentNodeLevels = new();

        // 存档点正式解锁（全局；键 save_point_id，局外传送列表用）
        public List<SavePointUnlockPersist> SavePointUnlocks = new();

        // 隐秘据点已解锁设施 id（TbSecretBaseFacility.facility_id）
        public List<string> SecretBaseUnlockedFacilityIds = new();

        // 隐秘据点建设等级（TbSecretBaseBuildLevel.level，卷轴右边界随等级扩大）
        public int SecretBaseBuildLevel = 1;

        public Dictionary<string, MapRumorPersist> MapRumorByMapId = new();

        // 逻辑区域收编控制度（键 logic_area map_id）
        public Dictionary<string, int> LogicAreaControlByMapId = new();

        // 地图小剧情触发器消费态：键 mapId|triggerId
        public Dictionary<string, bool> MicroPlotConsumedByKey = new();

        // 角色装备（与主背包互斥：穿上时从背包扣除；存档需与背包一致）
        public List<EquippedGearEntry> EquippedGear = new();

        public List<BodyPartPersist> BodyParts = new();

        public List<string> OwnedRuneIds = new();
        public List<string> UnlockedRuneUpgradeIds = new();
        public List<RuneEquipPersist> EquippedRunes = new();

        // 本地玩家功能解锁（原 SaveData 根级 FuncOpenList 已迁入）
        public List<EFuncOpenType> FuncOpenList = new();
    }

    [Serializable]
    public class RuneEquipPersist
    {
        public int Slot;
        public string RuneId;
    }

    [Serializable]
    public class TalentNodeLevelPersist
    {
        public int NodeId;
        public int Level;
    }

    [Serializable]
    public class QuickSlotBindingPersist
    {
        public string ItemId;
        public long ItemInstanceId;
    }

    [Serializable]
    public class EquippedGearEntry
    {
        public int PartId;
        public int EquippedIndex;
        public string ItemId;
        public long ItemInstanceId;
        public long EquipAuxData;

        // 旧档字段，读档时忽略
        public int Category;
        public int SlotIndex;
    }

    [Serializable]
    public class BodyPartPersist
    {
        public int PartId;
        public int Level;
        public long Exp;
    }

    [Serializable]
    public class LearnedSkillEntry
    {
        public string SkillId;
        public int Level;
    }

    [Serializable]
    public class GrantedSkillEntry
    {
        public string SkillId;
        public int Level;
    }

    [Serializable]
    public class SavePointUnlockPersist
    {
        public string SavePointId;
        public bool Unlocked;
        public bool TributeSubmitted;
        public Dictionary<string, long> TributePut = new();
    }
    [Serializable]
    public class DreamEntryTendencyWinCounts
    {
        public int CharDreamEntryId;
        public int ForceWins;
        public int SoothingWins;
        public int TrickWins;
    }

    [Serializable]
    public class NpcCharacterPersistData
    {
        public List<string> LocalSwitches;

        public bool DesireCrystalTaken;
        public int DesireCrystalTakenDay; // 获取天数

        public List<string> FinishedUniqDreamingIds = new(); // 已完成的唯一入梦入口

        public List<DreamEntryTendencyWinCounts> DreamEntryWinCounts = new();

        public int FavorValue;
        public int GiftsGivenToday;
        public int LastGiftSettlementDay = -1;
    }

    // 主背包单格稀疏持久化：仅存占格条目，使用 SlotIndex（与 PlayerBag.GetItemByIdx 平面下标一致）
    [Serializable]
    public class MainBagSlotPersist
    {
        public int SlotIndex;
        public string ItemId;
        public long Count;
        public long ItemInstanceId;
    }

    /// <summary>
    /// 单格仓库道具（空格 ItemId 为空或 Count 为 0）。
    /// </summary>
    [Serializable]
    public class WarehouseSlotPersist
    {
        public string ItemId;
        public long Count;
        public long ItemInstanceId;
    }

    [Serializable]
    public class WarehousePagePersist
    {
        public List<WarehouseSlotPersist> Slots = new List<WarehouseSlotPersist>();
    }

    // 跨地图的全局运行时（警戒条、通缉等），不随单张地图卸载而丢失
    [Serializable]
    public class WantedChannelPersist
    {
        public int BehaveType;

        public int ScaledVal;
    }

    [Serializable]
    public class GlobalRuntimePersistData
    {
        public int AlertVal;

        /// <summary>兼容旧档：effective 通缉（各通道 max）的镜像，便于排查。</summary>
        public int WantedScaledVal;
        public float WantedLastTime;

        /// <summary>新：分罪类通道缩放值；非空时优先于 WantedScaledVal 迁入逻辑。</summary>
        public List<WantedChannelPersist> WantedChannels;

        /// <summary>
        /// 世界结算日计数；每推进一次触发垂钓点按 N 日补满等逻辑。
        /// </summary>
        public int SettlementDayIndex;

        /// <summary>本次冒险经 SavePoint 保险箱已存入的欲望碎片数量。</summary>
        public long SavePointVaultDesireShardDepositedThisRun;
    }

    /// <summary>
    /// 单点垂钓存档（键为地图刷新项 UniqName）
    /// </summary>
    [Serializable]
    public class FishingSpotRuntimeSave
    {
        public string CfgId;
        public int Remaining;
        public int LastRestockSettlementDayIndex;
    }


    /// <summary>
    /// 废墟垂钓存档（键为地图刷新项 UniqName）
    /// </summary>
    [Serializable]
    public class RepairPointRuntimeSave
    {
        public string UniqName;
        public bool IsRepaired;
        public Dictionary<string, long> PutMaterial;
        public int RepairProgress;
    }

    [Serializable]
    public class MapInteractPointPersistData
    {
        public List<string> LocalSwitches;
    }

    [Serializable]
    public class RefreshRuntimePersist
    {
        public int StaticId;
        public long EntityInstId;
        public float LastRespawnTime;
        public float LastDestroyTime;

        /// <summary>ERefreshSlotRemovalReason 整数值；新存档写入。</summary>
        public int LastRemovalReason;

        // 旧版 bool 字段：反序列化时迁移为 VisibilityCondition。
        [JsonProperty("LastRemovalWasVisibilityCond")]
        private bool LegacyLastRemovalWasVisibilityCond
        {
            set
            {
                if (value && LastRemovalReason == 0)
                {
                    LastRemovalReason = (int)ERefreshSlotRemovalReason.VisibilityCondition;
                }
            }
        }
    }

    [Serializable]
    public class RumorActiveEntry
    {
        public string RumorId;
        public int PurchasedSettlementDay;
        /// <summary>到期日：当 SettlementDayIndex >= ExpireSettlementDay 时过期</summary>
        public int ExpireSettlementDay;
        public bool IsRandomKind;
        public bool Revealed;
    }

    [Serializable]
    public class MapRumorPersist
    {
        public List<RumorActiveEntry> ActiveIntel = new();
        public int RandomRollSettlementDay = -1;
        public List<string> RandomOfferRumorIds = new();
    }

    // 单张地图上的逻辑状态：区域邪恶警戒、动态刷新 CD、实体 Record 快照
    [Serializable]
    public class MapRuntimePersistData
    {
        public long AreaAlertValue;

        public List<RefreshRuntimePersist> RefreshStates = new();

        public Dictionary<long, int> RecordToRefreshStaticId = new();

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        public List<LogicEntityRecord> EntityRecords = new();
    }

    [Serializable]
    public class SaveData
    {
        public MetaData Meta;

        [JsonProperty("Player")]
        public PlayerData PlayerData;

        // 稀疏列表：每项含 SlotIndex，仅序列化占格条目
        public List<MainBagSlotPersist> MainInventorySlots;

        /// <summary>
        /// 仓库分页数据，顺序与运行时页索引一致。
        /// </summary>
        public List<WarehousePagePersist> WarehousePages;

        public string CurrentMapId;
        public Vector2 CurrentPos;

        public OpenWorldReturnBookmark LastOpenWorldBeforeHome;
        public OpenWorldReturnBookmark LastOpenWorldBeforeSecretBase;
        public List<BuffPersistData> PlayerBuffs;

        public GlobalRuntimePersistData GlobalRuntime;

        public List<EFuncOpenType> FuncOpenList = new();

        public Dictionary<string, MapRuntimePersistData> MapRuntimeByMapId = new();

        public long NextLogicEntityIdHint;

        // 已分配的最大 ItemInstanceId（与 NextLogicEntityIdHint 语义一致）
        public long NextItemInstanceIdHint;

        public SaveData()
        {
            Meta = new MetaData();
            PlayerData = new PlayerData();
            MainInventorySlots = new List<MainBagSlotPersist>();
            WarehousePages = new List<WarehousePagePersist>();
            PlayerBuffs = new List<BuffPersistData>();
        }

        public static void EnsureHydrated(SaveData data)
        {
            if (data == null) return;
            data.Meta ??= new MetaData();
            data.PlayerData ??= new PlayerData();
            data.PlayerData.GlobalSwitchMap ??= new Dictionary<string, bool>();
            data.PlayerData.FishingSpotByUniqName ??= new Dictionary<string, FishingSpotRuntimeSave>();
            data.PlayerData.HomeRuinByUniqName ??= new Dictionary<string, RepairPointRuntimeSave>();
            data.PlayerData.InteractPointByUniqName ??= new Dictionary<string, MapInteractPointPersistData>();
            data.PlayerData.HomeProsperity = Math.Max(0, data.PlayerData.HomeProsperity);
            data.PlayerData.HomeCurrentPopulation = Math.Max(0, data.PlayerData.HomeCurrentPopulation);
            data.PlayerData.LearnedSkills ??= new List<LearnedSkillEntry>();
            data.PlayerData.GrantedPassives ??= new List<GrantedSkillEntry>();
            data.PlayerData.GrantedActives ??= new List<GrantedSkillEntry>();
            data.PlayerData.NormalSkillSlotOverrides ??= new List<string>();
            data.PlayerData.PassiveSkillSlotOverrides ??= new List<string>();
            data.PlayerData.NpcCharacterPersistByKey ??= new Dictionary<string, NpcCharacterPersistData>();
            data.PlayerData.TalentNodeLevels ??= new List<TalentNodeLevelPersist>();
            data.PlayerData.SavePointUnlocks ??= new List<SavePointUnlockPersist>();
            data.PlayerData.SecretBaseUnlockedFacilityIds ??= new List<string>();
            if (data.PlayerData.SecretBaseBuildLevel < 1)
            {
                data.PlayerData.SecretBaseBuildLevel = 1;
            }
            data.PlayerData.MapRumorByMapId ??= new Dictionary<string, MapRumorPersist>();
            data.PlayerData.LogicAreaControlByMapId ??= new Dictionary<string, int>();
            data.PlayerData.MicroPlotConsumedByKey ??= new Dictionary<string, bool>();
            data.PlayerData.OwnedRuneIds ??= new List<string>();
            data.PlayerData.UnlockedRuneUpgradeIds ??= new List<string>();
            data.PlayerData.EquippedRunes ??= new List<RuneEquipPersist>();
            data.PlayerData.FuncOpenList ??= new List<EFuncOpenType>();
            if (data.PlayerData.FuncOpenList.Count == 0 && data.FuncOpenList != null && data.FuncOpenList.Count > 0)
            {
                data.PlayerData.FuncOpenList.AddRange(data.FuncOpenList);
            }

            data.MainInventorySlots ??= new List<MainBagSlotPersist>();
            data.WarehousePages ??= new List<WarehousePagePersist>();
            data.PlayerBuffs ??= new List<BuffPersistData>();
            data.GlobalRuntime ??= new GlobalRuntimePersistData();
            data.MapRuntimeByMapId ??= new Dictionary<string, MapRuntimePersistData>();
        }

        public static void SyncLogicEntityIdCounterFromSave(SaveData data)
        {
            if (data == null) return;
            long maxId = data.NextLogicEntityIdHint;
            if (data.MapRuntimeByMapId != null)
            {
                foreach (var kv in data.MapRuntimeByMapId)
                {
                    var block = kv.Value;
                    if (block?.EntityRecords == null) continue;
                    foreach (var rec in block.EntityRecords)
                    {
                        if (rec != null) maxId = Math.Max(maxId, rec.Id);
                    }
                }
            }

            if (maxId > 0)
            {
                My.GameLogicManager.LogicEntityIdInst = Math.Max(My.GameLogicManager.LogicEntityIdInst, maxId + 1);
            }
        }

        public static long CollectMaxItemInstanceId(SaveData data)
        {
            if (data == null)
            {
                return 0;
            }

            long maxId = data.NextItemInstanceIdHint;

            if (data.MainInventorySlots != null)
            {
                foreach (var row in data.MainInventorySlots)
                {
                    if (row != null && row.ItemInstanceId > 0)
                    {
                        maxId = Math.Max(maxId, row.ItemInstanceId);
                    }
                }
            }

            if (data.WarehousePages != null)
            {
                foreach (var page in data.WarehousePages)
                {
                    if (page?.Slots == null)
                    {
                        continue;
                    }

                    foreach (var slot in page.Slots)
                    {
                        if (slot != null && slot.ItemInstanceId > 0)
                        {
                            maxId = Math.Max(maxId, slot.ItemInstanceId);
                        }
                    }
                }
            }

            var pd = data.PlayerData;
            if (pd != null)
            {
                if (pd.EquippedGear != null)
                {
                    foreach (var e in pd.EquippedGear)
                    {
                        if (e != null && e.ItemInstanceId > 0)
                        {
                            maxId = Math.Max(maxId, e.ItemInstanceId);
                        }
                    }
                }

                maxId = Math.Max(maxId, CollectMaxItemInstanceIdFromQuickSlots(pd.WeaponQuickSlotOverrides));
                maxId = Math.Max(maxId, CollectMaxItemInstanceIdFromQuickSlots(pd.ConsumableQuickSlotOverrides));
            }

            return maxId;
        }

        static long CollectMaxItemInstanceIdFromQuickSlots(List<QuickSlotBindingPersist> bindings)
        {
            long maxId = 0;
            if (bindings == null)
            {
                return maxId;
            }

            foreach (var b in bindings)
            {
                if (b != null && b.ItemInstanceId > 0)
                {
                    maxId = Math.Max(maxId, b.ItemInstanceId);
                }
            }

            return maxId;
        }

        public static void SyncItemInstanceIdCounterFromSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            long maxId = CollectMaxItemInstanceId(data);
            if (maxId > 0)
            {
                My.GameLogicManager.ItemInstanceIdCounter =
                    Math.Max(My.GameLogicManager.ItemInstanceIdCounter, maxId + 1);
            }
        }

        public static void WriteItemInstanceIdHintToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            long maxId = CollectMaxItemInstanceId(data);
            long runtimeMax = My.GameLogicManager.ItemInstanceIdCounter - 1;
            if (runtimeMax > 0)
            {
                maxId = Math.Max(maxId, runtimeMax);
            }

            data.NextItemInstanceIdHint = maxId;
        }
    }
}
