

using System.Collections.Generic;
using System;
using UnityEngine;

namespace My.Dialog
{

    [Serializable]
    public abstract class DialogCommandBase
    {
        public virtual string GetSummary() => $"";
    }

    // --- 具体子类示例 ---

    [Serializable]
    public class DialogueTextCommand : DialogCommandBase
    {
        public string Speaker;
        [TextArea(2, 5)] public string Content;
        public AudioClip VoiceLine;

        public override string GetSummary() => $"[Talk]";
    }

    [Serializable]
    public class SetImageCommand : DialogCommandBase
    {
        public Sprite image;
        public enum ImgPos { Left, Center, Right, Background }
        public ImgPos position;

        public override string GetSummary() => $"[SetImage]";
        //public override string GetSummary() => $"[Img] {position} - {(image ? image.name : "None")}";
    }

    [Serializable]
    public class ChoiceCommand : DialogCommandBase
    {
        public float timeLimit = 0;
        public List<DialogChoiceOption> Options = new List<DialogChoiceOption>();

        public override string GetSummary() => $"[Choice]";
        //public override string GetSummary() => $"[Choice] {Options.Count} Options";
    }

    [Serializable]
    public class DialogChoiceOption
    {
        public string Text;
        public string TargetStepId;

        [SerializeReference]
        public List<DialogCondition> Conditions1 = new List<DialogCondition>();
    }

    [Serializable]
    public abstract class DialogCondition
    {

    }

    [Serializable]
    public class ConditionLocalVariableInt : DialogCondition
    {
        // 为演示简单，保持基本结构
        public string VariableKey;
        public enum CompareType { Equals, Greater, Less, GE, LE, NotEquals }
        public CompareType Compare;
        public int Value;
    }

    [Serializable]
    public class ConditionLocalVariableString : DialogCondition
    {
        // 为演示简单，保持基本结构
        public string VariableKey;
        public enum CompareType { Equals, NotEquals }
        public CompareType Compare;
        public string Value;
    }

}