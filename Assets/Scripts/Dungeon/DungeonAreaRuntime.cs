using System.Collections.Generic;
using My.Map.Logic;
using My.MapExport;
using UnityEngine;

namespace My.Dungeon
{
    // 地牢 overlay 运行时：房间分区、进房激活、刷怪门控
    public class DungeonAreaRuntime
    {
        private GameLogicAreaManager _area;
        private RoomSpatialIndex _spatialIndex;
        private RoomController _roomController;

        public static DungeonAreaRuntime Create(
            GameLogicAreaManager area,
            GameLogicManager logicManager,
            DungeonGenerationResult genResult)
        {
            var runtime = new DungeonAreaRuntime
            {
                _area = area,
                _spatialIndex = RoomSpatialIndex.Build(genResult),
            };
            runtime._roomController = new RoomController(runtime, logicManager, runtime._spatialIndex);

            foreach (var room in genResult.Rooms)
            {
                var roomId = room.GraphNodeId.ToString();
                area.RuntimeRoomInfos[roomId] = new LogicRoomInfo { RoomId = roomId };
            }

            return runtime;
        }

        public void Tick(float dt)
        {
            _roomController?.Tick(dt);
        }

        public void Dispose()
        {
            _roomController = null;
            _spatialIndex = null;
            _area = null;
        }

        public bool ShouldAllowSpawn(DynamicEntityRefreshInfo refreshInfo)
        {
            if (refreshInfo == null || refreshInfo.DungeonNodeId < 0)
            {
                return true;
            }

            switch (refreshInfo.SpawnPolicy)
            {
                case EDungeonSpawnPolicy.Immediate:
                    return true;
                case EDungeonSpawnPolicy.OnRoomEnter:
                    return _roomController != null &&
                           _roomController.IsRoomActivated(refreshInfo.DungeonNodeId);
                case EDungeonSpawnPolicy.OnRoomClear:
                    return false;
                default:
                    return true;
            }
        }

        public void NotifyRoomRefresh(int dungeonNodeId)
        {
            if (_area == null)
            {
                return;
            }

            foreach (var refreshInfo in _area.EntityRefreshInfo)
            {
                if (refreshInfo.DungeonNodeId != dungeonNodeId)
                {
                    continue;
                }

                _area.HandleOneRefreshInfo(refreshInfo);
            }
        }

        public LogicRoomInfo TryGetRoomByPos(Vector2 logicPos)
        {
            if (_spatialIndex == null ||
                !_spatialIndex.TryGetRoomNodeId(logicPos, out var nodeId))
            {
                return null;
            }

            var roomId = nodeId.ToString();
            if (_area != null && _area.RuntimeRoomInfos.TryGetValue(roomId, out var info))
            {
                return info;
            }

            return new LogicRoomInfo { RoomId = roomId };
        }

        private sealed class RoomController
        {
            private readonly DungeonAreaRuntime _runtime;
            private readonly GameLogicManager _logicManager;
            private readonly RoomSpatialIndex _spatialIndex;
            private readonly HashSet<int> _activatedRooms = new();

            private int _currentNodeId = -1;
            private float _checkTimer;

            public RoomController(
                DungeonAreaRuntime runtime,
                GameLogicManager logicManager,
                RoomSpatialIndex spatialIndex)
            {
                _runtime = runtime;
                _logicManager = logicManager;
                _spatialIndex = spatialIndex;
                ActivateRoom(0);
            }

            public bool IsRoomActivated(int nodeId)
            {
                return nodeId >= 0 && _activatedRooms.Contains(nodeId);
            }

            public void Tick(float dt)
            {
                if (_logicManager?.playerLogicEntity == null || _spatialIndex == null)
                {
                    return;
                }

                _checkTimer -= dt;
                if (_checkTimer > 0f)
                {
                    return;
                }

                _checkTimer = 0.3f;
                var playerPos = _logicManager.playerLogicEntity.Pos;
                if (!_spatialIndex.TryGetRoomNodeId(playerPos, out var nodeId))
                {
                    if (_currentNodeId >= 0)
                    {
                        _currentNodeId = -1;
                    }

                    return;
                }

                if (_currentNodeId == nodeId)
                {
                    return;
                }

                _currentNodeId = nodeId;
                ActivateRoom(nodeId);
            }

            private void ActivateRoom(int nodeId)
            {
                if (nodeId < 0 || !_activatedRooms.Add(nodeId))
                {
                    return;
                }

                _runtime.NotifyRoomRefresh(nodeId);
            }
        }

