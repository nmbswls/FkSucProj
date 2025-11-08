using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "GP/AITemplate/Shooter")]
[Serializable]
public class DefaultAIParamTemplate4Shooter : ScriptableObject
{
    public string Name;
    public float KeepDistance;
    public float EmergencyDistance;
    public float AttackRate;
}
