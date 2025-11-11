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
        public List<AITransition> Transitions;
        public List<string> ActionNames;
    }
    public List<StateInfo> States = new();

    [SerializeReference]
    public List<AIAction> Actions = new();
}
