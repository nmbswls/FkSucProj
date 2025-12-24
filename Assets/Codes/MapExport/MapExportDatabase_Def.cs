using System.Collections.Generic;
using System;
using UnityEngine;
using My.Map.Scene;
using My.Map;
using My.Map.Entity;


namespace My.MapExport
{
    public enum ENamedPointType
    {
        Normal,
        BornPos,
        DigPoint,
        GuardSpawner,
        WalkerStart,
    }


    [Serializable]
    public struct NamedPoint
    {
        public string Name;
        public ENamedPointType PointType;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    [Serializable]
    public struct NamedPath
    {
        public string Name;
        public List<string> Points;
        public string Tag;
    }


    [Serializable]
    public struct StaticPrefabItem
    {
        public int ItemId;
        public string Key;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public CommonCheckCond AppearCond;
    }



    [Serializable]
    public class DynamicEntityRefreshInfo
    {
        public int UniqId; // 场景内唯一id 用于检查是否已创建 自动分配

        /// <summary>
        /// 出现条件
        /// </summary>
        public CommonCheckCond AppearCond;

        /// <summary>
        /// 消失条件
        /// </summary>
        public CommonCheckCond DisappearCond;

        // 重生相关信息
        public bool WillRespawn = false;
        public float RespawnInterval = 0;

        [SerializeReference]
        public EntityInitInfo InitInfo;
    }

    [Serializable]
    public abstract class EntityInitInfo
    {
        public abstract EEntityType EntityType { get; }
        public string CfgId;
        public Vector2 Position;
        public Vector2 FaceDir;


        public string BindRoomId;
        public EFactionId OrgFactionId;
    }

    [Serializable]
    public abstract class EntityInitInfo4Unit : EntityInitInfo
    {
        public UnitMoveBehaveInfo.EMoveBehaveType MoveMode;
        public string EnmityConfId;

        public bool IsPeace;

        public bool InitUnsensored;
    }

    [Serializable]
    public class EntityInitInfo4Player : EntityInitInfo4Unit
    {
        public override EEntityType EntityType => EEntityType.Player;
    }

    [Serializable]
    public class EntityInitInfo4InteractPoint : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.InteractPoint;

        public SerializableDict<string, string> Variables = new();
    }

    [Serializable]
    public class EntityInitInfo4LootPoint : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.LootPoint;
    }

    [Serializable]
    public class EntityInitInfo4Npc : EntityInitInfo4Unit
    {
        public override EEntityType EntityType => EEntityType.Npc;
    }

    [Serializable]
    public class EntityInitInfo4AreaEffect : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.AreaEffect;
    }

    [Serializable]
    public class EntityInitInfo4DestroyObj : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.DestroyObj;
    }

    [Serializable]
    public class EntityInitInfo4GatherPoint : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.GatherPoint;
    }

    [Serializable]
    public class EntityInitInfo4AttractPoint : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.AttractPoint;
    }

    [Serializable]
    public class EntityInitInfo4HomePlacement : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.HomePlacement;
    }

    [Serializable]
    public class EntityInitInfo4EventGroup : EntityInitInfo4Unit
    {
        public override EEntityType EntityType => EEntityType.EventGroup;
    }

    [Serializable]
    public class EntityInitInfo4PatrolGroup : EntityInitInfo
    {
        public enum ELoopMode
        {
            None,
            PingPong,
            Circle,
        }

        [Serializable]
        public class PatrolOneInfo
        {
            public int GroupIdx = 0;
            [SerializeReference]
            public EntityInitInfo InitInfo;
        }

        public override EEntityType EntityType => EEntityType.PatrolGroup;

        public float MoveSpeed = 0.2f;
        public List<string> Waypoints = new();
        public ELoopMode LoopMode;
        public List<PatrolOneInfo> GroupUnits = new();
    }
}

