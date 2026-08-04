using cfg.demo;
using My.Config;
using My.Player;
using My;

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

                if (partGear.BaseArm != 0)
                {
                    lines.AppendLine($"基础护甲: +{partGear.BaseArm}");
                }

                if (partGear.BaseHpMax != 0)
                {
                    lines.AppendLine($"生命上限: +{partGear.BaseHpMax}");
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

        public static string BuildDetailText(ItemStack stack)
        {
            if (stack == null || stack.IsEmpty) return string.Empty;
            var text = BuildDetailText(stack.ItemID, stack.Count);
            var weapon = HumanWeaponCatalog.GetInstance(stack);
            if (weapon != null)
            {
                var lines = new System.Text.StringBuilder(text);
                lines.AppendLine();
                lines.AppendLine(weapon.IsIdentified ? "鉴定状态：已鉴定" : "鉴定状态：未鉴定");
                if (weapon.IsIdentified)
                {
                    foreach (var affix in HumanWeaponCatalog.GetAffixDisplayLines(stack))
                        lines.AppendLine("词条：" + affix);
                }
                else
                {
                    lines.AppendLine("词条：鉴定后揭示");
                }
                return lines.ToString().TrimEnd();
            }
            var armar = HumanArmarCatalog.GetInstance(stack);
            if (armar != null)
            {
                var lines = new System.Text.StringBuilder(text);
                if (!armar.IsIdentified)
                {
                    lines.AppendLine(BuildHumanArmarValuePreview(stack));
                }
                lines.AppendLine();
                lines.AppendLine(armar.IsIdentified ? "鉴定状态：已鉴定" : "鉴定状态：未鉴定");
                if (armar.IsIdentified)
                {
                    foreach (var affix in HumanArmarCatalog.GetAffixDisplayLines(stack))
                        lines.AppendLine("词条：" + affix);
                }
                else
                {
                    lines.AppendLine("词条：鉴定后揭示");
                }
                return lines.ToString().TrimEnd();
            }
            return text;
        }

        static string BuildHumanArmarValuePreview(ItemStack stack)
        {
            var progression = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.ProgressionSystem;
            long preview = progression?.GetFinalAttribute((int)EYCAttribute.HumanArmarValuePreview) ?? 0;
            if (preview <= 0) return "估值：需要学习模糊估值";
            long value = HumanArmarCatalog.GetPotentialMarketValue(stack);
            if (value <= 0) return "估值：暂时无法判断";
            long precision = progression?.GetFinalAttribute((int)EYCAttribute.HumanArmarValuePrecision) ?? 0;
            double error = precision > 0 ? .12 : .35;
            long min = System.Math.Max(0, (long)System.Math.Floor(value * (1d - error)));
            long max = (long)System.Math.Ceiling(value * (1d + error));
            return $"估值范围：{min} - {max}";
        }
    }
}
