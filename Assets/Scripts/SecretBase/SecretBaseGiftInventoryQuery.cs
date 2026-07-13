using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using My.Player.Bag;
using UnityEngine;

namespace My.SecretBase
{
    public sealed class SecretBaseGiftStackEntry
    {
        public string ItemId;
        public long Count;
    }

    // 从主背包与仓库汇总可赠送礼物（Gift 类型且存在于 TbItemGift）
    public static class SecretBaseGiftInventoryQuery
    {
        public static List<SecretBaseGiftStackEntry> BuildList(PlayerInventorySystem inv)
        {
            var merged = new Dictionary<string, long>();
            if (inv == null)
            {
                return new List<SecretBaseGiftStackEntry>();
            }

            AccumulateBag(inv.MainBag, merged);
            AccumulateBag(inv.WarehouseBag, merged);

            var list = new List<SecretBaseGiftStackEntry>(merged.Count);
            foreach (var kv in merged)
            {
                list.Add(new SecretBaseGiftStackEntry { ItemId = kv.Key, Count = kv.Value });
            }

            list.Sort((a, b) => string.CompareOrdinal(a.ItemId, b.ItemId));
            return list;
        }

        static void AccumulateBag(PlayerBag bag, Dictionary<string, long> merged)
        {
            if (bag == null)
            {
                return;
            }

            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                TryAddStack(bag.NormalSlots[i], merged);
            }

            if (bag.ExtraSlots == null)
            {
                return;
            }

            for (int i = 0; i < bag.ExtraSlots.Count; i++)
            {
                TryAddStack(bag.ExtraSlots[i], merged);
            }
        }

        static void TryAddStack(ItemStack stack, Dictionary<string, long> merged)
        {
            if (stack == null || stack.Count <= 0 || string.IsNullOrEmpty(stack.ItemID))
            {
                return;
            }

            if (ItemCatalog.GetGiftDef(stack.ItemID) == null)
            {
                return;
            }

            if (merged.TryGetValue(stack.ItemID, out var prev))
            {
                merged[stack.ItemID] = prev + stack.Count;
            }
            else
            {
                merged[stack.ItemID] = stack.Count;
            }
        }

        public static bool HasGiftItem(PlayerInventorySystem inv, string itemId)
        {
            if (inv == null || string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            foreach (var e in BuildList(inv))
            {
                if (e.ItemId == itemId && e.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
