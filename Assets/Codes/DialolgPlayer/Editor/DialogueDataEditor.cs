
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Dialog
{
    [CreateAssetMenu(fileName = "NewDialoguePoly", menuName = "Dialogue/Dialogue Data (Poly)")]
    public class EditorDialogueData : ScriptableObject
    {
        public string DialogId;
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


    // --- 抽象基类 ---
    [Serializable]
    public abstract class EditorDialogCommand
    {
        public bool IsFolded = true; // 内部折叠
        public virtual string GetSummary() => $"";
    }


    // --- 具体子类示例 ---

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

        public string Speaker;
        [TextArea(2, 5)] public string Content;
        public AudioClip VoiceLine;

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

        public List<string> SimpleOptions = new();
        public List<List<TextLine>> SimpleBranchTextLines = new();

        public override string GetSummary() => $"[Talk]";
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
    public class EditorSimpleFuncCommand : EditorDialogCommand
    {
        public enum ESimpleFuncType
        {
            None,
            SrcLocalSwitch
        }
        public ESimpleFuncType SimpleFuncType;
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

}