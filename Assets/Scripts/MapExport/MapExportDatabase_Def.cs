using System.Collections.Generic;
using System;
using UnityEngine;
using My.Map.Scene;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using cfg.demo;


namespace My.MapExport
{
    public enum ENamedPointType
    {
        Normal,
        BornPos,
        DigPoint,
        GuardSpawner,
        WalkerStart,

        PatrolPoint, // 路点类型
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
    public class PortalNetworkNodeExport
    {
        public string NodeId;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    [Serializable]
    public class PortalNetworkEdgeExport
    {
        public string NodeA;
        public string NodeB;
        public float Weight;
    }

    [Serializable]
    public class PortalNetworkExport
    {
        public string NetworkId;
        public List<PortalNetworkNodeExport> Nodes = new();
        public List<PortalNetworkEdgeExport> Edges = new();
    }

    // 供 JsonUtility 写盘（不支持顶层 List）
    [Serializable]
    public class PortalNetworksJsonRoot
    {
        public string area_id;
        public PortalNetworkJsonEntry[] networks;
    }

    [Serializable]
    public class PortalNetworkJsonEntry
    {
        public string network_id;
        public PortalNetworkNodeJson[] nodes;
        public PortalNetworkEdgeJson[] edges;
    }

    [Serializable]
    public class PortalNetworkNodeJson
    {
        public string node_id;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public class PortalNetworkEdgeJson
    {
        public string node_a;
        public string node_b;
        public float weight;
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
        /// <summary>
        /// 自动赋值
        /// </summary>
        public int StaticId; // 场景内唯一id 用于检查是否已创建 自动分配

        /// <summary>
        /// 唯一名字
        /// </summary>
        public string UniqName;

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
        public bool InitNoLogic;

        // 具名 NPC 跨场景档案主键；空则不走 WorldNpcCharacterPersistRegistry
        public string CharacterKey = string.Empty;
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
        public override EEntityType EntityType => EEntityType.HomeFacility;

        public long BindingFacilityId;
    }

    [Serializable]
    public class EntityInitInfo4Teleporter: EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.Teleporter;

        public string TargetMapName;
        public string TargetNamedPoint;
    }

    [Serializable]
    public class EntityInitInfo4SimpleBlock : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.SimpleBlock;

        public float SizeX;
        public float SizeY;
    }
    


    [Serializable]
    public class EntityInitInfo4EventGroup : EntityInitInfo4InteractPoint
    {
        public override EEntityType EntityType => EEntityType.EventGroup;
    }

    [Serializable]
    public class EntityInitInfo4FacilityRuin : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.FacilityRuin;
    }

    [Serializable]
    public class EntityInitInfo4DynamicSpawner : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.DynamicSpawner;
    }
    

    [Serializable]
    public class EntityInitInfo4SavePoint : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.SavePoint;
    }

    [Serializable]
    public class EntityInitInfo4FishingSpot : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.FishingSpot;
    }

    [Serializable]
    public class EntityInitInfo4Trap : EntityInitInfo
    {
        public override EEntityType EntityType => EEntityType.Trap;
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

