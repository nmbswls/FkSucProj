using System;
using System.Collections.Generic;
using System.Text;
using cfg.demo;
using My.Config;
using My.Home;
using My.Map;

namespace My.UI.Home
{
    // 城镇设施详情面板：配表文案与富文本格式化
    public static class TownFacilityDetailUiFormatter
    {
        const string ColorMuted = "#9AA8B8";
        const string ColorGood = "#8FD49A";
        const string ColorBad = "#E88888";
        const string ColorAccent = "#7EB6DA";

        public static string ResolveItemName(string itemId)
        {
            var def = ItemCatalog.GetItemDef(itemId);
            if (def != null && !string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return string.IsNullOrEmpty(itemId) ? "道具" : itemId;
        }

        public static string FormatOutputItems(IReadOnlyList<TalentUnlockCost> outputs)
        {
            return FormatDailyOutputs(outputs);
        }

        public static string FormatOutputInterval(int intervalDays)
        {
            int days = Math.Max(1, intervalDays);
            return days <= 1 ? "每 1 天" : $"每 {days} 天";
        }

        public static string FormatDailyOutputs(IReadOnlyList<TalentUnlockCost> outputs)
        {
            if (outputs == null || outputs.Count == 0)
            {
                return $"<color={ColorMuted}>暂无日产出</color>";
            }

            var parts = new List<string>();
            foreach (var output in outputs)
            {
                if (output == null || string.IsNullOrEmpty(output.ItemId) || output.Count <= 0)
                {
                    continue;
                }

                parts.Add($"{ResolveItemName(output.ItemId)} <color={ColorGood}>+{output.Count}</color>");
            }

            return parts.Count == 0
                ? $"<color={ColorMuted}>暂无日产出</color>"
                : string.Join("    ", parts);
        }

        public static string FormatUpgradeCosts(
            IReadOnlyList<TalentUnlockCost> costs,
            Player.PlayerSystemManager playerDataManager)
        {
            if (costs == null || costs.Count == 0)
            {
                return $"<color={ColorMuted}>无消耗</color>";
            }

            var lines = new StringBuilder();
            foreach (var cost in costs)
            {
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                long owned = playerDataManager?.InventorySystem?.MainBag?.GetItemCount(cost.ItemId) ?? 0;
                bool enough = owned >= cost.Count;
                string amountColor = enough ? ColorGood : ColorBad;
                string itemName = ResolveItemName(cost.ItemId);
                lines.Append("• ");
                lines.Append(itemName);
                lines.Append("  <color=");
                lines.Append(amountColor);
                lines.Append('>');
                lines.Append(owned);
                lines.Append('/');
                lines.Append(cost.Count);
                lines.Append("</color>\n");
            }

            var text = lines.ToString().TrimEnd();
            return string.IsNullOrEmpty(text) ? $"<color={ColorMuted}>无消耗</color>" : text;
        }

        public static string FormatUnlockConds(IReadOnlyList<CommonCheckCond> conds, GameLogicManager glm)
        {
            if (conds == null || conds.Count == 0)
            {
                return string.Empty;
            }

            var lines = new List<string>();
            foreach (var cond in conds)
            {
                if (cond == null)
                {
                    continue;
                }

                bool met = glm == null || glm.CheckCommonCond(cond);
                string detail = FormatCondDetail(cond);
                if (string.IsNullOrEmpty(detail))
                {
                    continue;
                }

                string color = met ? ColorGood : ColorBad;
                lines.Add($"<color={color}>• {detail}</color>");
            }

            return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
        }

        public static string ResolveFailReason(string failReason)
        {
            return failReason switch
            {
                "area_not_controlled" => "尚未控制该区域，无法升级",
                "no_facility_development_cfg" => "缺少设施升级配置",
                "max_level" => "设施已满级",
                "no_upgrade_cfg" => "缺少下一级配置",
                "unlock_cond_fail" => "解锁条件未满足",
                "not_enough_item" => "升级材料不足",
                "invalid_site" => "设施站点无效",
                "site_area_mismatch" => "设施与当前区域不匹配",
                _ => "暂时无法升级",
            };
        }

        public static string BuildUpgradeHint(
            bool canUpgrade,
            string failReason,
            FacilityDevelopmentLevel nextLevelDef,
            GameLogicManager glm)
        {
            if (canUpgrade)
            {
                return $"<color={ColorAccent}>满足升级条件，点击下方按钮确认</color>";
            }

            if (nextLevelDef == null)
            {
                return $"<color={ColorMuted}>已达最高等级</color>";
            }

            if (failReason == "unlock_cond_fail")
            {
                string condText = FormatUnlockConds(nextLevelDef.UnlockConds, glm);
                if (!string.IsNullOrEmpty(condText))
                {
                    return condText;
                }
            }

            if (failReason == "not_enough_item" && nextLevelDef.UnlockCosts != null)
            {
                return $"<color={ColorBad}>材料不足，请凑齐消耗后再试</color>";
            }

            return $"<color={ColorBad}>{ResolveFailReason(failReason)}</color>";
        }

        static string FormatCondDetail(CommonCheckCond cond)
        {
            return cond.Type switch
            {
                ECommonCheckType.OwnItem => $"持有 {ResolveItemName(cond.Param5)} x{cond.Param1}",
                ECommonCheckType.TaskFinish => $"完成任务 {cond.Param1}",
                ECommonCheckType.TaskStep => $"任务步骤 {cond.Param1}",
                ECommonCheckType.CheckVariable => string.IsNullOrEmpty(cond.Param5)
                    ? "满足剧情条件"
                    : $"满足条件 {cond.Param5}",
                ECommonCheckType.FuncOpen => $"解锁功能 {(EFuncOpenType)cond.Param1}",
                ECommonCheckType.CharacterFavorLevel => $"{ResolveCharacterName(cond.Param5)} 好感达到 Lv.{cond.Param1}",
                ECommonCheckType.CultTechNodeLevel => $"教团科技节点 {cond.Param1} 达到 Lv{(cond.Param2 > 0 ? cond.Param2 : 1)}",
                ECommonCheckType.CultAttributeAtLeast => $"教团属性 {cond.Param5} ≥ {cond.Param1}",
                ECommonCheckType.StatAtLeast => $"统计 {cond.Param5}{(string.IsNullOrEmpty(cond.Param6) ? string.Empty : $"/{cond.Param6}")} ≥ {cond.Param1}",
                ECommonCheckType.AlwaysFail => "条件未满足",
                _ => "解锁条件未满足",
            };
        }

        static string ResolveCharacterName(string characterKey)
        {
            var row = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(characterKey);
            return row != null && !string.IsNullOrEmpty(row.Name) ? row.Name : characterKey;
        }
    }
}
