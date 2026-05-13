using System;
using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Player.Bag;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public enum EGearCategory
    {
        Equip = 0,
        Pocket = 1,
        Insertion = 2,
        Misc = 3,
    }

    public static class GearCategoryRules
    {
        public const string MiscGearStackTag = "gear_misc";

        public static bool ItemMatchesCategory(ItemData def, EGearCategory cat)
        {
            if (def == null)
            {
                return false;
            }

            switch (cat)
            {
                case EGearCategory.Equip:
                    return def.ItemType == EItemType.Equip;
                case EGearCategory.Pocket:
                    return def.ItemType == EItemType.Pocket;
                case EGearCategory.Insertion:
                    return def.ItemType == EItemType.Insertion;
                case EGearCategory.Misc:
                    if (def.ItemType != EItemType.Normal)
                    {
                        return false;
                    }

                    if (def.StackTags == null)
                    {
                        return false;
                    }

                    for (int i = 0; i < def.StackTags.Count; i++)
                    {
                        if (def.StackTags[i] == MiscGearStackTag)
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }
    }

    public sealed class EquippedGearRuntimeSlot
    {
        public string ItemId;
        public long ItemInstanceId;
        public ItemInstanceInfo InstanceInfoCopy;
    }

    public sealed class PlayerEquipmentManager
    {
        public const int CategoryCount = 4;

        readonly PlayerSystemManager _playerMgr;
        readonly List<EquippedGearRuntimeSlot>[] _slots;

        public PlayerEquipmentManager(PlayerSystemManager playerMgr)
        {
            _playerMgr = playerMgr;
            _slots = new List<EquippedGearRuntimeSlot>[CategoryCount];
            for (int i = 0; i < CategoryCount; i++)
            {
                _slots[i] = new List<EquippedGearRuntimeSlot>();
            }
        }

        GameLogicManager Logic => _playerMgr?.logicManager;
        PlayerInventorySystem Inv => _playerMgr?.InventorySystem;
        PlayerProgressionSystem Prog => _playerMgr?.ProgressionSystem;

        public int GetSlotCap(EGearCategory cat)
        {
            int bas = 1;
            if (cat == EGearCategory.Misc && Prog != null)
            {
                int bonus = (int)Prog.GetFinalAttribute((int)EYCAttribute.ExtraJingYuanSlot);
                bas += Mathf.Max(0, bonus);
            }

            return bas;
        }

        void EnsureCategoryStructure(EGearCategory cat)
        {
            int cap = GetSlotCap(cat);
            var list = _slots[(int)cat];
            while (list.Count < cap)
            {
                list.Add(null);
            }

            while (list.Count > cap)
            {
                int last = list.Count - 1;
                if (list[last] != null)
                {
                    if (!TryUnequip(cat, last, out _))
                    {
                        break;
                    }
                }

                list.RemoveAt(last);
            }
        }

        public void EnsureAllCategoriesSized()
        {
            for (int i = 0; i < CategoryCount; i++)
            {
                EnsureCategoryStructure((EGearCategory)i);
            }
        }

        public EquippedGearRuntimeSlot GetSlot(EGearCategory cat, int index)
        {
            var list = _slots[(int)cat];
            if (index < 0 || index >= list.Count)
            {
                return null;
            }

            return list[index];
        }

        public void InitializeFromSave(SaveData save)
        {
            for (int c = 0; c < CategoryCount; c++)
            {
                _slots[c].Clear();
            }

            if (save?.PlayerData?.EquippedGear == null || save.PlayerData.EquippedGear.Count == 0)
            {
                EnsureAllCategoriesSized();
                return;
            }

            foreach (var e in save.PlayerData.EquippedGear)
            {
                if (e == null || string.IsNullOrEmpty(e.ItemId))
                {
                    continue;
                }

                if (e.Category < 0 || e.Category >= CategoryCount)
                {
                    continue;
                }

                var cat = (EGearCategory)e.Category;
                EnsureCategoryStructure(cat);
                var list = _slots[(int)cat];
                while (list.Count <= e.SlotIndex)
                {
                    list.Add(null);
                }

                if (e.SlotIndex < 0 || e.SlotIndex >= list.Count)
                {
                    continue;
                }

                var def = ItemCatalog.GetItemDef(e.ItemId);
                ItemInstanceInfo instCopy = null;
                if (def != null && def.ItemType == EItemType.Equip)
                {
                    instCopy = new ItemInstance4Equip { RandVal = e.EquipAuxData };
                }

                list[e.SlotIndex] = new EquippedGearRuntimeSlot
                {
                    ItemId = e.ItemId,
                    ItemInstanceId = e.ItemInstanceId,
                    InstanceInfoCopy = instCopy,
                };
            }

            EnsureAllCategoriesSized();
            TryReconcileMainBagAgainstEquipped();
        }

        public void SaveTo(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.EquippedGear ??= new List<EquippedGearEntry>();
            pd.EquippedGear.Clear();
            for (int ci = 0; ci < CategoryCount; ci++)
            {
                var cat = (EGearCategory)ci;
                EnsureCategoryStructure(cat);
                var list = _slots[ci];
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s == null || string.IsNullOrEmpty(s.ItemId))
                    {
                        continue;
                    }

                    long aux = 0;
                    if (s.InstanceInfoCopy is ItemInstance4Equip eq)
                    {
                        aux = eq.RandVal;
                    }

                    pd.EquippedGear.Add(new EquippedGearEntry
                    {
                        Category = ci,
                        SlotIndex = i,
                        ItemId = s.ItemId,
                        ItemInstanceId = s.ItemInstanceId,
                        EquipAuxData = aux,
                    });
                }
            }
        }

        public void BindProgressionGear()
        {
            Prog?.GearManager?.BindEquipment(this);
            Prog?.GearManager?.RebuildStatProvidersFromEquipment();
        }

        public void NotifyPlayerReady(GameLogicManager logic)
        {
            if (logic?.playerLogicEntity == null || logic.globalBuffManager == null)
            {
                return;
            }

            ResyncAllGearBuffs(logic);
            Prog?.GearManager?.RebuildStatProvidersFromEquipment();
        }

        public void ResyncAllGearBuffs(GameLogicManager logic)
        {
            if (logic?.playerLogicEntity == null || logic.globalBuffManager == null)
            {
                return;
            }

            for (int ci = 0; ci < CategoryCount; ci++)
            {
                var cat = (EGearCategory)ci;
                var list = _slots[ci];
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s == null || string.IsNullOrEmpty(s.ItemId))
                    {
                        continue;
                    }

                    RemoveGearBuffForSlot(logic, cat, i, s.ItemId);
                }
            }

            for (int ci = 0; ci < CategoryCount; ci++)
            {
                var cat = (EGearCategory)ci;
                var list = _slots[ci];
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s == null || string.IsNullOrEmpty(s.ItemId))
                    {
                        continue;
                    }

                    ApplyGearBuffForSlot(logic, cat, i, s.ItemId);
                }
            }
        }

        static long MakeGearBuffSrcKey(EGearCategory cat, int slotIndex) => (long)cat * 4096L + slotIndex + 1L;

        void ApplyGearBuffForSlot(GameLogicManager logic, EGearCategory cat, int slotIndex, string itemId)
        {
            var def = ItemCatalog.GetItemDef(itemId);
            if (def == null || string.IsNullOrEmpty(def.SpecialBuffId))
            {
                return;
            }

            var player = logic.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            long src = MakeGearBuffSrcKey(cat, slotIndex);
            logic.globalBuffManager.RequestAddBuff(player.Id, def.SpecialBuffId, 1, -1f, null, src);
        }

        void RemoveGearBuffForSlot(GameLogicManager logic, EGearCategory cat, int slotIndex, string itemId)
        {
            var def = ItemCatalog.GetItemDef(itemId);
            if (def == null || string.IsNullOrEmpty(def.SpecialBuffId))
            {
                return;
            }

            var player = logic.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            long src = MakeGearBuffSrcKey(cat, slotIndex);
            logic.globalBuffManager.RemoveAllBuffById(player.Id, def.SpecialBuffId, 0, null, src);
        }

        public bool TryUnequip(EGearCategory cat, int slotIndex, out string failReason)
        {
            failReason = null;
            EnsureCategoryStructure(cat);
            var list = _slots[(int)cat];
            if (slotIndex < 0 || slotIndex >= list.Count)
            {
                failReason = "bad_slot";
                return false;
            }

            var slot = list[slotIndex];
            if (slot == null || string.IsNullOrEmpty(slot.ItemId))
            {
                failReason = "empty";
                return false;
            }

            if (Logic != null)
            {
                RemoveGearBuffForSlot(Logic, cat, slotIndex, slot.ItemId);
            }

            var back = ItemCatalog.CreateItemStack(slot.ItemId, 1);
            if (back != null)
            {
                back.ItemInstanceId = slot.ItemInstanceId;
                back.InstanceInfo = CloneInstanceInfo(slot.InstanceInfoCopy);
            }

            list[slotIndex] = null;

            if (back != null && Inv != null)
            {
                long put = Inv.GiveItemToPlayer(back.ItemID, back.Count);
                if (put < 1)
                {
                    failReason = "bag_full";
                    list[slotIndex] = slot;
                    if (Logic != null)
                    {
                        ApplyGearBuffForSlot(Logic, cat, slotIndex, slot.ItemId);
                    }

                    return false;
                }
            }

            Prog?.GearManager?.RebuildStatProvidersFromEquipment();
            Prog?.ProgressionRoot?.ForceDirty();
            return true;
        }

        public bool TryEquipFromMainBagSlot(EGearCategory cat, int gearSlotIndex, int mainBagFlatIndex, out string failReason)
        {
            failReason = null;
            EnsureCategoryStructure(cat);
            var list = _slots[(int)cat];
            if (gearSlotIndex < 0 || gearSlotIndex >= list.Count)
            {
                failReason = "bad_gear_slot";
                return false;
            }

            if (Inv?.MainBag == null)
            {
                failReason = "no_bag";
                return false;
            }

            var stack = Inv.MainBag.GetItemByIdx(mainBagFlatIndex);
            if (stack == null || stack.IsEmpty)
            {
                failReason = "no_item";
                return false;
            }

            var def = ItemCatalog.GetItemDef(stack.ItemID);
            if (!GearCategoryRules.ItemMatchesCategory(def, cat))
            {
                failReason = "wrong_category";
                return false;
            }

            if (ItemCatalog.IsInstanceType(def.ItemType))
            {
                if (stack.ItemInstanceId == 0)
                {
                    failReason = "need_instance";
                    return false;
                }
            }

            if (list[gearSlotIndex] != null)
            {
                if (!TryUnequip(cat, gearSlotIndex, out failReason))
                {
                    return false;
                }
            }

            long removed = Inv.MainBag.RemoveAt(mainBagFlatIndex, 1);
            if (removed < 1)
            {
                failReason = "remove_fail";
                return false;
            }

            var instCopy = CloneInstanceInfo(stack.InstanceInfo);

            list[gearSlotIndex] = new EquippedGearRuntimeSlot
            {
                ItemId = stack.ItemID,
                ItemInstanceId = stack.ItemInstanceId,
                InstanceInfoCopy = instCopy,
            };

            if (Logic != null && Logic.playerLogicEntity != null)
            {
                ApplyGearBuffForSlot(Logic, cat, gearSlotIndex, stack.ItemID);
            }

            Prog?.GearManager?.RebuildStatProvidersFromEquipment();
            Prog?.ProgressionRoot?.ForceDirty();
            return true;
        }

        static ItemInstanceInfo CloneInstanceInfo(ItemInstanceInfo src)
        {
            if (src == null)
            {
                return null;
            }

            if (src is ItemInstance4Equip e)
            {
                return new ItemInstance4Equip { RandVal = e.RandVal };
            }

            if (src is ItemInstance4Insertion i)
            {
                return new ItemInstance4Insertion { Lifetime = i.Lifetime, BuffTickTimer = i.BuffTickTimer };
            }

            return null;
        }

        void TryReconcileMainBagAgainstEquipped()
        {
            if (Inv?.MainBag == null)
            {
                return;
            }

            for (int ci = 0; ci < CategoryCount; ci++)
            {
                var cat = (EGearCategory)ci;
                var list = _slots[ci];
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s == null || string.IsNullOrEmpty(s.ItemId))
                    {
                        continue;
                    }

                    TryRemoveOneMatchingFromMainBag(s.ItemId, s.ItemInstanceId);
                }
            }
        }

        void TryRemoveOneMatchingFromMainBag(string itemId, long instanceId)
        {
            var bag = Inv.MainBag;
            var def = ItemCatalog.GetItemDef(itemId);

            for (int pass = 0; pass < 2; pass++)
            {
                var slots = pass == 0 ? bag.NormalSlots : bag.ExtraSlots;
                for (int i = 0; i < slots.Count; i++)
                {
                    var st = slots[i];
                    if (st == null || st.IsEmpty || st.ItemID != itemId)
                    {
                        continue;
                    }

                    if (def != null && ItemCatalog.IsInstanceType(def.ItemType))
                    {
                        if (instanceId != 0 && st.ItemInstanceId != instanceId)
                        {
                            continue;
                        }
                    }

                    if (pass == 0)
                    {
                        bag.RemoveAt(i, 1);
                    }
                    else
                    {
                        int flat = bag.BasicCapacity + i;
                        bag.RemoveAt(flat, 1);
                    }

                    return;
                }
            }
        }

        public IEnumerable<(EGearCategory cat, int index, string itemId)> EnumerateEquipped()
        {
            for (int ci = 0; ci < CategoryCount; ci++)
            {
                var cat = (EGearCategory)ci;
                var list = _slots[ci];
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s != null && !string.IsNullOrEmpty(s.ItemId))
                    {
                        yield return (cat, i, s.ItemId);
                    }
                }
            }
        }

        public List<(int bagFlatIndex, ItemStack stack)> ListMainBagCandidates(EGearCategory cat)
        {
            var r = new List<(int, ItemStack)>();
            if (Inv?.MainBag == null)
            {
                return r;
            }

            var bag = Inv.MainBag;
            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                var st = bag.NormalSlots[i];
                if (st == null || st.IsEmpty)
                {
                    continue;
                }

                var def = ItemCatalog.GetItemDef(st.ItemID);
                if (GearCategoryRules.ItemMatchesCategory(def, cat))
                {
                    r.Add((i, st));
                }
            }

            for (int j = 0; j < bag.ExtraSlots.Count; j++)
            {
                var st = bag.ExtraSlots[j];
                if (st == null || st.IsEmpty)
                {
                    continue;
                }

                var def = ItemCatalog.GetItemDef(st.ItemID);
                if (GearCategoryRules.ItemMatchesCategory(def, cat))
                {
                    r.Add((bag.BasicCapacity + j, st));
                }
            }

            return r;
        }
    }
}
