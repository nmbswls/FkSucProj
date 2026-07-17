using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;

namespace My.Player.Alchemy
{
    public static class AlchemyUnlockUtil
    {
        public static bool IsRecipeUnlocked(AlchemyRecipe recipe) => CheckUnlock(recipe?.UnlockMode ?? EForgeUnlockMode.None,
            recipe?.UnlockParam, recipe?.UnlockItemMinCount ?? 0);

        public static int ResolveToolExtraMaterialSlots(IReadOnlyList<string> activeToolIds)
        {
            int extra = 0;
            if (activeToolIds == null)
            {
                return extra;
            }

            for (int i = 0; i < activeToolIds.Count; i++)
            {
                var tool = AlchemyCatalog.GetTool(activeToolIds[i]);
                if (tool != null)
                {
                    extra += tool.ExtraMaterialSlots;
                }
            }

            return extra;
        }

        static bool CheckUnlock(EForgeUnlockMode mode, string unlockParam, long unlockItemMinCount)
        {
            switch (mode)
            {
                case EForgeUnlockMode.None:
                    return true;
                case EForgeUnlockMode.QuestFinished:
                    return CheckQuestFinished(unlockParam);
                case EForgeUnlockMode.HasItem:
                    return CheckHasItemUnlock(unlockParam, unlockItemMinCount);
                case EForgeUnlockMode.GlobalSwitch:
                    return CheckGlobalSwitch(unlockParam);
                default:
                    return false;
            }
        }

        static bool CheckHasItemUnlock(string unlockParam, long unlockItemMinCount)
        {
            string itemId = unlockParam;
            long min = unlockItemMinCount > 0 ? unlockItemMinCount : 1L;
            if (!string.IsNullOrEmpty(unlockParam))
            {
                int bar = unlockParam.IndexOf('|');
                if (bar >= 0)
                {
                    itemId = unlockParam.Substring(0, bar).Trim();
                    var tail = unlockParam.Substring(bar + 1).Trim();
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
            return inv != null && inv.GetCarriedItemTotal(itemId) >= minCount;
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
