using System.Collections.Generic;
using System;
using UnityEngine;
using My.Map.Scene;
using My.Map;
using My.Map.Entity;


namespace My.MapExport
{

    [CreateAssetMenu(fileName = "MapExportDatabase", menuName = "MapExport/Chunk Static Database")]
    public class MapExportDatabase : ScriptableObject
    {

        


        


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
        public class ChunkExportItem
        {
            public ChunkKey Chunk;
            public List<StaticPrefabItem> StaticItems = new List<StaticPrefabItem>();
            public List<Segment2D> FovStaticSegments = new();
        }


        #region 基地相关



        #endregion

        // area id
        public string AreaId;

        // 以列表形式序列化，兼容 Unity 序列化
        public List<ChunkExportItem> Buckets = new List<ChunkExportItem>();

        public List<DynamicEntityRefreshInfo> EntityRefreshInfo = new List<DynamicEntityRefreshInfo>();


        public List<NamedPoint> NamedPoints = new List<NamedPoint>();
        public List<NamedPath> NamedPaths = new List<NamedPath>();

        // 运行时便捷查询（可选）
        private Dictionary<(int x, int y), List<StaticPrefabItem>> _prefabMap;

        private Dictionary<(int x, int y), List<Segment2D>> _segmentMap;

        // 
        private Dictionary<string, NamedPoint?> _namedPointMap;
        private Dictionary<string, NamedPath?> _namedPathMap;
        public NamedPoint? FindNamedPointByName(string name)
        {
            if (_namedPointMap == null)
            {
                BuildRuntimeMap();
            }

            _namedPointMap.TryGetValue(name, out var point);
            return point;
        }

        public NamedPath? FindNamedPathByName(string name)
        {
            if (_namedPathMap == null)
            {
                BuildRuntimeMap();
            }

            _namedPathMap.TryGetValue(name, out var path);
            return path;
        }

        public void BuildRuntimeMap()
        {
            _prefabMap = new Dictionary<(int x, int y), List<StaticPrefabItem>>();
            foreach (var b in Buckets)
            {
                var key = (b.Chunk.X, b.Chunk.Y);
                _prefabMap[key] = b.StaticItems;
            }

            _namedPointMap = new Dictionary<string, NamedPoint?>();
            foreach (var p in NamedPoints)
            {
                var key = p.Name;
                _namedPointMap[key] = p;
            }

            _namedPathMap = new Dictionary<string, NamedPath?>();
            foreach (var p in NamedPaths)
            {
                var key = p.Name;
                _namedPathMap[key] = p;
            }

            //_roomMap = new Dictionary<(int x, int y), List<RoomExportInfo>>();
            //foreach (var b in Buckets)
            //{
            //    var key = (b.Chunk.X, b.Chunk.Y);
            //    _roomMap[key] = b.RoomExportInfos;
            //}

            _segmentMap = new Dictionary<(int x, int y), List<Segment2D>>();
            foreach (var b in Buckets)
            {
                var key = (b.Chunk.X, b.Chunk.Y);
                _segmentMap[key] = b.FovStaticSegments;
            }
        }

        public IEnumerable<StaticPrefabItem> GetChunkStaticItems(int x, int y)
        {
            if (_prefabMap == null) BuildRuntimeMap();
            if (_prefabMap.TryGetValue((x, y), out var list)) return list;
            return Array.Empty<StaticPrefabItem>();
        }

        public IEnumerable<Segment2D> GetChunkSegments(int x, int y)
        {
            if (_segmentMap == null) BuildRuntimeMap();
            if (_segmentMap.TryGetValue((x, y), out var list)) return list;
            return Array.Empty<Segment2D>();
        }

        private Dictionary<(int x, int y), List<RoomExportInfo>> _roomMap;
        public IEnumerable<RoomExportInfo> GetChunkRooms(int x, int y)
        {
            if (_roomMap == null) BuildRuntimeMap();
            if (_roomMap.TryGetValue((x, y), out var list)) return list;
            return Array.Empty<RoomExportInfo>();
        }


    }
}

