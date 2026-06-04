#if UNITY_EDITOR
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;

namespace My.Map.DualGrid.Editor
{
    [InitializeOnLoad]
    static class DualGridSceneOverlay
    {
        static DualGridSceneOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView view)
        {
            var map = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<DualTileMap>()
                : null;

            map ??= Object.FindObjectOfType<DualTileMap>();

            if (map == null || map.DataTilemap == null)
            {
                return;
            }

            var e = Event.current;
            if (e == null || e.type != EventType.MouseMove && e.type != EventType.Repaint)
            {
                return;
            }

            var world = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            world.z = 0;
            var logicCell = map.DataTilemap.WorldToCell(world);
            var viewCell = logicCell;

            var logicCenter = map.DataTilemap.GetCellCenterWorld(logicCell);
            var grid = map.ResolveGrid();
            var cellSize = grid != null ? grid.cellSize : Vector3.one;

            var viewCenter = map.ViewTilemap != null
                ? map.ViewTilemap.GetCellCenterWorld(viewCell)
                : logicCenter + DualGridCore.GetViewLocalOffset(cellSize);

            Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.9f);
            Handles.DrawWireCube(logicCenter, cellSize);

            Handles.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);
            var viewSize = cellSize * 0.35f;
            Handles.DrawWireCube(viewCenter, new Vector3(viewSize.x, viewSize.y, 0.01f));

            var label = $"Logic {logicCell}\nView {viewCell}";
            if (map.BrushRegistry != null
                && map.BrushRegistry.TryResolveViewCorner(map.DataTilemap, viewCell, out byte tid, out int mask))
            {
                label += $"\nShow T{tid} mask {mask}";
            }

            Handles.Label(logicCenter, label);
        }
    }
}
#endif
