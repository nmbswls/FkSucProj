using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bag
{

    public static class FakeItemDatabase
    {
        [Header("Mock Items")]
        public static List<ItemData> Items = new List<ItemData>();

        //private Dictionary<string, ItemData> dict = new Dictionary<string, ItemData>();

        //void Awake()
        //{
        //    Instance = this;
        //    dict.Clear();
        //    foreach (var it in Items)
        //    {
        //        if (!dict.ContainsKey(it.ItemID))
        //            dict.Add(it.ItemID, it);
        //    }
        //}

        public static Sprite GetIcon(string id)
        {
            //if (Instance == null) return null;
            //return Instance.dict.TryGetValue(id, out var data) ? data.Icon : null;
            return null;
        }

        public static int GetMaxStack(string id)
        {
            return 99;
        }

        public static bool CanUse(string id)
        {
            //if (Instance == null) return false;
            if(id == "banana")
            {
                return true;
            }
            //return Instance.dict.TryGetValue(id, out var data) ? data.Usable : false;
            return false;
        }

        //public static bool CanEquip(string id, out string slot)
        //{
        //    slot = "";
        //    if (Instance == null) return false;
        //    if (Instance.dict.TryGetValue(id, out var data))
        //    {
        //        slot = data.EquipSlot;
        //        return !string.IsNullOrEmpty(slot);
        //    }
        //    return false;
        //}
    }


    [Serializable]
    public class ItemStack
    {
        public string ItemID;
        public int Count;

        public ItemStack() { }
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

        //public int MaxStack => ItemDatabase.GetMaxStack(ItemID);
        public int MaxStack { get { return 999; } }

        public int AddToStack(int amount)
        {
            int canAdd = Math.Max(0, MaxStack - Count);
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

    [System.Serializable]
    public class PlayerInventoryModel
    {
        public int Capacity = 30;
        public List<ItemStack> Slots = new List<ItemStack>();

        public PlayerInventoryModel(int capacity)
        {
            Capacity = capacity;
            Slots = new List<ItemStack>(capacity);
            for (int i = 0; i < capacity; i++)
                Slots.Add(null);
        }

        // 尝试向背包添加（堆叠优先，再找空位），返回成功放入数量
        public int TryAdd(ItemStack incoming)
        {
            if (incoming == null || incoming.IsEmpty) return 0;
            int remaining = incoming.Count;
            // 先堆叠
            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                var s = Slots[i];
                if (s != null && s.ItemID == incoming.ItemID && s.Count < s.MaxStack)
                {
                    int added = s.AddToStack(remaining);
                    remaining -= added;
                }
            }
            // 再找空位
            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                if (Slots[i] == null || Slots[i].IsEmpty)
                {
                    int max = FakeItemDatabase.GetMaxStack(incoming.ItemID);
                    int put = Mathf.Min(max, remaining);
                    Slots[i] = new ItemStack(incoming.ItemID, put);
                    remaining -= put;
                }
            }
            return incoming.Count - remaining;
        }

        // 尝试将物品放到指定格子（堆叠或交换），返回成功移动数量
        public int TryAddToIndexOrStack(ItemStack incoming, int dstIndex)
        {
            if (incoming == null || incoming.IsEmpty) return 0;
            var dst = Slots[dstIndex];
            if (dst == null || dst.IsEmpty)
            {
                int max = FakeItemDatabase.GetMaxStack(incoming.ItemID);
                int put = Mathf.Min(max, incoming.Count);
                Slots[dstIndex] = new ItemStack(incoming.ItemID, put);
                return put;
            }
            // 同类堆叠
            if (dst.ItemID == incoming.ItemID && dst.Count < dst.MaxStack)
            {
                int added = dst.AddToStack(incoming.Count);
                return added;
            }
            // 不同物品：交换（把 incoming 放入，原 dst 移到最合适位置）
            // 简化：直接交换 src/dst 由控制器处理，此处仅返回0表示不能堆叠
            return 0;
        }

        public bool TryMove(int srcIndex, int dstIndex)
        {
            if (srcIndex == dstIndex) return false;
            var src = Slots[srcIndex];
            var dst = Slots[dstIndex];
            if (src == null || src.IsEmpty) return false;

            // 同类堆叠
            if (dst != null && !dst.IsEmpty && dst.ItemID == src.ItemID && dst.Count < dst.MaxStack)
            {
                int moved = dst.AddToStack(src.Count);
                src.RemoveFromStack(moved);
                if (src.Count <= 0) Slots[srcIndex] = null;
                return true;
            }
            // 交换
            Slots[dstIndex] = src;
            Slots[srcIndex] = dst;
            return true;
        }

        public bool TrySplit(int srcIndex, int count)
        {
            var src = Slots[srcIndex];
            if (src == null || src.IsEmpty) return false;
            if (count <= 0 || count >= src.Count) return false;

            // 找空位
            int emptyIdx = Slots.FindIndex(s => s == null || s.IsEmpty);
            if (emptyIdx < 0) return false;

            src.RemoveFromStack(count);
            Slots[emptyIdx] = new ItemStack(src.ItemID, count);
            return true;
        }

        public void RemoveAt(int index, int count)
        {
            var s = Slots[index];
            if (s == null) return;
            s.RemoveFromStack(count);
            if (s.Count <= 0) Slots[index] = null;
        }
    }


    public interface ILootableObj
    {
        List<ItemStack> LootItems { get; }
        void Add(ItemStack s);

        void RemoveFromIndex(int index, int count);
    }
}
