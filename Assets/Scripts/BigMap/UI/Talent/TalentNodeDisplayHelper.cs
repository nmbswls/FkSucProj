using System.Collections.Generic;
using System.Text;
using cfg.demo;
using My;
using My.Config;
using My.Map.Entity;
using My.Player;
using UnityEngine;

namespace My.UI.Talent
{
    public static class TalentNodeDisplayHelper
    {
        const int HoverSummaryMaxChars = 40;

        public struct NodeSnapshot
        {
            public int NodeId;
            public int CurrentLevel;
            public int MaxLevel;
            public int NextLevel;
            public bool IsMaxed;
            public PlayerTalentManager.TalentNodeVisualState State;
            public TalentNode NodeCfg;
            public TalentNodeLevel CurrentLevelRow;
            public TalentNodeLevel NextLevelRow;
        }

        public static bool TryBuildSnapshot(
            int nodeId,
            ITalentProgressionContext progression,
            out NodeSnapshot snapshot)
        {
            snapshot = default;
            if (nodeId <= 0)
            {
                return false;
            }

            var nodeCfg = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(nodeId);
            if (nodeCfg == null)
            {
                return false;
            }

            int current = progression?.GetTalentNodeLevel(nodeId) ?? 0;
            int max = nodeCfg.MaxLevel;
            bool isMaxed = current >= max;
            int next = isMaxed ? 0 : current + 1;

            snapshot = new NodeSnapshot
            {
                NodeId = nodeId,
                CurrentLevel = current,
                MaxLevel = max,
                NextLevel = next,
                IsMaxed = isMaxed,
                State = progression?.GetTalentNodeVisualState(nodeId)
                          ?? PlayerTalentManager.TalentNodeVisualState.Locked,
                NodeCfg = nodeCfg,
                CurrentLevelRow = current > 0
                    ? CfgMgr.Cfgs?.TbTalentNodeLevel?.Get(nodeId, current)
                    : null,
                NextLevelRow = next > 0
                    ? CfgMgr.Cfgs?.TbTalentNodeLevel?.Get(nodeId, next)
                    : null,
            };

            return true;
        }

        public static string GetDisplayName(in NodeSnapshot snapshot)
        {
            if (snapshot.NodeCfg != null && !string.IsNullOrEmpty(snapshot.NodeCfg.DisplayName))
            {
                return snapshot.NodeCfg.DisplayName;
            }

            return $"Node {snapshot.NodeId}";
        }

        public static string BuildHoverSummary(in NodeSnapshot snapshot)
        {
            if (snapshot.IsMaxed)
            {
                return BuildLevelEffectSummary(snapshot.CurrentLevelRow, true);
            }

            return BuildLevelEffectSummary(snapshot.NextLevelRow, true);
        }

        public static string BuildHoverStateLabel(in NodeSnapshot snapshot)
        {
            if (snapshot.IsMaxed)
            {
                return "已满级";
            }

            return snapshot.State switch
            {
                PlayerTalentManager.TalentNodeVisualState.Unlocked => "已解锁",
                PlayerTalentManager.TalentNodeVisualState.Unlockable => snapshot.CurrentLevel <= 0 ? "可解锁" : "可升级",
                _ => "未解锁",
            };
        }

        public static string BuildHoverHint(in NodeSnapshot snapshot, ITalentProgressionContext progression)
        {
            if (snapshot.IsMaxed)
            {
                return "已生效";
            }

            if (snapshot.State == PlayerTalentManager.TalentNodeVisualState.Unlockable)
            {
                return BuildPrimaryCostHint(snapshot.NextLevelRow, progression, true);
            }

            return BuildPrimaryBlockerHint(snapshot, progression);
        }

