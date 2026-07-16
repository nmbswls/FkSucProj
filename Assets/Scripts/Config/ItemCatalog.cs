using System;
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

            foreach (var gift in CfgMgr.Cfgs.TbItemGift.DataList)
            {
                var def = GetItemDef(gift.ItemId);
                if (def == null)
                {
                    Debug.LogWarning(
                        $"ItemCatalog: TbItemGift row '{gift.ItemId}' has no matching TbItemData row.");
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
            return u != null;
        }

        public static EItemUseConsumePolicy GetUseConsumePolicy(ItemUse useRow)
        {
            return useRow?.ConsumePolicy ?? EItemUseConsumePolicy.None;
        }

        public static bool ShouldConsumeOnUse(ItemUse useRow)
        {
            return GetUseConsumePolicy(useRow) != EItemUseConsumePolicy.None;
        }

        public static bool RequiresInstance(ItemData def)
        {
            return ItemTagCatalog.RequiresInstance(def);
        }

        public static bool RequiresInstance(string itemId)
        {
            return RequiresInstance(GetItemDef(itemId));
        }

        [System.Obsolete("Use tag-based RequiresInstance(ItemData) instead.")]
        public static bool IsInstanceType(EItemType itemType)
        {
            return false;
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

            if (RequiresInstance(def))
            {
                item.ItemInstanceId = My.GameLogicManager.ItemInstanceIdCounter++;
                ApplyFreshInstanceInfo(item, def);
            }

            return item;
        }

        // 从存档还原堆栈：已有 ItemInstanceId 时不占用新 id
        public static ItemStack HydrateItemStackFromPersist(string itemId, long count, long itemInstanceId, ItemInstanceInfo persistedInstanceInfo = null)
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
            if (!RequiresInstance(def))
            {
                item.InstanceInfo = persistedInstanceInfo?.Clone();
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

            item.InstanceInfo = persistedInstanceInfo?.Clone();
            ApplyFreshInstanceInfo(item, def);
            return item;
        }

        static void ApplyFreshInstanceInfo(ItemStack item, ItemData def)
        {
            if (item == null || def == null || !RequiresInstance(def))
            {
                return;
            }

            item.InstanceInfo ??= new ItemInstanceInfo();

            if (ItemTagCatalog.HasTag(def, EItemTag.PartGear))
            {
                item.InstanceInfo.GetOrAdd<ItemInstance4PartGear>();
            }

            if (ItemTagCatalog.HasTag(def, EItemTag.HumanWeapon))
            {
                item.InstanceInfo.GetOrAdd<ItemInstance4HumanWeapon>();
                if (HumanWeaponCatalog.IsHumanWeapon(item.ItemID))
                {
                    HumanWeaponCatalog.TryEnsureSeed(item);
                }
            }

            if (ItemTagCatalog.HasTag(def, EItemTag.HumanArmar))
            {
                item.InstanceInfo.GetOrAdd<ItemInstance4HumanArmar>();
                HumanArmarCatalog.TryEnsureSeed(item);
                HumanArmarCatalog.TryGenerateAffixes(item);
            }

            if (ItemTagCatalog.HasTag(def, EItemTag.Charge))
            {
                var charge = item.InstanceInfo.GetOrAdd<ItemInstance4UseCharge>();
                if (charge.MaxCharges <= 0)
                {
                    charge.MaxCharges = GetInitialUseCharges(def.ItemId);
                }

                if (charge.Charges <= 0)
                {
                    charge.Charges = charge.MaxCharges;
                }
            }

            if (ItemTagCatalog.HasTag(def, EItemTag.PremiumEssenceDrop))
            {
                item.InstanceInfo.GetOrAdd<ItemInstance4PremiumEssence>();
            }

            if (ItemTagCatalog.HasTag(def, EItemTag.Insertion))
            {
                var insertion = item.InstanceInfo.GetOrAdd<ItemInstance4Insertion>();
                if (insertion.BuffTickTimer <= 0)
                {
                    insertion.BuffTickTimer = LogicTime.time;
                }

                if (insertion.Lifetime <= 0 && def.AutoDestroyTime > 0)
                {
                    insertion.Lifetime = def.AutoDestroyTime;
                }
            }
        }

        static long GetInitialUseCharges(string itemId)
        {
            EnsureUseCache();
            if (string.IsNullOrEmpty(itemId)
                || _usesByItemId == null
                || !_usesByItemId.TryGetValue(itemId, out var uses)
                || uses == null)
            {
                return 1;
            }

            long result = 0;
            for (int i = 0; i < uses.Count; i++)
            {
                var use = uses[i];
                if (GetUseConsumePolicy(use) != EItemUseConsumePolicy.InstanceCharge)
                {
                    continue;
                }

                result = System.Math.Max(result, use.InitialCharges);
            }

            return result > 0 ? result : 1;
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

            if (intrinsic >= quasiUnlimitedThreshold)
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

            switch (def.StackProfile)
            {
                case EItemStackProfile.Single:
                    return 1;
                case EItemStackProfile.Tiny:
                    return 5;
                case EItemStackProfile.Small:
                    return 10;
                case EItemStackProfile.Normal:
                    return 20;
                case EItemStackProfile.Bulk:
                    return 50;
                case EItemStackProfile.Large:
                    return 99;
                case EItemStackProfile.Massive:
                    return 500;
                case EItemStackProfile.Unlimited:
                    return 999_999;
                case EItemStackProfile.Custom:
                default:
                    return def.StackCount > 0 ? def.StackCount : 10;
            }
        }

        // 负重：单件物品基础重量（按 stack_profile 档位估算，后续可改为独立配表字段）。
        public static long GetItemUnitWeight(string itemId)
        {
            var def = GetItemDef(itemId);
            if (def == null)
            {
                return 0;
            }

            if (ItemTagCatalog.HasTag(def, EItemTag.Big))
            {
                return 50;
            }

            switch (def.StackProfile)
            {
                case EItemStackProfile.Single:
                    return 1;
                case EItemStackProfile.Tiny:
                    return 1;
                case EItemStackProfile.Small:
                    return 2;
                case EItemStackProfile.Normal:
                    return 5;
                case EItemStackProfile.Bulk:
                    return 10;
                case EItemStackProfile.Large:
                    return 20;
                case EItemStackProfile.Massive:
                    return 40;
                case EItemStackProfile.Unlimited:
                    return 1;
                case EItemStackProfile.Custom:
                default:
                    return def.StackCount > 0 ? Math.Max(1, def.StackCount / 10) : 5;
            }
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

        public static bool IsQuickBarWeapon(string itemId)
        {
            return ItemTagCatalog.HasTag(itemId, EItemTag.HumanWeapon);
        }

        public static bool IsQuickBarConsumable(string itemId)
        {
            return ItemTagCatalog.HasTag(itemId, EItemTag.HumanTool);
        }
    }
}
