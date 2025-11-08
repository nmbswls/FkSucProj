using System.Collections.Generic;
using System;
using UnityEngine;
using Map.Logic.Chunk;
using Map.Entity;

[CreateAssetMenu(fileName = "ChunkStaticDatabase", menuName = "MapExport/Chunk Static Database")]
public class ChunkMapExportDatabase : ScriptableObject
{
    [Serializable]
    public struct NamedPoint
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    [Serializable]
    public struct StaticItem
    {
        public string Key;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    [Serializable]
    public class DynamicEntityAppearCond
    {
        public int Type;
        public int Param1;
        public int Param2;
    }

    [Serializable]
    public class DynamicEntityRefreshInfo
    {
        public int UniqId; // 场景内唯一id 用于检查是否已创建 自动分配

        public EEntityType EntityType;
        public string CfgId;

        public Vector2 Position;
        public Vector2 FaceDir;

        public string BindRoomId;

        public DynamicEntityAppearCond? AppearCond;

        [SerializeReference]
        public DynamicEntityInitInfo? InitInfo = null;
    }

    [Serializable]
    public class DynamicEntityInitInfo
    {

    }

    [Serializable]
    public class DynamicEntityInitInfo4PatrolGroup : DynamicEntityInitInfo
    {
        public float MoveSpeed = 0.2f;
        public List<string> Waypoints = new();
        public DynamicPatrolGroupExportGenerator.ELoopMode LoopMode;
        public List<DynamicPatrolGroupExportGenerator.PatrolOneInfo> GroupUnits = new();
    }


    [Serializable]
    public class DynamicEntityInitInfo4Unit : DynamicEntityInitInfo
    {
        public BaseUnitLogicEntity.EUnitEnmityMode EnmityMode;

        public BaseUnitLogicEntity.EUnitMoveActMode MoveMode;

        public bool IsPeace;
    }


    [Serializable]
    public class RoomExportInfo
    {
        public string RoomId;
        public Vector3 Position;
        public List<Vector2> AreaRanges;
    }


    [Serializable]
    public struct ChunkKey
    {
        public int X;
        public int Y;
    }

    [Serializable]
    public class ChunkItems
    {
        public ChunkKey Chunk;
        public List<StaticItem> StaticItems = new List<StaticItem>();

        public List<DynamicEntityRefreshInfo> EntityRefreshInfo = new List<DynamicEntityRefreshInfo>();

        public List<RoomExportInfo> RoomExportInfos = new();
    }

    // area id
    public string AreaId;

    // 以列表形式序列化，兼容 Unity 序列化
    public List<ChunkItems> Buckets = new List<ChunkItems>();


    public List<NamedPoint> NamedPoints = new List<NamedPoint>();

    // 运行时便捷查询（可选）
    private Dictionary<(int x, int y), List<StaticItem>> _prefabMap;

    // 
    private Dictionary<string, NamedPoint> _namedPointMap;

    public NamedPoint FindNamedPointByName(string name)
    {
        if(_namedPointMap == null)
        {
            BuildRuntimeMap();
        }

        _namedPointMap.TryGetValue(name, out var point);
        return point;
    }
    public void BuildRuntimeMap()
    {
        _prefabMap = new Dictionary<(int x, int y), List<StaticItem>>();
        foreach (var b in Buckets)
        {
            var key = (b.Chunk.X, b.Chunk.Y);
            _prefabMap[key] = b.StaticItems;
        }

        _namedPointMap = new Dictionary<string, NamedPoint> ();
        foreach (var p in NamedPoints)
        {
            var key = p.Name;
            _namedPointMap[key] = p;
        }

        _roomMap = new Dictionary<(int x, int y), List<RoomExportInfo>>();
        foreach (var b in Buckets)
        {
            var key = (b.Chunk.X, b.Chunk.Y);
            _roomMap[key] = b.RoomExportInfos;
        }
    }

    public IEnumerable<StaticItem> GetChunkStaticItems(int x, int y)
    {
        if (_prefabMap == null) BuildRuntimeMap();
        if (_prefabMap.TryGetValue((x, y), out var list)) return list;
        return Array.Empty<StaticItem>();
    }

    private Dictionary<(int x, int y), List<RoomExportInfo>> _roomMap;
    public IEnumerable<RoomExportInfo> GetChunkRooms(int x, int y)
    {
        if (_roomMap == null) BuildRuntimeMap();
        if (_roomMap.TryGetValue((x, y), out var list)) return list;
        return Array.Empty<RoomExportInfo>();
    }
    

}