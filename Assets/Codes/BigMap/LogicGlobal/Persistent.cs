

using My.Map.Entity;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace My.Map.Logic
{
    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord
    {
        public long Id;               // 全局唯一ID
        public EEntityType EntityType;
        public string CfgId;
        public Vector2 Position;
        public Vector2 FaceDir;

        public EFactionId FactionId;

        public bool DeadMark;
        public float LifeTime;

        public string BelongRoomId;
        //public bool AlwaysActive;
    }

    [Serializable]
    public class LogicEntityRecord4InteractPoint : LogicEntityRecord
    {
        public int Status;

        public Dictionary<string, string> DynamicVariables = new();
    }


    [Serializable]
    public class LogicEntityRecord4LootPoint : LogicEntityRecord
    {
        public string DynamicDropId;
    }

    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4UnitBase : LogicEntityRecord
    {
        public bool IsPeace;

        public UnitMoveBehaveInfo.EMoveBehaveType MoveBehaveType;

        public string EnmityConfId;
        public List<string> MoveWayPoints;

        public long PatrolFollowId;
        public Vector2 PatrolGroupRelativePos;
        public bool DisappearOnArrive;
        public string MovePath = null;
        public int CurrPathIdx = 0;
        public float CurrPathProgress = 0;

        // 仅保存特殊状态 buff丢弃
        public bool Unsensored;


    }

    [Serializable]
    public class LogicEntityRecord4Npc : LogicEntityRecord4UnitBase
    {

    }


    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4PatrolGroup : LogicEntityRecord
    {
        public float MoveSpeed;
        public int WayPointIdx = 0;
        public float WayPointDistance;
        public List<string> WayPointList = new();
        public bool IsBack = false;

        public List<long> PatrolUnitIds = new();
    }

    /// <summary>
    /// EventGroup
    /// </summary>
    [Serializable]
    public class LogicEntityRecord4EventGroup : LogicEntityRecord4InteractPoint
    {
        /// <summary>
        /// 受管理member集合
        /// </summary>
        public Dictionary<int, long> MemberEntityMap = new();
        public List<int> SleepMemberIds = new();

        public List<int> DestroyedMemberIds = new();
    }
}