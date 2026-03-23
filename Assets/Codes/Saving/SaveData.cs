
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Saving
{
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
        public PlayerData Player;
        public List<InventoryItemData> Inventory;
        //public WorldData World;

        public string CurrentMapId;
        public Vector2 CurrentPos;

        public SaveData()
        {
            Meta = new MetaData();
            Player = new PlayerData();
            Inventory = new List<InventoryItemData>();
            //World = new WorldData();
        }
    }

}