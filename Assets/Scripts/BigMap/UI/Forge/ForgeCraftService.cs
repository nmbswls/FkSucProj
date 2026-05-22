using cfg.demo;
using My;
using My.Config;
using My.Player.Bag;
using UnityEngine;

namespace My.UI.Forge
{
    // 锻造：材料校验、扣除；产物经 GiveItemToPlayer 入账（实例型在背包层逐件独立格）。
    public static class ForgeCraftService
    {
        const long DefaultResultCount = 1;

        public static bool CanCraft(PlayerInventorySystem inv, ForgeRecipe recipe)
        {
            return true;
            return TryCraftDryRun(inv, recipe, out _);
        }

        static bool TryCraftDryRun(PlayerInventorySystem inv, ForgeRecipe recipe, out string failReasonEn)
        {
            failReasonEn = "";
            if (recipe == null || inv == null)
            {
                failReasonEn = "Invalid recipe or inventory.";
                return false;
            }

            if (!HasAllMaterials(inv, recipe))
            {
                failReasonEn = "Insufficient materials.";
                return false;
            }

            var rc = EffectiveResult(recipe);
            if (rc.ItemIdOrEmpty.Length == 0)
            {
                return true;
            }

            var def = ItemCatalog.GetItemDef(rc.ItemIdOrEmpty);
            if (def == null)
            {
                failReasonEn = "Unknown result item.";
                return false;
            }

            if (def.IsAutoUse || def.ItemType == EItemType.Currency)
            {
                return true;
            }

            if (ItemCatalog.IsInstanceType(def.ItemType))
            {
                if (rc.Count > PlayerInventorySystem.MaxInstanceGrantBatch)
                {
                    failReasonEn = "Result count exceeds instance grant limit.";
                    return false;
                }

                var bag = inv.GetBagById(0);
                return bag != null && bag.CountDiscreteEmptySlots() >= rc.Count;
            }

            return inv.CanGainItems(rc.ItemIdOrEmpty, rc.Count);
        }

        static bool HasAllMaterials(PlayerInventorySystem inv, ForgeRecipe recipe)
        {
            if (recipe.Materials == null)
            {
                return true;
            }

            for (int i = 0; i < recipe.Materials.Count; i++)
            {
                var m = recipe.Materials[i];
                if (m == null || string.IsNullOrEmpty(m.ItemId) || m.Count <= 0)
                {
                    continue;
                }

                if (!inv.CheckHaveItem(m.ItemId, m.Count))
                {
                    return false;
                }
            }

            return true;
        }

        struct ResultSpec
        {
            public string ItemIdOrEmpty;
            public long Count;
        }

        static ResultSpec EffectiveResult(ForgeRecipe recipe)
        {
            string id = recipe.ResultItemId;
            if (string.IsNullOrEmpty(id) || id == "none")
            {
                return new ResultSpec { ItemIdOrEmpty = "", Count = 0 };
            }

            return new ResultSpec { ItemIdOrEmpty = id, Count = DefaultResultCount };
        }

        /// <summary>
        /// 尝试锻造：校验材料与背包空间，扣除材料后发奖（实例型逐件占位，不并入已有堆）。
        /// </summary>
        public static bool TryCraft(ForgeRecipe recipe, out string failReasonEn)
        {
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;

            if (!TryCraftDryRun(inv, recipe, out failReasonEn))
            {
                return false;
            }

            SpendMaterials(inv, recipe);

            var rc = EffectiveResult(recipe);
            if (string.IsNullOrEmpty(rc.ItemIdOrEmpty))
            {
                return true;
            }

            long gained = inv.GiveItemToPlayer(rc.ItemIdOrEmpty, rc.Count);
            if (gained < rc.Count)
            {
                failReasonEn = "Failed to grant full result.";
                Debug.LogError($"[Forge] Result grant incomplete id={recipe.Id} gained={gained} expected={rc.Count}");
                return false;
            }

            return true;
        }

        static void SpendMaterials(PlayerInventorySystem inv, ForgeRecipe recipe)
        {
            if (recipe.Materials == null)
            {
                return;
            }

            for (int i = 0; i < recipe.Materials.Count; i++)
            {
                var m = recipe.Materials[i];
                if (m == null || string.IsNullOrEmpty(m.ItemId) || m.Count <= 0)
                {
                    continue;
                }

                var left = inv.CostItem(m.ItemId, m.Count);
                if (left > 0)
                {
                    Debug.LogError($"[Forge] Cost material leftover id={m.ItemId} left={left}");
                }
            }
        }
    }
}
