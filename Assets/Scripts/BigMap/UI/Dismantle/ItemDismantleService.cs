using System;
using System.Collections.Generic;
using My.Config;
using My.Player;
using My.Player.Bag;
using UnityEngine;

namespace My.UI.Dismantle
{
    public static class ItemDismantleService
    {
        public static bool CanDismantle(PlayerInventorySystem inventory, EPlayerBagId bagId,
            string itemId, long amount, out string reason)
        {
            reason = string.Empty;
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext())
            {
                reason = "只能在秘密基地内进行分解。";
                return false;
            }

            var bag = ResolveBag(inventory, bagId);
            if (bag == null || string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                reason = "无效的分解请求。";
                return false;
            }

            var def = ItemCatalog.GetItemDef(itemId);
            if (def == null || !def.CanDismantle || !ItemDismantleCatalog.CanDismantle(itemId))
            {
                reason = "该物品不能分解。";
                return false;
            }

            if (ItemCatalog.RequiresInstance(def) || !def.Stackable)
            {
                reason = "当前仅支持分解可堆叠的普通物品。";
                return false;
            }

            if (bag.GetItemCount(itemId) < amount)
            {
                reason = bagId == EPlayerBagId.Storage ? "仓库中的数量不足。" : "随身背包中的数量不足。";
                return false;
            }

            var outputs = ItemDismantleCatalog.BuildOutputs(itemId, amount);
            if (outputs.Count == 0)
            {
                reason = "没有配置有效的分解产物。";
                return false;
            }

            return CanFitAfterExchange(bag, itemId, amount, outputs, out reason);
        }

        public static bool TryDismantle(EPlayerBagId bagId, string itemId, long amount, out string reason)
        {
            var inventory = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            if (!CanDismantle(inventory, bagId, itemId, amount, out reason))
            {
                return false;
            }

            var bag = ResolveBag(inventory, bagId);
            var normalSnapshot = CloneSlots(bag.NormalSlots);
            var extraSnapshot = CloneSlots(bag.ExtraSlots);

            try
            {
                if (bag.TryCostItem(itemId, amount) != 0)
                {
                    reason = "扣除待分解物品失败。";
                    Restore(bag, normalSnapshot, extraSnapshot);
                    return false;
                }

                foreach (var output in ItemDismantleCatalog.BuildOutputs(itemId, amount))
                {
                    if (bag.TryGiveItem(output.ItemId, output.Count) != output.Count)
                    {
                        reason = "当前容器空间不足，分解已回滚。";
                        Restore(bag, normalSnapshot, extraSnapshot);
                        return false;
                    }
                }

                reason = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                reason = "分解执行失败，物品已回滚。";
                Restore(bag, normalSnapshot, extraSnapshot);
                return false;
            }
        }

        static PlayerBag ResolveBag(PlayerInventorySystem inventory, EPlayerBagId bagId)
        {
            if (inventory == null) return null;
            return bagId == EPlayerBagId.Storage ? inventory.WarehouseBag : inventory.MainBag;
        }

        static bool CanFitAfterExchange(PlayerBag bag, string costItemId, long costCount,
            IReadOnlyList<DismantleOutput> outputs, out string reason)
        {
            var normal = ToVirtualSlots(bag.NormalSlots);
            var extra = ToVirtualSlots(bag.ExtraSlots);
            if (!RemoveVirtual(normal, extra, costItemId, costCount))
            {
                reason = "当前容器中的数量不足。";
                return false;
            }

            foreach (var output in outputs)
            {
                var def = ItemCatalog.GetItemDef(output.ItemId);
                if (def == null || ItemCatalog.RequiresInstance(def) || !def.Stackable)
                {
                    reason = "分解产物包含当前不支持的实例物品。";
                    return false;
                }

                if (!AddVirtual(bag, normal, extra, output.ItemId, output.Count))
                {
                    reason = "当前容器没有足够空间容纳全部分解产物。";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        static List<VirtualSlot> ToVirtualSlots(List<ItemStack> source)
        {
            var result = new List<VirtualSlot>(source.Count);
            foreach (var stack in source)
                result.Add(stack == null || stack.IsEmpty ? default : new VirtualSlot(stack.ItemID, stack.Count));
            return result;
        }

        static bool RemoveVirtual(List<VirtualSlot> normal, List<VirtualSlot> extra, string itemId, long amount)
        {
            amount = RemoveVirtualFrom(normal, itemId, amount);
            amount = RemoveVirtualFrom(extra, itemId, amount);
            return amount == 0;
        }

        static long RemoveVirtualFrom(List<VirtualSlot> slots, string itemId, long amount)
        {
            for (int i = 0; i < slots.Count && amount > 0; i++)
            {
                var slot = slots[i];
                if (slot.ItemId != itemId || slot.Count <= 0) continue;
                var removed = Math.Min(slot.Count, amount);
                slot.Count -= removed;
                amount -= removed;
                if (slot.Count <= 0) slot = default;
                slots[i] = slot;
            }
            return amount;
        }

        static bool AddVirtual(PlayerBag bag, List<VirtualSlot> normal, List<VirtualSlot> extra,
            string itemId, long amount)
        {
            var maxStack = bag.GetMaxStack(itemId);
            amount = FillExisting(normal, itemId, amount, maxStack);
            amount = FillEmpty(normal, itemId, amount, maxStack);
            amount = FillExisting(extra, itemId, amount, maxStack);
            while (amount > 0 && extra.Count < bag.MaxExtraCapacity)
            {
                var placed = Math.Min(maxStack, amount);
                extra.Add(new VirtualSlot(itemId, placed));
                amount -= placed;
            }
            return amount == 0;
        }

        static long FillExisting(List<VirtualSlot> slots, string itemId, long amount, long maxStack)
        {
            for (int i = 0; i < slots.Count && amount > 0; i++)
            {
                var slot = slots[i];
                if (slot.ItemId != itemId || slot.Count >= maxStack) continue;
                var placed = Math.Min(maxStack - slot.Count, amount);
                slot.Count += placed;
                slots[i] = slot;
                amount -= placed;
            }
            return amount;
        }

        static long FillEmpty(List<VirtualSlot> slots, string itemId, long amount, long maxStack)
        {
            for (int i = 0; i < slots.Count && amount > 0; i++)
            {
                if (!string.IsNullOrEmpty(slots[i].ItemId) && slots[i].Count > 0) continue;
                var placed = Math.Min(maxStack, amount);
                slots[i] = new VirtualSlot(itemId, placed);
                amount -= placed;
            }
            return amount;
        }

        static List<ItemStack> CloneSlots(List<ItemStack> source)
        {
            var result = new List<ItemStack>(source.Count);
            foreach (var stack in source)
            {
                result.Add(stack == null ? null : new ItemStack(stack.ItemID, stack.Count)
                {
                    ItemInstanceId = stack.ItemInstanceId,
                    InstanceInfo = stack.InstanceInfo?.Clone(),
                });
            }
            return result;
        }

        static void Restore(PlayerBag bag, List<ItemStack> normal, List<ItemStack> extra)
        {
            bag.NormalSlots = normal;
            bag.ExtraSlots = extra;
            bag.AfterSlotMutation();
        }

        struct VirtualSlot
        {
            public VirtualSlot(string itemId, long count) { ItemId = itemId; Count = count; }
            public string ItemId;
            public long Count;
        }
    }
}
