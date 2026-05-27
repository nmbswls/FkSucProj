using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
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
