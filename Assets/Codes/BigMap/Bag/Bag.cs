using Config;
using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static My.UI.AnyContainerItemCell;
using static UnityEditor.Progress;

namespace My.Player.Bag
{

    [Serializable]
    public class ItemStack
    {
        public string ItemID;
        public long Count;
        public long ItemInstanceId;

        public ItemStack()
        {

        }
        public ItemStack(string id, long count)
        {
            ItemID = id;
            Count = count;
        }

        public ItemStack Clone()
        {
            return new ItemStack(ItemID, Count);
        }

        public bool CanStackWith(ItemStack other)
        {
            if (other == null) return false;
            return other.ItemID == ItemID;
        }

        public long AddToStack(long amount, long maxStack)
        {
            long canAdd = Math.Max(0, maxStack - Count);
            long added = Math.Min(canAdd, amount);
            Count += added;
            return added;
        }

        public long RemoveFromStack(long amount)
        {
            long removed = Math.Min(amount, Count);
            Count -= removed;
            return removed;
        }

        public bool IsEmpty => string.IsNullOrEmpty(ItemID) || Count <= 0;
    }

    public static class ItemUtils
    {
        /// <summary>
        /// 移动或交换
        /// </summary>
        /// <returns></returns>
        public static bool MoveOrMergeOrSwapItem(IItemContainer srcContainer, int srcIdx, IItemContainer dstContainer, int dstIdx)
        {
            // 检查源idx是否合法
            if(!srcContainer.IsSlotIdxValid(srcIdx))
            {
                return false;
            }

            // 检查目标idx是否合法
            if (!dstContainer.IsSlotIdxValid(dstIdx))
            {
                return false;
            }

            // 检查源item是否存在且合法
            var srcItem = srcContainer.GetItemByIdx(srcIdx);
            if(srcItem == null || srcItem.Count <= 0)
            {
                Debug.LogError($"ItemUtils MoveOrMergeItem {srcIdx} {dstIdx}");
                return false;
            }

            var dstItem = dstContainer.GetItemByIdx(dstIdx);

            // 目标为空 可移动或部分移动
            if(dstItem == null || dstItem.Count <= 0)
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);

                // 全部移动
                if(srcItem.Count <= srcItemNewStackMax)
                {
                    dstContainer.SetItemData(dstIdx, srcItem);
                    srcContainer.SetItemData(srcIdx, null);
                    return true;
                }

                // 不支持部分移动
                if(srcItem.ItemInstanceId != 0)
                {
                    return false;
                }

                long canMove = srcItemNewStackMax;
                dstContainer.SetItemData(dstIdx, new ItemStack() { ItemID = srcItem.ItemID, Count = canMove });
                srcContainer.SetItemCount(srcIdx, srcItem.Count - canMove);

