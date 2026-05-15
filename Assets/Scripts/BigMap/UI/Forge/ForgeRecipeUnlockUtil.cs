using cfg.demo;
using My;
using My.Config;
using My.Player;
using UnityEngine;

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
                    return CheckHasItem(recipe.UnlockParam, 1);
                case EForgeUnlockMode.GlobalSwitch:
                    return CheckGlobalSwitch(recipe.UnlockParam);
                default:
                    return false;
            }
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
