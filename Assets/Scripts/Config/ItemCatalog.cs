using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Map;
using My.Player.Bag;
using UnityEngine;
using static My.UI.AnyContainerItemCell;

namespace My.Config
{
    // 道具：直接读 CfgMgr.TbItemData / TbItemUse，不经过中间镜像表
    public static class ItemCatalog
    {
        private static Dictionary<string, List<ItemUse>> _usesByItemId;

        public static void RebuildItemCaches()
        {
            _usesByItemId = new Dictionary<string, List<ItemUse>>();
            if (CfgMgr.Cfgs == null)
            {
                return;
            }

            foreach (var u in CfgMgr.Cfgs.TbItemUse.DataList)
            {
                if (!_usesByItemId.TryGetValue(u.ItemId, out var list))
                {
                    list = new List<ItemUse>();
                    _usesByItemId[u.ItemId] = list;
                }

                list.Add(u);
            }

            foreach (var key in _usesByItemId.Keys.ToList())
            {
                _usesByItemId[key] = _usesByItemId[key].OrderBy(x => x.Slot).ToList();
            }
        }

        public static ItemData GetItemDef(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbItemData.GetOrDefault(itemId);
        }

        public static ItemUse GetUseSlot(string itemId, int slot)
        {
            EnsureUseCache();
            if (_usesByItemId == null || !_usesByItemId.TryGetValue(itemId, out var list))
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Slot == slot)
                {
                    return list[i];
                }
            }

            return null;
        }

        public static ItemUse GetPrimaryUse(string itemId)
        {
            return GetUseSlot(itemId, 1);
        }

        public static bool CanUse(string itemId)
        {
            var u = GetPrimaryUse(itemId);
            return u != null && u.Usable;
        }

        public static bool IsInstanceType(EItemType itemType)
        {
            switch (itemType)
            {
                case EItemType.Equip:
                case EItemType.Pocket:
                case EItemType.Insertion:
                    return true;
                default:
                    return false;
            }
        }

        public static ItemStack CreateItemStack(string itemId, long count)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("ItemCatalog.CreateItemStack: itemId is null or empty.");
                return null;
            }

            if (CfgMgr.Cfgs == null)
            {
                Debug.LogWarning($"ItemCatalog.CreateItemStack: CfgMgr.Cfgs is null, cannot create '{itemId}'. Call CfgMgr.LoadGameConfigs first.");
                return null;
            }

            var def = GetItemDef(itemId);
            if (def == null)
            {
                Debug.LogWarning($"ItemCatalog.CreateItemStack: no TbItemData row for '{itemId}'.");
                return null;
            }

            var item = new ItemStack(itemId, count);

            if (IsInstanceType(def.ItemType))
            {
                item.ItemInstanceId = My.GameLogicManager.ItemInstanceIdCounter++;
                switch (def.ItemType)
                {
                    case EItemType.Equip:
                        item.InstanceInfo = new ItemInstance4Equip();
                        break;
                    case EItemType.Insertion:
                    {
                        var instInfo = new ItemInstance4Insertion
                        {
                            BuffTickTimer = LogicTime.time,
                            Lifetime = def.AutoDestroyTime,
                        };
                        item.InstanceInfo = instInfo;
                    }
                        break;
                }
            }

            return item;
        }

        public static int GetMaxStackByType(string itemId, EContainerType containerMode)
        {
            var def = GetItemDef(itemId);
            if (def == null)
            {
                return 0;
            }

            if (containerMode == EContainerType.Inventory || containerMode == EContainerType.SpecialInventory
                || containerMode == EContainerType.Warehouse)
            {
                if (def.MaxStackInventory > 0)
                {
                    return def.MaxStackInventory;
                }
            }
            else if (containerMode == EContainerType.Shop)
            {
                if (def.MaxStackShop > 0)
                {
                    return def.MaxStackShop;
                }
            }
            else if (containerMode == EContainerType.LootPoint)
            {
                if (def.MaxStackLoot > 0)
                {
                    return def.MaxStackLoot;
                }
            }

            if (def.StackType == EItemStackType.NoStack)
            {
                return 1;
            }

            if (def.StackType == EItemStackType.NoLimit)
            {
                return 999_999;
            }

            if (def.StackType == EItemStackType.Size1)
            {
                if (containerMode == EContainerType.Inventory || containerMode == EContainerType.SpecialInventory
                    || containerMode == EContainerType.Warehouse)
                {
                    return 10;
                }

                if (containerMode == EContainerType.Shop)
                {
                    return 5;
                }
            }

            return 5;
        }

        public static Sprite GetIcon(string id)
        {
            return null;
        }

        static void EnsureUseCache()
        {
            if (_usesByItemId == null)
            {
                RebuildItemCaches();
            }
        }
    }
}
