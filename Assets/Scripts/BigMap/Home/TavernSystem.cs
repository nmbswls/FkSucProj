using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using My.Player.Bag;
using My.Player.Cooking;
using My.Saving;

namespace My.Home
{
    [Serializable]
    public sealed class TavernDishSlotState
    {
        public string ItemId = string.Empty;
        public int Count;
    }

    public sealed class TavernTownState
    {
        public readonly List<TavernDishSlotState> Slots = new();
        public int LastSettlementDay = -1;
        public long LastGoldEarned;
        public int LastInfluenceEarned;
        public int LastSoldCount;

        public TavernTownState()
        {
            for (int i = 0; i < TavernSystem.SlotCount; i++) Slots.Add(new TavernDishSlotState());
        }
    }

    public sealed class TavernSystem
    {
        public const int SlotCount = 3;
        static readonly ECookingStyleTag[] Rotation =
        {
            ECookingStyleTag.Homestyle, ECookingStyleTag.Hearty, ECookingStyleTag.Refreshing,
            ECookingStyleTag.Sweet, ECookingStyleTag.Refined, ECookingStyleTag.Festive,
            ECookingStyleTag.Exotic, ECookingStyleTag.Nostalgic,
        };

        readonly GameLogicManager _logic;
        readonly Dictionary<string, TavernTownState> _states = new(StringComparer.Ordinal);

        public TavernSystem(GameLogicManager logic)
        {
            _logic = logic;
            _logic.EventOnOneDayBalance += OnOneDayBalance;
        }

        public void LoadFromSave(SaveData data)
        {
            _states.Clear();
            if (data?.TavernByTownId == null) return;
            foreach (var pair in data.TavernByTownId)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null) continue;
                var state = new TavernTownState
                {
                    LastSettlementDay = pair.Value.LastSettlementDay,
                    LastGoldEarned = pair.Value.LastGoldEarned,
                    LastInfluenceEarned = pair.Value.LastInfluenceEarned,
                    LastSoldCount = pair.Value.LastSoldCount,
                };
                for (int i = 0; i < SlotCount && i < pair.Value.Slots.Count; i++)
                {
                    state.Slots[i].ItemId = pair.Value.Slots[i]?.ItemId ?? string.Empty;
                    state.Slots[i].Count = Math.Max(0, pair.Value.Slots[i]?.Count ?? 0);
                }
                _states[pair.Key] = state;
            }
        }

        public void ApplyToSaveData(SaveData data)
        {
            if (data == null) return;
            data.TavernByTownId ??= new Dictionary<string, TavernTownPersist>(StringComparer.Ordinal);
            data.TavernByTownId.Clear();
            foreach (var pair in _states)
            {
                var persist = new TavernTownPersist
                {
                    LastSettlementDay = pair.Value.LastSettlementDay,
                    LastGoldEarned = pair.Value.LastGoldEarned,
                    LastInfluenceEarned = pair.Value.LastInfluenceEarned,
                    LastSoldCount = pair.Value.LastSoldCount,
                };
                foreach (var slot in pair.Value.Slots)
                    persist.Slots.Add(new TavernDishSlotPersist { ItemId = slot.ItemId, Count = slot.Count });
                data.TavernByTownId[pair.Key] = persist;
            }
        }

        public TavernTownState GetState(string townId, bool create = true)
        {
            if (string.IsNullOrEmpty(townId)) return null;
            if (_states.TryGetValue(townId, out var state)) return state;
            if (!create) return null;
            state = new TavernTownState();
            _states[townId] = state;
            return state;
        }

        public ECookingStyleTag[] GetActiveTags(string townId)
        {
            int day = _logic?.SettlementDayIndex ?? 0;
            int stableHash = 17;
            string key = townId ?? string.Empty;
            for (int i = 0; i < key.Length; i++) stableHash = stableHash * 31 + key[i];
            int offset = Math.Abs(stableHash) % Rotation.Length;
            return new[] { Rotation[(day + offset) % Rotation.Length], Rotation[(day + offset + 1) % Rotation.Length] };
        }

        public bool TryFill(string townId, string itemId, int count, PlayerInventorySystem inventory, out string reason)
        {
            reason = string.Empty;
            if (inventory?.WarehouseBag == null || string.IsNullOrEmpty(townId) || string.IsNullOrEmpty(itemId) || count <= 0)
            { reason = "invalid"; return false; }
            if (!ItemTagCatalog.HasTag(ItemCatalog.GetItemDef(itemId), EItemTag.Food))
            { reason = "not_food"; return false; }
            var state = GetState(townId);
            int slot = state.Slots.FindIndex(s => string.IsNullOrEmpty(s.ItemId));
            if (slot < 0) { reason = "full"; return false; }
            if (inventory.WarehouseBag.TryCostItem(itemId, count) != 0)
            { reason = "insufficient"; return false; }
            state.Slots[slot].ItemId = itemId;
            state.Slots[slot].Count = count;
            return true;
        }

        void OnOneDayBalance(GameLogicManager.OneDayBalanceInfo balanceInfo)
        {
            int settlementDay = _logic.SettlementDayIndex;
            foreach (var pair in _states)
            {
                var state = pair.Value;
                if (state.LastSettlementDay >= settlementDay) continue;
                long gold = 0; int influence = 0; int sold = 0;
                var hotTags = GetActiveTags(pair.Key);
                foreach (var slot in state.Slots)
                {
                    if (string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0) continue;
                    var recipe = CookingCatalog.GetRecipeByDish(slot.ItemId);
                    int unitValue = 10 + (recipe?.Level ?? 1) * 5;
                    int unitInfluence = 1;
                    if (recipe?.StyleTags != null)
                    {
                        for (int i = 0; i < recipe.StyleTags.Count; i++)
                        {
                            if (recipe.StyleTags[i] == hotTags[0] || recipe.StyleTags[i] == hotTags[1])
                            { unitValue += 5; unitInfluence++; break; }
                        }
                    }
                    gold += (long)unitValue * slot.Count;
                    influence += unitInfluence * slot.Count;
                    sold += slot.Count;
                    slot.ItemId = string.Empty; slot.Count = 0;
                }
                state.LastSettlementDay = settlementDay;
                state.LastGoldEarned = gold; state.LastInfluenceEarned = influence; state.LastSoldCount = sold;
                if (gold > 0) _logic.playerDataManager?.GiveItemToPlayer("gold", gold);
                if (influence > 0) _logic.homeDataManager?.AddTownInfluence(pair.Key, influence);
                if (balanceInfo != null)
                {
                    balanceInfo.TavernGoldEarned += gold;
                    balanceInfo.TavernInfluenceEarned += influence;
                    balanceInfo.TavernSoldCount += sold;
                }
            }
        }
    }
}