        public static string BuildDetailBody(in NodeSnapshot snapshot, ITalentProgressionContext progression)
        {
            var lines = new StringBuilder();

            if (!string.IsNullOrEmpty(snapshot.NodeCfg?.Description))
            {
                lines.AppendLine(snapshot.NodeCfg.Description);
                lines.AppendLine();
            }

            if (snapshot.CurrentLevel > 0 && snapshot.CurrentLevelRow != null)
            {
                lines.AppendLine("当前效果");
                lines.AppendLine(BuildLevelEffectDetail(snapshot.CurrentLevelRow));
                lines.AppendLine();
            }

            if (!snapshot.IsMaxed && snapshot.NextLevelRow != null)
            {
                lines.AppendLine(snapshot.CurrentLevel <= 0 ? "解锁效果" : "下一级效果");
                lines.AppendLine(BuildLevelEffectDetail(snapshot.NextLevelRow));
                lines.AppendLine();
            }

            AppendPrerequisiteSection(lines, snapshot, progression);
            AppendConditionSection(lines, snapshot);
            AppendCostSection(lines, snapshot, progression);

            return lines.ToString().TrimEnd();
        }

        public static string BuildDetailStatusHint(in NodeSnapshot snapshot, ITalentProgressionContext progression)
        {
            if (snapshot.IsMaxed)
            {
                return "已满级";
            }

            return snapshot.State switch
            {
                PlayerTalentManager.TalentNodeVisualState.Unlocked => "已解锁，满足条件后可继续升级",
                PlayerTalentManager.TalentNodeVisualState.Unlockable => "条件已满足，点击节点下方按钮解锁或升级",
                _ => BuildPrimaryBlockerHint(snapshot, progression),
            };
        }

        static string BuildPrimaryBlockerHint(in NodeSnapshot snapshot, ITalentProgressionContext progression)
        {
            if (snapshot.IsMaxed || snapshot.NextLevelRow == null)
            {
                return string.Empty;
            }

            if (snapshot.NextLevelRow.PrereqNodeIds != null)
            {
                for (int i = 0; i < snapshot.NextLevelRow.PrereqNodeIds.Count; i++)
                {
                    int preId = snapshot.NextLevelRow.PrereqNodeIds[i];
                    if (preId <= 0)
                    {
                        continue;
                    }

                    if ((progression?.GetTalentNodeLevel(preId) ?? 0) < 1)
                    {
                        return $"需：{ResolveNodeName(preId)}";
                    }
                }
            }

            var glm = ResolveLogic();
            if (snapshot.NextLevelRow.UnlockConds != null
                && snapshot.NextLevelRow.UnlockConds.Count > 0
                && glm != null
                && !glm.CheckCommonCondsAll(snapshot.NextLevelRow.UnlockConds))
            {
                string cond = BuildFirstCondHint(snapshot.NextLevelRow.UnlockConds);
                return string.IsNullOrEmpty(cond) ? "解锁条件未满足" : cond;
            }

            string costHint = BuildPrimaryCostHint(snapshot.NextLevelRow, progression, true);
            if (!string.IsNullOrEmpty(costHint))
            {
                return costHint;
            }

            return "暂不可解锁";
        }

