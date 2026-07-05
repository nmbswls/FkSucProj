using System.Collections.Generic;
using System;
using cfg.demo;
using UnityEngine;

namespace My.Dialog
{

    [Serializable]
    public class DialogueData
    {
        public List<DialogueStepData> Steps = new List<DialogueStepData>();
    }

    [Serializable]
    public class DialogueStepData
    {
        public string Id;
        public string Note;

        [SerializeReference]
        public List<DialogCommandData> Commands = new List<DialogCommandData>();
    }

    // --- 对话命令数据基类 ---
    [Serializable]
    public abstract class DialogCommandData
    {
    }

    [Serializable]
    public class OneTextLine
    {
        public string Speaker;
        public string Content;
        public string VoiceLine;
    }


    [Serializable]
    public class DialogCommandData4Text : DialogCommandData
    {
        public List<OneTextLine> TextLines = new();

        public string Speaker;
        public string Content;
        public string VoiceLine;
    }

    // 分支台词：每组选项对应一段逐行文本
    [Serializable]
    public class DialogCommandData4BranchText : DialogCommandData
    {
        public List<string> SimpleBranch;
        public List<List<OneTextLine>> SimpleTextLines;
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
    public class DialogCommandData4ShowImage : DialogCommandData
    {
        public string ImageName;
        public bool Visible = true;
        public float Alpha = 1f;
    }

    public enum EDialogSimpleFuncType
    {
        None = 0,
        SrcLocalSwitch,

        AddTmpEnmity,
        ClearWanted,

        Charmed,

        SetGlobalSwitch,
        Teleport,
        OpenFunc,
    }


    public enum EProbabilityType
    {
        None,
        Fixed,
        CharmRule,
    }


    [Serializable]
    public class DialogCommandData4MoveEntity : DialogCommandData
    {
        public string StaticName;
        public Vector2 MovePos;

        public bool ForceStartPos;
        public Vector2 StartPos;

        public float MoveDuration = 1.0f;
    }

    [Serializable]
    public class DialogCommandData4ActorAnim : DialogCommandData
    {
        public string AnimName;
    }


    [Serializable]
    public class DialogCommandData4SimpleFunc : DialogCommandData
    {
        public EDialogSimpleFuncType SimpleFuncType;
        public long Param1;
        public long Param2;
        public int Param3;
        public int Param4;
        public string Param5;
        public string Param6;
    }

    [Serializable]
    public class DialogCommandData4PlayTimeline : DialogCommandData
    {
        public string TimelineId;
        public bool WaitUntilFinished;
    }

    [Serializable]

    public class DialogCommandData4WaitTimelineSignal : DialogCommandData
    {
        public string SignalName; // WaitTimelineSignal：等待 Timeline 发出的信号名
    }

    [Serializable]
    public class DialogCommandData4ResumeTimeline : DialogCommandData
    {
    }


    [Serializable]
    public class DialogCommandData4Wait : DialogCommandData
    {
        public float WaitTime;
    }

    [Serializable]
    public class DialogCommandData4Choice : DialogCommandData
    {
        public float TimeLimit = 0;
        public List<DialogChoiceOption> Options = new List<DialogChoiceOption>();
    }


    // 选项里 TargetDialogId 可用的占位符（大小写不敏感），运行期替换为实际 dialog id
    public static class DialogChoicePlaceholders
    {
        public const string NpcResolvedPeace = "@NpcResolvedPeace";
    }

    [Serializable]
    public class DialogChoiceOption
    {
        public string Text;

        public string TargetStepId;

        // 非空：选择后结束当前对话并 PlayDialog 切换到该 id（可为字面表 id，或 DialogChoicePlaceholders 占位符）；与 Conditions1 组合可实现按任务等过滤出不同目标对话
        public string TargetDialogId = "";

        // 概率分支类型（与失败跳转等配合）
        public EProbabilityType ProbabilityType;
        public float ProbabilityParam1;

        public string FailTargetStepId;

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
        // 本地整型变量键名
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
        // 本地字符串变量键名
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
        // 与玩家实体 GetAttr(attrId) 比较：attrId 需在数值属性表中存在，否则为 0
        public string AttrIdForLevel = "PlayerSan";
        public int PlayerLevel;

        public override string GetSummary()
        {
            return $"[PlayerAttr>={PlayerLevel}] {AttrIdForLevel}";
        }
    }

    [ConditionMenuName("NPC/In Combat")]
    [Serializable]
    public class ConditionNpcInCombat : DialogCondition
    {
        public bool RequireInCombat = true;

        public override string GetSummary()
        {
            return RequireInCombat ? "[NPC] InCombat" : "[NPC] NotInCombat";
        }
    }

    [ConditionMenuName("NPC/Has Local Switch")]
    [Serializable]
    public class ConditionNpcHasLocalSwitch : DialogCondition
    {
        public string SwitchName = "";

        public override string GetSummary()
        {
            return $"[NPC Switch] {SwitchName}";
        }
    }

    [Serializable]
    public class SerializableCommonCheckCond
    {
        public ECommonCheckType Type;
        public long Param1;
        public long Param2;
        public long Param3;
        public long Param4;
        public string Param5 = "";
        public string Param6 = "";
    }

    [ConditionMenuName("Logic/CommonCheckCond List")]
    [Serializable]
    public class ConditionCommonCondsList : DialogCondition
    {
        public List<SerializableCommonCheckCond> Items = new();

        public override string GetSummary()
        {
            return $"[CommonConds] x{Items?.Count ?? 0}";
        }
    }

    [Serializable]
    public class DialogCommandData4SwitchDialogSegment : DialogCommandData
    {
        public string TargetStepId = "";
        public bool CancelTypingState = true;
    }

    [Serializable]
    public class DialogCommandData4DynamicNpcChoice : DialogCommandData
    {
        // 选项列表由运行期 DynamicNpcChoiceRuntime 按对话 id / Src NPC 等生成，不序列化在 JSON 中
        public float TimeLimit;
    }

    [Serializable]
    public class DialogCommandData4OpenShop : DialogCommandData
    {
        public int FixShop;
        public bool BindingShop;
    }

    [Serializable]
    public class DialogCommandData4QuestAction : DialogCommandData
    {
        public EDialogueQuestAction QuestAction;
        public int QuestId;
        public string ObjId;
    }
}
