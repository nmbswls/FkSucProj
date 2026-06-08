#if UNITY_EDITOR
using System.Collections.Generic;
using My.Map.DualGrid;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid.Editor
{
    // Unity 画笔拖拽时 SetTiles(notify:false)，tilemapTileChanged 仅在 MouseUp 后触发。
    // 在绘制过程中按 data 格增量刷新 View，只更新受影响角落。
    [InitializeOnLoad]
    static class DualGridLivePaintRefresh
    {
        static readonly HashSet<Vector3Int> ScratchCells = new HashSet<Vector3Int>();
        static Vector3Int? _lastPaintCell;
        static DualTileMap _activeMap;

        static DualGridLivePaintRefresh()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView view)
        {
            if (Application.isPlaying || !InTilemapPaintMode())
            {
                ResetStroke();
                return;
            }

            var e = Event.current;
            if (e == null)
            {
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    if (!IsPaintingOrErasing(e))
                    {
                        return;
                    }

                    TryRefreshPaintStroke();
                    break;
                case EventType.MouseUp:
                    if (_activeMap != null && _lastPaintCell.HasValue)
                    {
                        TryRefreshPaintStroke();
                    }

                    ResetStroke();
                    break;
            }
        }

        static bool InTilemapPaintMode()
        {
            if (!GridPaintingState.isEditing || GridPaintingState.scenePaintTarget == null)
            {
                return false;
            }

            var activeType = ToolManager.activeToolType;
            return activeType != null && activeType.IsSubclassOf(typeof(TilemapEditorTool));
        }

        static bool IsPaintingOrErasing(Event evt)
        {
            return IsToolActive(typeof(PaintTool))
                   || IsToolActive(typeof(EraseTool))
                   || (evt.button == 0 && evt.shift);
        }

        static bool IsToolActive(System.Type toolType)
        {
            return ToolManager.activeToolType == toolType;
        }

        static void TryRefreshPaintStroke()
        {
            var map = ResolveActiveMap();
            if (map == null)
            {
                return;
            }

            if (!TryGetPaintDataCell(map, out var currentCell))
            {
                return;
            }

            ScratchCells.Clear();
            if (_lastPaintCell.HasValue && _lastPaintCell.Value != currentCell)
            {
                foreach (var point in GetPointsOnLine(
                             new Vector2Int(_lastPaintCell.Value.x, _lastPaintCell.Value.y),
                             new Vector2Int(currentCell.x, currentCell.y)))
                {
                    CollectBrushFootprint(new Vector3Int(point.x, point.y, currentCell.z), ScratchCells);
                }
            }
            else
            {
                CollectBrushFootprint(currentCell, ScratchCells);
            }

            if (ScratchCells.Count > 0)
            {
                map.QueueEditorLiveDataCells(ScratchCells);
                SceneView.RepaintAll();
            }

            _lastPaintCell = currentCell;
            _activeMap = map;
        }

        static DualTileMap ResolveActiveMap()
        {
            var paintTarget = GridPaintingState.scenePaintTarget;
            if (paintTarget == null)
            {
                return null;
            }

            var tilemap = paintTarget.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                return null;
            }

            var map = paintTarget.GetComponentInParent<DualTileMap>();
            if (map == null || map.DataTilemap != tilemap)
            {
                return null;
            }

            if (!map.AutoRefreshInEditor || !map.IsConfigured(out _))
            {
                return null;
            }

            return map;
        }

        static bool TryGetPaintDataCell(DualTileMap map, out Vector3Int cell)
        {
            cell = default;
            if (map.DataTilemap == null)
            {
                return false;
            }

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var plane = new Plane(Vector3.forward, map.DataTilemap.transform.position);
            if (!plane.Raycast(ray, out float dist))
            {
                return false;
            }

            cell = map.DataTilemap.WorldToCell(ray.GetPoint(dist));
            return true;
        }

        static void CollectBrushFootprint(Vector3Int anchorCell, HashSet<Vector3Int> output)
        {
            var brush = GridPaintingState.gridBrush;
            if (brush is GridBrush gridBrush && gridBrush.cellCount > 0)
            {
                var min = anchorCell - gridBrush.pivot;
                var size = gridBrush.size;
                for (int z = 0; z < size.z; z++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        for (int x = 0; x < size.x; x++)
                        {
                            output.Add(min + new Vector3Int(x, y, z));
                        }
                    }
                }

                return;
            }

            output.Add(anchorCell);
        }

        // Bresenham，与 Unity GridEditorUtility.GetPointsOnLine 一致（该类为 internal）。
        static IEnumerable<Vector2Int> GetPointsOnLine(Vector2Int p1, Vector2Int p2)
        {
            int x0 = p1.x;
            int y0 = p1.y;
            int x1 = p2.x;
            int y1 = p2.y;

            bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
            if (steep)
            {
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }

            int dx = x1 - x0;
            int dy = Mathf.Abs(y1 - y0);
            int error = dx / 2;
            int ystep = y0 < y1 ? 1 : -1;
            int y = y0;
            for (int x = x0; x <= x1; x++)
            {
                yield return steep ? new Vector2Int(y, x) : new Vector2Int(x, y);
                error -= dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }
        }

        static void ResetStroke()
        {
            _lastPaintCell = null;
            _activeMap = null;
            ScratchCells.Clear();
        }
    }
}
#endif
