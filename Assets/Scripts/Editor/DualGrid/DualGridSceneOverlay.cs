#if UNITY_EDITOR
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

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

            if (map == null)
            {
                map = Object.FindObjectOfType<DualTileMap>();
            }

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
            var viewTm = map.ViewLayers != null && map.ViewLayers.Length > 0
                ? map.ViewLayers[0].ViewTilemap
                : null;
            var grid = map.ResolveGrid();
            var cellSize = grid != null ? grid.cellSize : Vector3.one;

            var viewCenter = viewTm != null
                ? viewTm.GetCellCenterWorld(viewCell)
                : logicCenter + DualGridCore.GetViewLocalOffset(cellSize);

            Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.9f);
            Handles.DrawWireCube(logicCenter, cellSize);

            Handles.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);
            var viewSize = cellSize * 0.35f;
            Handles.DrawWireCube(viewCenter, new Vector3(viewSize.x, viewSize.y, 0.01f));

            Handles.Label(logicCenter, $"Logic {logicCell}\nView {viewCell}");
        }
    }
}
#endif
