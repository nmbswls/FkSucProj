using System;
using System.Collections;
using System.Collections.Generic;
using Map.Entity.AI;
using My.Map.Entity.AI;
using UnityEngine;

[CreateAssetMenu(menuName = "GP/UnitAI/AIBrainParamsConfig")]
[System.Serializable]
public class AIBrainParamsConfig : ScriptableObject
{
    public float VisionRange = 7f;
    public float VisionFOV = 140f;
    public float LoseTargetGrace = 1.2f;

    public bool ExitCombatBoundary = true;
    public float ExitCombatBoundaryRange = 10.0f;
    public float ExitCombatMinRecoverTime = 1f;

    public float GoodBattleDistance = 3.0f;

    public string SpecialAnimTag1;
    public string SpecialAnimTag2;
    public string SpecialAnimTag3;
    public string SpecialAnimTag4;

}
