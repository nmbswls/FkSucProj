using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.Player.Cooking
{
    public static class CookingCatalog
    {
        public const int MaxStyleTagsPerDish = 2;

        public static CookingRecipe GetRecipe(int recipeId) =>
            CfgMgr.Cfgs?.TbCookingRecipe?.GetOrDefault(recipeId);

        public static IReadOnlyList<CookingRecipe> GetAllRecipes()
        {
            var source = CfgMgr.Cfgs?.TbCookingRecipe?.DataList;
            var result = source == null ? new List<CookingRecipe>() : new List<CookingRecipe>(source);
            result.Sort((a, b) =>
            {
                int compare = a.Sort.CompareTo(b.Sort);
                return compare != 0 ? compare : a.Id.CompareTo(b.Id);
            });
            return result;
        }

        public static CookingRecipe GetRecipeByDish(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            var rows = CfgMgr.Cfgs?.TbCookingRecipe?.DataList;
            if (rows == null)
            {
                return null;
            }
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null && (rows[i].ResultItemId == itemId || rows[i].QualityResultItemId == itemId))
                {
                    return rows[i];
                }
            }
            return null;
        }

        public static string ResolveIconSprite(CookingRecipe recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }
            if (!string.IsNullOrEmpty(recipe.IconSprite))
            {
                return recipe.IconSprite;
            }
            return ItemCatalog.GetItemDef(recipe.ResultItemId)?.SpriteName ?? string.Empty;
        }

        public static bool ValidateRecipe(CookingRecipe recipe, out string reason)
        {
            reason = string.Empty;
            if (recipe == null || recipe.Id <= 0 || string.IsNullOrEmpty(recipe.ResultItemId)
                || string.IsNullOrEmpty(recipe.QualityResultItemId) || recipe.OutputCount <= 0)
            {
                reason = "Recipe identity or output is invalid.";
                return false;
            }
            if (recipe.Level <= 0)
            {
                reason = "Recipe level must be positive.";
                return false;
            }
            if (recipe.StyleTags == null || recipe.StyleTags.Count > MaxStyleTagsPerDish)
            {
                reason = $"A dish must have at most {MaxStyleTagsPerDish} style tags.";
                return false;
            }
            if (recipe.IngredientItemIds == null || recipe.IngredientCounts == null
                || recipe.IngredientItemIds.Count == 0
                || recipe.IngredientItemIds.Count != recipe.IngredientCounts.Count)
            {
                reason = "Ingredient ids and counts must be non-empty parallel lists.";
                return false;
            }

            var uniqueIngredients = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < recipe.IngredientItemIds.Count; i++)
            {
                string itemId = recipe.IngredientItemIds[i];
                if (string.IsNullOrEmpty(itemId) || recipe.IngredientCounts[i] <= 0)
                {
                    reason = $"Ingredient at index {i} is invalid.";
                    return false;
                }
                if (!uniqueIngredients.Add(itemId))
                {
                    reason = $"Ingredient item '{itemId}' is duplicated; merge it into one fixed cost.";
                    return false;
                }
                if (ItemCatalog.GetItemDef(itemId) == null)
                {
                    reason = $"Ingredient item '{itemId}' does not exist.";
                    return false;
                }
                if (!ItemCatalog.IsCookingIngredient(itemId))
                {
                    reason = $"Ingredient item '{itemId}' is not marked as a cooking ingredient.";
                    return false;
                }
            }

            if (ItemCatalog.GetItemDef(recipe.ResultItemId) == null)
            {
                reason = $"Result item '{recipe.ResultItemId}' does not exist.";
                return false;
            }
            if (ItemCatalog.GetItemDef(recipe.QualityResultItemId) == null)
            {
                reason = $"Quality result item '{recipe.QualityResultItemId}' does not exist.";
                return false;
            }
            if (ItemCatalog.CanUse(recipe.ResultItemId))
            {
                reason = $"Dish '{recipe.ResultItemId}' must not define item_use.";
                return false;
            }
            return true;
        }

        public static void ValidateAll()
        {
            var recipes = CfgMgr.Cfgs?.TbCookingRecipe?.DataList;
            if (recipes == null)
            {
                return;
            }
            var dishIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (!ValidateRecipe(recipe, out string reason))
                {
                    Debug.LogError($"[Cooking] Invalid recipe id={recipe?.Id}: {reason}");
                }
                else if (!dishIds.Add(recipe.ResultItemId))
                {
                    Debug.LogError($"[Cooking] Multiple recipes produce dish '{recipe.ResultItemId}'.");
                }
            }
        }
    }
}
