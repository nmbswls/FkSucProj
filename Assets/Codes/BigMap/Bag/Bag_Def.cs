
using My;
using My.Map;
using My.Player.Bag;
using SuperScrollView;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Config.FakeItemConf;
using static My.Map.BaseUnitLogicEntity.ControlledMoveCtx;
using static My.UI.AnyContainerItemCell;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.UIElements.UxmlAttributeDescription;

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
        public string DisplayName;

        public enum EItemType
        {
            Normal,
            Currency,
            Equip,
            Pocket,

            Insertion,
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

        public enum EItemUseType
        {
            None,
            AddHunger,
            GiveDrop,
        }

        [Serializable]
        public class ItemUseCfg
        {
            public bool Usable = false;
            public bool CostOnUse = true;
            public float UseCd;
            public float UseTime = 0.5f;


            public EItemUseType UseType;
            public string UseParams;
            public long Param1;
            public long Param2;
            public string Param5;
            public string Param6;
        }

        public ItemUseCfg UseCfg1;
        public ItemUseCfg UseCfg2;
        public ItemUseCfg UseCfg3;

        public bool AutoDestroy;
        public float AutoDestroyTime;
        public string SpecialBuffId;
        public float SpecialBuffInterval;

        public bool AutoPick;

        public bool IsAutoUse;
    }

    public static class FakeItemDatabase
    {

        private static Dictionary<string, FakeItemConf> _dict = null;

        public static ItemStack CreateItemStack(string itemId, long count)
        {
            var itemConf = GetItem(itemId);
            if (itemConf == null) return null;

            var item = new ItemStack(itemId, count);

            if(IsInstanceType(itemConf.ItemType))
            {
                item.ItemInstanceId = GameLogicManager.ItemInstanceIdCounter++;
                switch(itemConf.ItemType)
                {
                    case EItemType.Equip:
                    {
                        item.InstanceInfo = new ItemInstance4Equip();
                    }
                    break;
                    case EItemType.Insertion:
                    {
                        var instInfo = new ItemInstance4Insertion();
                        instInfo.BuffTickTimer = LogicTime.time;
                        instInfo.Lifetime = itemConf.AutoDestroyTime;

                        item.InstanceInfo = instInfo;
                    }
                    break;
                }
            }
            
            return item;
        }

        public static bool IsInstanceType(EItemType itemType)
        {
            switch (itemType)
            {
                case EItemType.Equip:
                case EItemType.Pocket:

                case EItemType.Insertion:
                    return true;
            }

            return false;
        }

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
                    item.ItemType = EItemType.Normal;
                    item.StackType = EStackType.Size1;
                    item.SpriteName = "small_stone";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "stick";
                    item.ItemType = EItemType.Normal;
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "stick";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "wood";
                    item.ItemType = EItemType.Normal;
                    item.StackType = EStackType.Size3;
                    item.SpriteName = "wood";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "banana";
                    item.ItemType = EItemType.Normal;
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "banana";

                    item.RevealEffectType = ERevealEffectType.AddGcVal;
                    item.RevealParam1 = 5000;

                    item.UseCfg1 = new ItemUseCfg()
                    {
                        Usable = true,
                        CostOnUse = true,
                        UseCd = 10.0f,
                        UseTime = 1.5f,
                    };

                    _dict[item.ItemId] = item;
                }
                {
                    var item = new FakeItemConf();
                    item.ItemId = "qiezi";
                    item.ItemType = EItemType.Normal;
                    item.StackType = EStackType.Size2;
                    item.SpriteName = "qiezi";

                    item.RevealEffectType = ERevealEffectType.AddGcVal;
                    item.RevealParam1 = 5000;

                    _dict[item.ItemId] = item;
                }
                {
                    var item = new FakeItemConf();
                    item.ItemId = "bangbangtang";
                    item.ItemType = EItemType.Normal;
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
                    item.ItemId = "j";
                    item.ItemType = EItemType.Currency;
                    item.StackType = EStackType.NoLimit;
                    item.SpriteName = "j";

                    _dict[item.ItemId] = item;
                }
                
                {
                    var item = new FakeItemConf();
                    item.ItemId = "chanzi";
                    item.ItemType = EItemType.Equip;
                    item.StackType = EStackType.NoStack;
                    item.SpriteName = "chanzi";

                    _dict[item.ItemId] = item;
                }


                {
                    var item = new FakeItemConf();
                    item.ItemId = "key_a1_001";
                    item.ItemType = EItemType.Normal;
                    item.StackType = EStackType.NoStack;
                    item.SpriteName = "key_a1_01";

                    _dict[item.ItemId] = item;
                }
                {
                    var item = new FakeItemConf();
                    item.ItemId = "key_a1_002";
                    item.ItemType = EItemType.Normal;
                    item.StackType = EStackType.NoStack;
                    item.SpriteName = "key_a1_02";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "insertion_maoqiu";
                    item.ItemType = EItemType.Insertion;
                    item.StackType = EStackType.NoStack;
                    item.SpriteName = "insertion_maoqiu";

                    item.AutoDestroy = true;
                    item.AutoDestroyTime = 30.0f;

                    item.SpecialBuffInterval = 5.0f;
                    item.SpecialBuffId = "insertion_debuff_small";

                    _dict[item.ItemId] = item;
                }

                {
                    var item = new FakeItemConf();
                    item.ItemId = "j_drop_small";
                    item.ItemType = EItemType.Normal;
                    item.StackType = EStackType.NoStack;
                    item.SpriteName = "j_drop_small";

                    item.UseCfg1 = new ItemUseCfg()
                    {
                        Usable = true,
                        UseCd = 0,
                        UseTime = 0,

                        UseType = EItemUseType.GiveDrop,
                        Param5 = "j_drop_small",
                    };

                    item.AutoPick = true;
                    item.IsAutoUse = true;

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
            var cfg = GetItem(id);
            if(cfg == null || cfg.UseCfg1 == null)
            {
                return false;
            }
            
            return true;
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
