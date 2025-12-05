
using My.Player.Bag;
using SuperScrollView;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Config.FakeItemConf;
using static My.UI.AnyContainerItemCell;

namespace Config
{


    [Serializable]
    public class FakeItemConf
    {
        public enum EStackType
        {
            NoStack,
            Size1,
            Size2,
            Size3,
            Size4,
            Custom,
            NoLimit,
        }
        public string ItemId;

        public enum EItemType
        {
            Normal,
            Currency,
        }
        public EItemType ItemType;


        public EStackType StackType = EStackType.NoStack;
        public int StackCount = 0;
        public string SpriteName;

        public bool CanDrop = true;

        public int RareTier = 0;

        public enum ERevealEffectType
        { 
            None,
            AddGcVal,
            CostClothes,
        }

        public ERevealEffectType RevealEffectType;
        public long RevealParam1;
        public long RevealParam2;
    }

    public static class FakeItemDatabase
    {

        private static Dictionary<string, FakeItemConf> _dict = null;

        public static int GetMaxStackByType(string itemId, EContainerType containerMode)
        {
            var conf = GetItem(itemId);
            if (conf == null)
            {
                return 0;
            }

            if(conf.StackType == FakeItemConf.EStackType.NoStack)
            {
                return 1;
            }

            if(conf.StackType == FakeItemConf.EStackType.Size1)
            {
                if(containerMode == EContainerType.Inventory)
                {
                    return 10;
                }
                else if (containerMode == EContainerType.Shop)
                {
                    return 5;
                }
            }

            return 5;

        }

        public static FakeItemConf GetItem(string itemId)
        {
            if(_dict == null)
            {
                _dict = new();

                {
                    var item = new FakeItemConf();
                    item.ItemId = "small_stone";
                    item.StackType = EStackType.Size1;
                    item.SpriteName = "small_stone";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "stick";
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "stick";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "wood";
                    item.StackType = EStackType.Size3;
                    item.SpriteName = "wood";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "banana";
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "banana";

                    item.RevealEffectType = ERevealEffectType.AddGcVal;
                    item.RevealParam1 = 5000;

                    _dict[item.ItemId] = item;
                }
                {
                    var item = new FakeItemConf();
                    item.ItemId = "qiezi";
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "qiezi";

                    item.RevealEffectType = ERevealEffectType.AddGcVal;
                    item.RevealParam1 = 5000;

                    _dict[item.ItemId] = item;
                }
                {
                    var item = new FakeItemConf();
                    item.ItemId = "bangbangtang";
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "bangbangtang";

                    _dict[item.ItemId] = item;
                }
                {
                    var item = new FakeItemConf();
                    item.ItemId = "flower_01";
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "flower_01";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "flower_02";
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "flower_02";

                    _dict[item.ItemId] = item;
                }
                {
                    var item = new FakeItemConf();
                    item.ItemId = "flower_03";
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "flower_03";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "berry";
                    item.StackType = EStackType.Size1;
                    item.SpriteName = "berry";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "gold";
                    item.ItemType = EItemType.Currency;
                    item.StackType = EStackType.NoLimit;
                    item.SpriteName = "gold";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "chanzi";
                    item.StackType = EStackType.NoStack;
                    item.SpriteName = "chanzi";

                    _dict[item.ItemId] = item;
                }
            }

            _dict.TryGetValue(itemId, out var conf);
            return conf;
        }

        //void Awake()
        //{
        //    Instance = this;
        //    dict.Clear();
        //    foreach (var it in Items)
        //    {
        //        if (!dict.ContainsKey(it.ItemID))
        //            dict.Add(it.ItemID, it);
        //    }
        //}

        public static Sprite GetIcon(string id)
        {
            //if (Instance == null) return null;
            //return Instance.dict.TryGetValue(id, out var data) ? data.Icon : null;
            return null;
        }

        //public static int GetMaxStack(string id)
        //{
        //    return 99;
        //}

        public static bool CanUse(string id)
        {
            //if (Instance == null) return false;
            if (id == "banana")
            {
                return true;
            }
            //return Instance.dict.TryGetValue(id, out var data) ? data.Usable : false;
            return false;
        }

        //public static bool CanEquip(string id, out string slot)
        //{
        //    slot = "";
        //    if (Instance == null) return false;
        //    if (Instance.dict.TryGetValue(id, out var data))
        //    {
        //        slot = data.EquipSlot;
        //        return !string.IsNullOrEmpty(slot);
        //    }
        //    return false;
        //}
    }

}
