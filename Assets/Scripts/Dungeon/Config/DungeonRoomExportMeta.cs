using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
    [CreateAssetMenu(fileName = "DungeonRoomExportMeta", menuName = "Dungeon/Room Export Meta")]
    public class DungeonRoomExportMeta : ScriptableObject
    {
        public string TemplateId = string.Empty;
        public EDungeonRoomRole Role = EDungeonRoomRole.Combat;
        public int Weight = 1;
        public Vector2Int SizeCells = new(12, 10);

        public byte[] WalkableMask = Array.Empty<byte>();

        public List<DungeonDoorSocketExport> DoorSockets = new();
        public List<DungeonEntitySlotExport> EntitySlots = new();

        public bool IsWalkableLocal(int x, int y)
        {
            if (x < 0 || y < 0 || x >= SizeCells.x || y >= SizeCells.y)
            {
                return false;
            }

            int idx = y * SizeCells.x + x;
            if (WalkableMask == null || idx >= WalkableMask.Length)
            {
                return false;
            }

            return WalkableMask[idx] != 0;
        }

        public void EnsureMaskSize()
        {
            int need = SizeCells.x * SizeCells.y;
            if (WalkableMask == null || WalkableMask.Length != need)
            {
                WalkableMask = new byte[need];
            }
        }

        public Vector2Int GetBornLocalCell()
        {
            if (DoorSockets != null && DoorSockets.Count > 0)
            {
                var door = DoorSockets[0];
                int cx = door.LocalCell.x + Mathf.Max(0, door.Width / 2);
                int cy = door.LocalCell.y;
                switch (door.Direction)
                {
                    case EDungeonCardinalDir.South:
                        cy = Mathf.Min(SizeCells.y - 2, door.LocalCell.y + 1);
                        break;
                    case EDungeonCardinalDir.North:
                        cy = Mathf.Max(1, door.LocalCell.y - 1);
                        break;
                    case EDungeonCardinalDir.West:
                        cx = Mathf.Min(SizeCells.x - 2, door.LocalCell.x + 1);
                        break;
                    case EDungeonCardinalDir.East:
                        cx = Mathf.Max(1, door.LocalCell.x - 1);
                        break;
                }

                if (IsWalkableLocal(cx, cy))
                {
                    return new Vector2Int(cx, cy);
                }
            }

            for (int y = 1; y < SizeCells.y - 1; y++)
            {
                for (int x = 1; x < SizeCells.x - 1; x++)
                {
                    if (IsWalkableLocal(x, y))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return new Vector2Int(SizeCells.x / 2, SizeCells.y / 2);
        }
    }
}
