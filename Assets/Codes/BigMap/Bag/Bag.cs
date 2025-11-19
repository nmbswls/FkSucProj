using Config;
using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static My.UI.AnyContainerItemCell;

namespace My.Player.Bag
{

    [Serializable]
    public class ItemStack
    {
        public string ItemID;
        public int Count;

        public ItemStack()
        {

        }
        public ItemStack(string id, int count)
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

        public int AddToStack(int amount, int maxStack)
        {
            int canAdd = Math.Max(0, maxStack - Count);
            int added = Math.Min(canAdd, amount);
            Count += added;
            return added;
        }

        public int RemoveFromStack(int amount)
        {
            int removed = Math.Min(amount, Count);
            Count -= removed;
            return removed;
        }

        public bool IsEmpty => string.IsNullOrEmpty(ItemID) || Count <= 0;
    }

    [Serializable]
    public class PlayerBag
    {
        public int BagId;
        public int BasicCapacity = 30;
        public int MaxExtraCapacity = 10;
        
        public List<ItemStack> NormalSlots = new List<ItemStack>();
        public List<ItemStack> ExtraSlots = new List<ItemStack>();
        public event Action EvOnBagUpdate;

        public bool Locked = false;

        public int GetMaxStack(string itemId)
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

        public int TryCostItem(string itemId, int costItem)
        {
            int leftCount = costItem;
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
                if (NormalSlots[i].Count <= 0)
                {
                    NormalSlots[i] = null;
                }
            }

            for (int i = ExtraSlots.Count - 1; i >= 0; i--)
            {
                if (ExtraSlots[i].Count <= 0 || ExtraSlots[i] == null)
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
        public int TryAdd(ItemStack incoming)
        {
            if (incoming == null || incoming.IsEmpty) return 0;
            int remaining = incoming.Count;

            var maxStack = GetMaxStack(incoming.ItemID);
            // 先尝试再普通格子里堆叠
            for (int i = 0; i < NormalSlots.Count && remaining > 0; i++)
            {
                var s = NormalSlots[i];
                if (s != null && s.ItemID == incoming.ItemID && s.Count < GetMaxStack(s.ItemID))
                {
                    int added = s.AddToStack(remaining, maxStack);
                    remaining -= added;
                }
            }
            // 再找普通格子空位
            for (int i = 0; i < NormalSlots.Count && remaining > 0; i++)
            {
                if (NormalSlots[i] == null || NormalSlots[i].IsEmpty)
                {
                    int max = GetMaxStack(incoming.ItemID);
                    int put = Mathf.Min(max, remaining);
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
                        int added = s.AddToStack(remaining, maxStack);
                        remaining -= added;
                    }
                }

                if (remaining > 0)
                {
                    // 额外还有空位
                    while (ExtraSlots.Count < MaxExtraCapacity && remaining > 0)
                    {
                        int put = Mathf.Min(maxStack, remaining);
                        ExtraSlots.Add(new ItemStack(incoming.ItemID, put));
                        remaining -= put;
                    }
                }
            }

            return incoming.Count - remaining;
        }

        // 尝试将物品放到指定格子（堆叠或交换），返回成功移动数量
        public int TryAddToIndexOrStack(ItemStack incoming, int dstIndex)
        {
            if (incoming == null || incoming.IsEmpty) return 0;

            var maxStack = GetMaxStack(incoming.ItemID);
            if (dstIndex < NormalSlots.Count)
            {
                var dst = NormalSlots[dstIndex];
                // 放入空的
                if (dst == null || dst.IsEmpty)
                {
                    int put = Mathf.Min(maxStack, incoming.Count);
                    NormalSlots[dstIndex] = new ItemStack(incoming.ItemID, put);
                    return put;
                }
                // 同类堆叠
                if (dst.ItemID == incoming.ItemID && dst.Count < maxStack)
                {
                    int added = dst.AddToStack(incoming.Count, maxStack);
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
                    int added = dst.AddToStack(incoming.Count, maxStack);
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
                int put = Mathf.Min(maxStack, incoming.Count);
                NormalSlots[dstIndex] = new ItemStack(incoming.ItemID, put);
                return put;
            }
        }


        public bool TrySplit(int srcIndex, int count)
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


        public int RemoveAt(int index, int count)
        {
            if (index < NormalSlots.Count)
            {
                var s = NormalSlots[index];
                if (s == null) return 0;
                int removed = s.RemoveFromStack(count);
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
                return;
            }

            if(idx - BasicCapacity < ExtraSlots.Count)
            {
                ExtraSlots[idx - BasicCapacity] = item;
                return;
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

        public int AddItem(int bagId, int idx, string itemId, int amount)
        {

            var bag = GetBagById(bagId);
            if (bag == null)
            {
                return 0;
            }

            int put = bag.TryAddToIndexOrStack(new ItemStack() { ItemID = itemId, Count = amount}, idx);
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

            var src = srcBag.GetItemByIdx(srcIndex);
            var dst = dstBag.GetItemByIdx(dstIndex);
            if (src == null || src.IsEmpty) return false;

            var maxStackDst = dstBag.GetMaxStack(dst.ItemID);

            // 同类堆叠
            if (dst != null && !dst.IsEmpty && dst.ItemID == src.ItemID && dst.Count < maxStackDst)
            {
                int moved = dst.AddToStack(src.Count, maxStackDst);
                src.RemoveFromStack(moved);
                if (src.Count <= 0) srcBag.ClearEmptyItems();
                return true;
            }

            // 交换或移动
            if(dst == null)
            {
                int dstStackMax = dstBag.GetMaxStack(src.ItemID);
                if(src.Count > dstStackMax)
                {
                    Debug.Log("TrySwapOrMove dst stack fail.");
                    return false;
                }
                dstBag.SetItemData(dstIndex, src);
            }
            else
            {
                int dstStackMax = dstBag.GetMaxStack(src.ItemID);
                if (src.Count > dstStackMax)
                {
                    Debug.Log("TrySwapOrMove dst stack fail.");
                    return false;
                }

                int srcStackMax = srcBag.GetMaxStack(dst.ItemID);
                if (dst.Count > srcStackMax)
                {
                    Debug.Log("TrySwapOrMove src stack fail.");
                    return false;
                }

                // 交换
                srcBag.SetItemData(srcIndex, dst);
                dstBag.SetItemData(dstIndex, src);
            }
            
            //NormalSlots[dstIndex] = src;
            //NormalSlots[srcIndex] = dst;
            return true;
        }

        public PlayerBag GetBagById(int bagId)
        {
            if (bagId == 0) return MainBag;
            SpeBags.TryGetValue(bagId, out var bag);
            return bag;
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
    }
}
