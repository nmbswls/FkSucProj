using Map.Entity;
using My.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.MapExport
{
    public class DynamicPatrolGroupExportGenerator : DynamicEntityExportGenerator
    {
        public float MoveSpeed = 0.2f;
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
}

