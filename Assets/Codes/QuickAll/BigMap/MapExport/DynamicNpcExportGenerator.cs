using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  Map.Entity;
using My.Map;


public class DynamicNpcExportGenerator : DynamicEntityExportGenerator
{
    public string NpcName;
    public int CampId;

    public BaseUnitLogicEntity.EUnitEnmityMode EnmityMode;
    public BaseUnitLogicEntity.EUnitMoveActMode MoveMode;

    public bool IsPeace;

    public float WanderRange = 1f;

    public bool IsPatrolLoop;
    public List<string> WaypointList; // 如果是patrol 根据路点移动

    public string NpcTag = string.Empty;

    public bool InitUnsensored = false;

}