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

            var dataCell = map.WorldToDataCell(world);
            var viewCell = map.ViewTilemap != null ? map.WorldToViewCell(world) : dataCell;

            var logicCenter = map.DataTilemap.GetCellCenterWorld(dataCell);
            var viewCenter = map.ViewTilemap != null
                ? map.ViewTilemap.GetCellCenterWorld(viewCell)
                : logicCenter;

            Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.9f);
            var grid = map.ResolveGrid();
            var cellSize = grid != null ? grid.cellSize : Vector3.one;
            Handles.DrawWireCube(logicCenter, cellSize);

            Handles.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);
            var viewSize = cellSize * 0.35f;
            Handles.DrawWireCube(viewCenter, new Vector3(viewSize.x, viewSize.y, 0.01f));

            var label = $"Data {dataCell} | View {viewCell}";
            if (map.TryResolveViewCorner(viewCell, out byte tid, out int mask))
            {
                label += $"\nResolve(view) T{tid} mask {mask}";
            }
            else if (map.TryResolveAtDataCell(dataCell, out tid, out mask))
            {
                label += $"\nResolve(data) T{tid} mask {mask}";
            }

            Handles.Label(logicCenter, label);
        }
    }
}
#endif
