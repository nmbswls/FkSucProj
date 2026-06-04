using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    // View 层 localPosition = +0.5 cell（与 Data 同 Grid、Tilemap 以格心为锚）时：
    // 落笔 dataCell 后刷新四角 View：dataCell - CornerOffsets[i]
    // mask 采样见 DataCellsAroundView + PaletteBitForDataCell（与 Palette 示意图一致）
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

        // 落笔后除 4 个角点 View 外，再刷新外圈一圈（否则外缘 mask 不更新、会残留旧图）
        public static readonly Vector3Int[] ViewHaloOffsets =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, -1, 0),
        };

        static readonly Vector3Int[] CellScratch = new Vector3Int[4];

        // 四格相对 viewCell（顺序：东北、西北、东南、西南）
        static readonly Vector3Int[] DataCellsAroundView =
        {
            new Vector3Int(1, 1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 0, 0),
        };

        // Palette 示意上排为 bit0/1、下排为 bit2/3；与格点 Y 对齐（避免上下边沿对调）
        static readonly int[] PaletteBitForDataCell =
        {
            2, 3, 0, 1,
        };

        public static Vector3 GetViewLocalOffset(Vector3 cellSize)
        {
            return new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
        }

        public static void GetViewCornersAroundDataCell(Vector3Int dataCell, Vector3Int[] buffer)
        {
            for (int i = 0; i < 4; i++)
            {
                buffer[i] = dataCell - CornerOffsets[i];
            }
        }

        public static void GetDataCellsForViewCorner(Vector3Int viewCell, Vector3Int[] buffer)
        {
            for (int i = 0; i < 4; i++)
            {
                buffer[i] = viewCell + DataCellsAroundView[i];
            }
        }

        public static void CollectViewsToRefreshAroundDataCell(Vector3Int dataCell, System.Collections.Generic.HashSet<Vector3Int> output)
        {
            GetViewCornersAroundDataCell(dataCell, CellScratch);
            for (int i = 0; i < 4; i++)
            {
                var corner = CellScratch[i];
                output.Add(corner);
                for (int h = 0; h < ViewHaloOffsets.Length; h++)
                {
                    output.Add(corner + ViewHaloOffsets[h]);
                }
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

            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                var dataCell = viewCell + DataCellsAroundView[i];
                var brush = data.GetTile(dataCell);
                if (brush != null && registry.TryGetTerrainId(brush, out var id) && id == terrainId)
                {
                    mask |= 1 << PaletteBitForDataCell[i];
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
