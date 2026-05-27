using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
    public class DungeonRoomController
    {
        private readonly DungeonAreaRuntime _runtime;
        private readonly GameLogicManager _logicManager;
        private readonly DungeonRoomSpatialIndex _spatialIndex;
        private readonly HashSet<int> _activatedRooms = new();

        private int _currentNodeId = -1;
        private float _checkTimer;

        public DungeonRoomController(
            DungeonAreaRuntime runtime,
            GameLogicManager logicManager,
            DungeonRoomSpatialIndex spatialIndex)
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

        public int CurrentNodeId => _currentNodeId;

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
            OnRoomEntered(nodeId);
        }

        public void ActivateRoom(int nodeId)
        {
            if (nodeId < 0 || !_activatedRooms.Add(nodeId))
            {
                return;
            }

            _runtime.NotifyRoomRefresh(nodeId);
        }

        private void OnRoomEntered(int nodeId)
        {
            ActivateRoom(nodeId);
        }
    }
}
