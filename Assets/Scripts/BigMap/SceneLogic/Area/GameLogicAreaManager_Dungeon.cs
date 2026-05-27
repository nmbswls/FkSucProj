using My.Dungeon;
using My.MapExport;
using UnityEngine;

namespace My.Map.Logic
{
    public partial class GameLogicAreaManager
    {
        private DungeonRoomSpatialIndex _dungeonRoomSpatialIndex;
        private DungeonRoomController _dungeonRoomController;
        private bool _isDungeonOverlay;

        private void InitDungeonRuntime(DungeonGenerationResult genResult)
        {
            _isDungeonOverlay = true;
            _dungeonRoomSpatialIndex = DungeonRoomSpatialIndex.Build(genResult);
            _dungeonRoomController = new DungeonRoomController(this, logicManager, _dungeonRoomSpatialIndex);

            foreach (var room in genResult.Rooms)
            {
                var roomId = room.GraphNodeId.ToString();
                RuntimeRoomInfos[roomId] = new LogicRoomInfo { RoomId = roomId };
            }
        }

        private void ClearDungeonRuntime()
        {
            _isDungeonOverlay = false;
            _dungeonRoomController = null;
            _dungeonRoomSpatialIndex = null;
        }

        private void TickDungeonRoom(float dt)
        {
            _dungeonRoomController?.Tick(dt);
        }

        public bool ShouldSpawnByDungeonPolicy(DynamicEntityRefreshInfo refreshInfo)
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
                    return _dungeonRoomController != null &&
                           _dungeonRoomController.IsRoomActivated(refreshInfo.DungeonNodeId);
                case EDungeonSpawnPolicy.OnRoomClear:
                    return false;
                default:
                    return true;
            }
        }

        public void ForceCheckDungeonRoomRefresh(int dungeonNodeId)
        {
            foreach (var refreshInfo in EntityRefreshInfo)
            {
                if (refreshInfo.DungeonNodeId != dungeonNodeId)
                {
                    continue;
                }

                HandleOneRefreshInfo(refreshInfo);
            }
        }

        public bool IsDungeonOverlayActive => _isDungeonOverlay;
    }
}
