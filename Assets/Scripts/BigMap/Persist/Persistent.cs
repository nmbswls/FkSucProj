

using My.Map.Entity;
using System.Collections.Generic;
using System;
using UnityEngine;
using My.Player.Bag;

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
        public Vector2 FaceDir = Vector2.right;

        public EFactionId FactionId;

        public float LifeTime;

        public string BelongRoomId;
        public bool Activated = true;
        public long LifeBindEntityId;
        //public bool AlwaysActive;

        public List<string> LocalSwitches = null;
        public bool MarkDestroyed = false; // 逻辑死亡/销毁标记，由 RefreshEntityRecordInfo 等与运行时对齐
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
        public bool ItemInitialized = false;
        public int DynamicDropId;
        public List<ItemStack> InnerItems = new();
    }

    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4UnitBase : LogicEntityRecord
    {
        // 仅保存特殊状态 buff丢弃
        public bool Unsensored;
        public bool MarkDefeated; // 击败标记
        public bool MarkAttaching;

        public bool MarkNoLogic; // 非逻辑状态
    }

    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4Player : LogicEntityRecord4UnitBase
    {
        public class PlayerAttachment
        {
            public long AttachSrcId;
            public string AttachTypeId;
        }
        public List<PlayerAttachment> AttachUnits = new();
    }


    [Serializable]
    public class LogicEntityRecord4Npc : LogicEntityRecord4UnitBase
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

        public bool IsForeigner;
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
        /// 已创建的集合
        /// </summary>
        public Dictionary<int, long> MemberId2EntityMap = new();

        public List<int> CurrActiveMembers = new();
        
        //public List<int> SleepMemberIds = new();
        //public List<int> DestroyedMemberIds = new();
    }

    /// <summary>
    /// 设施
    /// </summary>
    [Serializable]
    public class LogicEntityRecord4HomeFacility : LogicEntityRecord4InteractPoint
    {
        public long BindingFacilityId;
    }

    // 
    [Serializable]
    public class LogicEntityRecord4DynamicSpawner : LogicEntityRecord
    {
        
    }

    [Serializable]
    public class LogicEntityRecord4Teleporter : LogicEntityRecord
    {
        public string TargetMap;
        public string TargetNamedPoint;
    }

    [Serializable]
    public class LogicEntityRecord4SimpleBlock : LogicEntityRecord
    {
        public float SizeX;
        public float SizeY;
    }
}

