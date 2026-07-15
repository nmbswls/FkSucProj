using System.Collections.Generic;
using cfg.demo;
using My.Player;

namespace My.Cfg_Ex
{
    // 任务对话专用显示条件：依赖任务实例上的 ObjectiveDialog 触发态，不走 CommonCheckCond。
    public static class QuestDialogShowCondUtil
    {
        public static bool Passes(IReadOnlyList<QuestDialogShowCond> conds, QuestInstance quest)
        {
            if (conds == null || conds.Count == 0)
            {
                return true;
            }

            if (quest == null)
            {
                return false;
            }

            for (int i = 0; i < conds.Count; i++)
            {
                var cond = conds[i];
                if (cond == null || cond.Type == EQuestDialogShowCondType.None)
                {
                    continue;
                }

                if (!Eval(cond, quest))
                {
                    return false;
                }
            }

            return true;
        }

        static bool Eval(QuestDialogShowCond cond, QuestInstance quest)
        {
            switch (cond.Type)
            {
                case EQuestDialogShowCondType.ObjectiveDialogTriggered:
                    return quest.IsObjectiveDialogTriggered(cond.DialogId);
                case EQuestDialogShowCondType.ObjectiveDialogNotTriggered:
                    return !quest.IsObjectiveDialogTriggered(cond.DialogId);
                default:
                    return true;
            }
        }
    }
}
