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
        public const string FallbackFulfillSuccess = "npc_quest_fulfill_ok";
        public const string FallbackFulfillFail = "npc_quest_fulfill_fail";

        public static bool IsHubRole(EQuestDialogRole role)
        {
            return role == EQuestDialogRole.Accept
                || role == EQuestDialogRole.Fulfill
                || role == EQuestDialogRole.Remind;
        }

        public static string Resolve(int questId, string objId, string characterKey, EQuestDialogRole role)
        {
            foreach (var row in CfgMgr.Cfgs.TbQuestInteractDialog.DataList)
            {
                if (row == null || row.DialogRole != role)
                {
                    continue;
                }

                if (row.QuestId != questId)
                {
                    continue;
                }

                if (!string.Equals(row.CharacterKey, characterKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (RequiresObjId(role) && !string.Equals(row.ObjId ?? "", objId ?? "", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(row.DialogId))
                {
                    return row.DialogId;
                }
            }

            return GetFallback(role);
        }

        public static List<QuestInteractDialogData> ListByCharacterKey(string characterKey)
        {
            var result = new List<QuestInteractDialogData>();
            if (string.IsNullOrEmpty(characterKey))
            {
                return result;
            }

            foreach (var row in CfgMgr.Cfgs.TbQuestInteractDialog.DataList)
            {
                if (row == null)
                {
                    continue;
                }

                if (!string.Equals(row.CharacterKey, characterKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsHubRole(row.DialogRole))
                {
                    continue;
                }

                result.Add(row);
            }

            return result;
        }

        public static string GetFallback(EQuestDialogRole role)
        {
            return role switch
            {
                EQuestDialogRole.AcceptSuccess => FallbackAcceptSuccess,
                EQuestDialogRole.AcceptFail => FallbackAcceptFail,
                EQuestDialogRole.FulfillSuccess => FallbackFulfillSuccess,
                EQuestDialogRole.FulfillFail => FallbackFulfillFail,
                _ => "",
            };
        }

        private static bool RequiresObjId(EQuestDialogRole role)
        {
            return role == EQuestDialogRole.Fulfill
                || role == EQuestDialogRole.FulfillSuccess
                || role == EQuestDialogRole.FulfillFail;
        }
    }
}
