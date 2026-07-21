using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player.Bag;
using UnityEngine;

namespace My.Player.Cooking
{
    public static class CookingCraftService
    {
        public const int MaxBatchCount = 99;
        public const float QualityChance = 0.10f;

        public static bool IsRecipeUnlocked(CookingRecipe recipe, PlayerSystemManager player)
        {
            return recipe != null
                && (string.IsNullOrEmpty(recipe.UnlockKey) || player?.CheckHasParam(recipe.UnlockKey) == true);
        }

        public static CookingCraftQuote BuildQuote(
            PlayerInventorySystem inventory,
            PlayerSystemManager player,
            CookingRecipe recipe,
            int batchCount)
        {
            var ingredientQuotes = new List<CookingIngredientQuote>();
            var quote = new CookingCraftQuote
            {
                Recipe = recipe,
                BatchCount = batchCount,
                Ingredients = ingredientQuotes,
                Result = ECookingActionResult.InvalidRequest,
            };

            if (inventory == null || recipe == null || batchCount <= 0 || batchCount > MaxBatchCount)
            {
                return quote;
            }

            quote.IsUnlocked = IsRecipeUnlocked(recipe, player);
            if (!quote.IsUnlocked)
            {
                quote.Result = ECookingActionResult.Locked;
                return quote;
            }

            quote.IsConfigValid = CookingCatalog.ValidateRecipe(recipe, out _);
            if (!quote.IsConfigValid
                || string.IsNullOrEmpty(recipe.QualityResultItemId)
                || ItemCatalog.GetItemDef(recipe.QualityResultItemId) == null
                || ItemCatalog.CanUse(recipe.QualityResultItemId)
                || !TryMultiply(recipe.OutputCount, batchCount, out long outputCount))
            {
                quote.Result = ECookingActionResult.InvalidConfig;
                return quote;
            }
            quote.OutputCount = outputCount;
            quote.QualityResultItemId = recipe.QualityResultItemId;
            quote.QualityChance = QualityChance;

            bool hasMaterials = true;
            for (int i = 0; i < recipe.IngredientItemIds.Count; i++)
            {
                if (!TryMultiply(recipe.IngredientCounts[i], batchCount, out long required))
                {
                    quote.Result = ECookingActionResult.InvalidRequest;
                    return quote;
                }
                long owned = inventory.GetItemTotal(recipe.IngredientItemIds[i], includeWarehouse: true);
                ingredientQuotes.Add(new CookingIngredientQuote
                {
                    ItemId = recipe.IngredientItemIds[i],
                    RequiredCount = required,
                    OwnedCount = owned,
                });
                hasMaterials &= owned >= required;
            }

            quote.HasMaterials = hasMaterials;
            quote.HasOutputSpace = inventory.CanGainItems(recipe.ResultItemId, outputCount)
                && inventory.CanGainItems(recipe.QualityResultItemId, outputCount);
            quote.Result = !quote.HasMaterials
                ? ECookingActionResult.InsufficientItems
                : !quote.HasOutputSpace
                    ? ECookingActionResult.InventoryFull
                    : ECookingActionResult.Success;
            return quote;
        }

        public static ECookingActionResult TryCraft(
            PlayerInventorySystem inventory,
            PlayerSystemManager player,
            CookingRecipe recipe,
            int batchCount,
            out long outputCount)
        {
            return TryCraft(inventory, player, recipe, batchCount, out outputCount, out _);
        }

        public static ECookingActionResult TryCraft(
            PlayerInventorySystem inventory,
            PlayerSystemManager player,
            CookingRecipe recipe,
            int batchCount,
            out long outputCount,
            out long qualityOutputCount)
        {
            outputCount = 0;
            qualityOutputCount = 0;
            var quote = BuildQuote(inventory, player, recipe, batchCount);
            if (!quote.CanCraft)
            {
                return quote.Result;
            }

            var spent = new List<CookingIngredientQuote>(quote.Ingredients.Count);
            for (int i = 0; i < quote.Ingredients.Count; i++)
            {
                var ingredient = quote.Ingredients[i];
                long left = inventory.CostItem(ingredient.ItemId, ingredient.RequiredCount);
                long consumed = ingredient.RequiredCount - left;
                if (consumed > 0)
                {
                    spent.Add(new CookingIngredientQuote { ItemId = ingredient.ItemId, RequiredCount = consumed });
                }
                if (left != 0)
                {
                    RestoreIngredients(inventory, spent);
                    return ECookingActionResult.UnexpectedInventoryFailure;
                }
            }

            long qualityOutput = 0;
            long normalOutput = 0;
            for (long i = 0; i < quote.OutputCount; i++)
            {
                if (UnityEngine.Random.value < QualityChance) qualityOutput++;
                else normalOutput++;
            }

            long grantedNormal = normalOutput > 0
                ? inventory.GiveItemToPlayer(recipe.ResultItemId, normalOutput)
                : 0;
            long grantedQuality = qualityOutput > 0
                ? inventory.GiveItemToPlayer(recipe.QualityResultItemId, qualityOutput)
                : 0;
            if (grantedNormal != normalOutput || grantedQuality != qualityOutput)
            {
                if (grantedNormal > 0)
                {
                    inventory.CostItem(recipe.ResultItemId, grantedNormal);
                }
                if (grantedQuality > 0)
                {
                    inventory.CostItem(recipe.QualityResultItemId, grantedQuality);
                }
                RestoreIngredients(inventory, spent);
                Debug.LogError($"[Cooking] Output grant changed after preflight, recipe={recipe.Id}.");
                return ECookingActionResult.UnexpectedInventoryFailure;
            }

            outputCount = grantedNormal + grantedQuality;
            qualityOutputCount = grantedQuality;
            return ECookingActionResult.Success;
        }

        static void RestoreIngredients(PlayerInventorySystem inventory, List<CookingIngredientQuote> spent)
        {
            for (int i = 0; i < spent.Count; i++)
            {
                long restored = inventory.GiveItemToPlayer(spent[i].ItemId, spent[i].RequiredCount);
                if (restored != spent[i].RequiredCount)
                {
                    Debug.LogError($"[Cooking] Failed to restore ingredient '{spent[i].ItemId}'.");
                }
            }
        }

        static bool TryMultiply(long value, long multiplier, out long result)
        {
            result = 0;
            if (value < 0 || multiplier < 0 || (value != 0 && multiplier > long.MaxValue / value))
            {
                return false;
            }
            result = value * multiplier;
            return true;
        }
    }
}
