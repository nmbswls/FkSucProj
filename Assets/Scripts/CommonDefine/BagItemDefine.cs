
using System;
using My.Config;
using UnityEngine;

namespace My
{

    [Serializable]
    public abstract class ItemInstanceInfo
    {

    }

    [Serializable]
    public class ItemInstance4Equip : ItemInstanceInfo
    {
        public long RandVal;
    }

    [Serializable]
    public class ItemInstance4Insertion : ItemInstanceInfo
    {
        public float Lifetime;

        public float BuffTickTimer;
    }


    [Serializable]
    public class ItemStack
    {
        public string ItemID;
        public long Count;
        public long ItemInstanceId;
        public ItemInstanceInfo InstanceInfo;

        public ItemStack(string id, long count)
        {
            ItemID = id;
            Count = count;
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
        static void PostPlayerBagMutation(IItemContainer a, IItemContainer b)
        {
            //if (ReferenceEquals(a, b) && a is PlayerBag same)
            //{
            //    same.AfterSlotMutation();
            //    return;
            //}
            //if (a is PlayerBag pa)
            //{
            //    pa.AfterSlotMutation();
            //}
            //if (b is PlayerBag pb)
            //{
            //    pb.AfterSlotMutation();
            //}
        }

        public static bool MoveOrMergeOrSwapItem(IItemContainer srcContainer, int srcIdx, IItemContainer dstContainer, int dstIdx)
        {
            if (!srcContainer.IsSlotIdxValid(srcIdx))
            {
                return false;
            }

            if (!dstContainer.IsSlotIdxValid(dstIdx))
            {
                return false;
            }

            var srcItem = srcContainer.GetItemByIdx(srcIdx);
            if (srcItem == null || srcItem.Count <= 0)
            {
                Debug.LogError($"ItemUtils MoveOrMergeItem {srcIdx} {dstIdx}");
                return false;
            }

            var dstItem = dstContainer.GetItemByIdx(dstIdx);

            // ?????????????????????????????????????????
            if (dstItem == null || dstItem.Count <= 0)
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);

                // ???????????????????????????????
                if (srcItem.Count <= srcItemNewStackMax)
                {
                    dstContainer.SetItemData(dstIdx, srcItem);
                    srcContainer.SetItemData(srcIdx, null);
                    PostPlayerBagMutation(srcContainer, dstContainer);
                    return true;
                }

                // ????? ID ???????????????????????
                if (srcItem.ItemInstanceId != 0)
                {
                    return false;
                }

                long canMove = srcItemNewStackMax;
                var newItem = ItemCatalog.CreateItemStack(srcItem.ItemID, canMove);
                dstContainer.SetItemData(dstIdx, newItem);
                srcContainer.SetItemCount(srcIdx, srcItem.Count - canMove);

                PostPlayerBagMutation(srcContainer, dstContainer);
                return true;
            }
            // ????????? ID ???????????????????????
            else if (dstItem.ItemID == srcItem.ItemID && srcItem.ItemInstanceId == 0 && dstItem.ItemInstanceId == 0)
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);
                var dstItemNewStackMax = srcContainer.GetMaxStack(dstItem.ItemID);

                long canMove1 = srcItemNewStackMax - dstItem.Count;
                long canMove2 = dstItemNewStackMax - srcItem.Count;
                // ????????????????????????
                if (canMove1 <= 0 && canMove2 <= 0)
                {
                    return false;
                }

                // ???????????????????
                if (canMove1 > 0)
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

                PostPlayerBagMutation(srcContainer, dstContainer);
                return true;
            }
            // ????????????????????????????????????????????????????????
            else
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);
                var dstItemNewStackMax = srcContainer.GetMaxStack(dstItem.ItemID);

                if (srcItem.Count > srcItemNewStackMax)
                {
                    return false;
                }
                if (dstItem.Count > dstItemNewStackMax)
                {
                    return false;
                }

                // ????????????
                // todo???? ItemInstanceId ?????????????????????
                dstContainer.SetItemData(dstIdx, srcItem);
                srcContainer.SetItemData(srcIdx, dstItem);

                PostPlayerBagMutation(srcContainer, dstContainer);
                return true;
            }
        }
    }

    public interface IItemContainer
    {
        long GetMaxStack(string itemId);

        /// <summary>
        /// ??????????????????null ???????
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="item"></param>
        void SetItemData(int idx, ItemStack item);

        /// <summary>
        /// ?????????????????????
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
        /// ?????????? ID ????????????????
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        long GetItemCount(string itemId);

        /// <summary>
        /// ????????????????
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
        /// ????????? Inventory ???????????????
        /// </summary>
        Warehouse,
    }


    public enum EBagStorageLayout
    {
        Grid,
        Compact,
    }
}