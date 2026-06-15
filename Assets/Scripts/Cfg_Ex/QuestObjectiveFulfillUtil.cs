using cfg.demo;

namespace My.Cfg_Ex
{
    // 对话交互「尝试完成 objective」：按 obj_type 判断是否支持及展示名
    public static class QuestObjectiveFulfillUtil
    {
        public static bool SupportsDialogFulfill(EQuestObjectiveType objType)
        {
            return objType == EQuestObjectiveType.SubmitItem;
        }

        public static string GetFulfillOptionFallbackText(QuestStepObjective objData)
        {
            if (objData == null)
            {
                return "完成目标";
            }

            if (objData.ObjType == EQuestObjectiveType.SubmitItem)
            {
                var itemId = objData.ObjP4;
                var itemDef = My.Config.ItemCatalog.GetItemDef(itemId);
                var itemName = itemDef?.DisplayName ?? itemId;
                return $"递交 {itemName}";
            }

            return objData.FormatDesc ?? "完成目标";
        }
    }
}
