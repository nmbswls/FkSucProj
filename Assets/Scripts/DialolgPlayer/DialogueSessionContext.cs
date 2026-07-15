using System.Collections.Generic;

namespace My.Dialog
{
    // 会话 Vars 中系统保留 key（派发/查表用，与台词插值变量区分）
    public static class DialogueSessionKeys
    {
        public const string QuestAction = "quest_action";
        public const string QuestId = "quest_id";
        public const string ObjId = "obj_id";
        public const string CharacterKey = "character_key";
        public const string EntryDialogId = "entry_dialog_id";
        public const string AcceptDialogId = "accept_dialog_id";
        public const string ObjectiveDialogId = "objective_dialog_id";
    }

    // 对话运行期上下文，不进 DialogChoiceOption JSON
    public class DialogueSessionContext
    {
        public Dictionary<string, string> Vars = new Dictionary<string, string>();

        public bool TryGetVar(string key, out string value)
        {
            value = null;
            if (Vars == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            return Vars.TryGetValue(key, out value);
        }
    }
}
