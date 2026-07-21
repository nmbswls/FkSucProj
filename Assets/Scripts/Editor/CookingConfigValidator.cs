using System;
using System.Collections.Generic;
using My.Config;
using My.Player.Cooking;
using My.Player.Bag;
using My.Player;
using My;
using cfg.demo;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace My.Editor
{
    public static class CookingConfigValidator
    {
        [MenuItem("Tools/Validate/Cooking Config")]
        public static void ValidateFromMenu()
        {
            ValidateBatch();
            Debug.Log("[Cooking] Configuration validation passed.");
        }

        public static void ValidateBatch()
        {
            CfgMgr.LoadGameConfigs();
            var errors = new List<string>();
            var recipes = CfgMgr.Cfgs?.TbCookingRecipe?.DataList;
            if (recipes == null || recipes.Count == 0)
            {
                throw new BuildFailedException("Cooking recipes are missing.");
            }

            var dishIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (!CookingCatalog.ValidateRecipe(recipe, out string reason))
                {
                    errors.Add($"recipe {recipe?.Id}: {reason}");
                    continue;
                }
                if (!dishIds.Add(recipe.ResultItemId))
                {
                    errors.Add($"dish '{recipe.ResultItemId}' has multiple recipes");
                }
                if (ItemCatalog.CanUse(recipe.ResultItemId))
                {
                    errors.Add($"dish '{recipe.ResultItemId}' has item_use; cooking dishes must remain non-combat items");
                }
                if (ItemCatalog.GetGiftDef(recipe.QualityResultItemId) == null)
                {
                    errors.Add($"quality dish '{recipe.QualityResultItemId}' is missing its gift profile");
                }
                if (ItemCatalog.GetGiftDef(recipe.ResultItemId) == null)
                {
                    errors.Add($"dish '{recipe.ResultItemId}' is missing its gift profile");
                }
            }

            if (errors.Count > 0)
            {
                throw new BuildFailedException("Cooking configuration is invalid:\n" + string.Join("\n", errors));
            }

            ValidateCraftSmokeTest();
            Debug.Log($"[Cooking] Validated {recipes.Count} fixed recipes.");
        }

        static void ValidateCraftSmokeTest()
        {
            var inventory = new PlayerInventorySystem();
            inventory.MainBag.InitBag(EPlayerBagId.Default, 20, 0, EBagStorageLayout.Grid);
            inventory.MindFacetBag.InitBag(EPlayerBagId.Mind, 10, 0, EBagStorageLayout.Compact);
            inventory.WarehouseBag.InitBag(EPlayerBagId.Storage, 20, 0, EBagStorageLayout.Compact);
            inventory.GiveItemToPlayer("produce_berry", 2);
            inventory.GiveItemToPlayer("mind_facet_01", 1);

            var mindRecipe = CookingCatalog.GetRecipe(2001);
            var quote = CookingCraftService.BuildQuote(inventory, null, mindRecipe, 1);
            if (!quote.CanCraft)
            {
                throw new BuildFailedException("Cooking smoke test could not quote the mindfacet recipe.");
            }

            var result = CookingCraftService.TryCraft(inventory, null, mindRecipe, 1, out long output, out long qualityOutput);
            if (result != ECookingActionResult.Success
                || output != 1
                || inventory.GetItemTotal("mind_facet_01", true) != 0
                || inventory.GetItemTotal("dish_slumber_compote", true) + inventory.GetItemTotal("dish_slumber_compote_quality", true) != 1
                || qualityOutput < 0 || qualityOutput > 1)
            {
                throw new BuildFailedException("Cooking smoke test failed to consume mindfacet or grant its dish.");
            }

            var lockedRecipe = CookingCatalog.GetRecipe(3001);
            if (CookingCraftService.BuildQuote(inventory, null, lockedRecipe, 1).Result != ECookingActionResult.Locked)
            {
                throw new BuildFailedException("Cooking smoke test expected the gated recipe to remain locked.");
            }
        }
    }
}
