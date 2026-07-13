using System;
using cfg.demo;
using My.Config;

namespace My.Player
{
    [Serializable]
    public struct QuickSlotBinding
    {
        public string ItemId;
        public long ItemInstanceId;

        public bool IsEmpty => string.IsNullOrEmpty(ItemId);

        public static QuickSlotBinding Empty => new QuickSlotBinding { ItemId = string.Empty, ItemInstanceId = 0 };

        public static QuickSlotBinding Fungible(string itemId)
        {
            return new QuickSlotBinding { ItemId = itemId ?? string.Empty, ItemInstanceId = 0 };
        }

        public static QuickSlotBinding Pinned(string itemId, long itemInstanceId)
        {
            return new QuickSlotBinding { ItemId = itemId ?? string.Empty, ItemInstanceId = itemInstanceId };
        }
    }

    public static class QuickSlotAssignRules
    {
        public static bool RequiresInstancePin(ItemData def)
        {
            if (def == null)
            {
                return false;
            }

            return ItemCatalog.RequiresInstance(def) || !def.Stackable;
        }

        public static bool TryNormalizeAssign(string itemId, long itemInstanceId, out QuickSlotBinding binding, out string failReason)
        {
            binding = QuickSlotBinding.Empty;
            failReason = null;

            if (string.IsNullOrEmpty(itemId))
            {
                failReason = "empty_item";
                return false;
            }

            var def = ItemCatalog.GetItemDef(itemId);
            if (def == null)
            {
                failReason = "no_item_def";
                return false;
            }

            if (RequiresInstancePin(def))
            {
                if (itemInstanceId == 0)
                {
                    failReason = "need_instance";
                    return false;
                }

                binding = QuickSlotBinding.Pinned(itemId, itemInstanceId);
                return true;
            }

            binding = QuickSlotBinding.Fungible(itemId);
            return true;
        }
    }
}
