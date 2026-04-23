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
using My.Player;

namespace My.Player.Bag
{

    
    public class PlayerInventorySystem : IPlayerSystem
    {
        protected GameLogicManager LogicManager { get; private set; }

        public PlayerBag MainBag;
        public PlayerBag WarehouseBag;
        public Dictionary<EPlayerBagId, PlayerBag> SpeBags = new Dictionary<EPlayerBagId, PlayerBag>();

        readonly Dictionary<EItemType, List<int>> _warehouseTypeToIndices = new Dictionary<EItemType, List<int>>();
        bool _warehouseCategoryDirty = true;

        public Dictionary<string, float> ItemUseCd = new();

        public Dictionary<string, long> CurrencyBag = new();

        public event Action<EPlayerBagId, string, long> EventOnGainItem;


        public PlayerInventorySystem()
        {
            MainBag = new PlayerBag();
            WarehouseBag = new PlayerBag();
            
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            MainBag.InitBag(0, 60, 0);

            int storageSlots = 100;
            WarehouseBag.InitBag(EPlayerBagId.Storage, storageSlots, 0);

            var secretBag = new PlayerBag();
            secretBag.InitBag(EPlayerBagId.Secret, 5, 3);
            SpeBags[EPlayerBagId.Secret] = secretBag;

            WarehouseBag.EvOnBagUpdate += delegate { MarkWarehouseCategoryDirty(); };

            ApplyWarehouseFromSave(savingData);
        }

        void MarkWarehouseCategoryDirty()
        {
            _warehouseCategoryDirty = true;
        }

        void EnsureWarehouseCategoryIndex()
        {
            if (!_warehouseCategoryDirty || WarehouseBag == null)
            {
                return;
            }
            _warehouseTypeToIndices.Clear();
            for (int i = 0; i < WarehouseBag.NormalSlots.Count; i++)
            {
                var st = WarehouseBag.NormalSlots[i];
                if (st == null || st.IsEmpty)
                {
                    continue;
                }
                var def = ItemCatalog.GetItemDef(st.ItemID);
                var et = def != null ? def.ItemType : EItemType.Normal;
                if (!_warehouseTypeToIndices.TryGetValue(et, out var li))
                {
                    li = new List<int>();
                    _warehouseTypeToIndices[et] = li;
                }
                li.Add(i);
            }
            _warehouseCategoryDirty = false;
        }

        public IReadOnlyList<int> GetWarehouseSlotIndicesForItemTypeFilter(int typeFilterInt)
        {
            EnsureWarehouseCategoryIndex();
            if (typeFilterInt < 0)
            {
                return null;
            }
            var t = (EItemType)typeFilterInt;
            if (_warehouseTypeToIndices.TryGetValue(t, out var list))
            {
                return list;
            }
            return Array.Empty<int>();
        }


        /// <summary>
        /// 从存档中读取
        /// </summary>
        /// <param name="save"></param>
        private void ApplyWarehouseFromSave(SaveData save)
        {
            if (WarehouseBag == null)
            {
                return;
            }
            for (int i = 0; i < WarehouseBag.NormalSlots.Count; i++)
            {
                WarehouseBag.NormalSlots[i] = null;
            }
            WarehouseBag.ExtraSlots.Clear();

            if (save?.WarehousePages == null || save.WarehousePages.Count == 0)
            {
                MarkWarehouseCategoryDirty();
                return;
            }

            var flat = new List<ItemStack>();
            foreach (var page in save.WarehousePages)
            {
                if (page?.Slots == null)
                {
                    continue;
                }
                foreach (var slot in page.Slots)
                {
                    if (slot == null || string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0)
                    {
                        continue;
                    }
                    var st = ItemCatalog.CreateItemStack(slot.ItemId, slot.Count);
                    if (st != null && slot.ItemInstanceId != 0)
                    {
                        st.ItemInstanceId = slot.ItemInstanceId;
                    }
                    if (st != null)
                    {
                        flat.Add(st);
                    }
                }
            }

            for (int i = 0; i < WarehouseBag.NormalSlots.Count; i++)
            {
                WarehouseBag.NormalSlots[i] = i < flat.Count ? flat[i] : null;
            }
            WarehouseBag.AfterSlotMutation();
            MarkWarehouseCategoryDirty();
        }

        private float _bagTimer;

        public void Tick(float dt)
        {
            if (LogicTime.time - _bagTimer < 0.3f)
            {
                return;
            }
            _bagTimer = LogicTime.time;

            if (LogicManager.MainStage != GameLogicManager.EMainGameStage.Running)
            {
                return;
            }

            TickInsertionBuffsOnBag(MainBag, dt);
            TickInsertionBuffsOnBag(WarehouseBag, dt);
            foreach (var bag in SpeBags.Values)
            {
                TickInsertionBuffsOnBag(bag, dt);
            }
        }

