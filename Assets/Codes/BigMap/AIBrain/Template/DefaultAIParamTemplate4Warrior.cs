using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "GP/AITemplate/Warrior")]
[Serializable]
public class DefaultAIParamTemplate4Warrior : ScriptableObject
{
    public string Name;
    public float KeepDistance;
    public float AttackRate;
}
