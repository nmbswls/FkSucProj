using cfg.demo;
using My;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Saving;
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

    
    public class PlayerInventorySystem : IPlayerSystem
    {
        public PlayerSystemManager DataManager;
        public PlayerBag MainBag;
        public Dictionary<EPlayerBagId, PlayerBag> SpeBags = new Dictionary<EPlayerBagId, PlayerBag>();

        ///// <summary>
        ///// 仓库分页，与 <see cref="WarehouseConfig.BagIdFirst"/> 起的 BagId 一一对应。
        ///// </summary>
        //public readonly List<PlayerBag> WarehousePageBags = new List<PlayerBag>();

        public Dictionary<string, float> ItemUseCd = new();

        public Dictionary<string, long> CurrencyBag = new();

        public event Action<EPlayerBagId, string, long> EventOnGainItem;


        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            MainBag = new();
            MainBag.InitBag(0, 60, 0);

            if(true)
            {
                var bagId = EPlayerBagId.Secret;
                var bag = new PlayerBag();

                // 通过savingData 搜集背包扩容信息
                bag.InitBag(bagId, 5, 3);
            }


            //while (WarehousePageBags.Count < WarehouseConfig.PageCount)
            //{
            //    var wb = new PlayerBag();
            //    wb.InitBag(WarehouseConfig.BagIdFirst + WarehousePageBags.Count, WarehouseConfig.SlotsPerPage, 0);
            //    WarehousePageBags.Add(wb);
            //}

            //for (int p = 0; p < WarehousePageBags.Count && p < save.WarehousePages.Count; p++)
            //{
            //    var bag = WarehousePageBags[p];
            //    var page = save.WarehousePages[p];
            //    for (int i = 0; i < bag.NormalSlots.Count; i++)
            //    {
            //        bag.NormalSlots[i] = null;
            //    }
            //    if (page?.Slots == null)
            //    {
            //        continue;
            //    }
            //    for (int s = 0; s < page.Slots.Count && s < bag.NormalSlots.Count; s++)
            //    {
            //        var slot = page.Slots[s];
            //        if (slot == null || string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0)
            //        {
            //            continue;
            //        }
            //        bag.NormalSlots[s] = ItemCatalog.CreateItemStack(slot.ItemId, slot.Count);
            //        if (bag.NormalSlots[s] != null && slot.ItemInstanceId != 0)
            //        {
            //            bag.NormalSlots[s].ItemInstanceId = slot.ItemInstanceId;
            //        }
            //    }
            //}
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

        /// <summary>
        /// 任意已注册背包（含仓库页）内拆分堆叠。
        /// </summary>
        public bool TrySplitItemInBag(int bagId, int index, long count)
        {
            var bag = GetBagById(bagId);
            if (bag == null)
            {
                return false;
            }
            return bag.TrySplit(index, count);
        }

        /// <summary>
        /// 从指定背包格移除并生成世界掉落（主背包、特殊栏、仓库等）。
        /// </summary>
        public void DropItemToGround(int bagId, int index, long count)
        {
            var bag = GetBagById(bagId);
            if (bag == null)
            {
                Debug.LogError($"DropItemToGround fail bag not found {bagId}");
                return;
            }
            var item = bag.GetItemByIdx(index);
            if (item == null)
            {
                return;
            }
            long dropCount = bag.RemoveAt(index, count);
            if (dropCount > 0 && MainGameManager.Instance != null && MainGameManager.Instance.playerScenePresenter != null)
            {
                Vector2 centerPos = MainGameManager.Instance.playerScenePresenter.GetWorldPosition();
                MainGameManager.Instance.gameLogicManager.globalDropCollection.CreateDrop(
                    item.ItemID,
                    dropCount,
                    centerPos + UnityEngine.Random.insideUnitCircle * 0.3f,
                    false,
                    centerPos);
            }
            bag.ClearEmptyItems();
        }

        public PlayerBag GetBagById(int bagId)
        {
            if (bagId == 0) return MainBag;
            if (bagId >= WarehouseConfig.BagIdFirst)
            {
                int idx = bagId - WarehouseConfig.BagIdFirst;
                if (idx >= 0 && idx < WarehousePageBags.Count)
                {
                    return WarehousePageBags[idx];
                }
                return null;
            }
            SpeBags.TryGetValue(bagId, out var bag);
            return bag;
        }

        /// <summary>
        /// 从存档恢复仓库各页槽位（缺页则扩容空页）。
        /// </summary>
        

        /// <summary>
        /// 将仓库各页写入存档列表（长度固定为页数）。
        /// </summary>
        public void WriteWarehouseToSave(My.Saving.SaveData save)
        {
            if (save == null)
            {
                return;
            }
            save.WarehousePages ??= new List<My.Saving.WarehousePagePersist>();
            save.WarehousePages.Clear();
            foreach (var bag in WarehousePageBags)
            {
                var page = new My.Saving.WarehousePagePersist();
                for (int i = 0; i < bag.NormalSlots.Count; i++)
                {
                    var st = bag.NormalSlots[i];
                    if (st == null || st.IsEmpty)
                    {
                        page.Slots.Add(new My.Saving.WarehouseSlotPersist());
                    }
                    else
                    {
                        page.Slots.Add(new My.Saving.WarehouseSlotPersist
                        {
                            ItemId = st.ItemID,
                            Count = st.Count,
                            ItemInstanceId = st.ItemInstanceId,
                        });
                    }
                }
                save.WarehousePages.Add(page);
            }
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
