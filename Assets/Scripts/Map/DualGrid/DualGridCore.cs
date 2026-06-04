using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    // View 角点 viewCell 对应 Data 四格（Tilemap y 向上）：
    //   bit0 = viewCell           右上
    //   bit1 = viewCell - (1,0)   左上
    //   bit2 = viewCell - (0,1)   右下
    //   bit3 = viewCell - (1,1)   左下
    // Data 格 dataCell 落笔后刷新的 View 角点：dataCell + (0,0),(1,0),(0,1),(1,1)
    public static class DualGridCore
    {
        public const int CornerMaskCount = 16;

        public static readonly Vector3Int[] CornerOffsets =
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
        };

        static readonly Vector3Int[] CellScratch = new Vector3Int[4];

        public static Vector3 GetViewLocalOffset(Vector3 cellSize)
        {
            return new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
        }

        public static void GetViewCornersAroundDataCell(Vector3Int dataCell, Vector3Int[] buffer)
        {
            for (int i = 0; i < 4; i++)
            {
                buffer[i] = dataCell + CornerOffsets[i];
            }
        }

        public static void GetDataCellsForViewCorner(Vector3Int viewCell, Vector3Int[] buffer)
        {
            for (int i = 0; i < 4; i++)
            {
                buffer[i] = viewCell - CornerOffsets[i];
            }
        }

        public static int StableHash(Vector3Int cell)
        {
            unchecked
            {
                return cell.x * 73856093 ^ cell.y * 19349663 ^ cell.z * 83492791;
            }
        }

        public static int ComputeCornerMask(
            Tilemap data,
            DualGridBrushRegistry registry,
            Vector3Int viewCell,
            byte terrainId)
        {
            if (data == null || registry == null || terrainId == 0)
            {
                return 0;
            }

            GetDataCellsForViewCorner(viewCell, CellScratch);

            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                var brush = data.GetTile(CellScratch[i]);
                if (brush != null && registry.TryGetTerrainId(brush, out var id) && id == terrainId)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        public static void GetLogicCellsForViewCorner(Vector3Int viewCell, Vector3Int[] buffer)
        {
            GetDataCellsForViewCorner(viewCell, buffer);
        }
    }
}
