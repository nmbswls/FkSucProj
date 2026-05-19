using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My;
using My.Map;
using My.Player;
using UnityEngine;

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

            if (def.QuickBarKind != EQuickBarItemKind.None)
            {
                return def.QuickBarKind;
            }

            // 表未填列时的临时兜底，导表后可删
            if (itemId == "small_knife")
            {
                return EQuickBarItemKind.Weapon;
            }

            if (itemId == "evil_scroll_01")
            {
                return EQuickBarItemKind.Consumable;
            }

            return EQuickBarItemKind.None;
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
