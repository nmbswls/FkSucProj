using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    // 坐标与 16 邻接 mask（bit0 左下 bit1 右下 bit2 左上 bit3 右上）
    public static class DualGridCore
    {
        public const int CornerMaskCount = 16;

        static readonly Vector3Int[] CellScratch = new Vector3Int[4];

        public static Vector3 GetViewLocalOffset(Vector3 cellSize)
        {
            return new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
        }

        public static void GetViewCornersAroundLogicCell(Vector3Int logicCell, Vector3Int[] buffer)
        {
            buffer[0] = new Vector3Int(logicCell.x, logicCell.y, logicCell.z);
            buffer[1] = new Vector3Int(logicCell.x - 1, logicCell.y, logicCell.z);
            buffer[2] = new Vector3Int(logicCell.x, logicCell.y - 1, logicCell.z);
            buffer[3] = new Vector3Int(logicCell.x - 1, logicCell.y - 1, logicCell.z);
        }

        public static int StableHash(Vector3Int cell)
        {
            unchecked
            {
                return cell.x * 73856093 ^ cell.y * 19349663 ^ cell.z * 83492791;
            }
        }

        public static int ComputeCornerMask(Tilemap data, DualGridBrushRegistry registry, Vector3Int viewCell, byte terrainId)
        {
            if (data == null || registry == null || terrainId == 0)
            {
                return 0;
            }

            CellScratch[0] = new Vector3Int(viewCell.x, viewCell.y, viewCell.z);
            CellScratch[1] = new Vector3Int(viewCell.x + 1, viewCell.y, viewCell.z);
            CellScratch[2] = new Vector3Int(viewCell.x, viewCell.y + 1, viewCell.z);
            CellScratch[3] = new Vector3Int(viewCell.x + 1, viewCell.y + 1, viewCell.z);

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
            buffer[0] = new Vector3Int(viewCell.x, viewCell.y, viewCell.z);
            buffer[1] = new Vector3Int(viewCell.x + 1, viewCell.y, viewCell.z);
            buffer[2] = new Vector3Int(viewCell.x, viewCell.y + 1, viewCell.z);
            buffer[3] = new Vector3Int(viewCell.x + 1, viewCell.y + 1, viewCell.z);
        }
    }
}
