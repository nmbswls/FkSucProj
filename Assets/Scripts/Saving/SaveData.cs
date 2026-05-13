
using System;
using System.Collections.Generic;
using My.Map;
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

        // 已学技能（SkillId + Level；列表顺序即装配/展示顺序）
        public List<LearnedSkillEntry> LearnedSkills = new();

        public List<string> NormalSkillSlotOverrides = new();
        public List<string> ItemSlotOverrides = new();

        // 重要具名 NPC：键为 CharacterKey；LocalSwitches 由 Registry 在运行时维护（随 SetLocalSwitch 更新），与单图 Record 存盘周期解耦
        public Dictionary<string, NpcCharacterPersistData> NpcCharacterPersistByKey = new();

        // 魔力衣装（潜入选定后锁定）
        public string MagicClothesDefId;
        public bool MagicClothesLockedForStealth;

        // 已解锁天赋节点（对应 Luban TbTalentNode / PlayerTalentManager）
        public List<int> UnlockedTalentNodeIds = new();

        public Dictionary<string, MapRumorPersist> MapRumorByMapId = new();

        // 角色装备（与主背包互斥：穿上时从背包扣除；存档需与背包一致）
        public List<EquippedGearEntry> EquippedGear = new();
    }

    [Serializable]
    public class EquippedGearEntry
    {
        public int Category;
        public int SlotIndex;
        public string ItemId;
        public long ItemInstanceId;
        public long EquipAuxData;
    }

    [Serializable]
    public class LearnedSkillEntry
    {
        public string SkillId;
        public int Level;
    }
    [Serializable]
    public class NpcCharacterPersistData
    {
        public List<string> LocalSwitches;

        public bool DesireCrystalTaken;
        public int DesireCrystalTakenDay; // 获取天数

        public List<string> FinishedUniqDreamingIds = new();
    }

    [Serializable]
    public class InventoryItemData
    {
        public string ItemID;
        public int Amount;
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

        public List<InventoryItemData> Inventory;

        /// <summary>
        /// 仓库分页数据，顺序与运行时页索引一致。
        /// </summary>
        public List<WarehousePagePersist> WarehousePages;

        public string CurrentMapId;
        public Vector2 CurrentPos;

        public OpenWorldReturnBookmark LastOpenWorldBeforeHome;
        public List<BuffPersistData> PlayerBuffs;

        public GlobalRuntimePersistData GlobalRuntime;

        public List<EFuncOpenType> FuncOpenList = new();

        public Dictionary<string, MapRuntimePersistData> MapRuntimeByMapId = new();

        public long NextLogicEntityIdHint;

        public SaveData()
        {
            Meta = new MetaData();
            PlayerData = new PlayerData();
            Inventory = new List<InventoryItemData>();
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
            data.PlayerData.HomeProsperity = Math.Max(0, data.PlayerData.HomeProsperity);
            data.PlayerData.HomeCurrentPopulation = Math.Max(0, data.PlayerData.HomeCurrentPopulation);
            data.PlayerData.LearnedSkills ??= new List<LearnedSkillEntry>();
            data.PlayerData.NormalSkillSlotOverrides ??= new List<string>();
            data.PlayerData.NpcCharacterPersistByKey ??= new Dictionary<string, NpcCharacterPersistData>();
            data.PlayerData.UnlockedTalentNodeIds ??= new List<int>();
            data.PlayerData.MapRumorByMapId ??= new Dictionary<string, MapRumorPersist>();
            data.Inventory ??= new List<InventoryItemData>();
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
    }
}
