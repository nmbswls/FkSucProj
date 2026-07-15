using System;
using System.Collections.Generic;
using cfg.demo;
using My.Cfg_Ex;
using My.Config;
using My.Dialog;
using My.Player;

namespace My.Dialog
{
    public struct QuestHubOption
    {
        public string OptionText;
        public string EntryDialogId;
        public DialogueSessionContext Session;
        public int SortOrder;
        public int Priority;
    }

    // Hub 选项源：Accept / Objective / Remind 三张表，按 character_key 过滤。
    public static class QuestHubOptionBuilder
    {
        private const int SortObjective = 0;
        private const int SortAccept = 1;
        private const int SortRemind = 2;

        public static List<QuestHubOption> Build(string characterKey, PlayerQuestSystem questSystem, My.GameLogicManager glm)
        {
            var result = new List<QuestHubOption>();
            if (string.IsNullOrEmpty(characterKey) || questSystem == null || glm == null)
            {
                return result;
            }

            foreach (var row in QuestDialogResolver.ListObjectiveByCharacter(characterKey))
            {
                if (TryBuildObjective(row, questSystem, out var opt))
                {
                    result.Add(opt);
                }
            }

            foreach (var row in QuestDialogResolver.ListAcceptByCharacter(characterKey))
            {
                if (TryBuildAccept(row, questSystem, glm, out var opt))
                {
                    result.Add(opt);
                }
            }

            foreach (var row in QuestDialogResolver.ListRemindByCharacter(characterKey))
            {
                if (TryBuildRemind(row, questSystem, out var opt))
                {
                    result.Add(opt);
                }
            }

            result.Sort(CompareOptions);
            return result;
        }

        private static int CompareOptions(QuestHubOption a, QuestHubOption b)
        {
            var order = a.SortOrder.CompareTo(b.SortOrder);
            if (order != 0)
            {
                return order;
            }

            return a.Priority.CompareTo(b.Priority);
        }

        private static bool TryBuildRemind(QuestRemindDialog row, PlayerQuestSystem questSystem, out QuestHubOption option)
        {
            option = default;
            if (row == null || !questSystem.CheckQuestRunning(row.QuestId))
            {
                return false;
            }

            // 空对话不进 Hub，避免点了无反应
            if (string.IsNullOrEmpty(row.DialogId))
            {
                return false;
            }

            var quest = questSystem.GetQuest(row.QuestId);
            if (quest?.ActiveStep == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(row.StepId))
            {
                return false;
            }

            if (!string.Equals(quest.ActiveStep.CacheStepCfg.StepId, row.StepId, StringComparison.Ordinal))
            {
                return false;
            }

            // 需先触发指定 ObjectiveDialog，才出现可重复的 Remind
            if (row.RequireObjectiveDialogId > 0
                && !quest.IsObjectiveDialogTriggered(row.RequireObjectiveDialogId))
            {
                return false;
            }

            if (!QuestDialogShowCondUtil.Passes(row.ShowCond, quest))
            {
                return false;
            }

            option = new QuestHubOption
            {
                OptionText = ResolveOptionText(row.OptionText, "我还该做什么？"),
                EntryDialogId = row.DialogId,
                Session = QuestDialogSession.CreateRemind(row.QuestId, row.CharacterKey, row.DialogId),
                SortOrder = SortRemind,
                Priority = row.Priority,
            };
            return true;
        }

        private static bool TryBuildAccept(
            QuestAcceptDialog row,
            PlayerQuestSystem questSystem,
            My.GameLogicManager glm,
            out QuestHubOption option)
        {
            option = default;
            if (row == null)
            {
                return false;
            }

            var questCfg = CfgMgr.Cfgs.TbQuestData.GetOrDefault(row.QuestId);
            if (questCfg == null || questCfg.IsAutoAccept)
            {
                return false;
            }

            if (questSystem.CheckQuestRunning(row.QuestId) || questSystem.CheckQuestFinish(row.QuestId))
            {
                return false;
            }

            if (row.OpenCond != null)
            {
                foreach (var cond in row.OpenCond)
                {
                    if (cond != null && !glm.CheckCommonCond(cond, GamePlayerIds.Local))
                    {
                        return false;
                    }
                }
            }

            foreach (var cond in questCfg.AcceeptCond)
            {
                if (!questSystem.Ctx.CheckCommonCond(cond))
                {
                    return false;
                }
            }

            option = new QuestHubOption
            {
                OptionText = ResolveOptionText(row.OptionText, $"关于{questCfg.Name}……"),
                EntryDialogId = row.EntryDialogId,
                Session = QuestDialogSession.CreateAccept(row.QuestId, row.CharacterKey, row.EntryDialogId, row.Id),
                SortOrder = SortAccept,
                Priority = row.Priority,
            };
            return true;
        }

        private static bool TryBuildObjective(QuestObjectiveDialog row, PlayerQuestSystem questSystem, out QuestHubOption option)
        {
            option = default;
            if (row == null || string.IsNullOrEmpty(row.ObjId))
            {
                return false;
            }

            if (!questSystem.CheckQuestRunning(row.QuestId))
            {
                return false;
            }

            var quest = questSystem.GetQuest(row.QuestId);
            var step = quest?.ActiveStep;
            if (step == null || step.IsCompleted)
            {
                return false;
            }

            if (!QuestDialogShowCondUtil.Passes(row.ShowCond, quest))
            {
                return false;
            }

            if (!step.objectiveMap.TryGetValue(row.ObjId, out var objRuntime))
            {
                return false;
            }

            // once：仅在兑现成功并 Mark 后隐藏（失败不 Mark，可重试）
            if (row.Once && quest.IsObjectiveDialogTriggered(row.Id))
            {
                return false;
            }

            if (!QuestObjectiveFulfillUtil.CanPresentDialogFulfill(objRuntime, questSystem))
            {
                return false;
            }

            option = new QuestHubOption
            {
                OptionText = ResolveOptionText(row.OptionText, QuestObjectiveFulfillUtil.GetFulfillOptionFallbackText(objRuntime.Data)),
                EntryDialogId = row.EntryDialogId,
                Session = QuestDialogSession.CreateObjective(
                    row.QuestId, row.ObjId, row.CharacterKey, row.EntryDialogId, row.Id),
                SortOrder = SortObjective,
                Priority = row.Priority,
            };
            return true;
        }

        private static string ResolveOptionText(string optionText, string fallback)
        {
            return !string.IsNullOrEmpty(optionText) ? optionText : fallback;
        }
    }
}
