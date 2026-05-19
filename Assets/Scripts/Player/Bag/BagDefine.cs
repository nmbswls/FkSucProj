

using My;
using My.Config;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace My.Player
{

    

    [Serializable]
    public class PlayerBag : IItemContainer
    {
        public EPlayerBagId BagId;
        public EContainerType StackContainerType { get; private set; }
        public int BasicCapacity = 30;
        public int MaxExtraCapacity = 10;

        public List<ItemStack> NormalSlots = new List<ItemStack>();
        public List<ItemStack> ExtraSlots = new List<ItemStack>();
        public event Action EvOnBagUpdate;

        public bool Locked = false;

        public EBagStorageLayout StorageLayout { get; private set; }

        public static EBagStorageLayout ResolveStorageLayout(EPlayerBagId bagId)
        {
            return (int)bagId == 0 ? EBagStorageLayout.Grid : EBagStorageLayout.Compact;
        }

        public static EContainerType ResolveStackContainerType(EPlayerBagId bagId)
        {
            switch (bagId)
            {
                case EPlayerBagId.Storage:
                    return EContainerType.Warehouse;
                case EPlayerBagId.Default:
                    return EContainerType.Inventory;
                default:
                    return EContainerType.SpecialInventory;
            }
        }

        public long GetMaxStack(string itemId)
        {
            return ItemCatalog.GetMaxStackByType(itemId, StackContainerType);
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
            AfterSlotMutation();

            return leftCount;
        }

        public void ClearEmptyItems(bool stripNullExtras = true)
        {
            for (int i = 0; i < NormalSlots.Count; i++)
            {
                if (NormalSlots[i] != null && NormalSlots[i].Count <= 0)
                {
                    NormalSlots[i] = null;
                }
            }

            if (stripNullExtras)
            {
                for (int i = ExtraSlots.Count - 1; i >= 0; i--)
                {
                    if (ExtraSlots[i] == null || ExtraSlots[i].Count <= 0)
                    {
                        ExtraSlots.RemoveAt(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < ExtraSlots.Count; i++)
                {
                    if (ExtraSlots[i] != null && ExtraSlots[i].Count <= 0)
                    {
                        ExtraSlots[i] = null;
                    }
                }
            }
        }

        // 存档读入后的收口：不剔除扩展栏中的 null 占位，且不 FlushExtraIntoPrimaryWherePossible（避免打散稀疏 SlotIndex 对齐）。
        public void FinishHydrateMutation()
        {
            ClearEmptyItems(stripNullExtras: false);
            if (StorageLayout == EBagStorageLayout.Compact)
            {
                CompactPackPrimary();
            }

            EvOnBagUpdate?.Invoke();
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
            StackContainerType = ResolveStackContainerType(bagId);
            this.BasicCapacity = capacity;
            this.MaxExtraCapacity = extraCapacity;
            StorageLayout = ResolveStorageLayout(bagId);

            NormalSlots.Clear();
            ExtraSlots.Clear();

            for (int i = 0; i < capacity; i++)
            {
                NormalSlots.Add(null);
            }
        }

        public void CompactPackPrimary()
        {
            if (StorageLayout != EBagStorageLayout.Compact)
            {
                return;
            }

            int w = 0;
            for (int r = 0; r < NormalSlots.Count; r++)
            {
                var s = NormalSlots[r];
                if (s != null && !s.IsEmpty)
                {
                    if (w != r)
                    {
                        NormalSlots[w] = s;
                    }
                    w++;
                }
            }
            for (; w < NormalSlots.Count; w++)
            {
                NormalSlots[w] = null;
            }
        }

        bool NormalHasVacancy()
        {
            foreach (var s in NormalSlots)
            {
                if (s == null || s.IsEmpty)
                {
                    return true;
                }
            }
            return false;
        }

        public void FlushExtraIntoPrimaryWherePossible()
        {
            if (MaxExtraCapacity <= 0)
            {
                return;
            }

            while (ExtraSlots.Count > 0 && NormalHasVacancy())
            {
                var st = ExtraSlots[0];
                ExtraSlots.RemoveAt(0);
                if (st == null || st.IsEmpty)
                {
                    continue;
                }
                long c = st.Count;
                long absorbed = TryGiveItem(st.ItemID, c, -1, allowExtra: false);
                if (absorbed <= 0)
                {
                    ExtraSlots.Insert(0, st);
                    break;
                }
                if (absorbed < c)
                {
                    ExtraSlots.Insert(0, ItemCatalog.CreateItemStack(st.ItemID, c - absorbed));
                    break;
                }
            }

            if (StorageLayout == EBagStorageLayout.Compact)
            {
                CompactPackPrimary();
            }
        }

        public void AfterSlotMutation()
        {
            ClearEmptyItems(stripNullExtras: true);
            if (StorageLayout == EBagStorageLayout.Compact)
            {
                CompactPackPrimary();
            }
            FlushExtraIntoPrimaryWherePossible();
            EvOnBagUpdate?.Invoke();
        }

        /// <summary>
        /// ???????????????????????????????????????????????????????
        /// </summary>
        /// <param name="incoming"></param>
        /// <returns></returns>
        public long TryGiveItem(string itemId, long count, int preferredIdx = -1, bool allowExtra = true)
        {
            if (itemId == null || count <= 0) return 0;
            var itemConf = ItemCatalog.GetItemDef(itemId);
            if (itemConf == null) return 0;

            long remaining = count;



            var maxStack = GetMaxStack(itemId);
            // ????????????????????????????????????
            if (preferredIdx != -1)
            {
                // ??????????????????????????
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

            // ????????????????? ID ???????????
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
                if (StorageLayout == EBagStorageLayout.Compact)
                {
                    CompactPackPrimary();
                }
                EvOnBagUpdate?.Invoke();
                return count;
            }

            // ???????????????
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
                if (StorageLayout == EBagStorageLayout.Compact)
                {
                    CompactPackPrimary();
                }
                EvOnBagUpdate?.Invoke();
                return count;
            }

            if (allowExtra && MaxExtraCapacity > 0)
            {
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
                    while (ExtraSlots.Count < MaxExtraCapacity && remaining > 0)
                    {
                        var put = Math.Min(maxStack, remaining);
                        var newItem = ItemCatalog.CreateItemStack(itemId, put);
                        ExtraSlots.Add(newItem);
                        remaining -= put;
                    }
                }
            }

            if (StorageLayout == EBagStorageLayout.Compact)
            {
                CompactPackPrimary();
            }

            EvOnBagUpdate?.Invoke();
            return count - remaining;
        }

        /// <summary>
        /// 将整张堆栈放入空格，不与现有同名堆叠合并（用于锻造等产物：实例型各占一格）。
        /// </summary>
        public bool TryPlaceStackWithoutMerge(ItemStack stack)
        {
            if (stack == null || stack.IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < NormalSlots.Count; i++)
            {
                if (NormalSlots[i] == null || NormalSlots[i].IsEmpty)
                {
                    NormalSlots[i] = stack;
                    AfterSlotMutation();
                    return true;
                }
            }

            if (MaxExtraCapacity > 0 && ExtraSlots.Count < MaxExtraCapacity)
            {
                ExtraSlots.Add(stack);
                AfterSlotMutation();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 可放入独立堆栈的空位数：主格子空位 + 扩展栏尚可追加条数。
        /// </summary>
        public int CountDiscreteEmptySlots()
        {
            int n = 0;
            foreach (var s in NormalSlots)
            {
                if (s == null || s.IsEmpty)
                {
                    n++;
                }
            }

            if (MaxExtraCapacity > 0)
            {
                n += Mathf.Max(0, MaxExtraCapacity - ExtraSlots.Count);
            }

            return n;
        }

        public bool TrySplit(int srcIndex, long count)
        {
            // ?????????????????????????????
            if (srcIndex < NormalSlots.Count)
            {
                var src = NormalSlots[srcIndex];
                if (src == null || src.IsEmpty) return false;
                if (count <= 0 || count >= src.Count) return false;

                // ????? ID ?????????????
                if (src.ItemInstanceId != 0)
                {
                    return false;
                }

                // ?????????????????????
                int emptyIdx = NormalSlots.FindIndex(s => s == null || s.IsEmpty);
                if (emptyIdx < 0) return false;

                src.RemoveFromStack(count);
                NormalSlots[emptyIdx] = ItemCatalog.CreateItemStack(src.ItemID, count);
                AfterSlotMutation();
                return true;
            }

            return false;
        }


        public long RemoveAt(int index, long count)
        {
            if (index >= 0 && index < NormalSlots.Count)
            {
                var s = NormalSlots[index];
                if (s == null) return 0;
                var removed = s.RemoveFromStack(count);
                if (s.Count <= 0) NormalSlots[index] = null;
                AfterSlotMutation();
                return removed;
            }

            int extraIdx = index - BasicCapacity;
            if (extraIdx >= 0 && extraIdx < ExtraSlots.Count)
            {
                var s = ExtraSlots[extraIdx];
                if (s == null) return 0;
                var removed = s.RemoveFromStack(count);
                if (s.Count <= 0)
                {
                    ExtraSlots.RemoveAt(extraIdx);
                }
                AfterSlotMutation();
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
        /// ????????????????????????????????????
        /// </summary>
        /// <param name="slotIdx"></param>
        /// <returns></returns>
        public bool IsSlotIdxValid(int slotIdx)
        {
            if (slotIdx >= 0 && slotIdx < BasicCapacity)
            {
                return true;
            }

            if (MaxExtraCapacity <= 0)
            {
                return false;
            }

            int e = slotIdx - BasicCapacity;
            return e >= 0 && e <= ExtraSlots.Count && e < MaxExtraCapacity;
        }

        /// <summary>
        /// ????????????????????????
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
        Storage = 100,
    }
}