                return true;
            }
            // 目标为同类可堆叠 尝试合并
            else if (dstItem.ItemID == srcItem.ItemID && srcItem.ItemInstanceId == 0 && dstItem.ItemInstanceId == 0)
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);
                var dstItemNewStackMax = srcContainer.GetMaxStack(dstItem.ItemID);

                long canMove1 = srcItemNewStackMax - dstItem.Count;
                long canMove2 = dstItemNewStackMax - srcItem.Count;
                // 可移动数量太少 尝试交换
                if (canMove1 <= 0 && canMove2 <= 0)
                {
                    return false;
                }

                // 尝试两个方向的移动
                if(canMove1 > 0)
                {
                    dstContainer.SetItemCount(dstIdx, dstItem.Count + canMove1);
                    if (srcItem.Count - canMove1 <= 0)
                    {
                        srcContainer.SetItemData(srcIdx, null);
                    }
                    else
                    {
                        srcContainer.SetItemCount(srcIdx, srcItem.Count - canMove1);
                    }
                }
                else
                {
                    srcContainer.SetItemCount(srcIdx, srcItem.Count + canMove2);
                    if (dstItem.Count - canMove2 <= 0)
                    {
                        dstContainer.SetItemData(dstIdx, null);
                    }
                    else
                    {
                        dstContainer.SetItemCount(dstIdx, dstItem.Count - canMove2);
                    }
                }

                return true;
            }
            // 不可合并 尝试交换
            else
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);
                var dstItemNewStackMax = srcContainer.GetMaxStack(dstItem.ItemID);

                if(srcItem.Count > srcItemNewStackMax)
                {
                    return false;
                }
                if (dstItem.Count > dstItemNewStackMax)
                {
                    return false;
                }

                // 执行交换
                // todo 允许在有空位时强行移动过来
                dstContainer.SetItemData(dstIdx, srcItem);
                srcContainer.SetItemData(srcIdx, dstItem);

                return true;
            }
        }
    }

    public interface IItemContainer
    {
        long GetMaxStack(string itemId);

        /// <summary>
        /// 底层设置信息
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="item"></param>
        void SetItemData(int idx, ItemStack item);

        /// <summary>
        /// 修改数量
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
        /// 获取道具总量
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        long GetItemCount(string itemId);

        /// <summary>
        /// 获取目标item
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
    }

    [Serializable]
    public class PlayerBag : IItemContainer
    {
        public int BagId;
        public int BasicCapacity = 30;
        public int MaxExtraCapacity = 10;
        
        public List<ItemStack> NormalSlots = new List<ItemStack>();
        public List<ItemStack> ExtraSlots = new List<ItemStack>();
        public event Action EvOnBagUpdate;

        public bool Locked = false;

        public long GetMaxStack(string itemId)
        {
            // 普通背包这么读
            if(BagId == 0)
            {
                return FakeItemDatabase.GetMaxStackByType(itemId, EContainerType.Inventory);
            }
            return FakeItemDatabase.GetMaxStackByType(itemId, EContainerType.SpecialInventory);
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
            if(leftCount > 0)
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
            
            if(leftCount > 0)
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
            if(idx < NormalSlots.Count)
            {
                return NormalSlots[idx];
            }

            if(idx - BasicCapacity < ExtraSlots.Count)
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
        public void InitBag(int bagId, int capacity, int extraCapacity)
        {
            this.BagId = bagId;
            this.BasicCapacity = capacity;
            this.MaxExtraCapacity = extraCapacity;

            for(int i=0;i<capacity;i++)
            {
                NormalSlots.Add(null);
            }
        }

        /// <summary>
        /// 增加到背包 不指定位置
        /// </summary>
        /// <param name="incoming"></param>
        /// <returns></returns>
        public long TryAdd(ItemStack incoming)
        {
            if (incoming == null || incoming.IsEmpty) return 0;
            long remaining = incoming.Count;

            var maxStack = GetMaxStack(incoming.ItemID);
            // 先尝试再普通格子里堆叠
            for (int i = 0; i < NormalSlots.Count && remaining > 0; i++)
            {
                var s = NormalSlots[i];
                if (s != null && s.ItemID == incoming.ItemID && s.Count < GetMaxStack(s.ItemID))
                {
                    var added = s.AddToStack(remaining, maxStack);
                    remaining -= added;
                }
            }
            // 再找普通格子空位
            for (int i = 0; i < NormalSlots.Count && remaining > 0; i++)
            {
                if (NormalSlots[i] == null || NormalSlots[i].IsEmpty)
                {
                    var max = GetMaxStack(incoming.ItemID);
                    var put = Math.Min(max, remaining);
                    NormalSlots[i] = new ItemStack(incoming.ItemID, put);
                    remaining -= put;
                }
            }

            // 如果背包可超载 在超载中寻找
            if (remaining > 0 && MaxExtraCapacity > 0)
            {
                // 先堆叠
                for (int i = 0; i < ExtraSlots.Count && remaining > 0; i++)
                {
                    var s = ExtraSlots[i];
                    if (s != null && s.ItemID == incoming.ItemID && s.Count < maxStack)
                    {
                        var added = s.AddToStack(remaining, maxStack);
                        remaining -= added;
                    }
                }

                if (remaining > 0)
                {
                    // 额外还有空位
                    while (ExtraSlots.Count < MaxExtraCapacity && remaining > 0)
                    {
                        var put = Math.Min(maxStack, remaining);
                        ExtraSlots.Add(new ItemStack(incoming.ItemID, put));
                        remaining -= put;
                    }
                }
            }

            return incoming.Count - remaining;
        }

        // 尝试将物品放到指定格子（堆叠或交换），返回成功移动数量
        public long TryAddToIndexOrStack(ItemStack incoming, int dstIndex)
        {
            if (incoming == null || incoming.IsEmpty) return 0;

            var maxStack = GetMaxStack(incoming.ItemID);
            if (dstIndex < NormalSlots.Count)
            {
                var dst = NormalSlots[dstIndex];
                // 放入空的
                if (dst == null || dst.IsEmpty)
                {
                    var put = Math.Min(maxStack, incoming.Count);
                    NormalSlots[dstIndex] = new ItemStack(incoming.ItemID, put);
                    return put;
                }
                // 同类堆叠
                if (dst.ItemID == incoming.ItemID && dst.Count < maxStack)
                {
                    var added = dst.AddToStack(incoming.Count, maxStack);
                    return added;
                }
                return 0;
            }
            else if (dstIndex < BasicCapacity + ExtraSlots.Count)
            {
                var dst = ExtraSlots[dstIndex - BasicCapacity];
                // 放入空的
                if (dst == null || dst.IsEmpty)
                {
                    Debug.LogError("extra cant be null");
                    return 0;
                }
                // 同类堆叠
                if (dst.ItemID == incoming.ItemID && dst.Count < maxStack)
                {
                    var added = dst.AddToStack(incoming.Count, maxStack);
                    return added;
                }
                return 0;
            }
            // 放在末尾
            else
            {
                if(ExtraSlots.Count >= MaxExtraCapacity)
                {
                    return 0; 
                }
                var put = Math.Min(maxStack, incoming.Count);
                NormalSlots[dstIndex] = new ItemStack(incoming.ItemID, put);
                return put;
            }
        }


        public bool TrySplit(int srcIndex, long count)
        {
            // 只分离普通背包
            if (srcIndex < NormalSlots.Count)
            {
                var src = NormalSlots[srcIndex];
                if (src == null || src.IsEmpty) return false;
                if (count <= 0 || count >= src.Count) return false;

                // 找空位
                int emptyIdx = NormalSlots.FindIndex(s => s == null || s.IsEmpty);
                if (emptyIdx < 0) return false;

                src.RemoveFromStack(count);
                NormalSlots[emptyIdx] = new ItemStack(src.ItemID, count);
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
            else if(idx - BasicCapacity < MaxExtraCapacity)
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
        /// 是否是合法idx
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
        /// 设置item数量
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="count"></param>
        public void SetItemCount(int idx, long count)
        {
            if (idx < NormalSlots.Count)
            {
                if(NormalSlots[idx] != null)
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



    [System.Serializable]
    public class PlayerInventoryModel
    {
        public PlayerBag MainBag;

        public Dictionary<int, PlayerBag> SpeBags = new Dictionary<int, PlayerBag>();


        public PlayerInventoryModel()
        {
            MainBag = new();
            MainBag.InitBag(0, 60, 0);

            for(int i=1;i<=4;i++)
            {
                var bag = new PlayerBag();
                bag.InitBag(i, 5, 3);

                SpeBags[bag.BagId] = bag;
            }
        }

        public long AddItem(int bagId, int idx, string itemId, int amount)
        {

            var bag = GetBagById(bagId);
            if (bag == null)
            {
                return 0;
            }

            var put = bag.TryAddToIndexOrStack(new ItemStack() { ItemID = itemId, Count = amount}, idx);
            return put;
        }

        public long GiveItem(string itemId, long amount)
        {

            var bag = GetBagById(0);
            if (bag == null)
            {
                return 0;
            }

            var put = bag.TryAdd(new ItemStack() { ItemID = itemId, Count = amount });
            return put;
        }

        /// <summary>
        /// 尝试交换或移动
        /// </summary>
        /// <param name="srcBagId"></param>
        /// <param name="srcIndex"></param>
        /// <param name="dstBagId"></param>
        /// <param name="dstIndex"></param>
        /// <returns></returns>
        public bool TrySwapOrMove(int srcBagId, int srcIndex, int dstBagId, int dstIndex)
        {
            // 原地交换
            if (srcBagId == dstBagId  && srcIndex == dstIndex) return false;

            // 有个包找不到
            var srcBag = GetBagById(srcBagId); 
            var dstBag = GetBagById(dstBagId);
            if (srcBag == null || dstBag == null) return false;


            return ItemUtils.MoveOrMergeOrSwapItem(srcBag, srcIndex, dstBag, dstIndex);
        }

        public PlayerBag GetBagById(int bagId)
        {
            if (bagId == 0) return MainBag;
            SpeBags.TryGetValue(bagId, out var bag);
            return bag;
        }

        public bool CanGainItems(string itemId, long count)
        {
            if (count == 0)
                return true;
            var baseBag = MainBag;
            var maxStack = baseBag.GetMaxStack(itemId);
            int needSlot = (int)(((count - 1) / maxStack + 1));

            int empty = 0;
            foreach(var slot in baseBag.NormalSlots)
            {
                if (slot != null) continue;
                empty += 1;
                if(empty >= needSlot)
                {
                    return true;
                }
            }

            return false;
        }
    }


    public interface ILootableObj
    {
        List<ItemStack> LootItems { get; }

        bool IsRevealed(int itemIdx);

        void TickUnReveal(float dt);

        int GetCurrUnrealed();

        void Add(ItemStack s, int dstIdx);


        void RemoveFromIndex(int index, int count);

        EContainerType GetContainerType();

        event Action<int> EnOnUnrealed;

        IItemContainer GetLootItemContainer();
    }
}
