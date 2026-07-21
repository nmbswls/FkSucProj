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
    public static class ItemGearRules
    {
        public static bool IsGearItem(ItemData def)
        {
            return def != null
                && !ItemTagCatalog.HasTag(def, EItemTag.HumanArmar)
                && PartGearCatalog.HasDetail(def.ItemId);
        }

        public static bool MatchesPart(ItemData def, EBodyPart part)
        {
            if (def == null || part == EBodyPart.None || !IsGearItem(def))
            {
                return false;
            }

            return PartGearCatalog.GetBodyPart(def.ItemId) == part;
        }

        public static int GetSlotCost(ItemData def)
        {
            return def != null ? PartGearCatalog.GetSlotCost(def.ItemId) : 1;
        }

        public static int GetSlotCost(string itemId)
        {
            return PartGearCatalog.GetSlotCost(itemId);
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
        readonly PlayerSystemManager _playerMgr;
        readonly List<EquippedGearRuntimeSlot>[] _partSlots;

        public PlayerEquipmentManager(PlayerSystemManager playerMgr)
        {
            _playerMgr = playerMgr;
            _partSlots = new List<EquippedGearRuntimeSlot>[BodyPartUtil.PartSlotCount];
            for (int i = 0; i < _partSlots.Length; i++)
            {
                _partSlots[i] = new List<EquippedGearRuntimeSlot>();
            }
        }

        GameLogicManager Logic => _playerMgr?.logicManager;
        PlayerInventorySystem Inv => _playerMgr?.InventorySystem;
        PlayerProgressionSystem Prog => _playerMgr?.ProgressionSystem;

        public int GetPartGearPointCap(EBodyPart part)
        {
            if (part == EBodyPart.None)
            {
                return 0;
            }

            var def = BodyPartCatalog.GetPartDef(part);
            int cap = def?.BaseGearPoint ?? 1;
            var ycAttr = BodyPartCatalog.MapPartToGearPointYc(part);
            if (Prog != null && ycAttr != EYCAttribute.None)
            {
                cap += (int)Prog.GetFinalAttribute((int)ycAttr);
            }

            return Mathf.Max(0, cap);
        }

        public int GetUsedGearPoint(EBodyPart part)
        {
            var list = GetPartList(part);
            if (list == null)
            {
                return 0;
            }

            int used = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var slot = list[i];
                if (slot == null || string.IsNullOrEmpty(slot.ItemId))
                {
                    continue;
                }

                var def = ItemCatalog.GetItemDef(slot.ItemId);
                used += ItemGearRules.GetSlotCost(def);
            }

            return used;
        }

        List<EquippedGearRuntimeSlot> GetPartList(EBodyPart part)
        {
            int idx = BodyPartUtil.ToSlotIndex(part);
            if (idx < 0 || idx >= _partSlots.Length)
            {
                return null;
            }

            return _partSlots[idx];
        }

        public void EnsureAllPartsBudget()
        {
            foreach (var def in BodyPartCatalog.GetAllPartsSorted())
            {
                EnsurePartBudget(def.PartId);
            }
        }

        void EnsurePartBudget(EBodyPart part)
        {
            var list = GetPartList(part);
            if (list == null)
            {
                return;
            }

            while (GetUsedGearPoint(part) > GetPartGearPointCap(part) && list.Count > 0)
            {
                int last = list.Count - 1;
                if (!TryUnequip(part, last, out _))
                {
                    list.RemoveAt(last);
                    break;
                }
            }
        }

        public IReadOnlyList<EquippedGearRuntimeSlot> GetEquippedOnPart(EBodyPart part)
        {
            return GetPartList(part) ?? (IReadOnlyList<EquippedGearRuntimeSlot>)Array.Empty<EquippedGearRuntimeSlot>();
        }

        public EquippedGearRuntimeSlot GetEquippedSlot(EBodyPart part, int equippedIndex)
        {
            var list = GetPartList(part);
            if (list == null || equippedIndex < 0 || equippedIndex >= list.Count)
            {
                return null;
            }

            return list[equippedIndex];
        }

        public void InitializeFromSave(SaveData save)
        {
            for (int i = 0; i < _partSlots.Length; i++)
            {
                _partSlots[i].Clear();
            }

            if (save?.PlayerData?.EquippedGear != null)
            {
                var grouped = new Dictionary<EBodyPart, List<EquippedGearEntry>>();
                foreach (var e in save.PlayerData.EquippedGear)
                {
                    if (e == null || string.IsNullOrEmpty(e.ItemId))
                    {
                        continue;
                    }

                    if (!Enum.IsDefined(typeof(EBodyPart), e.PartId))
                    {
                        continue;
                    }

                    var part = (EBodyPart)e.PartId;
                    if (!grouped.TryGetValue(part, out var bucket))
                    {
                        bucket = new List<EquippedGearEntry>();
                        grouped[part] = bucket;
                    }

                    bucket.Add(e);
                }

                foreach (var kv in grouped)
                {
                    var list = GetPartList(kv.Key);
                    if (list == null)
                    {
                        continue;
                    }

                    kv.Value.Sort((a, b) => a.EquippedIndex.CompareTo(b.EquippedIndex));
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        var e = kv.Value[i];
                        var restored = ItemCatalog.HydrateItemStackFromPersist(e.ItemId, 1, e.ItemInstanceId, e.InstanceInfo);

                        list.Add(new EquippedGearRuntimeSlot
                        {
                            ItemId = e.ItemId,
                            ItemInstanceId = e.ItemInstanceId,
                            InstanceInfoCopy = restored?.InstanceInfo?.Clone(),
                        });
                    }
                }
            }

            EnsureAllPartsBudget();
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
            foreach (var def in BodyPartCatalog.GetAllPartsSorted())
            {
                var part = def.PartId;
                var list = GetPartList(part);
                if (list == null)
                {
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s == null || string.IsNullOrEmpty(s.ItemId))
                    {
                        continue;
                    }

                    pd.EquippedGear.Add(new EquippedGearEntry
                    {
                        PartId = (int)part,
                        EquippedIndex = i,
                        ItemId = s.ItemId,
                        ItemInstanceId = s.ItemInstanceId,
                        InstanceInfo = s.InstanceInfoCopy?.Clone(),
                    });
                }
            }
        }

        public void PostInit()
        {
            BindProgressionGear();
            Prog?.ProgressionRoot?.ForceDirty();
        }

        public void BindProgressionGear()
        {
            Prog?.GearManager?.BindEquipment(this);
            Prog?.GearManager?.RebuildStatProvidersFromEquipment();
        }

        public void NotifyPlayerReady(GameLogicManager logic)
        {
            if (logic == null)
            {
                Debug.LogError("[PlayerEquipmentManager] NotifyPlayerReady failed: GameLogicManager is null");
                return;
            }

            if (logic.playerLogicEntity == null)
            {
                Debug.LogError("[PlayerEquipmentManager] NotifyPlayerReady failed: playerLogicEntity is null");
                return;
            }

            if (logic.globalBuffManager == null)
            {
                Debug.LogError("[PlayerEquipmentManager] NotifyPlayerReady failed: globalBuffManager is null");
                return;
            }

            // 需在 SpawnEntity 完成 Initialize/OnSpawn 后调用，确保技能/属性/Buff 容器已就绪
            ResyncAllGearBuffs(logic);
            Prog?.GearManager?.RebuildStatProvidersFromEquipment();
        }

        public void ResyncAllGearBuffs(GameLogicManager logic)
        {
            if (logic?.playerLogicEntity == null || logic.globalBuffManager == null)
            {
                return;
            }

            foreach (var def in BodyPartCatalog.GetAllPartsSorted())
            {
                var part = def.PartId;
                var list = GetPartList(part);
                if (list == null)
                {
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s == null || string.IsNullOrEmpty(s.ItemId))
                    {
                        continue;
                    }

                    RemoveGearBuffForSlot(logic, part, i, s.ItemId);
                }
            }

            foreach (var def in BodyPartCatalog.GetAllPartsSorted())
            {
                var part = def.PartId;
                var list = GetPartList(part);
                if (list == null)
                {
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s == null || string.IsNullOrEmpty(s.ItemId))
                    {
                        continue;
                    }

                    ApplyGearBuffForSlot(logic, part, i, s.ItemId);
                }
            }
        }

        static long MakeGearBuffSrcKey(EBodyPart part, int equippedIndex) => (long)part * 4096L + equippedIndex + 1L;

        void ApplyGearBuffForSlot(GameLogicManager logic, EBodyPart part, int equippedIndex, string itemId)
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

            long src = MakeGearBuffSrcKey(part, equippedIndex);
            logic.globalBuffManager.RequestAddBuff(player.Id, def.SpecialBuffId, 1, -1f, null, src);
        }

        void RemoveGearBuffForSlot(GameLogicManager logic, EBodyPart part, int equippedIndex, string itemId)
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

            long src = MakeGearBuffSrcKey(part, equippedIndex);
            logic.globalBuffManager.RemoveAllBuffById(player.Id, def.SpecialBuffId, 0, null, src);
        }

        public bool TryUnequip(EBodyPart part, int equippedIndex, out string failReason)
        {
            failReason = null;
            var list = GetPartList(part);
            if (list == null || equippedIndex < 0 || equippedIndex >= list.Count)
            {
                failReason = "bad_slot";
                return false;
            }

            var slot = list[equippedIndex];
            if (slot == null || string.IsNullOrEmpty(slot.ItemId))
            {
                failReason = "empty";
                return false;
            }

            if (Logic != null)
            {
                RemoveGearBuffForSlot(Logic, part, equippedIndex, slot.ItemId);
            }

            var back = ItemCatalog.CreateItemStack(slot.ItemId, 1);
            if (back != null)
            {
                back.ItemInstanceId = slot.ItemInstanceId;
                back.InstanceInfo = CloneInstanceInfo(slot.InstanceInfoCopy);
            }

            list.RemoveAt(equippedIndex);

            if (back != null && Inv?.MainBag != null)
            {
                if (!Inv.MainBag.TryPlaceStackWithoutMerge(back))
                {
                    failReason = "bag_full";
                    list.Insert(equippedIndex, slot);
                    if (Logic != null)
                    {
                        ApplyGearBuffForSlot(Logic, part, equippedIndex, slot.ItemId);
                    }

                    return false;
                }
            }

            Prog?.GearManager?.RebuildStatProvidersFromEquipment();
            Prog?.ProgressionRoot?.ForceDirty();
            _playerMgr?.BodyPartSystem?.RebuildAllLocalStats();
            return true;
        }

        public bool CanEquipFromMainBag(EBodyPart part, int mainBagFlatIndex, out string failReason)
        {
            failReason = null;
            if (part == EBodyPart.None)
            {
                failReason = "bad_part";
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
            if (!ItemGearRules.MatchesPart(def, part))
            {
                failReason = "wrong_part";
                return false;
            }

            if (!PartGearCatalog.MeetsPartLevel(stack.ItemID, _playerMgr?.BodyPartSystem?.GetPartState(part)))
            {
                failReason = "part_level_low";
                return false;
            }

            int cost = ItemGearRules.GetSlotCost(def);
            if (GetUsedGearPoint(part) + cost > GetPartGearPointCap(part))
            {
                failReason = "no_gear_point";
                return false;
            }

            if (ItemCatalog.RequiresInstance(def) && stack.ItemInstanceId == 0)
            {
                failReason = "need_instance";
                return false;
            }

            return true;
        }

        public bool TryEquipFromMainBag(EBodyPart part, int mainBagFlatIndex, out string failReason)
        {
            failReason = null;
            if (part == EBodyPart.None)
            {
                failReason = "bad_part";
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
            if (!ItemGearRules.MatchesPart(def, part))
            {
                failReason = "wrong_part";
                return false;
            }

            if (!PartGearCatalog.MeetsPartLevel(stack.ItemID, _playerMgr?.BodyPartSystem?.GetPartState(part)))
            {
                failReason = "part_level_low";
                return false;
            }

            int cost = ItemGearRules.GetSlotCost(def);
            if (GetUsedGearPoint(part) + cost > GetPartGearPointCap(part))
            {
                failReason = "no_gear_point";
                return false;
            }

            if (ItemCatalog.RequiresInstance(def))
            {
                if (stack.ItemInstanceId == 0)
                {
                    failReason = "need_instance";
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
            var list = GetPartList(part);
            int equippedIndex = list.Count;
            list.Add(new EquippedGearRuntimeSlot
            {
                ItemId = stack.ItemID,
                ItemInstanceId = stack.ItemInstanceId,
                InstanceInfoCopy = instCopy,
            });

            if (Logic != null && Logic.playerLogicEntity != null)
            {
                ApplyGearBuffForSlot(Logic, part, equippedIndex, stack.ItemID);
            }

            Prog?.GearManager?.RebuildStatProvidersFromEquipment();
            Prog?.ProgressionRoot?.ForceDirty();
            _playerMgr?.BodyPartSystem?.RebuildAllLocalStats();
            return true;
        }

        static ItemInstanceInfo CloneInstanceInfo(ItemInstanceInfo src)
        {
            if (src == null)
            {
                return null;
            }

            return src.Clone();
        }

        void TryReconcileMainBagAgainstEquipped()
        {
            if (Inv?.MainBag == null)
            {
                return;
            }

            foreach (var def in BodyPartCatalog.GetAllPartsSorted())
            {
                var list = GetPartList(def.PartId);
                if (list == null)
                {
                    continue;
                }

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

                    if (def != null && ItemCatalog.RequiresInstance(def))
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

        public IEnumerable<(EBodyPart part, int index, string itemId)> EnumerateEquipped()
        {
            foreach (var def in BodyPartCatalog.GetAllPartsSorted())
            {
                var part = def.PartId;
                var list = GetPartList(part);
                if (list == null)
                {
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    if (s != null && !string.IsNullOrEmpty(s.ItemId))
                    {
                        yield return (part, i, s.ItemId);
                    }
                }
            }
        }

        public List<(int bagFlatIndex, ItemStack stack)> ListMainBagCandidates(EBodyPart part)
        {
            var r = new List<(int, ItemStack)>();
            if (Inv?.MainBag == null || part == EBodyPart.None)
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
                if (ItemGearRules.MatchesPart(def, part))
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
                if (ItemGearRules.MatchesPart(def, part))
                {
                    r.Add((bag.BasicCapacity + j, st));
                }
            }

            return r;
        }
    }

    static class BodyPartUtil
    {
        public const int PartSlotCount = 7;

        public static int ToSlotIndex(EBodyPart part)
        {
            return part switch
            {
                EBodyPart.Mouth => 1,
                EBodyPart.Breast => 2,
                EBodyPart.Womb => 3,
                EBodyPart.Tail => 4,
                EBodyPart.Wing => 5,
                EBodyPart.Skin => 6,
                _ => -1,
            };
        }
    }
}
