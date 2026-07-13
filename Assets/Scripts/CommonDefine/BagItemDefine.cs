
using System;
using System.Collections.Generic;
using My.Config;
using My.Player;
using Newtonsoft.Json;
using UnityEngine;

namespace My
{

    [Serializable]
    public class ItemInstanceInfo
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        public List<ItemInstanceComponent> Components = new();

        public T Get<T>() where T : ItemInstanceComponent
        {
            if (Components == null)
            {
                return null;
            }

            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] is T found)
                {
                    return found;
                }
            }

            return null;
        }

        public T GetOrAdd<T>() where T : ItemInstanceComponent, new()
        {
            var found = Get<T>();
            if (found != null)
            {
                return found;
            }

            Components ??= new List<ItemInstanceComponent>();
            found = new T();
            Components.Add(found);
            return found;
        }

        public ItemInstanceInfo Clone()
        {
            var clone = new ItemInstanceInfo();
            if (Components == null)
            {
                return clone;
            }

            foreach (var component in Components)
            {
                var copied = component?.Clone();
                if (copied != null)
                {
                    clone.Components.Add(copied);
                }
            }

            return clone;
        }
    }

    [Serializable]
    public abstract class ItemInstanceComponent
    {
        public abstract ItemInstanceComponent Clone();

    }

    [Serializable]
    public class ItemInstance4PartGear : ItemInstanceComponent
    {
        public override ItemInstanceComponent Clone()
        {
            return new ItemInstance4PartGear();
        }
    }

    [Serializable]
    public class ItemInstance4HumanWeapon : ItemInstanceComponent
    {
        public override ItemInstanceComponent Clone()
        {
            return new ItemInstance4HumanWeapon();
        }
    }

    [Serializable]
    public class ItemInstance4Insertion : ItemInstanceComponent
    {
        public float Lifetime;

        public float BuffTickTimer;

        public override ItemInstanceComponent Clone()
        {
            return new ItemInstance4Insertion
            {
                Lifetime = Lifetime,
                BuffTickTimer = BuffTickTimer,
            };
        }
    }

    [Serializable]
    public class ItemInstance4UseCharge : ItemInstanceComponent
    {
        public long Charges;

        public long MaxCharges;

        public override ItemInstanceComponent Clone()
        {
            return new ItemInstance4UseCharge
            {
                Charges = Charges,
                MaxCharges = MaxCharges,
            };
        }
    }


    [Serializable]
    public class ItemStack
    {
        public string ItemID;
        public long Count;
        public long ItemInstanceId;
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        public ItemInstanceInfo InstanceInfo;

        public ItemStack(string id, long count)
        {
            ItemID = id;
            Count = count;
        }

        public bool CanStackWith(ItemStack other)
        {
            if (other == null)
            {
                return false;
            }

            if (ItemID != other.ItemID)
            {
                return false;
            }

            if (ItemInstanceId != 0 || other.ItemInstanceId != 0)
            {
                return ItemInstanceId != 0 && ItemInstanceId == other.ItemInstanceId;
            }

            return true;
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

        static bool CanContainerAccept(IItemContainer container, ItemStack item)
        {
            if (item == null || item.IsEmpty)
            {
                return true;
            }

            return container is not PlayerBag bag || bag.CanAcceptItem(item.ItemID);
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
                Debug.LogError($"ItemUtils MoveOrMergeItem {srcIdx} {dstIdx} c:{srcItem?.Count ?? -1}");
                return false;
            }

            var dstItem = dstContainer.GetItemByIdx(dstIdx);
            if (!CanContainerAccept(dstContainer, srcItem) || !CanContainerAccept(srcContainer, dstItem))
            {
                return false;
            }

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

    // 须与 cfg.demo.EContainerType 数值一致（Inventory=0 … Warehouse=4）
    public enum EContainerType
    {
        Inventory,
        LootPoint,
        SpecialInventory,
        Shop,
        Warehouse,
        QuickBarWeapon,
        QuickBarConsumable,
    }


    public enum EBagStorageLayout
    {
        Grid,
        Compact,
    }
}
