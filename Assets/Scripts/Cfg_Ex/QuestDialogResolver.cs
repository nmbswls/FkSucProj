using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;

namespace My.Cfg_Ex
{
    public static class QuestDialogResolver
    {
        public const string FallbackAcceptSuccess = "npc_quest_accept_ok";
        public const string FallbackAcceptFail = "npc_quest_accept_fail";
        public const string FallbackObjectiveSuccess = "npc_quest_fulfill_ok";
        public const string FallbackObjectiveFail = "npc_quest_fulfill_fail";

        public static QuestAcceptDialog FindAccept(int questId, string characterKey)
        {
            if (string.IsNullOrEmpty(characterKey) || CfgMgr.Cfgs?.TbQuestAcceptDialog == null)
            {
                return null;
            }

            foreach (var row in CfgMgr.Cfgs.TbQuestAcceptDialog.DataList)
            {
                if (row == null || row.QuestId != questId)
                {
                    continue;
                }

                if (string.Equals(row.CharacterKey, characterKey, StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }

        public static QuestAcceptDialog FindAcceptById(int id)
        {
            return id > 0 ? CfgMgr.Cfgs?.TbQuestAcceptDialog?.GetOrDefault(id) : null;
        }

        public static QuestObjectiveDialog FindObjective(int questId, string objId, string characterKey)
        {
            if (string.IsNullOrEmpty(characterKey) || string.IsNullOrEmpty(objId)
                || CfgMgr.Cfgs?.TbQuestObjectiveDialog == null)
            {
                return null;
            }

            foreach (var row in CfgMgr.Cfgs.TbQuestObjectiveDialog.DataList)
            {
                if (row == null || row.QuestId != questId)
                {
                    continue;
                }

                if (!string.Equals(row.ObjId, objId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(row.CharacterKey, characterKey, StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }

        public static QuestObjectiveDialog FindObjectiveById(int id)
        {
            return id > 0 ? CfgMgr.Cfgs?.TbQuestObjectiveDialog?.GetOrDefault(id) : null;
        }

        public static string ResolveAcceptResult(int acceptDialogId, int questId, string characterKey, bool ok)
        {
            var row = FindAcceptById(acceptDialogId) ?? FindAccept(questId, characterKey);
            if (row != null)
            {
                // 行存在但结果对话为空：不播通用 fallback，直接回 Hub
                return ok ? row.SuccessDialogId : row.FailDialogId;
            }

            return ok ? FallbackAcceptSuccess : FallbackAcceptFail;
        }

        public static string ResolveObjectiveResult(int objectiveDialogId, int questId, string objId, string characterKey, bool ok)
        {
            var row = FindObjectiveById(objectiveDialogId) ?? FindObjective(questId, objId, characterKey);
            if (row != null)
            {
                // 行存在但结果对话为空：不播通用 fallback（家园直兑 Talk 等）
                return ok ? row.SuccessDialogId : row.FailDialogId;
            }

            return ok ? FallbackObjectiveSuccess : FallbackObjectiveFail;
        }

        public static List<QuestAcceptDialog> ListAcceptByCharacter(string characterKey)
        {
            var result = new List<QuestAcceptDialog>();
            AppendByCharacter(CfgMgr.Cfgs?.TbQuestAcceptDialog?.DataList, characterKey, result);
            return result;
        }

        public static List<QuestRemindDialog> ListRemindByCharacter(string characterKey)
        {
            var result = new List<QuestRemindDialog>();
            AppendByCharacter(CfgMgr.Cfgs?.TbQuestRemindDialog?.DataList, characterKey, result);
            return result;
        }

        public static List<QuestObjectiveDialog> ListObjectiveByCharacter(string characterKey)
        {
            var result = new List<QuestObjectiveDialog>();
            AppendByCharacter(CfgMgr.Cfgs?.TbQuestObjectiveDialog?.DataList, characterKey, result);
            return result;
        }

        static void AppendByCharacter<T>(IReadOnlyList<T> list, string characterKey, List<T> result)
            where T : class
        {
            if (list == null || string.IsNullOrEmpty(characterKey) || result == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null)
                {
                    continue;
                }

                string key = null;
                if (row is QuestAcceptDialog a)
                {
                    key = a.CharacterKey;
                }
                else if (row is QuestRemindDialog r)
                {
                    key = r.CharacterKey;
                }
                else if (row is QuestObjectiveDialog o)
                {
                    key = o.CharacterKey;
                }

                if (string.Equals(key, characterKey, StringComparison.Ordinal))
                {
                    result.Add(row);
                }
            }
        }
    }
}
