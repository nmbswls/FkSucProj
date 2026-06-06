using My.Map.DualGrid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Cliff
{
    // Dual Grid：Data(0,0) 画台地；View(+0.5) 南缘检测；Cliff(+0.5) 与 View 格 1:1 落砖
    public static class CliffDualGridMapping
    {
        // row 0 在 topAnchorY（由 ClassifyColumn 按左右邻接 Y 取 max），向下叠 rowIndex
        public static Vector3Int ResolveCliffCell(int viewX, int topAnchorY, int rowIndex, int z)
        {
            return new Vector3Int(viewX, topAnchorY - rowIndex, z);
        }

        public static Vector3 GetCliffLocalOffset(Vector3 cellSize, bool useDualGridOffset)
        {
            return useDualGridOffset ? DualGridCore.GetViewLocalOffset(cellSize) : Vector3.zero;
        }

        public static void SyncCliffTilemapSettings(Tilemap view, Tilemap cliff)
        {
            if (view == null || cliff == null)
            {
                return;
            }

            cliff.tileAnchor = view.tileAnchor;
            cliff.orientation = view.orientation;
        }

        public static Vector3 ResolveCliffNorthEdgeWorld(Tilemap cliff, Vector3Int cliffCell, Vector3 cellSize)
        {
            var center = cliff.GetCellCenterWorld(cliffCell);
            return center + new Vector3(0f, cellSize.y * 0.5f, 0f);
        }

        public static Vector3 ResolveViewSouthEdgeWorld(Tilemap view, Vector3Int viewSouthEdgeCell)
        {
            return view.GetCellCenterWorld(viewSouthEdgeCell);
        }
    }
}
