
using System;
using System.Collections.Generic;
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
    }

    [Serializable]
    public class InventoryItemData
    {
        public string ItemID;
        public int Amount;
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
            data.Inventory ??= new List<InventoryItemData>();
            data.PlayerBuffs ??= new List<BuffPersistData>();
        }
    }
}
