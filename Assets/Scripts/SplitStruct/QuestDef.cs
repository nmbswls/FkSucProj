

using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

namespace My.Def.Quest
{

    [Serializable]
    public class QuestData
    {
        public int QuestId;
        public string title;
        // 引用类型处理：不存 Sprite 对象，只存路径
        public string iconPath;
        public string description;

        public int InitStepId;

        public QuestStepData[] steps;

        // 辅助方法：构建ID索引
        public Dictionary<int, QuestStepData> BuildStepMap()
        {
            var map = new Dictionary<int, QuestStepData>();
            foreach (var s in steps) map[s.stepId] = s;
            return map;
        }
    }


    [Serializable]
    public abstract class QuestCondition
    {
        public abstract string GetDescription();

        public int RequireProgress;
    }

    [Serializable]
    public class QuestConditionKill : QuestCondition
    {
        public string KillMonsterId;
        public List<string> SpecKillMonsterIds;

        public string KillTag;

        public override string GetDescription()
        {
            return "QuestConditionKill";
        }
    }

    [Serializable]
    public class QuestConditionHasSwitch : QuestCondition
    {
        public string SwitchName;

        public override string GetDescription()
        {
            return "QuestConditionHasSwitch";
        }
    }

    [Serializable]
    public class QuestConditionHasItem : QuestCondition
    {
        public string ItemId;

        public override string GetDescription()
        {
            return "QuestConditionHasItem";
        }
    }

    [Serializable]
    public class QuestConditionGainItem : QuestCondition
    {
        public string ItemId;

        public override string GetDescription()
        {
            return "QuestConditionGainItem";
        }
    }

    // 组合条件
    [Serializable]
    public class QuestConditionComposite : QuestCondition
    {
        public enum LogicType { And, Or }
        public LogicType logicType;

        [SerializeReference]
        public List<QuestCondition> conditions = new List<QuestCondition>();

        public override string GetDescription()
        {
            string op = logicType == LogicType.And ? "AND" : "OR";
            return $"{op} Group ({conditions.Count})";
        }

        //public override bool IsSatisfied(QuestProgress progress)
        //{
        //    if (conditions.Count == 0) return true;

        //    switch (logicType)
        //    {
        //        case LogicType.And:
        //            foreach (var cond in conditions)
        //                if (!cond.IsSatisfied(progress))
        //                    return negate ? true : false;
        //            return negate ? false : true;

        //        case LogicType.Or:
        //            foreach (var cond in conditions)
        //                if (cond.IsSatisfied(progress))
        //                    return negate ? false : true;
        //            return negate ? true : false;
        //    }
        //    return false;
        //}
    }


    [Serializable]
    public class ConditionData
    {
        public QuestCondition ConditionCfg;
    }

    [Serializable]
    public class ObjectiveData
    {
        public int objectiveId;

        [Tooltip("UI显示的描述")]
        public string text;

        [Tooltip("达成条件")]
        public ConditionData condition;

        [Tooltip("是否初始隐藏")]
        public bool isHidden;

        [Tooltip("是否可选")]
        public bool isOption;

        [Header("标签系统")]
        [Tooltip("当此目标完成时，给任务实例打上这些标签 (Internal Tags)")]
        public string[] completionTags;
    }



    [Serializable]
    public class StepOutcomeData
    {
        public string outcomeName;    // 比如 "正面突击"
        public string description;    // UI描述 "击杀守卫"

        public int completeId;
        public int[] NeedObjectiveIds;
        public int nextStepId;     // 达成后跳转的ID列表
    }


    [Serializable]
    public class QuestStepData
    {
        public int stepId;
        public bool isRoot;           // 是否是起始步骤
        public bool isAuto;           // 是否是起始步骤

        [Header("完成路径")]
        public StepOutcomeData[] outcomes;

        [Header("目标")]
        public ObjectiveData[] objectives;

        [Header("失败条件 (可选)")]
        public ConditionData failCondition;
    }

}
