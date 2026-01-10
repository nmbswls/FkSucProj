

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
        public bool ShowWhenFail;

        [SerializeReference]
        public List<DialogCondition> Conditions1 = new List<DialogCondition>();
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ConditionMenuNameAttribute : System.Attribute
    {
        public string MenuPath;
        public ConditionMenuNameAttribute(string path) { MenuPath = path; }
    }

    [Serializable]
    public abstract class DialogCondition
    {
        public virtual string GetSummary() => "[Unknown]";
    }

    [ConditionMenuName("Local/Check Int")]
    [Serializable]
    public class ConditionLocalVariableInt : DialogCondition
    {
        // 为演示简单，保持基本结构
        public string VariableKey;
        public enum CompareType { Equals, Greater, Less, GE, LE, NotEquals }
        public CompareType Compare;
        public int Value;


        public override string GetSummary()
        {
            string op = "";
            switch (Compare)
            {
                case ConditionLocalVariableInt.CompareType.Equals: op = "=="; break;
                case ConditionLocalVariableInt.CompareType.Greater: op = ">"; break;
                case ConditionLocalVariableInt.CompareType.Less: op = "<"; break;
                case ConditionLocalVariableInt.CompareType.GE: op = ">="; break;
                case ConditionLocalVariableInt.CompareType.LE: op = "<="; break;
                case ConditionLocalVariableInt.CompareType.NotEquals: op = "!="; break;
            }
            return $"[Int] {(string.IsNullOrEmpty(VariableKey) ? "Null" : VariableKey)} {op} {Value}";
        }
    }

    [ConditionMenuName("Local/Check String")]
    [Serializable]
    public class ConditionLocalVariableString : DialogCondition
    {
        // 为演示简单，保持基本结构
        public string VariableKey;
        public enum CompareType { Equals, NotEquals }
        public CompareType Compare;
        public string Value;

        public override string GetSummary()
        {
            string op = (Compare == ConditionLocalVariableString.CompareType.Equals) ? "==" : "!=";
            return $"[String] {(string.IsNullOrEmpty(VariableKey) ? "Null" : VariableKey)} {op} \"{Value}\"";
        }
    }

    [ConditionMenuName("Player/Level")]
    [Serializable]
    public class ConditionCheckPlayerLevel : DialogCondition
    {
        public int PlayerLevel;

        public override string GetSummary()
        {
            return $"[PlayerLevel] {PlayerLevel}";
        }
    }

}