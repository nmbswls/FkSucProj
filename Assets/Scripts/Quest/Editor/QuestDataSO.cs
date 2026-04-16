using My.Def.Quest;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Quest Data")]
public class QuestDataSO : ScriptableObject
{
    public int questId;
    public string title;
    public int InitStepId;
    [TextArea] public string description;

    public QuestStepData[] steps;

    // 辅助方法：构建ID索引
    public Dictionary<int, QuestStepData> BuildStepMap()
    {
        var map = new Dictionary<int, QuestStepData>();
        foreach (var s in steps) map[s.stepId] = s;
        return map;
    }
}