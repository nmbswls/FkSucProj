using My.Map.Logic;
using My.MapExport;
using UnityEngine;

namespace My.Dungeon
{
    public class DungeonAreaRuntime
    {
        private GameLogicAreaManager _area;
        private DungeonRoomSpatialIndex _spatialIndex;
        private DungeonRoomController _roomController;

        public static DungeonAreaRuntime Create(
            GameLogicAreaManager area,
            GameLogicManager logicManager,
            DungeonGenerationResult genResult)
        {
            var runtime = new DungeonAreaRuntime
            {
                _area = area,
                _spatialIndex = DungeonRoomSpatialIndex.Build(genResult),
            };
            runtime._roomController = new DungeonRoomController(runtime, logicManager, runtime._spatialIndex);

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
    }
}
