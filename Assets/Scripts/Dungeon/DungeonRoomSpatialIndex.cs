using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
    public class DungeonRoomSpatialIndex
    {
        private readonly Dictionary<Vector3Int, int> _cellToNodeId = new();
        private readonly Dictionary<int, List<Vector3Int>> _nodeCells = new();

        public static DungeonRoomSpatialIndex Build(DungeonGenerationResult result)
        {
            var index = new DungeonRoomSpatialIndex();
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

        public bool TryGetRoomNodeId(Vector3Int cell, out int nodeId)
        {
            return _cellToNodeId.TryGetValue(cell, out nodeId);
        }

        public IReadOnlyList<Vector3Int> GetRoomCells(int nodeId)
        {
            if (_nodeCells.TryGetValue(nodeId, out var cells))
            {
                return cells;
            }

            return System.Array.Empty<Vector3Int>();
        }
    }
}
