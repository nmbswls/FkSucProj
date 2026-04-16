using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Entity;
using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static My.Map.Fight.FightStruct;
using static My.UI.AnyContainerItemCell;
using static UnityEditor.Progress;

namespace My.Player.Bag
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
            if(!srcContainer.IsSlotIdxValid(srcIdx))
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
            if(srcItem == null || srcItem.Count <= 0)
            {
                Debug.LogError($"ItemUtils MoveOrMergeItem {srcIdx} {dstIdx}");
                return false;
            }

            var dstItem = dstContainer.GetItemByIdx(dstIdx);

            // 目标槽为空：整格迁入，或拆出一部分迁入（受叠加上限约束）
            if(dstItem == null || dstItem.Count <= 0)
            {
                var srcItemNewStackMax = dstContainer.GetMaxStack(srcItem.ItemID);

                // 整堆不超过目标容器单格上限时直接迁入
                if(srcItem.Count <= srcItemNewStackMax)
                {
                    dstContainer.SetItemData(dstIdx, srcItem);
                    srcContainer.SetItemData(srcIdx, null);
                    return true;
                }

                // 带实例 ID 的堆叠不可拆分迁入，只能整格操作
                if(srcItem.ItemInstanceId != 0)
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
            // 不同道具或存在实例：整格互换（需双方数量均不超过对方容器单格上限）
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

                // 整格交换两堆道具
                // todo：带 ItemInstanceId 时需额外校验是否允许交换
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
            // 主背包与特殊背包按容器类型取不同叠加上限
            if(BagId == 0)
            {
                return ItemCatalog.GetMaxStackByType(itemId, EContainerType.Inventory);
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
                if(preferredIdx < NormalSlots.Count)
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

            if(remaining <= 0)
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
                if(src.ItemInstanceId != 0)
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



    public class PlayerInventoryModel
    {
        public PlayerSystemManager DataManager;
        public PlayerBag MainBag;

        public Dictionary<int, PlayerBag> SpeBags = new Dictionary<int, PlayerBag>();


        public Dictionary<string, float> ItemUseCd = new();

        public Dictionary<string, long> CurrencyBag = new();

        public event Action<string, long> EventOnGainItem;
        public PlayerInventoryModel(PlayerSystemManager dataManager)
        {
            this.DataManager = dataManager;

            MainBag = new();
            MainBag.InitBag(0, 60, 0);

            for(int i=1;i<=4;i++)
            {
                var bag = new PlayerBag();
                bag.InitBag(i, 5, 3);

                SpeBags[bag.BagId] = bag;
            }
        }

        private float _bagTimer;

        public void Tick(float dt)
        {
            if (LogicTime.time - _bagTimer < 0.3f)
            {
                return;
            }
            _bagTimer = LogicTime.time;

            foreach (var bag in SpeBags.Values)
            {
                for(int i=0;i<bag.NormalSlots.Count;i++)
                {
                    if (bag.NormalSlots[i] == null)continue;

                    if(bag.NormalSlots[i].InstanceInfo is ItemInstance4Insertion insertion)
                    {
                        var itemConf = ItemCatalog.GetItemDef(bag.NormalSlots[i].ItemID);
                        if(itemConf != null && itemConf.AutoDestroy)
                        {
                            insertion.Lifetime -= dt;
                            if(insertion.Lifetime <= 0)
                            {
                                bag.NormalSlots[i] = null;
                                continue;
                            }

                            if(!string.IsNullOrEmpty(itemConf.SpecialBuffId) && LogicTime.time - insertion.BuffTickTimer > itemConf.SpecialBuffInterval)
                            {
                                insertion.BuffTickTimer += itemConf.SpecialBuffInterval;

                                DataManager.logicManager.globalBuffManager.RequestAddBuff(DataManager.logicManager.playerLogicEntity.Id, itemConf.SpecialBuffId, 1);
                            }
                        }
                    }
                }

                for(int i=bag.ExtraSlots.Count -1; i>=0; i--)
                {
                    if (bag.ExtraSlots[i].InstanceInfo is ItemInstance4Insertion insertion)
                    {
                        var itemConf = ItemCatalog.GetItemDef(bag.ExtraSlots[i].ItemID);
                        if (itemConf != null && itemConf.AutoDestroy)
                        {
                            insertion.Lifetime -= dt;
                            if (insertion.Lifetime <= 0)
                            {
                                bag.ExtraSlots.RemoveAt(i);


                                DataManager.logicManager.viewer.ShowFakeFxEffect("-"+itemConf.DisplayName, DataManager.logicManager.playerLogicEntity.Pos);
                                continue;
                            }

                            if (!string.IsNullOrEmpty(itemConf.SpecialBuffId) && LogicTime.time - insertion.BuffTickTimer > itemConf.SpecialBuffInterval)
                            {
                                insertion.BuffTickTimer += itemConf.SpecialBuffInterval;

                                DataManager.logicManager.globalBuffManager.RequestAddBuff(DataManager.logicManager.playerLogicEntity.Id, itemConf.SpecialBuffId, 1);
                            }
                        }
                    }
                }
            }
        }


        public bool CheckHaveItem(string itemId, long count)
        {
            long totalNum = 0;

            for (int bagId = 0; bagId <= 4; bagId++)
            {
                var bag = GetBagById(bagId);
                if (bag == null)
                {
                    continue;
                }

                var bagCount = bag.GetItemCount(itemId);
                totalNum += bagCount;
                if (totalNum >= count)
                {
                    return true;
                }
            }

            CurrencyBag.TryGetValue(itemId, out var currencyVal);
            totalNum += currencyVal;

            if (totalNum >= count)
            {
                return true;
            }
            return false;
        }

        public long CostItem(string itemId, long count)
        {
            if (count <= 0)
            {
                return 0;
            }

            long leftCount = count;
            var itemConf = ItemCatalog.GetItemDef(itemId);
            if (itemConf != null && itemConf.ItemType == EItemType.Currency)
            {
                CurrencyBag.TryGetValue(itemId, out var itemVal);
                if (itemVal > leftCount)
                {
                    CurrencyBag[itemId] = itemVal - leftCount;
                    leftCount = 0;
                }
                else
                {
                    CurrencyBag[itemId] = 0;
                    leftCount -= itemVal;
                }
            }

            for (int bagId = 0; bagId <= 4; bagId++)
            {
                var bag = GetBagById(bagId);
                if (bag == null)
                {
                    continue;
                }

                leftCount = bag.TryCostItem(itemId, leftCount);

                if (leftCount <= 0)
                {
                    break;
                }
            }

            return leftCount;
        }

        public long GiveItem(string itemId, long amount, int bagId)
        {
            var itemConf = ItemCatalog.GetItemDef(itemId);
            if (itemConf == null)
            {
                return 0;
            }

            if (itemConf.ItemType == EItemType.Currency)
            {
                CurrencyBag[itemId] = CurrencyBag.GetValueOrDefault(itemId) + amount;
                return amount;
            }

            if (itemConf.IsAutoUse)
            {
                var useRow = ItemCatalog.GetPrimaryUse(itemId);
                if (useRow != null)
                {
                    DataManager.logicManager.HandleUseItem(DataManager.logicManager.playerLogicEntity.Id, amount, useRow);
                }

                return amount;
            }

            var bag = GetBagById(bagId);
            if (bag == null)
            {
                return 0;
            }

            var put = bag.TryGiveItem(itemId, amount);

            EventOnGainItem?.Invoke(itemId, put);

            return put;
        }

        

        /// <summary>
        /// 跨背包移动、合并或交换（同包同槽无操作）
        /// </summary>
        /// <param name="srcBagId"></param>
        /// <param name="srcIndex"></param>
        /// <param name="dstBagId"></param>
        /// <param name="dstIndex"></param>
        /// <returns></returns>
        public bool TrySwapOrMove(int srcBagId, int srcIndex, int dstBagId, int dstIndex)
        {
            // 同背包且同索引，无需处理
            if (srcBagId == dstBagId  && srcIndex == dstIndex) return false;

            // 解析源、目标背包实例
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

        void RemoveFromIndex(int index, int count);

        EContainerType GetContainerType();

        event Action<int> EnOnUnrealed;

        IItemContainer GetLootItemContainer();

        void TryUseLootPoint();
    }
}