        void TickInsertionBuffsOnBag(PlayerBag bag, float dt)
        {
            if (bag == null)
            {
                return;
            }

            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                if (bag.NormalSlots[i] == null)
                {
                    continue;
                }

                if (bag.NormalSlots[i].InstanceInfo is ItemInstance4Insertion insertion)
                {
                    var itemConf = ItemCatalog.GetItemDef(bag.NormalSlots[i].ItemID);
                    if (itemConf != null && itemConf.AutoDestroy)
                    {
                        insertion.Lifetime -= dt;
                        if (insertion.Lifetime <= 0)
                        {
                            bag.NormalSlots[i] = null;
                            continue;
                        }

                        if (!string.IsNullOrEmpty(itemConf.SpecialBuffId) && LogicTime.time - insertion.BuffTickTimer > itemConf.SpecialBuffInterval)
                        {
                            insertion.BuffTickTimer += itemConf.SpecialBuffInterval;

                            LogicManager.globalBuffManager.RequestAddBuff(LogicManager.playerLogicEntity.Id, itemConf.SpecialBuffId, 1);
                        }
                    }
                }
            }

            for (int i = bag.ExtraSlots.Count - 1; i >= 0; i--)
            {
                if (bag.ExtraSlots[i] == null)
                {
                    continue;
                }

                if (bag.ExtraSlots[i].InstanceInfo is ItemInstance4Insertion insertion)
                {
                    var itemConf = ItemCatalog.GetItemDef(bag.ExtraSlots[i].ItemID);
                    if (itemConf != null && itemConf.AutoDestroy)
                    {
                        insertion.Lifetime -= dt;
                        if (insertion.Lifetime <= 0)
                        {
                            bag.ExtraSlots.RemoveAt(i);

                            LogicManager.viewer.ShowFakeFxEffect("-" + itemConf.DisplayName, LogicManager.playerLogicEntity.Pos);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(itemConf.SpecialBuffId) && LogicTime.time - insertion.BuffTickTimer > itemConf.SpecialBuffInterval)
                        {
                            insertion.BuffTickTimer += itemConf.SpecialBuffInterval;

                            LogicManager.globalBuffManager.RequestAddBuff(LogicManager.playerLogicEntity.Id, itemConf.SpecialBuffId, 1);
                        }
                    }
                }
            }
        }


        public bool CheckHaveItem(string itemId, long count)
        {
            long totalNum = 0;

            void Acc(PlayerBag bag)
            {
                if (bag == null)
                {
                    return;
                }
                totalNum += bag.GetItemCount(itemId);
            }

            Acc(MainBag);
            if (totalNum >= count)
            {
                return true;
            }
            Acc(WarehouseBag);
            if (totalNum >= count)
            {
                return true;
            }
            foreach (var bag in SpeBags.Values)
            {
                Acc(bag);
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

            leftCount = MainBag.TryCostItem(itemId, leftCount);
            if (leftCount > 0)
            {
                foreach (var bag in SpeBags.Values)
                {
                    leftCount = bag.TryCostItem(itemId, leftCount);
                    if (leftCount <= 0)
                    {
                        break;
                    }
                }
            }
            if (leftCount > 0 && WarehouseBag != null)
            {
                leftCount = WarehouseBag.TryCostItem(itemId, leftCount);
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
                    LogicManager.HandleUseItem(LogicManager.playerLogicEntity.Id, amount, useRow);
                }

                return amount;
            }

            var bag = GetBagById(bagId);
            if (bag == null)
            {
                return 0;
            }

            var put = bag.TryGiveItem(itemId, amount);

            EventOnGainItem?.Invoke((EPlayerBagId)bagId, itemId, put);

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
        }

        public PlayerBag GetBagById(int bagId)
        {
            if (bagId == 0)
            {
                return MainBag;
            }
            if (bagId == (int)EPlayerBagId.Storage)
            {
                return WarehouseBag;
            }
            SpeBags.TryGetValue((EPlayerBagId)bagId, out var bag);
            return bag;
        }

        public void WriteWarehouseToSave(My.Saving.SaveData save)
        {
            if (save == null || WarehouseBag == null)
            {
                return;
            }
            save.WarehousePages ??= new List<My.Saving.WarehousePagePersist>();
            save.WarehousePages.Clear();
            var page = new My.Saving.WarehousePagePersist();
            for (int i = 0; i < WarehouseBag.NormalSlots.Count; i++)
            {
                var st = WarehouseBag.NormalSlots[i];
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
