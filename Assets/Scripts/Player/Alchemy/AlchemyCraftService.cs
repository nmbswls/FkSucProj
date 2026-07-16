using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Player.Bag;
using UnityEngine;

namespace My.Player.Alchemy
{
    // 炼金合成：校验锅炉/工具/素材，解析功效集合，匹配配方并扣除材料。
    public static class AlchemyCraftService
    {
        const long DefaultResultCount = 1;

        public static bool CanCraft(
            PlayerInventorySystem inv,
            string furnaceId,
            IReadOnlyList<string> activeToolIds,
            IReadOnlyList<AlchemyInputSlot> materialSlots,
            AlchemyRecipe recipe,
            int progressLevel = -1)
        {
            return TryCraftDryRun(inv, furnaceId, activeToolIds, materialSlots, recipe, progressLevel, out _, out _);
        }

        public static bool TryResolveMix(
            string furnaceId,
            IReadOnlyList<string> activeToolIds,
            IReadOnlyList<AlchemyInputSlot> materialSlots,
            out AlchemyMixState mixState,
            out string failReasonEn,
            int progressLevel = -1)
        {
            int level = progressLevel >= 0 ? progressLevel : AlchemyUnlockUtil.ResolveProgressLevel();
            return AlchemyMixResolver.TryResolve(furnaceId, activeToolIds, materialSlots, level, out mixState, out failReasonEn);
        }

        public static bool TryCraft(
            string furnaceId,
            IReadOnlyList<string> activeToolIds,
            IReadOnlyList<AlchemyInputSlot> materialSlots,
            AlchemyRecipe recipe,
            out string failReasonEn,
            int progressLevel = -1)
        {
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            return TryCraft(inv, furnaceId, activeToolIds, materialSlots, recipe, out failReasonEn, progressLevel);
        }

        public static bool TryCraft(
            PlayerInventorySystem inv,
            string furnaceId,
            IReadOnlyList<string> activeToolIds,
            IReadOnlyList<AlchemyInputSlot> materialSlots,
            AlchemyRecipe recipe,
            out string failReasonEn,
            int progressLevel = -1)
        {
            if (!TryCraftDryRun(inv, furnaceId, activeToolIds, materialSlots, recipe, progressLevel, out var mixState, out failReasonEn))
            {
                return false;
            }

            SpendMaterials(inv, materialSlots);

            var resultItemId = recipe.ResultItemId;
            if (string.IsNullOrEmpty(resultItemId) || resultItemId == "none")
            {
                return true;
            }

            long gained = inv.GiveItemToPlayer(resultItemId, DefaultResultCount);
            if (gained < DefaultResultCount)
            {
                failReasonEn = "Failed to grant full result.";
                Debug.LogError($"[Alchemy] Result grant incomplete id={recipe.Id} gained={gained} expected={DefaultResultCount}");
                return false;
            }

            return true;
        }

        static bool TryCraftDryRun(
            PlayerInventorySystem inv,
            string furnaceId,
            IReadOnlyList<string> activeToolIds,
            IReadOnlyList<AlchemyInputSlot> materialSlots,
            AlchemyRecipe recipe,
            int progressLevel,
            out AlchemyMixState mixState,
            out string failReasonEn)
        {
            mixState = null;
            failReasonEn = "";

            if (recipe == null || inv == null)
            {
                failReasonEn = "Invalid recipe or inventory.";
                return false;
            }

            if (!AlchemyUnlockUtil.IsRecipeUnlocked(recipe))
            {
                failReasonEn = "Recipe is locked.";
                return false;
            }

            int level = progressLevel >= 0 ? progressLevel : AlchemyUnlockUtil.ResolveProgressLevel();
            if (!AlchemyMixResolver.TryResolve(furnaceId, activeToolIds, materialSlots, level, out mixState, out failReasonEn))
            {
                return false;
            }

            if (!AlchemyMixResolver.MatchesRecipe(mixState, recipe))
            {
                failReasonEn = "Materials do not satisfy recipe.";
                return false;
            }

            if (!HasAllMaterials(inv, materialSlots))
            {
                failReasonEn = "Insufficient materials.";
                return false;
            }

            if (!string.IsNullOrEmpty(recipe.ResultItemId) && recipe.ResultItemId != "none")
            {
                var def = ItemCatalog.GetItemDef(recipe.ResultItemId);
                if (def == null)
                {
                    failReasonEn = "Unknown result item.";
                    return false;
                }

                if (!inv.CanGainItems(recipe.ResultItemId, DefaultResultCount))
                {
                    failReasonEn = "Not enough space for result item.";
                    return false;
                }
            }

            return true;
        }

        static bool HasAllMaterials(PlayerInventorySystem inv, IReadOnlyList<AlchemyInputSlot> materialSlots)
        {
            if (materialSlots == null)
            {
                return true;
            }

            var required = new Dictionary<string, long>();
            for (int i = 0; i < materialSlots.Count; i++)
            {
                var slot = materialSlots[i];
                if (string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0)
                {
                    continue;
                }

                required.TryGetValue(slot.ItemId, out var current);
                required[slot.ItemId] = current + slot.Count;
            }

            foreach (var pair in required)
            {
                if (!inv.CheckHaveItem(pair.Key, pair.Value))
                {
                    return false;
                }
            }

            return true;
        }

        static void SpendMaterials(PlayerInventorySystem inv, IReadOnlyList<AlchemyInputSlot> materialSlots)
        {
            if (materialSlots == null)
            {
                return;
            }

            var required = new Dictionary<string, long>();
            for (int i = 0; i < materialSlots.Count; i++)
            {
                var slot = materialSlots[i];
                if (string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0)
                {
                    continue;
                }

                required.TryGetValue(slot.ItemId, out var current);
                required[slot.ItemId] = current + slot.Count;
            }

            foreach (var pair in required)
            {
                var left = inv.CostItem(pair.Key, pair.Value);
                if (left > 0)
                {
                    Debug.LogError($"[Alchemy] Cost material leftover id={pair.Key} left={left}");
                }
            }
        }
    }
}
