using UnityEditor;

namespace My.Dialog
{
    public static class DialogueEditorCommandLabels
    {
        public static string GetSummary(DialogCommandData cmd)
        {
            if (cmd == null)
                return "Empty Slot";

            switch (cmd)
            {
                case DialogCommandData4Text _:
                    return "[Talk]";
                case DialogCommandData4BranchText b:
                    return $"[Branch] {b.SimpleBranch?.Count ?? 0} Options";
                case DialogCommandData4SetImage _:
                    return "[SetImage]";
                case DialogCommandData4MoveEntity m:
                    return $"[ActorMove] {m.StaticName}";
                case DialogCommandData4ActorAnim a:
                    return $"[ActorAnim] {a.AnimName}";
                case DialogCommandData4SimpleFunc f:
                    return $"[Func] {f.SimpleFuncType}";
                case DialogCommandData4JumpTo _:
                    return "[Jump]";
                case DialogCommandData4Choice _:
                    return "[Choice]";
                case DialogCommandData4PlayTimeline t:
                    return $"[PlayerTimeline] {t.TimelineId}";
                case DialogCommandData4WaitTimelineSignal w:
                    return $"[WaitTimelineSignal] {w.SignalName}";
                case DialogCommandData4ResumeTimeline _:
                    return "[Resume]";
                case DialogCommandData4Wait _:
                    return "[Wait]";
                case DialogCommandData4SwitchDialogSegment s:
                    return $"[SwitchSegment] -> {s.TargetStepId}";
                case DialogCommandData4DynamicNpcChoice _:
                    return "[DynamicNpcChoice] runtime-generated";
                case DialogCommandData4OpenShop o:
                    return $"[OpenShop] fix={o.FixShop}";
                case DialogCommandData4QuestAction q:
                    return $"[QuestAction] {q.QuestAction} q={q.QuestId}";
                default:
                    return $"[{cmd.GetType().Name}]";
            }
        }

        public static string GetMenuName(System.Type type)
        {
            string name = type.Name;
            if (name.StartsWith("DialogCommandData4"))
                name = name.Substring("DialogCommandData4".Length);
            return ObjectNames.NicifyVariableName(name);
        }
    }
}
