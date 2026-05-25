using cfg.demo;
using My.Config;

namespace My.UI
{
    public static class ItemHoverDetailUtil
    {
        public static string BuildDetailText(string itemId, long stackCount = 1)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return string.Empty;
            }

            var def = ItemCatalog.GetItemDef(itemId);
            if (def == null)
            {
                return itemId;
            }

            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"类型: {def.ItemType}");
            if (stackCount > 1)
            {
                lines.AppendLine($"数量: {stackCount}");
            }

            if (def.GearSlotCost > 0)
            {
                lines.AppendLine($"装备点数: {def.GearSlotCost}");
            }

            if (def.GearBodyPart != EBodyPart.None)
            {
                var partDef = BodyPartCatalog.GetPartDef(def.GearBodyPart);
                string partName = partDef != null && !string.IsNullOrEmpty(partDef.DisplayName)
                    ? partDef.DisplayName
                    : def.GearBodyPart.ToString();
                lines.AppendLine($"部位: {partName}");
            }

            return lines.ToString().TrimEnd();
        }
    }
}
