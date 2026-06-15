using System.Collections.Generic;

namespace My.Dialog
{
    public enum EDialogueQuestAction
    {
        None = 0,
        Accept = 1,
        Fulfill = 2,
        Remind = 3,
    }

    // 任务对话 session 的写入与解析，逻辑不放在 DialogueSessionContext 上
    public static class QuestDialogSession
    {
        public const string ActionAccept = "accept";
        public const string ActionFulfill = "fulfill";
        public const string ActionRemind = "remind";

        public static DialogueSessionContext CreateAccept(int questId, string characterKey, string entryDialogId)
        {
            return Create(ActionAccept, questId, null, characterKey, entryDialogId);
        }

        public static DialogueSessionContext CreateFulfill(int questId, string objId, string characterKey, string entryDialogId)
        {
            return Create(ActionFulfill, questId, objId, characterKey, entryDialogId);
        }

        public static DialogueSessionContext CreateRemind(int questId, string characterKey, string entryDialogId)
        {
            return Create(ActionRemind, questId, null, characterKey, entryDialogId);
        }

        public static DialogueSessionContext Create(
            string action,
            int questId,
            string objId,
            string characterKey,
            string entryDialogId)
        {
            var vars = new Dictionary<string, string>();
            SetVar(vars, DialogueSessionKeys.QuestAction, action);
            SetVar(vars, DialogueSessionKeys.QuestId, questId.ToString());
            SetVar(vars, DialogueSessionKeys.ObjId, objId);
            SetVar(vars, DialogueSessionKeys.CharacterKey, characterKey);
            SetVar(vars, DialogueSessionKeys.EntryDialogId, entryDialogId);
            return new DialogueSessionContext { Vars = vars };
        }

        public static bool TryGetAction(DialogueSessionContext session, out EDialogueQuestAction action)
        {
            action = EDialogueQuestAction.None;
            if (session == null || !session.TryGetVar(DialogueSessionKeys.QuestAction, out var raw) || string.IsNullOrEmpty(raw))
            {
                return false;
            }

            switch (raw)
            {
                case ActionAccept:
                    action = EDialogueQuestAction.Accept;
                    return true;
                case ActionFulfill:
                    action = EDialogueQuestAction.Fulfill;
                    return true;
                case ActionRemind:
                    action = EDialogueQuestAction.Remind;
                    return true;
                default:
                    return false;
            }
        }

        public static string GetEntryDialogId(DialogueSessionContext session)
        {
            return session != null && session.TryGetVar(DialogueSessionKeys.EntryDialogId, out var value) ? value : "";
        }

        public static bool TryResolveAccept(DialogueSessionContext session, out int questId, out string characterKey)
        {
            questId = 0;
            characterKey = null;
            if (!TryGetAction(session, out var action) || action != EDialogueQuestAction.Accept)
            {
                return false;
            }

            return TryGetQuestId(session, out questId) && TryGetCharacterKey(session, out characterKey);
        }

        public static bool TryResolveFulfill(DialogueSessionContext session, out int questId, out string objId, out string characterKey)
        {
            questId = 0;
            objId = null;
            characterKey = null;
            if (!TryGetAction(session, out var action) || action != EDialogueQuestAction.Fulfill)
            {
                return false;
            }

            if (!TryGetQuestId(session, out questId) || !TryGetCharacterKey(session, out characterKey))
            {
                return false;
            }

            return session.TryGetVar(DialogueSessionKeys.ObjId, out objId) && !string.IsNullOrEmpty(objId);
        }

        public static bool TryResolveRemind(DialogueSessionContext session, out int questId, out string characterKey)
        {
            questId = 0;
            characterKey = null;
            if (!TryGetAction(session, out var action) || action != EDialogueQuestAction.Remind)
            {
                return false;
            }

            return TryGetQuestId(session, out questId) && TryGetCharacterKey(session, out characterKey);
        }

        public static bool TryGetQuestId(DialogueSessionContext session, out int questId)
        {
            questId = 0;
            if (session == null || !session.TryGetVar(DialogueSessionKeys.QuestId, out var raw) || string.IsNullOrEmpty(raw))
            {
                return false;
            }

            return int.TryParse(raw, out questId) && questId > 0;
        }

        public static bool TryGetCharacterKey(DialogueSessionContext session, out string characterKey)
        {
            characterKey = null;
            return session != null
                && session.TryGetVar(DialogueSessionKeys.CharacterKey, out characterKey)
                && !string.IsNullOrEmpty(characterKey);
        }

        private static void SetVar(Dictionary<string, string> vars, string key, string value)
        {
            if (vars == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            vars[key] = value ?? "";
        }
    }
}
