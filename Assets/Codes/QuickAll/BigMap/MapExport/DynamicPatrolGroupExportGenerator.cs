using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DynamicPatrolGroupExportGenerator : DynamicEntityExportGenerator
{
    public float MoveSpeed = 0.2f ;
    public List<string> Waypoints = new();
    public enum ELoopMode
    {
        None,
        PingPong,
        Circle,
    }
    public ELoopMode LoopMode;

    [Serializable]
    public class PatrolOneInfo
    {
        public EEntityType EntityType;
        public string CfgId;
        public Vector2 RelativePos;
    }

    public List<PatrolOneInfo> GroupUnits = new();
}