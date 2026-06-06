using My.Map.DualGrid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Cliff
{
    // Dual Grid 坐标约定（与 DualGridCore 一致）：
    // - 南缘检测、深度/角判定：始终在 Data 格 (dataCell)
    // - Cliff 落砖格号：与 View 同格网（localPosition +0.5 cell）
    // - Data 南缘段 [dataXLeft..dataXRight] 宽 W → Cliff 列 [dataXLeft-1..dataXRight] 宽 W+1
    // - 落砖 Y = dataSouthEdgeY - 1 - rowIndex（紧贴南缘下方，不画到南缘格本身）
    public static class CliffDualGridMapping
    {
        public struct CliffColumnRange
        {
            public int DataXLeft;
            public int DataXRight;
            public int DataSouthEdgeY;
            public int Z;
            public int CliffXLeft;
            public int CliffXRight;
            public bool DualGrid;

            public int DataWidth => DataXRight - DataXLeft + 1;
            public int CliffWidth => CliffXRight - CliffXLeft + 1;
        }

        public static CliffColumnRange ResolveColumnRange(
            int dataXLeft,
            int dataXRight,
            int dataSouthEdgeY,
            int z,
            bool dualGrid)
        {
            ResolveCliffColumnRange(dataXLeft, dataXRight, dualGrid, out int cliffXLeft, out int cliffXRight);
            return new CliffColumnRange
            {
                DataXLeft = dataXLeft,
                DataXRight = dataXRight,
                DataSouthEdgeY = dataSouthEdgeY,
                Z = z,
                CliffXLeft = cliffXLeft,
                CliffXRight = cliffXRight,
                DualGrid = dualGrid,
            };
        }

        public static void ResolveCliffColumnRange(
            int dataXLeft,
            int dataXRight,
            bool dualGrid,
            out int cliffXLeft,
            out int cliffXRight)
        {
            cliffXLeft = dualGrid ? dataXLeft - 1 : dataXLeft;
            cliffXRight = dataXRight;
        }

        public static Vector3Int ResolveCliffCell(int cliffX, int dataSouthEdgeY, int rowIndex, int z)
        {
            return new Vector3Int(cliffX, dataSouthEdgeY - 1 - rowIndex, z);
        }

        public static bool IsWestCapColumn(int cliffX, int dataXLeft, bool dualGrid)
        {
            return dualGrid && cliffX < dataXLeft;
        }

        public static bool IsEastCapColumn(int cliffX, int dataXRight, bool dualGrid)
        {
            // 当前几何下南侧 View 行不需要东扩列
            return false;
        }

        // 西扩列无 Data 南缘格；其余列 cliffX 与 Data 南缘格 x 对齐
        public static bool TryResolveDataSouthEdgeCell(
            int cliffX,
            int dataXLeft,
            int dataSouthEdgeY,
            int z,
            bool dualGrid,
            out Vector3Int dataCell)
        {
            if (IsWestCapColumn(cliffX, dataXLeft, dualGrid))
            {
                dataCell = default;
                return false;
            }

            dataCell = new Vector3Int(cliffX, dataSouthEdgeY, z);
            return true;
        }

        // 首行 Cliff 覆盖的 View 格（南缘正下方一行）
        public static void CollectSouthFaceViewCells(
            in CliffColumnRange range,
            System.Collections.Generic.List<Vector3Int> output)
        {
            output.Clear();
            for (int x = range.CliffXLeft; x <= range.CliffXRight; x++)
            {
                output.Add(new Vector3Int(x, range.DataSouthEdgeY - 1, range.Z));
            }
        }

        // Data 南缘段首格对应的西南 View 格 = 西扩列首行 Cliff 格
        public static Vector3Int ResolveWestCapViewCell(in CliffColumnRange range)
        {
            return new Vector3Int(range.DataXLeft - 1, range.DataSouthEdgeY - 1, range.Z);
        }

        public static Vector3 GetCliffLocalOffset(Vector3 cellSize, bool useDualGridOffset)
        {
            return useDualGridOffset ? DualGridCore.GetViewLocalOffset(cellSize) : Vector3.zero;
        }

        public static void SyncCliffTilemapSettings(Tilemap data, Tilemap cliff)
        {
            if (data == null || cliff == null)
            {
                return;
            }

            cliff.tileAnchor = data.tileAnchor;
            cliff.orientation = data.orientation;
        }

        public static Vector3 ResolveCliffNorthEdgeWorld(Tilemap cliff, Vector3Int cliffCell, Vector3 cellSize)
        {
            var center = cliff.GetCellCenterWorld(cliffCell);
            return center + new Vector3(0f, cellSize.y * 0.5f, 0f);
        }

        public static Vector3 ResolveDataSouthEdgeWorld(Tilemap data, Vector3Int dataSouthEdgeCell)
        {
            return data.GetCellCenterWorld(dataSouthEdgeCell);
        }
    }
}
