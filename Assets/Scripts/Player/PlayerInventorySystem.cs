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
        // 单次发放实例型道具上限，避免 CreateItemStack 循环过多导致卡顿
        public const int MaxInstanceGrantBatch = 32;

        protected GameLogicManager LogicManager { get; private set; }

        public PlayerBag MainBag;
        public PlayerBag MindFacetBag; // 精神相关背包


        public PlayerBag WarehouseBag;
        public PlayerBag FurnitureWarehouseBag;
        public PlayerBag PlantBag;
        public Dictionary<EPlayerBagId, PlayerBag> SpeBags = new Dictionary<EPlayerBagId, PlayerBag>();


        public PlayerBag ImportantItemBag; // 仅用来存放珍贵物品 极少量

        public Dictionary<string, float> ItemUseCd = new();

        public Dictionary<string, long> CurrencyBag = new();

        public event Action<EPlayerBagId, string, long> EventOnGainItem;

        public PlayerInventorySystem()
        {
            MainBag = new PlayerBag();
            WarehouseBag = new PlayerBag();
            FurnitureWarehouseBag = new PlayerBag();

            MindFacetBag = new PlayerBag();
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            this.LogicManager = ctx;

            MainBag = InitConfiguredBag(EPlayerBagId.Default, MainBag, 60, 0, EBagStorageLayout.Grid);
            MindFacetBag = InitConfiguredBag(EPlayerBagId.Mind, MindFacetBag, 30, 0, EBagStorageLayout.Compact);
            ImportantItemBag = InitConfiguredBag(EPlayerBagId.Important, ImportantItemBag, 8, 0, EBagStorageLayout.Compact);
            WarehouseBag = InitConfiguredBag(EPlayerBagId.Storage, WarehouseBag, 100, 0, EBagStorageLayout.Compact);
            FurnitureWarehouseBag = InitConfiguredBag(EPlayerBagId.FurnitureStorage, FurnitureWarehouseBag, 100, 0, EBagStorageLayout.Compact);
            SpeBags[EPlayerBagId.Secret] = InitConfiguredBag(EPlayerBagId.Secret, GetBagById((int)EPlayerBagId.Secret), 5, 3, EBagStorageLayout.Compact);

            RefreshSpecialBagsFromProgression();
            ApplyMainBagFromSave(savingData);
            ApplyWarehouseFromSave(savingData);
            ApplySpecialBagsFromSave(savingData);
        }

        public void PostInit(PlayerSystemManager owner)
        {
            if (owner?.ProgressionSystem?.ProgressionRoot != null)
            {
                owner.ProgressionSystem.ProgressionRoot.OnStatsChanged += delegate
                {
                    RefreshSpecialBagsFromProgression();
                };
            }
        }

        static ItemStack HydratePersistedStack(string itemId, long count, long itemInstanceId, ItemInstanceInfo instanceInfo = null)
        {
            return ItemCatalog.HydrateItemStackFromPersist(itemId, count, itemInstanceId, instanceInfo);
        }

        PlayerBag InitConfiguredBag(
            EPlayerBagId bagId,
            PlayerBag bag,
            int fallbackCapacity,
            int fallbackExtraCapacity,
            EBagStorageLayout fallbackLayout)
        {
            bag ??= new PlayerBag();
            var def = PlayerBagCatalog.GetDef(bagId);
            int capacity = PlayerBagCatalog.ResolveCapacity(def, LogicManager?.playerDataManager, fallbackCapacity);
            int extraCapacity = PlayerBagCatalog.ResolveExtraCapacity(def, fallbackExtraCapacity);
            var layout = PlayerBagCatalog.ResolveLayout(def, fallbackLayout);

            bag.InitBag(bagId, capacity, extraCapacity, layout);
            PlayerBagCatalog.ApplyAcceptedTags(bag, def);
            return bag;
        }

        static My.Saving.PlayerBagPersist FindBagPersist(List<My.Saving.PlayerBagPersist> bags, EPlayerBagId bagId)
        {
            if (bags == null)
            {
                return null;
            }

            int id = (int)bagId;
            for (int i = 0; i < bags.Count; i++)
            {
                if (bags[i] != null && bags[i].BagId == id)
                {
                    return bags[i];
                }
            }

            return null;
        }

        static My.Saving.PlayerBagPersist BuildBagPersist(PlayerBag bag)
        {
            if (bag == null)
            {
                return null;
            }

            var bagSave = new My.Saving.PlayerBagPersist
            {
                BagId = (int)bag.BagId,
            };

            int bc = bag.BasicCapacity;
            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                AddSlotPersist(bagSave, i, bag.NormalSlots[i]);
            }

            for (int e = 0; e < bag.ExtraSlots.Count; e++)
            {
                AddSlotPersist(bagSave, bc + e, bag.ExtraSlots[e]);
            }

            return bagSave;
        }

        void RefreshSpecialBagsFromProgression()
        {
            var defs = PlayerBagCatalog.GetAutoGainBagDefs();
            for (int i = 0; i < defs.Count; i++)
            {
                EnsureProgressionBag(defs[i]);
            }
        }

        void EnsureProgressionBag(cfg.demo.PlayerBagDef def)
        {
            if (def == null)
            {
                return;
            }

            var bagId = (EPlayerBagId)def.BagId;
            int capacity = PlayerBagCatalog.ResolveCapacity(def, LogicManager?.playerDataManager, 0);
            if (capacity <= 0)
            {
                return;
            }

            if (!SpeBags.TryGetValue(bagId, out var bag) || bag == null)
            {
                bag = new PlayerBag();
                bag.InitBag(
                    bagId,
                    capacity,
                    PlayerBagCatalog.ResolveExtraCapacity(def, 0),
                    PlayerBagCatalog.ResolveLayout(def, EBagStorageLayout.Compact));
                SpeBags[bagId] = bag;
            }
            else
            {
                ResizePrimaryCapacity(bag, capacity);
            }

            PlayerBagCatalog.ApplyAcceptedTags(bag, def);
            AssignKnownSpecialBagField(bagId, bag);
        }

        void AssignKnownSpecialBagField(EPlayerBagId bagId, PlayerBag bag)
        {
            if (bagId == EPlayerBagId.Plant)
            {
                PlantBag = bag;
            }
        }

        static void ResizePrimaryCapacity(PlayerBag bag, int capacity)
        {
            if (bag == null || capacity <= 0 || bag.BasicCapacity == capacity)
            {
                return;
            }

            if (capacity < bag.BasicCapacity)
            {
                return;
            }

            bag.BasicCapacity = capacity;
            while (bag.NormalSlots.Count < capacity)
            {
                bag.NormalSlots.Add(null);
            }

            bag.AfterSlotMutation();
        }

        void ApplyMainBagFromSave(SaveData save)
        {
            if (MainBag == null)
            {
                return;
            }

            MainBag.ExtraSlots.Clear();
            int ncap = MainBag.NormalSlots.Count;
            for (int si = 0; si < ncap; si++)
            {
                MainBag.NormalSlots[si] = null;
            }

            var entries = FindBagPersist(save?.PlayerInventoryBags, EPlayerBagId.Default)?.Slots
                          ?? save?.MainInventorySlots;
            if (entries == null || entries.Count == 0)
            {
                MainBag.FinishHydrateMutation();
                return;
            }

            int bc = MainBag.BasicCapacity;

            foreach (var row in entries)
            {
                if (row == null)
                {
                    continue;
                }

                if (row.SlotIndex < 0)
                {
                    Debug.LogWarning($"[ApplyMainBagFromSave] Invalid SlotIndex={row.SlotIndex}, skipping entry.");
                    continue;
                }

                var stack = HydratePersistedStack(row.ItemId, row.Count, row.ItemInstanceId, row.InstanceInfo);
                if (stack == null)
                {
                    Debug.LogWarning($"[ApplyMainBagFromSave] Invalid item at SlotIndex={row.SlotIndex} (missing id or zero count).");
                    continue;
                }

                if (row.SlotIndex < ncap)
                {
                    // 重复 SlotIndex：后写覆盖
                    MainBag.NormalSlots[row.SlotIndex] = stack;
                    continue;
                }

                int extraIdx = row.SlotIndex - bc;
                if (MainBag.MaxExtraCapacity <= 0)
                {
                    Debug.LogWarning($"[ApplyMainBagFromSave] SlotIndex={row.SlotIndex} targets extra slots but MaxExtraCapacity=0.");
                    continue;
                }

                if (extraIdx < 0 || extraIdx >= MainBag.MaxExtraCapacity)
                {
                    Debug.LogWarning($"[ApplyMainBagFromSave] SlotIndex={row.SlotIndex} out of extra range [0,{MainBag.MaxExtraCapacity}).");
                    continue;
                }

                while (MainBag.ExtraSlots.Count < extraIdx + 1 && MainBag.ExtraSlots.Count < MainBag.MaxExtraCapacity)
                {
                    MainBag.ExtraSlots.Add(null);
                }

                if (extraIdx >= MainBag.ExtraSlots.Count)
                {
                    Debug.LogWarning($"[ApplyMainBagFromSave] Could not allocate extra slot padding for SlotIndex={row.SlotIndex}.");
                    continue;
                }

                MainBag.ExtraSlots[extraIdx] = stack;
            }

            MainBag.FinishHydrateMutation();
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

            var commonPersist = save?.SecretBaseStorage?.CommonWarehouse;
            if (commonPersist != null
                && (commonPersist.BagId == (int)EPlayerBagId.Storage
                    || (commonPersist.Slots != null && commonPersist.Slots.Count > 0)))
            {
                ApplyBagPersistToBag(WarehouseBag, commonPersist);
            }
            else
            {
                ApplyLegacyWarehousePagesFromSave(save);
            }

            var furniturePersist = save?.SecretBaseStorage?.FurnitureWarehouse;
            if (FurnitureWarehouseBag != null && furniturePersist != null)
            {
                ApplyBagPersistToBag(FurnitureWarehouseBag, furniturePersist);
            }
        }

        void ApplyLegacyWarehousePagesFromSave(SaveData save)
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
                    var st = HydratePersistedStack(slot.ItemId, slot.Count, slot.ItemInstanceId, slot.InstanceInfo);
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
        }

        void ApplyBagPersistToBag(PlayerBag bag, My.Saving.PlayerBagPersist bagSave)
        {
            if (bag == null)
            {
                return;
            }

            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                bag.NormalSlots[i] = null;
            }
            bag.ExtraSlots.Clear();

            if (bagSave?.Slots == null)
            {
                bag.FinishHydrateMutation();
                return;
            }

            int bc = bag.BasicCapacity;
            foreach (var row in bagSave.Slots)
            {
                if (row == null || row.SlotIndex < 0)
                {
                    continue;
                }

                var stack = HydratePersistedStack(row.ItemId, row.Count, row.ItemInstanceId, row.InstanceInfo);
                if (stack == null || !bag.CanAcceptItem(stack.ItemID))
                {
                    continue;
                }

                if (row.SlotIndex < bag.NormalSlots.Count)
                {
                    bag.NormalSlots[row.SlotIndex] = stack;
                    continue;
                }

                int extraIdx = row.SlotIndex - bc;
                if (extraIdx < 0 || extraIdx >= bag.MaxExtraCapacity)
                {
                    continue;
                }

                while (bag.ExtraSlots.Count < extraIdx + 1 && bag.ExtraSlots.Count < bag.MaxExtraCapacity)
                {
                    bag.ExtraSlots.Add(null);
                }

                if (extraIdx < bag.ExtraSlots.Count)
                {
                    bag.ExtraSlots[extraIdx] = stack;
                }
            }

            bag.FinishHydrateMutation();
        }

        void ApplySpecialBagsFromSave(SaveData save)
        {
            bool appliedAny = false;
            if (save?.PlayerInventoryBags != null && save.PlayerInventoryBags.Count > 0)
            {
                foreach (var bagSave in save.PlayerInventoryBags)
                {
                    if (bagSave == null || bagSave.BagId == (int)EPlayerBagId.Default
                        || bagSave.BagId == (int)EPlayerBagId.Storage
                        || bagSave.BagId == (int)EPlayerBagId.FurnitureStorage)
                    {
                        continue;
                    }

                    var bag = GetBagById(bagSave.BagId);
                    if (bag == null)
                    {
                        continue;
                    }

                    ApplyBagPersistToBag(bag, bagSave);
                    appliedAny = true;
                }
            }

            if (appliedAny || save?.SpecialInventoryBags == null || save.SpecialInventoryBags.Count == 0)
            {
                return;
            }

            foreach (var bagSave in save.SpecialInventoryBags)
            {
                if (bagSave == null)
                {
                    continue;
                }

                var bag = GetBagById(bagSave.BagId);
                if (bag == null)
                {
                    continue;
                }

                for (int i = 0; i < bag.NormalSlots.Count; i++)
                {
                    bag.NormalSlots[i] = null;
                }
                bag.ExtraSlots.Clear();

                if (bagSave.Slots == null)
                {
                    bag.FinishHydrateMutation();
                    continue;
                }

                int bc = bag.BasicCapacity;
                foreach (var row in bagSave.Slots)
                {
                    if (row == null || row.SlotIndex < 0)
                    {
                        continue;
                    }

                    var stack = HydratePersistedStack(row.ItemId, row.Count, row.ItemInstanceId, row.InstanceInfo);
                    if (stack == null || !bag.CanAcceptItem(stack.ItemID))
                    {
                        continue;
                    }

                    if (row.SlotIndex < bag.NormalSlots.Count)
                    {
                        bag.NormalSlots[row.SlotIndex] = stack;
                        continue;
                    }

                    int extraIdx = row.SlotIndex - bc;
                    if (extraIdx < 0 || extraIdx >= bag.MaxExtraCapacity)
                    {
                        continue;
                    }

                    while (bag.ExtraSlots.Count < extraIdx + 1 && bag.ExtraSlots.Count < bag.MaxExtraCapacity)
                    {
                        bag.ExtraSlots.Add(null);
                    }

                    if (extraIdx < bag.ExtraSlots.Count)
                    {
                        bag.ExtraSlots[extraIdx] = stack;
                    }
                }

                bag.FinishHydrateMutation();
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

            if ( LogicManager.MainStage != GameLogicManager.EMainGameStage.Running)
            {
                return;
            }

            TickInsertionBuffsOnBag(MainBag, dt);
            TickInsertionBuffsOnBag(WarehouseBag, dt);
            TickInsertionBuffsOnBag(FurnitureWarehouseBag, dt);
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

                var insertion = bag.NormalSlots[i].InstanceInfo?.Get<ItemInstance4Insertion>();
                if (insertion != null)
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

                var insertion = bag.ExtraSlots[i].InstanceInfo?.Get<ItemInstance4Insertion>();
                if (insertion != null)
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
            Acc(FurnitureWarehouseBag);
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

        public bool CheckQuickSlotBindingAvailable(QuickSlotBinding binding)
        {
            if (binding.IsEmpty)
            {
                return false;
            }

            if (binding.ItemInstanceId == 0)
            {
                return GetCarriedItemTotal(binding.ItemId) > 0;
            }

            return TryFindCarriedStack(binding, out _, out _);
        }

        public bool TryFindCarriedStack(QuickSlotBinding binding, out int bagFlatIndex, out ItemStack stack)
        {
            return TryFindCarriedStackWithBag(binding, out _, out bagFlatIndex, out stack);
        }

        bool TryFindCarriedStackWithBag(QuickSlotBinding binding, out PlayerBag foundBag, out int bagFlatIndex, out ItemStack stack)
        {
            bagFlatIndex = -1;
            foundBag = null;
            stack = null;

            if (binding.IsEmpty)
            {
                return false;
            }

            if (binding.ItemInstanceId == 0)
            {
                if (!TryFindFirstCarriedStackByItemId(binding.ItemId, out foundBag, out bagFlatIndex, out stack))
                {
                    return false;
                }

                return stack != null && !stack.IsEmpty;
            }

            if (TryFindStackInBag(MainBag, binding, out bagFlatIndex, out stack))
            {
                foundBag = MainBag;
                return true;
            }

            foreach (var bag in SpeBags.Values)
            {
                if (TryFindStackInBag(bag, binding, out bagFlatIndex, out stack))
                {
                    foundBag = bag;
                    return true;
                }
            }

            return false;
        }

        bool TryFindFirstCarriedStackByItemId(string itemId, out PlayerBag foundBag, out int bagFlatIndex, out ItemStack stack)
        {
            foundBag = null;
            if (TryFindFirstStackInBagByItemId(MainBag, itemId, out bagFlatIndex, out stack))
            {
                foundBag = MainBag;
                return true;
            }

            foreach (var bag in SpeBags.Values)
            {
                if (TryFindFirstStackInBagByItemId(bag, itemId, out bagFlatIndex, out stack))
                {
                    foundBag = bag;
                    return true;
                }
            }

            bagFlatIndex = -1;
            stack = null;
            return false;
        }

        static bool TryFindFirstStackInBagByItemId(PlayerBag bag, string itemId, out int flatIndex, out ItemStack stack)
        {
            flatIndex = -1;
            stack = null;
            if (bag == null || string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            int slotCount = bag.NormalSlots.Count + bag.ExtraSlots.Count;
            for (int i = 0; i < slotCount; i++)
            {
                var st = bag.GetItemByIdx(i);
                if (st != null && !st.IsEmpty && st.ItemID == itemId)
                {
                    flatIndex = i;
                    stack = st;
                    return true;
                }
            }

            return false;
        }

        static bool TryFindStackInBag(PlayerBag bag, QuickSlotBinding binding, out int flatIndex, out ItemStack stack)
        {
            flatIndex = -1;
            stack = null;
            if (bag == null || binding.IsEmpty)
            {
                return false;
            }

            int slotCount = bag.NormalSlots.Count + bag.ExtraSlots.Count;
            for (int i = 0; i < slotCount; i++)
            {
                var st = bag.GetItemByIdx(i);
                if (st == null || st.IsEmpty || st.ItemID != binding.ItemId)
                {
                    continue;
                }

                if (binding.ItemInstanceId != 0 && st.ItemInstanceId != binding.ItemInstanceId)
                {
                    continue;
                }

                flatIndex = i;
                stack = st;
                return true;
            }

            return false;
        }

        public long CostQuickSlotBinding(QuickSlotBinding binding, long count = 1)
        {
            if (binding.IsEmpty || count <= 0)
            {
                return count;
            }

            if (binding.ItemInstanceId == 0)
            {
                return CostCarriedItem(binding.ItemId, count);
            }

            if (TryFindStackInBag(MainBag, binding, out var flatIndex, out _))
            {
                var removed = MainBag.RemoveAt(flatIndex, count);
                return count - removed;
            }

            foreach (var bag in SpeBags.Values)
            {
                if (TryFindStackInBag(bag, binding, out flatIndex, out _))
                {
                    var removed = bag.RemoveAt(flatIndex, count);
                    return count - removed;
                }
            }

            return count;
        }

        public bool TryConsumeItemUse(PlayerBag bag, int flatIndex, ItemUse useRow, long count = 1)
        {
            if (bag == null || useRow == null || count <= 0)
            {
                return false;
            }

            var stack = bag.GetItemByIdx(flatIndex);
            if (stack == null || stack.IsEmpty)
            {
                return false;
            }

            return TryConsumeStackUse(bag, flatIndex, stack, useRow, count);
        }

        public bool TryConsumeQuickSlotUse(QuickSlotBinding binding, ItemUse useRow, long count = 1)
        {
            if (binding.IsEmpty || useRow == null || count <= 0)
            {
                return false;
            }

            if (TryFindCarriedStackWithBag(binding, out var bag, out var flatIndex, out var stack))
            {
                if (bag != null)
                {
                    return TryConsumeStackUse(bag, flatIndex, stack, useRow, count);
                }
            }

            return false;
        }

        bool TryConsumeStackUse(PlayerBag bag, int flatIndex, ItemStack stack, ItemUse useRow, long count)
        {
            switch (ItemCatalog.GetUseConsumePolicy(useRow))
            {
                case EItemUseConsumePolicy.None:
                    return true;
                case EItemUseConsumePolicy.StackCount:
                    return bag.RemoveAt(flatIndex, count) == count;
                case EItemUseConsumePolicy.DestroyInstance:
                    return bag.RemoveAt(flatIndex, stack.Count) > 0;
                case EItemUseConsumePolicy.InstanceCharge:
                {
                    var charge = stack.InstanceInfo?.Get<ItemInstance4UseCharge>();
                    if (charge == null || charge.Charges < count)
                    {
                        Debug.LogWarning($"TryConsumeStackUse: missing or insufficient charges itemId={stack.ItemID}");
                        return false;
                    }

                    charge.Charges -= count;
                    bag.AfterSlotMutation();
                    return true;
                }
                default:
                    return false;
            }
        }

        public long GetCarriedItemTotal(string itemId)
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
            foreach (var bag in SpeBags.Values)
            {
                Acc(bag);
            }

            CurrencyBag.TryGetValue(itemId, out var currencyVal);
            totalNum += currencyVal;
            return totalNum;
        }

        // 身上携带（主包+特殊包+货币），不含仓库
        public long GetCarriedItemTotalExcludingWarehouse(string itemId)
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
            foreach (var bag in SpeBags.Values)
            {
                Acc(bag);
            }

            CurrencyBag.TryGetValue(itemId, out var currencyVal);
            totalNum += currencyVal;
            return totalNum;
        }

        // 仅从身上扣减，不碰仓库
        public long CostCarriedItem(string itemId, long count)
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

            return leftCount;
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
            if (leftCount > 0 && FurnitureWarehouseBag != null)
            {
                leftCount = FurnitureWarehouseBag.TryCostItem(itemId, leftCount);
            }

            return leftCount;
        }

        public long GiveItemToPlayer(string itemId, long amount)
        {
            var itemConf = ItemCatalog.GetItemDef(itemId);
            if (itemConf == null)
            {
                return 0;
            }

            // 货币不检查格子 但检查上限
            if (itemConf.ItemType == EItemType.Currency)
            {
                CurrencyBag[itemId] = CurrencyBag.GetValueOrDefault(itemId) + amount;
                var max = PlayerGamePlayRule.GetCurrencyMaxStack(LogicManager, itemId);
                if (CurrencyBag[itemId] > max)
                {
                    CurrencyBag[itemId] = max;
                }
                EventOnGainItem?.Invoke(EPlayerBagId.Default, itemId, amount);
                return amount;
            }

            var targetBags = GetGainTargetBags(itemConf);

            
            if (itemConf.IsAutoUse)
            {
                var useRow = ItemCatalog.GetPrimaryUse(itemId);
                if (useRow != null)
                {
                    LogicManager.HandleUseItem(LogicManager.playerLogicEntity.Id, amount, useRow, itemId);
                }

                return amount;
            }
            

            if (ItemCatalog.RequiresInstance(itemConf))
            {
                if (amount > MaxInstanceGrantBatch)
                {
                    Debug.LogWarning(
                        $"GiveItemToPlayer: instance batch {amount} exceeds limit {MaxInstanceGrantBatch}, itemId={itemId}");
                    return 0;
                }

                long total = 0;
                for (long i = 0; i < amount; i++)
                {
                    var stack = ItemCatalog.CreateItemStack(itemId, 1);
                    if (stack == null)
                    {
                        break;
                    }

                    bool placed = false;
                    foreach (var bag in targetBags)
                    {
                        if (bag != null && bag.TryPlaceStackWithoutMerge(stack))
                        {
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                    {
                        break;
                    }

                    total++;
                }

                if (total > 0)
                {
                    EventOnGainItem?.Invoke(EPlayerBagId.Default, itemId, total);
                }

                return total;
            }

            long remaining = amount;
            foreach (var bag in targetBags)
            {
                if (bag == null || remaining <= 0)
                {
                    continue;
                }

                remaining -= bag.TryGiveItem(itemId, remaining);
            }

            var put = amount - remaining;

            EventOnGainItem?.Invoke(EPlayerBagId.Default, itemId, put);

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
            if(bagId == (int)EPlayerBagId.Mind)
            {
                return MindFacetBag;
            }
            if (bagId == (int)EPlayerBagId.Important)
            {
                return ImportantItemBag;
            }

            if (bagId == (int)EPlayerBagId.Storage)
            {
                return WarehouseBag;
            }
            if (bagId == (int)EPlayerBagId.FurnitureStorage)
            {
                return FurnitureWarehouseBag;
            }
            SpeBags.TryGetValue((EPlayerBagId)bagId, out var bag);
            return bag;
        }

        public void WriteMainBagToSave(My.Saving.SaveData save)
        {
            if (save == null || MainBag == null)
            {
                return;
            }

            save.MainInventorySlots ??= new List<My.Saving.MainBagSlotPersist>();
            save.MainInventorySlots.Clear();

            save.PlayerInventoryBags ??= new List<My.Saving.PlayerBagPersist>();
            save.PlayerInventoryBags.Clear();
            save.PlayerInventoryBags.Add(BuildBagPersist(MainBag));
        }

        public void WriteWarehouseToSave(My.Saving.SaveData save)
        {
            if (save == null || WarehouseBag == null)
            {
                return;
            }
            save.WarehousePages ??= new List<My.Saving.WarehousePagePersist>();
            save.WarehousePages.Clear();

            save.SecretBaseStorage ??= new My.Saving.SecretBaseStoragePersist();
            save.SecretBaseStorage.CommonWarehouse = BuildBagPersist(WarehouseBag);
            save.SecretBaseStorage.FurnitureWarehouse = BuildBagPersist(FurnitureWarehouseBag);
        }

        public void WriteSpecialBagsToSave(My.Saving.SaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.SpecialInventoryBags ??= new List<My.Saving.PlayerBagPersist>();
            save.SpecialInventoryBags.Clear();
            save.PlayerInventoryBags ??= new List<My.Saving.PlayerBagPersist>();

            var seen = new HashSet<EPlayerBagId>();
            seen.Add(EPlayerBagId.Default);
            for (int i = 0; i < save.PlayerInventoryBags.Count; i++)
            {
                if (save.PlayerInventoryBags[i] != null)
                {
                    seen.Add((EPlayerBagId)save.PlayerInventoryBags[i].BagId);
                }
            }
            WriteSpecialBagToSave(save, MindFacetBag, seen);
            WriteSpecialBagToSave(save, ImportantItemBag, seen);
            foreach (var bag in SpeBags.Values)
            {
                WriteSpecialBagToSave(save, bag, seen);
            }
        }

        static void WriteSpecialBagToSave(
            My.Saving.SaveData save,
            PlayerBag bag,
            HashSet<EPlayerBagId> seen)
        {
            if (save == null || bag == null || bag.BagId == EPlayerBagId.Default
                || bag.BagId == EPlayerBagId.Storage || bag.BagId == EPlayerBagId.FurnitureStorage)
            {
                return;
            }

            if (!seen.Add(bag.BagId))
            {
                return;
            }

            var bagSave = BuildBagPersist(bag);

            if (bagSave.Slots.Count > 0)
            {
                save.PlayerInventoryBags.Add(bagSave);
            }
        }

        static void AddSlotPersist(My.Saving.PlayerBagPersist bagSave, int slotIndex, ItemStack st)
        {
            if (bagSave == null || st == null || st.IsEmpty)
            {
                return;
            }

            bagSave.Slots.Add(new My.Saving.MainBagSlotPersist
            {
                SlotIndex = slotIndex,
                ItemId = st.ItemID,
                Count = st.Count,
                ItemInstanceId = st.ItemInstanceId,
                InstanceInfo = st.InstanceInfo?.Clone(),
            });
        }

        public bool CanGainItems(string itemId, long count)
        {
            if (count == 0)
            {
                return true;
            }

            if (count < 0)
            {
                return false;
            }

            var itemConf = ItemCatalog.GetItemDef(itemId);
            if (itemConf == null)
            {
                return false;
            }

            if (itemConf.ItemType == EItemType.Currency || itemConf.IsAutoUse)
            {
                return true;
            }

            var targetBags = GetGainTargetBags(itemConf);
            if (targetBags.Count == 0)
            {
                return false;
            }

            if (ItemCatalog.RequiresInstance(itemConf))
            {
                if (count > MaxInstanceGrantBatch)
                {
                    return false;
                }

                long empty = 0;
                foreach (var bag in targetBags)
                {
                    if (bag != null && bag.CanAcceptItem(itemId))
                    {
                        empty += bag.CountDiscreteEmptySlots();
                    }
                }

                return empty >= count;
            }

            long remaining = count;
            foreach (var bag in targetBags)
            {
                remaining = ConsumeBagStackableSpace(bag, itemId, remaining);
                if (remaining <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        List<PlayerBag> GetGainTargetBags(cfg.demo.ItemData itemConf)
        {
            var result = new List<PlayerBag>();
            if (itemConf == null)
            {
                return result;
            }

            if (itemConf.ItemType == EItemType.MindFacet)
            {
                AddDistinctBag(result, GetBagById((int)EPlayerBagId.Mind));
                return result;
            }

            if (TryGetTagSpecialBagForItem(itemConf, out var tagBag))
            {
                AddDistinctBag(result, tagBag);
            }

            AddDistinctBag(result, GetBagById(0));
            return result;
        }

        static void AddDistinctBag(List<PlayerBag> result, PlayerBag bag)
        {
            if (bag == null || result.Contains(bag))
            {
                return;
            }

            result.Add(bag);
        }

        bool TryGetTagSpecialBagForItem(cfg.demo.ItemData itemConf, out PlayerBag bag)
        {
            bag = null;
            if (itemConf == null)
            {
                return false;
            }

            var defs = PlayerBagCatalog.GetAutoGainBagDefs();
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null
                    || def.AcceptedTags == null
                    || def.AcceptedTags.Count == 0
                    || !ItemTagCatalog.HasAnyTag(itemConf, def.AcceptedTags))
                {
                    continue;
                }

                var bagId = (EPlayerBagId)def.BagId;
                if (SpeBags.TryGetValue(bagId, out bag) && bag != null)
                {
                    return true;
                }
            }

            bag = null;
            return false;
        }

        static long ConsumeBagStackableSpace(PlayerBag bag, string itemId, long count)
        {
            if (bag == null || string.IsNullOrEmpty(itemId) || count <= 0)
            {
                return count;
            }

            long remaining = count;
            var maxStack = bag.GetMaxStack(itemId);
            if (maxStack <= 0)
            {
                return remaining;
            }

            remaining = ConsumeStackableSpace(remaining, itemId, maxStack, bag.NormalSlots);
            if (remaining <= 0)
            {
                return 0;
            }

            remaining = ConsumeEmptySlotSpace(remaining, maxStack, bag.NormalSlots);
            if (remaining <= 0)
            {
                return 0;
            }

            remaining = ConsumeStackableSpace(remaining, itemId, maxStack, bag.ExtraSlots);
            if (remaining <= 0)
            {
                return 0;
            }

            if (bag.MaxExtraCapacity > 0)
            {
                var appendableExtraSlots = Math.Max(0, bag.MaxExtraCapacity - bag.ExtraSlots.Count);
                remaining -= appendableExtraSlots * maxStack;
            }

            return Math.Max(0, remaining);
        }

        static long ConsumeStackableSpace(long remaining, string itemId, long maxStack, List<ItemStack> slots)
        {
            if (slots == null)
            {
                return remaining;
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.IsEmpty || slot.ItemID != itemId || slot.Count >= maxStack)
                {
                    continue;
                }

                remaining -= maxStack - slot.Count;
            }

            return remaining;
        }

        static long ConsumeEmptySlotSpace(long remaining, long maxStack, List<ItemStack> slots)
        {
            if (slots == null)
            {
                return remaining;
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot != null && !slot.IsEmpty)
                {
                    continue;
                }

                remaining -= maxStack;
            }

            return remaining;
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
