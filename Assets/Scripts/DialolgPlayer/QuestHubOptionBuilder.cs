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

    public static class QuestHubOptionBuilder
    {
        private const int SortRemind = 0;
        private const int SortAccept = 1;
        private const int SortFulfill = 2;

        public static List<QuestHubOption> Build(string characterKey, PlayerQuestSystem questSystem, My.GameLogicManager glm)
        {
            var result = new List<QuestHubOption>();
            if (string.IsNullOrEmpty(characterKey) || questSystem == null || glm == null)
            {
                return result;
            }

            foreach (var row in CfgMgr.Cfgs.TbQuestInteractDialog.DataList)
            {
                if (row == null || !QuestDialogResolver.IsHubRole(row.DialogRole))
                {
                    continue;
                }

                if (!string.Equals(row.CharacterKey, characterKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!PassesShowCond(row, glm))
                {
                    continue;
                }

                switch (row.DialogRole)
                {
                    case EQuestDialogRole.Remind:
                        if (TryBuildRemind(row, questSystem, out var remindOpt))
                        {
                            result.Add(remindOpt);
                        }
                        break;
                    case EQuestDialogRole.Accept:
                        if (TryBuildAccept(row, questSystem, out var acceptOpt))
                        {
                            result.Add(acceptOpt);
                        }
                        break;
                    case EQuestDialogRole.Fulfill:
                        if (TryBuildFulfill(row, questSystem, out var fulfillOpt))
                        {
                            result.Add(fulfillOpt);
                        }
                        break;
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

        private static bool PassesShowCond(QuestInteractDialogData row, My.GameLogicManager glm)
        {
            if (row.ShowCond == null || row.ShowCond.Count == 0)
            {
                return true;
            }

            foreach (var cond in row.ShowCond)
            {
                if (cond == null)
                {
                    continue;
                }

                if (!glm.CheckCommonCond(cond, GamePlayerIds.Local))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryBuildRemind(QuestInteractDialogData row, PlayerQuestSystem questSystem, out QuestHubOption option)
        {
            option = default;
            if (!questSystem.CheckQuestRunning(row.QuestId))
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

            option = new QuestHubOption
            {
                OptionText = ResolveOptionText(row, "我还该做什么？"),
                EntryDialogId = row.DialogId,
                Session = QuestDialogSession.CreateRemind(row.QuestId, row.CharacterKey, row.DialogId),
                SortOrder = SortRemind,
                Priority = row.Priority,
            };
            return true;
        }

        private static bool TryBuildAccept(QuestInteractDialogData row, PlayerQuestSystem questSystem, out QuestHubOption option)
        {
            option = default;
            var questCfg = CfgMgr.Cfgs.TbQuestData.GetOrDefault(row.QuestId);
            if (questCfg == null || questCfg.IsAutoAccept)
            {
                return false;
            }

            if (questSystem.CheckQuestRunning(row.QuestId) || questSystem.CheckQuestFinish(row.QuestId))
            {
                return false;
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
                OptionText = ResolveOptionText(row, $"关于{questCfg.Name}……"),
                EntryDialogId = row.DialogId,
                Session = QuestDialogSession.CreateAccept(row.QuestId, row.CharacterKey, row.DialogId),
                SortOrder = SortAccept,
                Priority = row.Priority,
            };
            return true;
        }

        private static bool TryBuildFulfill(QuestInteractDialogData row, PlayerQuestSystem questSystem, out QuestHubOption option)
        {
            option = default;
            if (string.IsNullOrEmpty(row.ObjId))
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

            if (!step.objectiveMap.TryGetValue(row.ObjId, out var objRuntime))
            {
                return false;
            }

            if (!QuestObjectiveFulfillUtil.SupportsDialogFulfill(objRuntime.Data.ObjType))
            {
                return false;
            }

            if (objRuntime.GetCurrProgress() >= objRuntime.GetRequireProgress())
            {
                return false;
            }

            option = new QuestHubOption
            {
                OptionText = ResolveOptionText(row, QuestObjectiveFulfillUtil.GetFulfillOptionFallbackText(objRuntime.Data)),
                EntryDialogId = row.DialogId,
                Session = QuestDialogSession.CreateFulfill(row.QuestId, row.ObjId, row.CharacterKey, row.DialogId),
                SortOrder = SortFulfill,
                Priority = row.Priority,
            };
            return true;
        }

        private static string ResolveOptionText(QuestInteractDialogData row, string fallback)
        {
            if (!string.IsNullOrEmpty(row.OptionText))
            {
                return row.OptionText;
            }

            return fallback;
        }
    }
}
