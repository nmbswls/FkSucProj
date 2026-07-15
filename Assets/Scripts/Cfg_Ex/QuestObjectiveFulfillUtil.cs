using cfg.demo;
using My.Player;

namespace My.Cfg_Ex
{
    public static class QuestObjectiveFulfillUtil
    {
        public static bool SupportsDialogFulfill(EQuestObjectiveType objType)
        {
            return objType == EQuestObjectiveType.SubmitItem
                || objType == EQuestObjectiveType.Talk;
        }

        // Hub 是否展示 ObjectiveDialog：目标未完成；SubmitItem 还需持有足够物品。
        // Talk 进度由已触发 ObjectiveDialog 统计，不读 ProgressVal。
        public static bool CanPresentDialogFulfill(QuestObjectiveRuntime objRuntime, PlayerQuestSystem questSystem)
        {
            if (objRuntime?.Data == null || questSystem == null)
            {
                return false;
            }

            if (!SupportsDialogFulfill(objRuntime.Data.ObjType))
            {
                return false;
            }

            if (objRuntime.GetCurrProgress() >= objRuntime.GetRequireProgress())
            {
                return false;
            }

            if (objRuntime.Data.ObjType == EQuestObjectiveType.SubmitItem)
            {
                var itemId = objRuntime.Data.ObjP4;
                var needCount = objRuntime.GetRequireProgress();
                if (string.IsNullOrEmpty(itemId) || needCount <= 0)
                {
                    return false;
                }

                var pdm = questSystem.Ctx?.playerDataManager;
                if (pdm == null || !pdm.CheckHaveItem(itemId, needCount))
                {
                    return false;
                }
            }

            return true;
        }

        public static string GetFulfillOptionFallbackText(QuestStepObjective objData)
        {
            if (objData == null)
            {
                return "Complete objective";
            }

            if (objData.ObjType == EQuestObjectiveType.SubmitItem)
            {
                var itemId = objData.ObjP4;
                var itemDef = My.Config.ItemCatalog.GetItemDef(itemId);
                var itemName = itemDef?.DisplayName ?? itemId;
                return $"Submit {itemName}";
            }

            if (objData.ObjType == EQuestObjectiveType.Talk)
            {
                return string.IsNullOrEmpty(objData.FormatDesc)
                    ? "Talk"
                    : objData.FormatDesc;
            }

            return objData.FormatDesc ?? "Complete objective";
        }
    }
}