        private sealed class RoomSpatialIndex
        {
            private readonly Dictionary<Vector3Int, int> _cellToNodeId = new();
            private readonly Dictionary<int, List<Vector3Int>> _nodeCells = new();

            public static RoomSpatialIndex Build(DungeonGenerationResult result)
            {
                var index = new RoomSpatialIndex();
                if (result?.Rooms == null)
                {
                    return index;
                }

                foreach (var room in result.Rooms)
                {
                    if (room.Meta == null)
                    {
                        continue;
                    }

                    var cells = new List<Vector3Int>();
                    var meta = room.Meta;
                    for (int y = 0; y < meta.SizeCells.y; y++)
                    {
                        for (int x = 0; x < meta.SizeCells.x; x++)
                        {
                            if (!meta.IsWalkableLocal(x, y))
                            {
                                continue;
                            }

                            var worldCell = new Vector3Int(
                                room.GridOriginCells.x + x,
                                room.GridOriginCells.y + y,
                                0);
                            index._cellToNodeId[worldCell] = room.GraphNodeId;
                            cells.Add(worldCell);
                        }
                    }

                    index._nodeCells[room.GraphNodeId] = cells;
                }

                return index;
            }

            public bool TryGetRoomNodeId(Vector2 worldPos, out int nodeId)
            {
                var cell = new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
                return _cellToNodeId.TryGetValue(cell, out nodeId);
            }
        }
    }

    // 怪物房随机刷怪格选取
    public static class DungeonRoomSpawnUtil
    {
        public static List<Vector2Int> CollectInteriorSpawnCells(PlacedRoom room)
        {
            var result = new List<Vector2Int>();
            if (room.Meta == null)
            {
                return result;
            }

            var meta = room.Meta;
            var excluded = BuildDoorExclusion(meta);

            for (int y = 1; y < meta.SizeCells.y - 1; y++)
            {
                for (int x = 1; x < meta.SizeCells.x - 1; x++)
                {
                    if (!meta.IsWalkableLocal(x, y))
                    {
                        continue;
                    }

                    if (excluded.Contains(new Vector2Int(x, y)))
                    {
                        continue;
                    }

                    result.Add(new Vector2Int(
                        room.GridOriginCells.x + x,
                        room.GridOriginCells.y + y));
                }
            }

            return result;
        }

        public static List<Vector2Int> PickRandomCells(List<Vector2Int> pool, int count, DungeonRng rng)
        {
            var picked = new List<Vector2Int>();
            if (pool == null || pool.Count == 0 || count <= 0)
            {
                return picked;
            }

            var work = new List<Vector2Int>(pool);
            count = Mathf.Min(count, work.Count);
            for (int i = work.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (work[i], work[j]) = (work[j], work[i]);
            }

            for (int i = 0; i < count; i++)
            {
                picked.Add(work[i]);
            }

            return picked;
        }

        private static HashSet<Vector2Int> BuildDoorExclusion(DungeonRoomExportMeta meta)
        {
            var excluded = new HashSet<Vector2Int>();
            if (meta.DoorSockets == null)
            {
                return excluded;
            }

            foreach (var door in meta.DoorSockets)
            {
                for (int i = 0; i < Mathf.Max(1, door.Width); i++)
                {
                    int cx = door.LocalCell.x;
                    int cy = door.LocalCell.y;
                    if (door.Direction == EDungeonCardinalDir.South || door.Direction == EDungeonCardinalDir.North)
                    {
                        cx = door.LocalCell.x + i;
                    }
                    else
                    {
                        cy = door.LocalCell.y + i;
                    }

                    excluded.Add(new Vector2Int(cx, cy));
                    excluded.Add(GetDoorInnerCell(door, cx, cy, meta));
                }
            }

            return excluded;
        }

        private static Vector2Int GetDoorInnerCell(
            DungeonDoorSocketExport door,
            int cx,
            int cy,
            DungeonRoomExportMeta meta)
        {
            switch (door.Direction)
            {
                case EDungeonCardinalDir.South:
                    cy = Mathf.Min(meta.SizeCells.y - 2, cy + 1);
                    break;
                case EDungeonCardinalDir.North:
                    cy = Mathf.Max(1, cy - 1);
                    break;
                case EDungeonCardinalDir.West:
                    cx = Mathf.Min(meta.SizeCells.x - 2, cx + 1);
                    break;
                case EDungeonCardinalDir.East:
                    cx = Mathf.Max(1, cx - 1);
                    break;
            }

            return new Vector2Int(cx, cy);
        }
    }
}
