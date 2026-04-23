

using My.Config;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace My.Player
{
    


    public interface IItemContainer
    {
        long GetMaxStack(string itemId);

        /// <summary>
        /// 写入指定槽位的道具堆（null 表示清空）
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="item"></param>
        void SetItemData(int idx, ItemStack item);

        /// <summary>
        /// 仅修改指定槽中堆叠的数量
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="item"></param>
        void SetItemCount(int idx, long count);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool IsSlotIdxValid(int slotIDx);


        /// <summary>
        /// 统计指定道具 ID 在本容器内的总数量
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        long GetItemCount(string itemId);

        /// <summary>
        /// 读取指定槽位的道具堆
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        ItemStack GetItemByIdx(int idx);
    }

    public enum EContainerType
    {
        Inventory,
        LootPoint,
        SpecialInventory,
        Shop,
        /// <summary>
        /// 仓库分页格；与 Inventory 叠加上限规则一致。
        /// </summary>
        Warehouse,
    }

    /// <summary>
    /// 仓库分页：BagId 为 BagIdFirst + 页索引（0..PageCount-1）。
    /// </summary>
    public static class WarehouseConfig
    {
        public const int BagIdFirst = 100;
        public const int PageCount = 4;
        public const int SlotsPerPage = 45;
    }

    [Serializable]
    public class PlayerBag : IItemContainer
    {
        public EPlayerBagId BagId;
        public int BasicCapacity = 30;
        public int MaxExtraCapacity = 10;

        public List<ItemStack> NormalSlots = new List<ItemStack>();
        public List<ItemStack> ExtraSlots = new List<ItemStack>();
        public event Action EvOnBagUpdate;

        public bool Locked = false;

        public long GetMaxStack(string itemId)
        {
            // 主背包与特殊背包按容器类型取不同叠加上限
            if (BagId == 0)
            {
                return ItemCatalog.GetMaxStackByType(itemId, EContainerType.Inventory);
            }
            if (BagId >= WarehouseConfig.BagIdFirst && BagId < WarehouseConfig.BagIdFirst + WarehouseConfig.PageCount)
            {
                return ItemCatalog.GetMaxStackByType(itemId, EContainerType.Warehouse);
            }
            return ItemCatalog.GetMaxStackByType(itemId, EContainerType.SpecialInventory);
        }

        public long GetItemCount(string itemId)
        {
            long totalNum = 0;

            foreach (var slot in NormalSlots)
            {
                if (slot == null) continue;
                if (slot.ItemID != itemId) { continue; }

                totalNum += slot.Count;

            }

            foreach (var slot in ExtraSlots)
            {
                if (slot == null) continue;
                if (slot.ItemID != itemId) { continue; }

                totalNum += slot.Count;
            }

            return totalNum;
        }

        public long TryCostItem(string itemId, long costItem)
        {
            long leftCount = costItem;
            if (leftCount > 0)
            {
                foreach (var slot in NormalSlots)
                {
                    if (slot == null) continue;
                    if (slot.ItemID != itemId) { continue; }

                    if (slot.Count > leftCount)
                    {
                        slot.Count -= leftCount;
                        leftCount = 0;
                    }
                    else
                    {
                        leftCount -= slot.Count;
                        slot.Count = 0;
                    }

                    if (leftCount <= 0)
                    {
                        break;
                    }
                }
            }

            if (leftCount > 0)
            {
                foreach (var slot in ExtraSlots)
                {
                    if (slot == null) continue;
                    if (slot.ItemID != itemId) { continue; }

                    if (slot.Count > leftCount)
                    {
                        slot.Count -= leftCount;
                        leftCount = 0;
                    }
                    else
                    {
                        leftCount -= slot.Count;
                        slot.Count = 0;
                    }

                    if (leftCount <= 0)
                    {
                        break;
                    }
                }
            }

            ClearEmptyItems();

            return leftCount;
        }

        public void ClearEmptyItems()
        {
            for (int i = 0; i < NormalSlots.Count; i++)
            {
                if (NormalSlots[i] != null && NormalSlots[i].Count <= 0)
                {
                    NormalSlots[i] = null;
                }
            }

            for (int i = ExtraSlots.Count - 1; i >= 0; i--)
            {
                if (ExtraSlots[i] == null || ExtraSlots[i].Count <= 0)
                {
                    ExtraSlots.RemoveAt(i);
                }
            }
        }

        public ItemStack GetItemByIdx(int idx)
        {
            if (idx < NormalSlots.Count)
            {
                return NormalSlots[idx];
            }

            if (idx - BasicCapacity < ExtraSlots.Count)
            {
                return ExtraSlots[idx - BasicCapacity];
            }

            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="extraCapacity"></param>
        public void InitBag(EPlayerBagId bagId, int capacity, int extraCapacity)
        {
            this.BagId = bagId;
            this.BasicCapacity = capacity;
            this.MaxExtraCapacity = extraCapacity;

            for (int i = 0; i < capacity; i++)
            {
                NormalSlots.Add(null);
            }
        }

        /// <summary>
        /// 将道具发放进本背包：优先槽合并、再全表合并、空位新建堆，最后扩展栏
        /// </summary>
        /// <param name="incoming"></param>
        /// <returns></returns>
        public long TryGiveItem(string itemId, long count, int preferredIdx = -1)
        {
            if (itemId == null || count <= 0) return 0;
            var itemConf = ItemCatalog.GetItemDef(itemId);
            if (itemConf == null) return 0;

            long remaining = count;



            var maxStack = GetMaxStack(itemId);
            // 若指定了优先槽，先尝试在该普通栏位合并或占位
            if (preferredIdx != -1)
            {
                // 优先槽须落在普通栏索引范围内
                if (preferredIdx < NormalSlots.Count)
                {
                    var s = NormalSlots[preferredIdx];
                    if (s != null && s.ItemID == itemId && s.Count < GetMaxStack(s.ItemID))
                    {
                        var added = s.AddToStack(remaining, maxStack);
                        remaining -= added;
                    }
                    else if (s == null || s.Count <= 0)
                    {
                        var put = Math.Min(maxStack, remaining);
                        NormalSlots[preferredIdx] = ItemCatalog.CreateItemStack(itemId, put);
                        remaining -= put;
                    }
                }
            }

            // 扫描普通栏：向已有同 ID 且未满的堆合并
            for (int i = 0; i < NormalSlots.Count && remaining > 0; i++)
            {
                var s = NormalSlots[i];
                if (s != null && s.ItemID == itemId && s.Count < GetMaxStack(s.ItemID))
                {
                    var added = s.AddToStack(remaining, maxStack);
                    remaining -= added;
                }
            }

            if (remaining <= 0)
            {
                return count;
            }

            // 普通栏找空槽新建堆叠
            for (int i = 0; i < NormalSlots.Count && remaining > 0; i++)
            {
                if (NormalSlots[i] == null || NormalSlots[i].IsEmpty)
                {
                    var put = Math.Min(maxStack, remaining);
                    NormalSlots[i] = ItemCatalog.CreateItemStack(itemId, put);
                    remaining -= put;
                }
            }

            if (remaining <= 0)
            {
                return count;
            }

            // 仍有剩余且允许扩展栏：先合并进已有扩展堆
            if (MaxExtraCapacity > 0)
            {
                // 扩展栏内同 ID 未满格
                for (int i = 0; i < ExtraSlots.Count && remaining > 0; i++)
                {
                    var s = ExtraSlots[i];
                    if (s != null && s.ItemID == itemId && s.Count < maxStack)
                    {
                        var added = s.AddToStack(remaining, maxStack);
                        remaining -= added;
                    }
                }

                if (remaining > 0)
                {
                    // 扩展栏未满则追加新堆，直到扩展上限
                    while (ExtraSlots.Count < MaxExtraCapacity && remaining > 0)
                    {
                        var put = Math.Min(maxStack, remaining);
                        var newItem = ItemCatalog.CreateItemStack(itemId, put);
                        ExtraSlots.Add(newItem);
                        remaining -= put;
                    }
                }
            }

            return count - remaining;
        }
        public bool TrySplit(int srcIndex, long count)
        {
            // 仅在普通栏支持拆分（扩展栏逻辑未实现）
            if (srcIndex < NormalSlots.Count)
            {
                var src = NormalSlots[srcIndex];
                if (src == null || src.IsEmpty) return false;
                if (count <= 0 || count >= src.Count) return false;

                // 带实例 ID 的堆叠不允许拆分
                if (src.ItemInstanceId != 0)
                {
                    return false;
                }

                // 找第一个空槽放置拆出的数量
                int emptyIdx = NormalSlots.FindIndex(s => s == null || s.IsEmpty);
                if (emptyIdx < 0) return false;

                src.RemoveFromStack(count);
                NormalSlots[emptyIdx] = ItemCatalog.CreateItemStack(src.ItemID, count);
                return true;
            }

            return false;
        }


        public long RemoveAt(int index, long count)
        {
            if (index < NormalSlots.Count)
            {
                var s = NormalSlots[index];
                if (s == null) return 0;
                var removed = s.RemoveFromStack(count);
                if (s.Count <= 0) NormalSlots[index] = null;
                return removed;
            }

            return 0;
        }

        public void SetItemData(int idx, ItemStack item)
        {
            if (idx < NormalSlots.Count)
            {
                NormalSlots[idx] = item;
            }
            else if (idx - BasicCapacity < MaxExtraCapacity)
            {
                int extraIdx = idx - BasicCapacity;
                if (extraIdx < ExtraSlots.Count)
                {
                    ExtraSlots[extraIdx] = item;
                }
                else
                {
                    ExtraSlots.Add(item);
                }
                if (item == null)
                {
                    ClearEmptyItems();
                }
            }
            else
            {
                Debug.LogError("Err idx " + idx + " bag id: " + BagId);
            }
        }

        /// <summary>
        /// 槽位索引是否落在普通栏或扩展栏合法范围内
        /// </summary>
        /// <param name="slotIdx"></param>
        /// <returns></returns>
        public bool IsSlotIdxValid(int slotIdx)
        {
            if (slotIdx < NormalSlots.Count)
            {
                return true;
            }

            if (slotIdx - BasicCapacity <= ExtraSlots.Count)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按全局槽索引修改对应堆叠的数量
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="count"></param>
        public void SetItemCount(int idx, long count)
        {
            if (idx < NormalSlots.Count)
            {
                if (NormalSlots[idx] != null)
                {
                    NormalSlots[idx].Count = count;
                }
            }
            else if (idx - BasicCapacity < MaxExtraCapacity)
            {
                int extraIdx = idx - BasicCapacity;
                if (extraIdx < ExtraSlots.Count)
                {
                    ExtraSlots[extraIdx].Count = count;
                }
            }
            else
            {
                Debug.LogError("SetItemCount Err idx " + idx + " bag id: " + BagId);
            }
        }
    }


    public enum EPlayerBagId
    {
        Default,
        Secret = 1,
        Pet = 2,
        Storage,
    }
}