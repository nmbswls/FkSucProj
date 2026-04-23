
using System;
using My.Config;

namespace My.Player
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
        /// <summary>
        /// 在容器间移动、合并堆叠或整格交换道具
        /// </summary>
        /// <returns></returns>
        public static bool MoveOrMergeOrSwapItem(IItemContainer srcContainer, int srcIdx, IItemContainer dstContainer, int dstIdx)
        {
            // 校验源槽位索引
            if (!srcContainer.IsSlotIdxValid(srcIdx))
            {
                return false;
            }

            // 校验目标槽位索引
            if (!dstContainer.IsSlotIdxValid(dstIdx))
            {
                return false;
            }

            // 读取源槽道具，无效则失败
            var srcItem = srcContainer.GetItemByIdx(srcIdx);
            if (srcItem == null || srcItem.Count <= 0)
            {
                Debug.LogError($"ItemUtils MoveOrMergeItem {srcIdx} {dstIdx}");
                return false;
            }

            var dstItem = dstContainer.GetItemByIdx(dstIdx);

            // 目标槽为空：整格迁入，或拆出一部分迁入（受叠加上限约束）
            if (dstItem == null || dstItem.Count <= 0)
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);

                // 整堆不超过目标容器单格上限时直接迁入
                if (srcItem.Count <= srcItemNewStackMax)
                {
                    dstContainer.SetItemData(dstIdx, srcItem);
                    srcContainer.SetItemData(srcIdx, null);
                    return true;
                }

                // 带实例 ID 的堆叠不可拆分迁入，只能整格操作
                if (srcItem.ItemInstanceId != 0)
                {
                    return false;
                }

                long canMove = srcItemNewStackMax;
                var newItem = ItemCatalog.CreateItemStack(srcItem.ItemID, canMove);
                dstContainer.SetItemData(dstIdx, newItem);
                srcContainer.SetItemCount(srcIdx, srcItem.Count - canMove);

                return true;
            }
            // 目标槽已有同 ID 且无实例：尝试双向匀量合并
            else if (dstItem.ItemID == srcItem.ItemID && srcItem.ItemInstanceId == 0 && dstItem.ItemInstanceId == 0)
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);
                var dstItemNewStackMax = srcContainer.GetMaxStack(dstItem.ItemID);

                long canMove1 = srcItemNewStackMax - dstItem.Count;
                long canMove2 = dstItemNewStackMax - srcItem.Count;
                // 两侧格均已满，无法再互相匀量
                if (canMove1 <= 0 && canMove2 <= 0)
                {
                    return false;
                }

                // 优先从源向目标补充可合并空间
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

                return true;
            }
            // 不同道具或存在实例：整格互换（需双方数量均不超过对方容器单格上限）
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

                // 整格交换两堆道具
                // todo：带 ItemInstanceId 时需额外校验是否允许交换
                dstContainer.SetItemData(dstIdx, srcItem);
                srcContainer.SetItemData(srcIdx, dstItem);

                return true;
            }
        }
    }
}