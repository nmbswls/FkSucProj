

using System.Collections.Generic;
using System;
using UnityEngine;

namespace My.Dialog
{

    [Serializable]
    public class DialogueData
    {
        public string DialogId;
        public List<DialogueStepData> Steps = new List<DialogueStepData>();
    }

    [Serializable]
    public class DialogueStepData
    {
        public string Id;
        public string Note;

        public List<DialogCommandData> Commands = new List<DialogCommandData>();
    }

    // --- 抽象基类 ---
    [Serializable]
    public abstract class DialogCommandData
    {
    }

    [Serializable]
    public class DialogCommandData4Text : DialogCommandData
    {
        public string Speaker;
        public string Content;
        public string VoiceLine;
    }

    [Serializable]
    public class DialogCommandData4JumpTo : DialogCommandData
    {
        public string TargetStepId;
    }


    [Serializable]
    public class DialogCommandData4SetImage : DialogCommandData
    {
        public string ImageName;
        public enum ImgPos { Left, Center, Right, Background }
        public ImgPos Position;
    }

    [Serializable]
    public class DialogCommandData4Choice : DialogCommandData
    {
        public float TimeLimit = 0;
        public List<DialogChoiceOption> Options = new List<DialogChoiceOption>();
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