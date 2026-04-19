
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Dialog
{
    [CreateAssetMenu(fileName = "NewDialoguePoly", menuName = "Dialogue/Dialogue Data (Poly)")]
    public class EditorDialogueData : ScriptableObject
    {
        public string DialogId;
        public bool LockTime;
        public List<string> ControlledEntityNames = new();
        public List<EditorStepData> Steps = new List<EditorStepData>();
    }

    [Serializable]
    public class EditorStepData
    {
        public string Id;
        public string Note;
        public bool IsExpanded;

        [SerializeReference]
        public List<EditorDialogCommand> Commands = new List<EditorDialogCommand>();
    }


    // --- 编辑器侧对话命令基类 ---
    [Serializable]
    public abstract class EditorDialogCommand
    {
        public bool IsFolded = true; // Inspector 中是否折叠子项
        public virtual string GetSummary() => $"";
    }


    // --- 具体命令类型（与运行时 Structs 对应） ---

    [Serializable]
    public class EditorDialogueCommand4Text : EditorDialogCommand
    {
        [Serializable]
        public class TextLine
        {
            public string Speaker;
            [TextArea(2, 5)] public string Content;
            public AudioClip VoiceLine;
        }

        public List<TextLine> TextLines = new();

        //public string Speaker;
        //[TextArea(2, 5)] public string Content;
        //public AudioClip VoiceLine;

        public override string GetSummary() => $"[Talk]";
    }

    [Serializable]
    public class EditorDialogueCommand4SimpleBranch : EditorDialogCommand
    {
        [Serializable]
        public class TextLine
        {
            public string Speaker;
            [TextArea(2, 5)] public string Content;
            public AudioClip VoiceLine;
        }

        [System.Serializable]
        public class BranchData
        {
            public string OptionText;
            public List<TextLine> ResultLines = new List<TextLine>();
        }

        public List<BranchData> Branches = new List<BranchData>();

        public override string GetSummary() => $"[Branch] {Branches.Count} Options";
    }


    [Serializable]
    public class EditorSetImageCommand : EditorDialogCommand
    {
        public Sprite Image;
        public enum ImgPos { Left, Center, Right, Background }
        public ImgPos Position;

        public override string GetSummary() => $"[SetImage]";
    }

    [Serializable]
    public class EditorActorMoveCommand : EditorDialogCommand
    {

        public string StaticName;
        public Vector2 MovePos;

        public bool ForceStartPos;
        public Vector2 StartPos;

        public float MoveDuration = 1.0f;

        public override string GetSummary() => $"[ActorMove] {StaticName}";
    }

    [Serializable]
    public class EditorActorAnimCommand : EditorDialogCommand
    {

        public string AnimName;

        public override string GetSummary() => $"[ActorAnim] {AnimName}";
    }



    [Serializable]
    public class EditorSimpleFuncCommand : EditorDialogCommand
    {
        
        public EDialogSimpleFuncType SimpleFuncType;
        public long Param1;
        public long Param2;
        public int Param3;
        public int Param4;
        public string Param5;
        public string Param6;
        public override string GetSummary() => $"[Func] {SimpleFuncType}";
    }


    [Serializable]
    public class EditorDialogueCommand4JumpTo : EditorDialogCommand
    {
        public string TargetStepId;

        public override string GetSummary() => $"[Jump]";
    }


    [Serializable]
    public class EditorChoiceCommand : EditorDialogCommand
    {
        public float TimeLimit = 0;
        public List<DialogChoiceOption> Options = new List<DialogChoiceOption>();

        public override string GetSummary() => $"[Choice]";
    }


    [Serializable]
    public class EditorPlayerTimelineCommand : EditorDialogCommand
    {

        public string TimelineId;
        public bool WaitUntilFinished;

        public override string GetSummary() => $"[PlayerTimeline]";
    }


    [Serializable]

    public class EditorWaitTimelineSignalCommand : EditorDialogCommand
    {
        public string SignalName; // Timeline SignalReceiver 上配置的信号名

        public override string GetSummary() => $"[WaitTimelineSignal]";
    }

    [Serializable]
    public class EditorCommandResumeTimeline : EditorDialogCommand
    {
        public override string GetSummary() => $"[Resume]";
    }

    [Serializable]
    public class EditorCommandWait : EditorDialogCommand
    {
        public float WaitTime;

        public override string GetSummary() => $"[Wait]";
    }

    [Serializable]
    public class EditorSwitchDialogSegmentCommand : EditorDialogCommand
    {
        public string TargetStepId = "";
        public bool CancelTypingState = true;

        public override string GetSummary() => $"[SwitchSegment] -> {TargetStepId}";
    }

    [Serializable]
    public class EditorDynamicNpcChoiceCommand : EditorDialogCommand
    {
        public float TimeLimit;

        public override string GetSummary() => "[DynamicNpcChoice] runtime-generated";
    }

}