using cfg.demo;
using My.Config;
using My.Player;

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
            var tagNames = ItemTagCatalog.GetVisibleTagDisplayNames(def);
            if (tagNames.Count > 0)
            {
                lines.AppendLine($"标签: {string.Join(" / ", tagNames)}");
            }

            if (stackCount > 1)
            {
                lines.AppendLine($"数量: {stackCount}");
            }

            var partGear = PartGearCatalog.GetOrDefault(itemId);
            if (partGear != null)
            {
                if (partGear.GearSlotCost > 0)
                {
                    lines.AppendLine($"装备点数: {partGear.GearSlotCost}");
                }

                if (partGear.BodyPart != EBodyPart.None)
                {
                    var partDef = BodyPartCatalog.GetPartDef(partGear.BodyPart);
                    string partName = partDef != null && !string.IsNullOrEmpty(partDef.DisplayName)
                        ? partDef.DisplayName
                        : partGear.BodyPart.ToString();
                    lines.AppendLine($"部位: {partName}");
                }

                if (!string.IsNullOrEmpty(partGear.Desc))
                {
                    lines.AppendLine(partGear.Desc);
                }

                if (partGear.MinPartLevel > 0)
                {
                    lines.AppendLine($"需要部位等级: {partGear.MinPartLevel}");
                }

                if (partGear.LocalBonuses != null)
                {
                    for (int i = 0; i < partGear.LocalBonuses.Count; i++)
                    {
                        var bonus = partGear.LocalBonuses[i];
                        if (bonus == null)
                        {
                            continue;
                        }

                        string name = BodyPartCatalog.GetLocalAttrDisplayName(bonus.AttrId);
                        lines.AppendLine($"加成: {name} +{bonus.Val}");
                    }
                }
            }

            return lines.ToString().TrimEnd();
        }
    }
}
