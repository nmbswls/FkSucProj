
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
    public class BuffPersistData
    {
        public string BuffId;
        public int Layer;
        public float RemainingLifetime;
        public long CasterEntityId;
        public long SrcBuffId;
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

        public Dictionary<string, bool> GlobalSwitchMap = new();

        public Dictionary<string, FishingSpotRuntimeSave> FishingSpotByUniqName = new();
    }

    [Serializable]
    public class InventoryItemData
    {
        public string ItemID;
        public int Amount;
    }

    // 跨地图的全局运行时（警戒条、通缉等），不随单张地图卸载而丢失
    [Serializable]
    public class GlobalRuntimePersistData
    {
        public int AlertVal;
        public int WantedScaledVal;
        public float WantedLastTime;

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

        public string CurrentMapId;
        public Vector2 CurrentPos;

        public OpenWorldReturnBookmark LastOpenWorldBeforeHome;
        public List<BuffPersistData> PlayerBuffs;

        public GlobalRuntimePersistData GlobalRuntime;

        public Dictionary<string, MapRuntimePersistData> MapRuntimeByMapId = new();

        public long NextLogicEntityIdHint;

        public SaveData()
        {
            Meta = new MetaData();
            PlayerData = new PlayerData();
            Inventory = new List<InventoryItemData>();
            PlayerBuffs = new List<BuffPersistData>();
        }

        public static void EnsureHydrated(SaveData data)
        {
            if (data == null) return;
            data.Meta ??= new MetaData();
            data.PlayerData ??= new PlayerData();
            data.PlayerData.GlobalSwitchMap ??= new Dictionary<string, bool>();
            data.PlayerData.FishingSpotByUniqName ??= new Dictionary<string, FishingSpotRuntimeSave>();
            data.Inventory ??= new List<InventoryItemData>();
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
