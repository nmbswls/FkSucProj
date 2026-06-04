#if UNITY_EDITOR
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid.Editor
{
    [CustomEditor(typeof(DualTileMap))]
    public class DualTileMapEditor : UnityEditor.Editor
    {
        Vector3Int _probeLogicCell;
        Vector3Int _probeViewCell;
        bool _useLogicProbe = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var map = (DualTileMap)target;
            if (map.ResolveGrid() == null)
            {
                EditorGUILayout.HelpBox(
                    "未找到 Grid：请在父物体上添加 Grid，或在本组件 Grid 字段手动指定。",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Hierarchy"))
            {
                CreateHierarchy(map);
            }
            if (GUILayout.Button("Refresh All"))
            {
                map.EnsureViewOffset();
                map.RefreshAll();
                EditorUtility.SetDirty(map);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus Data"))
            {
                FocusTilemap(map.DataTilemap);
            }
            if (GUILayout.Button("Focus View"))
            {
                var view = map.ViewLayers != null && map.ViewLayers.Length > 0
                    ? map.ViewLayers[0].ViewTilemap
                    : null;
                FocusTilemap(view);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Coordinate Probe", EditorStyles.boldLabel);
            _useLogicProbe = EditorGUILayout.Toggle("Probe Logic Cell", _useLogicProbe);
            if (_useLogicProbe)
            {
                _probeLogicCell = EditorGUILayout.Vector3IntField("Logic Cell", _probeLogicCell);
                DrawLogicProbe(map, _probeLogicCell);
            }
            else
            {
                _probeViewCell = EditorGUILayout.Vector3IntField("View Cell", _probeViewCell);
                DrawViewProbe(map, _probeViewCell);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static void CreateHierarchy(DualTileMap map)
        {
            Undo.RegisterFullObjectHierarchyUndo(map.gameObject, "Create Dual Grid Hierarchy");

            map.Grid = map.ResolveGrid();

            var dataGo = GetOrCreateChild(map.transform, "Data");
            var dataTm = GetOrCreateTilemap(dataGo);

            var viewGo = GetOrCreateChild(map.transform, "View");
            var viewTm = GetOrCreateTilemap(viewGo);

            map.DataTilemap = dataTm;

            if (map.ViewLayers == null || map.ViewLayers.Length == 0)
            {
                map.ViewLayers = new[]
                {
                    new DualTileMap.ViewLayer
                    {
                        ViewTilemap = viewTm,
                    },
                };
            }
            else
            {
                map.ViewLayers[0].ViewTilemap = viewTm;
            }

            map.EnsureViewOffset();
            EditorUtility.SetDirty(map);
        }

        static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null)
            {
                return t.gameObject;
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static Tilemap GetOrCreateTilemap(GameObject go)
        {
            var tm = go.GetComponent<Tilemap>();
            if (tm == null)
            {
                tm = Undo.AddComponent<Tilemap>(go);
            }

            if (go.GetComponent<TilemapRenderer>() == null)
            {
                Undo.AddComponent<TilemapRenderer>(go);
            }

            return tm;
        }

        static void FocusTilemap(Tilemap tilemap)
        {
            if (tilemap == null)
            {
                return;
            }

            Selection.activeGameObject = tilemap.gameObject;
            SceneView.FrameLastActiveSceneView();
        }

        static void DrawLogicProbe(DualTileMap map, Vector3Int logicCell)
        {
            if (map.DataTilemap == null)
            {
                return;
            }

            var viewCell = logicCell;
            EditorGUILayout.LabelField($"View corner: {viewCell}", EditorStyles.miniLabel);

            if (map.BrushRegistry == null || map.ViewLayers == null)
            {
                return;
            }

            for (int i = 0; i < map.ViewLayers.Length; i++)
            {
                var layer = map.ViewLayers[i];
                if (layer?.Palette == null)
                {
                    continue;
                }

                byte tid = layer.Palette.TerrainId;
                int mask = DualGridCore.ComputeCornerMask(map.DataTilemap, map.BrushRegistry, viewCell, tid);
                EditorGUILayout.LabelField($"Layer {i} TerrainId={tid} mask={mask} ({ToBinary(mask)})", EditorStyles.miniLabel);
            }
        }

        static void DrawViewProbe(DualTileMap map, Vector3Int viewCell)
        {
            var buffer = new Vector3Int[4];
            DualGridCore.GetLogicCellsForViewCorner(viewCell, buffer);
            EditorGUILayout.LabelField(
                $"Logic cells: {buffer[0]}, {buffer[1]}, {buffer[2]}, {buffer[3]}",
                EditorStyles.miniLabel);

            if (map.DataTilemap == null || map.BrushRegistry == null || map.ViewLayers == null)
            {
                return;
            }

            for (int i = 0; i < map.ViewLayers.Length; i++)
            {
                var layer = map.ViewLayers[i];
                if (layer?.Palette == null)
                {
                    continue;
                }

                byte tid = layer.Palette.TerrainId;
                int mask = DualGridCore.ComputeCornerMask(map.DataTilemap, map.BrushRegistry, viewCell, tid);
                EditorGUILayout.LabelField($"Layer {i} TerrainId={tid} mask={mask} ({ToBinary(mask)})", EditorStyles.miniLabel);
            }
        }

        static string ToBinary(int mask)
        {
            return System.Convert.ToString(mask, 2).PadLeft(4, '0');
        }
    }
}
#endif
