using System.Collections.Generic;
using System.Text;
using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.Player;

namespace My.UI.SkillLoadout
{
    public static class SkillLearnEntryTextUtil
    {
        public static string ResolveDisplayName(SkillLearnEntry entry, EntitySkillData skillCfg)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.DisplayName))
            {
                return entry.DisplayName;
            }

            if (skillCfg != null && !string.IsNullOrEmpty(skillCfg.Desc))
            {
                return skillCfg.Desc;
            }

            return entry?.SkillId ?? string.Empty;
        }

        public static string BuildLearnCostLine(IReadOnlyList<CommonCheckCond> conds)
        {
            if (conds == null || conds.Count == 0)
            {
                return "学习消耗：无";
            }

            var lines = new StringBuilder();
            lines.Append("学习消耗：");
            bool hasCost = false;

            for (var i = 0; i < conds.Count; i++)
            {
                var cond = conds[i];
                if (cond == null || cond.Type != ECommonCheckType.OwnItem)
                {
                    continue;
                }

                if (hasCost)
                {
                    lines.Append("，");
                }

                hasCost = true;
                lines.Append($"{ResolveItemName(cond.Param5)} x{cond.Param1}");
            }

            if (!hasCost)
            {
                lines.Append("无");
            }

            return lines.ToString();
        }

        public static string BuildDetailStatusLine(SkillLearnEntry entry, bool isLearned)
        {
            if (isLearned)
            {
                return "已学习，可拖拽到技能栏装备";
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr == null)
            {
                return "无法学习";
            }

            if (mgr.CanLearnSkillFromEntry(entry.EntryId, out _))
            {
                return "可学习，点击下方学习按钮确认";
            }

            string condHint = BuildFirstLearnCondHint(entry.LearnConds);
            return string.IsNullOrEmpty(condHint)
                ? "学习条件未满足"
                : $"学习条件未满足：{condHint}";
        }

        public static string BuildFirstLearnCondHint(IReadOnlyList<CommonCheckCond> conds)
        {
            if (conds == null)
            {
                return string.Empty;
            }

            for (var i = 0; i < conds.Count; i++)
            {
                var cond = conds[i];
                if (cond == null || cond.Type == ECommonCheckType.None)
                {
                    continue;
                }

                return BuildCondDetail(cond);
            }

            return string.Empty;
        }

        static string BuildCondDetail(CommonCheckCond cond)
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
                ECommonCheckType.AlwaysFail => "条件未满足",
                _ => "解锁条件未满足",
            };
        }

        static string ResolveItemName(string itemId)
        {
            var def = ItemCatalog.GetItemDef(itemId);
            if (def != null && !string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return string.IsNullOrEmpty(itemId) ? "道具" : itemId;
        }
    }
}
