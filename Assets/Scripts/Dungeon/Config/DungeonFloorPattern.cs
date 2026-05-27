using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Dungeon
{
    public enum EDungeonFloorPatternKind
    {
        Base,
        Accent,
    }

    [Serializable]
    public struct DungeonFloorPatternCell
    {
        public Vector2Int LocalOffset;
        public TileBase Tile;
    }

    [CreateAssetMenu(fileName = "DungeonFloorPattern", menuName = "Dungeon/Floor Pattern")]
    public class DungeonFloorPattern : ScriptableObject
    {
        public string PatternId = string.Empty;
        public EDungeonFloorPatternKind Kind = EDungeonFloorPatternKind.Base;
        public Vector2Int SizeCells = Vector2Int.one;
        public Vector2Int Anchor = Vector2Int.zero;
        public List<DungeonFloorPatternCell> Cells = new();
        public int Weight = 1;
        public int SizePriority = 10;

        public IEnumerable<Vector3Int> EnumerateWorldCells(Vector3Int anchor)
        {
            if (Cells == null)
            {
                yield break;
            }

            foreach (var cell in Cells)
            {
                yield return new Vector3Int(
                    anchor.x + cell.LocalOffset.x - Anchor.x,
                    anchor.y + cell.LocalOffset.y - Anchor.y,
                    0);
            }
        }
    }
}
