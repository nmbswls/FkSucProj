using System;
using System.Collections;
using System.Collections.Generic;
using Map.Entity.AI;
using My.Map.Entity.AI;
using UnityEngine;

[CreateAssetMenu(menuName = "GP/UnitAI/AIBehaviorConfig")]
[Serializable]
public class AIBehaviorConfig : ScriptableObject
{
    public string BehaviorName;

    [Serializable]
    public class StateInfo
    {
        public string Name = "Default";
        public List<AITransition> Transitions = new();
        public List<string> ActionNames = new();
    }
    public List<StateInfo> States = new();

    [SerializeReference]
    public List<AIActionCfg> Actions = new();

    /// <summary>
    /// 
    /// </summary>
    public List<AITransition> CommonTransitions = new();
}