        static string BuildPrimaryCostHint(TalentNodeLevel levelRow, ITalentProgressionContext progression, bool compact)
        {
            if (levelRow?.UnlockCosts == null || levelRow.UnlockCosts.Count == 0)
            {
                return string.Empty;
            }

            var pdm = ResolvePlayerManager();
            var parts = new List<string>();
            for (int i = 0; i < levelRow.UnlockCosts.Count; i++)
            {
                var cost = levelRow.UnlockCosts[i];
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                string itemName = ResolveItemName(cost.ItemId);
                if (compact)
                {
                    parts.Add($"{itemName} x{cost.Count}");
                    continue;
                }

                long owned = pdm?.InventorySystem?.MainBag?.GetItemCount(cost.ItemId) ?? 0;
                parts.Add($"{itemName} x{cost.Count}（持有 {owned}）");
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return compact ? $"消耗：{string.Join(" / ", parts)}" : string.Join("\n", parts);
        }

        static void AppendPrerequisiteSection(StringBuilder lines, in NodeSnapshot snapshot, ITalentProgressionContext progression)
        {
            if (snapshot.IsMaxed || snapshot.NextLevelRow?.PrereqNodeIds == null || snapshot.NextLevelRow.PrereqNodeIds.Count == 0)
            {
                return;
            }

            lines.AppendLine("前置");
            for (int i = 0; i < snapshot.NextLevelRow.PrereqNodeIds.Count; i++)
            {
                int preId = snapshot.NextLevelRow.PrereqNodeIds[i];
                if (preId <= 0)
                {
                    continue;
                }

                bool met = (progression?.GetTalentNodeLevel(preId) ?? 0) >= 1;
                lines.AppendLine($"- {ResolveNodeName(preId)} {(met ? "（已满足）" : "（未满足）")}");
            }

            lines.AppendLine();
        }

        static void AppendConditionSection(StringBuilder lines, in NodeSnapshot snapshot)
        {
            if (snapshot.IsMaxed || snapshot.NextLevelRow?.UnlockConds == null || snapshot.NextLevelRow.UnlockConds.Count == 0)
            {
                return;
            }

            var glm = ResolveLogic();
            lines.AppendLine("解锁条件");
            for (int i = 0; i < snapshot.NextLevelRow.UnlockConds.Count; i++)
            {
                var cond = snapshot.NextLevelRow.UnlockConds[i];
                if (cond == null || cond.Type == ECommonCheckType.None)
                {
                    continue;
                }

                bool met = glm != null && glm.CheckCommonCond(cond);
                lines.AppendLine($"- {BuildCondDetail(cond)} {(met ? "（已满足）" : "（未满足）")}");
            }

            lines.AppendLine();
        }

        static void AppendCostSection(StringBuilder lines, in NodeSnapshot snapshot, ITalentProgressionContext progression)
        {
            if (snapshot.IsMaxed)
            {
                return;
            }

            string costText = BuildPrimaryCostHint(snapshot.NextLevelRow, progression, false);
            if (string.IsNullOrEmpty(costText))
            {
                return;
            }

            lines.AppendLine("解锁消耗");
            lines.AppendLine(costText);
        }

        static string BuildLevelEffectSummary(TalentNodeLevel levelRow, bool truncate)
        {
            if (levelRow == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(levelRow.PassiveSkillId))
            {
                string passive = ResolvePassiveSummary(levelRow.PassiveSkillId);
                if (!string.IsNullOrEmpty(passive))
                {
                    return truncate ? Truncate(passive, HoverSummaryMaxChars) : passive;
                }
            }

            if (levelRow.StatBonuses != null && levelRow.StatBonuses.Count > 0 && levelRow.StatBonuses[0] != null)
            {
                string stat = FormatStatBonus(levelRow.StatBonuses[0]);
                return truncate ? Truncate(stat, HoverSummaryMaxChars) : stat;
            }

            return string.Empty;
        }

        static string BuildLevelEffectDetail(TalentNodeLevel levelRow)
        {
            if (levelRow == null)
            {
                return string.Empty;
            }

            var lines = new StringBuilder();
            if (levelRow.StatBonuses != null)
            {
                for (int i = 0; i < levelRow.StatBonuses.Count; i++)
                {
                    var bonus = levelRow.StatBonuses[i];
                    if (bonus == null)
                    {
                        continue;
                    }

                    lines.AppendLine($"- {FormatStatBonus(bonus)}");
                }
            }

            if (!string.IsNullOrEmpty(levelRow.PassiveSkillId))
            {
                string passive = ResolvePassiveSummary(levelRow.PassiveSkillId);
                if (!string.IsNullOrEmpty(passive))
                {
                    lines.AppendLine($"- 被动：{passive}");
                }
            }

            string text = lines.ToString().TrimEnd();
            return string.IsNullOrEmpty(text) ? "无额外效果" : text;
        }

        static string FormatStatBonus(TalentStatBonus bonus)
        {
            if (bonus == null)
            {
                return string.Empty;
            }

            if (bonus.HumanAttrId != EHumanCivilizationAttribute.None)
            {
                return $"人类文明属性 {bonus.HumanAttrId} +{bonus.Val}%";
            }

            switch ((EYCAttribute)bonus.AttrId)
            {
                case EYCAttribute.MainBagSlots:
                    return $"主背包槽位 +{bonus.Val}";
                case EYCAttribute.BigBagSlots:
                    return $"大件背包槽位 +{bonus.Val}";
                case EYCAttribute.CarryWeightBase:
                    return $"基础负重 +{bonus.Val}";
                case EYCAttribute.CarryWeightExtraFlat:
                    return $"额外负重 +{bonus.Val}";
                case EYCAttribute.CarryWeightExtraPercent:
                    return $"额外负重 +{bonus.Val / 100f:0.#}%";
                case EYCAttribute.MainBagWeightRatio:
                case EYCAttribute.SecretBagWeightRatio:
                case EYCAttribute.PlantBagWeightRatio:
                case EYCAttribute.KeyBagWeightRatio:
                case EYCAttribute.PotionBagWeightRatio:
                case EYCAttribute.BigBagWeightRatio:
                case EYCAttribute.MindBagWeightRatio:
                case EYCAttribute.ImportantBagWeightRatio:
                    return $"负重折算 {bonus.AttrId} {(bonus.Val >= 0 ? "+" : "")}{bonus.Val / 100f:0.#}%";
                case EYCAttribute.PhysicalPower:
                    return $"肉体强度 +{bonus.Val / 1000f:0.#}";
                case EYCAttribute.PhysicalResist:
                    return $"肉体耐受 +{bonus.Val / 1000f:0.#}";
            }

            if (Mathf.Abs(bonus.Val) >= 1000)
            {
                return $"属性{bonus.AttrId} +{bonus.Val / 100f:0.#}%";
            }

            return $"属性{bonus.AttrId} +{bonus.Val}";
        }

        static string ResolvePassiveSummary(string skillId)
        {
            var skill = SkillLibrary.GetSkillConfig(skillId);
            if (skill != null && !string.IsNullOrEmpty(skill.Desc))
            {
                return skill.Desc.Trim();
            }

            return skillId ?? string.Empty;
        }

        static string BuildFirstCondHint(IReadOnlyList<CommonCheckCond> conds)
        {
            if (conds == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < conds.Count; i++)
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
            if (cond == null)
            {
                return string.Empty;
            }

            return cond.Type switch
            {
                ECommonCheckType.OwnItem => $"持有 {ResolveItemName(cond.Param5)} x{cond.Param1}",
                ECommonCheckType.TaskFinish => $"完成任务 {cond.Param1}",
                ECommonCheckType.TaskStep => $"任务步骤 {cond.Param1}",
                ECommonCheckType.CheckVariable => string.IsNullOrEmpty(cond.Param5) ? "满足剧情条件" : $"满足条件 {cond.Param5}",
                ECommonCheckType.FuncOpen => $"解锁功能 {(EFuncOpenType)cond.Param1}",
                ECommonCheckType.CharacterFavorLevel => $"{ResolveCharacterName(cond.Param5)}好感达到 Lv{cond.Param1}",
                ECommonCheckType.AlwaysFail => "条件未满足",
                _ => "解锁条件未满足",
            };
        }

        static string ResolveNodeName(int nodeId)
        {
            var row = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(nodeId);
            if (row != null && !string.IsNullOrEmpty(row.DisplayName))
            {
                return row.DisplayName;
            }

            return $"节点 {nodeId}";
        }

        static string ResolveCharacterName(string characterKey)
        {
            var row = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(characterKey);
            return row != null && !string.IsNullOrEmpty(row.Name) ? row.Name : characterKey;
        }

        static string ResolveItemName(string itemId)
        {
            var def = ItemCatalog.GetItemDef(itemId);
            if (def != null && !string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return itemId ?? string.Empty;
        }

        static string Truncate(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            {
                return text ?? string.Empty;
            }

            return text.Substring(0, maxChars) + "...";
        }

        static GameLogicManager ResolveLogic()
        {
            return MainGameManager.Instance?.gameLogicManager;
        }

        static PlayerSystemManager ResolvePlayerManager()
        {
            return MainGameManager.Instance?.gameLogicManager?.playerDataManager;
        }
    }
}
