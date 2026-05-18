using cfg.demo;
using My;
using My.Config;

namespace My.UI.Forge
{
    public static class ForgeRecipeUnlockUtil
    {
        public static bool IsUnlocked(ForgeRecipe recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            switch (recipe.UnlockMode)
            {
                case EForgeUnlockMode.None:
                    return true;
                case EForgeUnlockMode.QuestFinished:
                    return CheckQuestFinished(recipe.UnlockParam);
                case EForgeUnlockMode.HasItem:
                    return CheckHasItemUnlock(recipe);
                case EForgeUnlockMode.GlobalSwitch:
                    return CheckGlobalSwitch(recipe.UnlockParam);
                default:
                    return false;
            }
        }

        static bool CheckHasItemUnlock(ForgeRecipe recipe)
        {
            string itemId = recipe.UnlockParam;
            long min = recipe.UnlockItemMinCount > 0 ? recipe.UnlockItemMinCount : 1L;
            if (!string.IsNullOrEmpty(recipe.UnlockParam))
            {
                int bar = recipe.UnlockParam.IndexOf('|');
                if (bar >= 0)
                {
                    itemId = recipe.UnlockParam.Substring(0, bar).Trim();
                    var tail = recipe.UnlockParam.Substring(bar + 1).Trim();
                    if (long.TryParse(tail, out var parsed))
                    {
                        min = parsed;
                    }
                }
            }

            return CheckHasItem(itemId, min);
        }

        static bool CheckQuestFinished(string param)
        {
            if (string.IsNullOrEmpty(param) || !int.TryParse(param, out var questId))
            {
                return false;
            }

            var qs = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.QuestSystem;
            return qs != null && qs.CheckQuestFinish(questId);
        }

        static bool CheckHasItem(string itemId, long minCount)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            if (inv == null)
            {
                return false;
            }

            return inv.GetCarriedItemTotal(itemId) >= minCount;
        }

        static bool CheckGlobalSwitch(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                return false;
            }

            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            return pdm != null && pdm.CheckHasParam(param);
        }
    }
}
