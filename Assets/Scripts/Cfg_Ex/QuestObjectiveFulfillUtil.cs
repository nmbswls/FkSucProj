using cfg.demo;

namespace My.Cfg_Ex
{
    public static class QuestObjectiveFulfillUtil
    {
        public static bool SupportsDialogFulfill(EQuestObjectiveType objType)
        {
            return objType == EQuestObjectiveType.SubmitItem
                || objType == EQuestObjectiveType.Talk;
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
