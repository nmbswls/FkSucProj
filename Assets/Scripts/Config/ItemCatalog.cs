using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My;
using My.Map;
using My.Player;
using UnityEngine;

namespace My.Config
{
    // 道具：直接读 CfgMgr.TbItemData / TbItemUse / TbItemGift，不经过中间镜像表
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

            ValidateGiftItemRows();
        }

        public static ItemGift GetGiftDef(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbItemGift.GetOrDefault(itemId);
        }

        public static bool HasGiftTag(string itemId, EGiftTag tag)
        {
            var g = GetGiftDef(itemId);
            return g != null && g.GiftTags != null && g.GiftTags.Contains(tag);
        }

        static void ValidateGiftItemRows()
        {
            if (CfgMgr.Cfgs == null)
            {
                return;
            }

            foreach (var def in CfgMgr.Cfgs.TbItemData.DataList)
            {
                if (def.ItemType != EItemType.Gift)
                {
                    continue;
                }

                if (GetGiftDef(def.ItemId) == null)
                {
                    Debug.LogWarning(
                        $"ItemCatalog: Gift item '{def.ItemId}' has no row in TbItemGift (item_gift sheet).");
                }
            }

            foreach (var gift in CfgMgr.Cfgs.TbItemGift.DataList)
            {
                var def = GetItemDef(gift.ItemId);
                if (def == null)
                {
                    Debug.LogWarning(
                        $"ItemCatalog: TbItemGift row '{gift.ItemId}' has no matching TbItemData row.");
                }
                else if (def.ItemType != EItemType.Gift)
                {
                    Debug.LogWarning(
                        $"ItemCatalog: TbItemGift row '{gift.ItemId}' but item_type is {def.ItemType}, expected Gift.");
                }
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
            return GetUseSlot(itemId, 0);
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
                case EItemType.HumanWeapon:
                case EItemType.PartGear:
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
                ApplyFreshInstanceInfo(item, def);
            }

            return item;
        }

        // 从存档还原堆栈：已有 ItemInstanceId 时不占用新 id
        public static ItemStack HydrateItemStackFromPersist(string itemId, long count, long itemInstanceId)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
            {
                return null;
            }

            if (CfgMgr.Cfgs == null)
            {
                Debug.LogWarning($"ItemCatalog.HydrateItemStackFromPersist: CfgMgr.Cfgs is null, cannot hydrate '{itemId}'.");
                return null;
            }

            var def = GetItemDef(itemId);
            if (def == null)
            {
                Debug.LogWarning($"ItemCatalog.HydrateItemStackFromPersist: no TbItemData row for '{itemId}'.");
                return null;
            }

            var item = new ItemStack(itemId, count);
            if (!IsInstanceType(def.ItemType))
            {
                return item;
            }

            if (itemInstanceId != 0)
            {
                item.ItemInstanceId = itemInstanceId;
            }
            else
            {
                item.ItemInstanceId = My.GameLogicManager.ItemInstanceIdCounter++;
            }

            ApplyFreshInstanceInfo(item, def);
            return item;
        }

        static void ApplyFreshInstanceInfo(ItemStack item, ItemData def)
        {
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

        public static int GetMaxStackByType(string itemId, EContainerType containerMode)
        {
            var def = GetItemDef(itemId);
            if (def == null)
            {
                return 0;
            }

            const int quasiUnlimitedThreshold = 999_000;

            if (!def.Stackable)
            {
                if (ItemStackPolicy.TryGetAbsoluteOverride(containerMode, itemId, out var absNs))
                {
                    return Mathf.Max(1, absNs);
                }

                return 1;
            }

            var intrinsic = GetIntrinsicBaseMaxStack(def);

            if (ItemStackPolicy.TryGetAbsoluteOverride(containerMode, itemId, out var abs))
            {
                return Mathf.Max(1, abs);
            }

            var ratio = ItemStackPolicy.ResolveStackRatio(containerMode, def);
            var scaled = (long)System.Math.Floor(intrinsic * (double)ratio);
            var result = (int)System.Math.Max(1L, scaled);

            if (def.StackCount >= quasiUnlimitedThreshold)
            {
                return (int)System.Math.Min(999_999L, result);
            }

            return result;
        }

        // 默认堆叠基数：可堆叠且 stack_count>0 用 stack_count；否则 10。货币等大堆叠在表内填 stack_count=999999

        static int GetIntrinsicBaseMaxStack(ItemData def)
        {
            if (!def.Stackable)
            {
                return 1;
            }

            if (def.StackCount > 0)
            {
                return def.StackCount;
            }

            return 10;
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

        public static EQuickBarItemKind GetQuickBarKind(string itemId)
        {
            var def = GetItemDef(itemId);
            if (def == null)
            {
                return EQuickBarItemKind.None;
            }

            return def.QuickBarKind;
        }

        public static bool IsQuickBarWeapon(string itemId)
        {
            return GetQuickBarKind(itemId) == EQuickBarItemKind.Weapon;
        }

        public static bool IsQuickBarConsumable(string itemId)
        {
            return GetQuickBarKind(itemId) == EQuickBarItemKind.Consumable;
        }
    }
}
