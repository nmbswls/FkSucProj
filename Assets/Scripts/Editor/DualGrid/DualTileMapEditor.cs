#if UNITY_EDITOR
using System;
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid.Editor
{
    [CustomEditor(typeof(DualTileMap))]
    public class DualTileMapEditor : UnityEditor.Editor
    {
        Vector3Int _probeCell;
        bool _probeAsDataCell;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var map = (DualTileMap)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Grid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DataTilemap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BrushRegistry"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ViewTilemap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ViewTile"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AutoRefreshInEditor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ViewSortingOrder"));

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "TryResolveViewCorner 参数：\n" +
                "① dataTilemap = DualTileMap 的 Data（不是 View）\n" +
                "② viewCell = View 层格子坐标\n" +
                "读 Data：viewCell-(0,0),(1,0),(0,1),(1,1)。落笔刷新 View：dataCell+(0,0),(1,0),(0,1),(1,1)。\n" +
                "调试 mask 请用 Probe 里「Data 格」或 View=dataCell+(1,1) 那一角，不要用鼠标在 View 上随便取的格。",
                MessageType.None);

            if (map.ResolveGrid() == null)
            {
                EditorGUILayout.HelpBox("未找到 Grid。", MessageType.Warning);
            }

            if (!map.IsConfigured(out var error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
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

            int dataCount = map.CountDataTiles();
            int viewCount = map.CountViewTiles();
            EditorGUILayout.LabelField($"Data cells: {dataCount} | View cells: {viewCount}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus Data"))
            {
                FocusTilemap(map.DataTilemap);
            }
            if (GUILayout.Button("Focus View"))
            {
                FocusTilemap(map.ViewTilemap);
            }
            EditorGUILayout.EndHorizontal();

            if (map.BrushRegistry != null && map.DataTilemap != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Probe", EditorStyles.boldLabel);
                _probeAsDataCell = EditorGUILayout.Toggle("坐标是 Data 格", _probeAsDataCell);
                _probeCell = EditorGUILayout.Vector3IntField(_probeAsDataCell ? "Data Cell" : "View Cell", _probeCell);

                if (_probeAsDataCell)
                {
                    DrawProbeDataCell(map, _probeCell);
                }
                else
                {
                    DrawProbeViewCell(map, _probeCell);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        static void DrawProbeDataCell(DualTileMap map, Vector3Int dataCell)
        {
            EditorGUILayout.LabelField($"Data 格 {dataCell} 落笔会刷新以下 View 角点：", EditorStyles.miniLabel);
            var viewCorners = new Vector3Int[4];
            DualGridCore.GetViewCornersAroundDataCell(dataCell, viewCorners);
            for (int i = 0; i < 4; i++)
            {
                DrawProbeViewCell(map, viewCorners[i], $"  View[{i}] {viewCorners[i]}");
            }
        }

        static void DrawProbeViewCell(DualTileMap map, Vector3Int viewCell, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                EditorGUILayout.LabelField(prefix, EditorStyles.miniLabel);
            }

            var reg = map.BrushRegistry;
            var dataCells = new Vector3Int[4];
            DualGridCore.GetDataCellsForViewCorner(viewCell, dataCells);
            EditorGUILayout.LabelField($"View {viewCell} 读取 Data：", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(DualGridCore.FormatCornerSample(map.DataTilemap, viewCell), EditorStyles.miniLabel);
            for (int i = 0; i < 4; i++)
            {
                var brush = map.DataTilemap.GetTile(dataCells[i]);
                string brushName = brush != null ? brush.name : "(空)";
                byte terrainId = 0;
                bool mapped = brush != null && reg.TryGetTerrainId(brush, out terrainId);
                EditorGUILayout.LabelField(
                    $"  [{i}] Data{dataCells[i]} = {brushName}" + (mapped ? $" → T{terrainId}" : " 未登记"),
                    EditorStyles.miniLabel);
            }

            if (map.TryResolveViewCorner(viewCell, out byte win, out int winMask))
            {
                EditorGUILayout.LabelField($"  => mask={winMask} ({ToBinary(winMask)}) T{win}", EditorStyles.miniLabel);
                if (map.TryGetViewSprite(viewCell, out var sprite) && sprite != null)
                {
                    EditorGUILayout.LabelField($"  => Sprite: {sprite.name}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.HelpBox("  => Palette 无对应 Sprite，View 不会显示。", MessageType.Warning);
                }

                if (map.ViewTile == null)
                {
                    EditorGUILayout.HelpBox("View Tile 未绑定，View 不会渲染。", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "  => mask=0：四格都空、笔刷未登记、或 Terrains 里没有对应 TerrainId+Palette。",
                    MessageType.Warning);
            }
        }

        public static void CreateHierarchy(DualTileMap map)
        {
            Undo.RegisterFullObjectHierarchyUndo(map.gameObject, "Create Dual Tile Map Hierarchy");
            map.Grid = map.ResolveGrid();

            var dataGo = GetOrCreateChild(map.transform, "Data");
            map.DataTilemap = GetOrCreateTilemap(dataGo);

            var viewGo = GetOrCreateChild(map.transform, "View");
            map.ViewTilemap = GetOrCreateTilemap(viewGo);

            map.EnsureViewOffset();
            EditorUtility.SetDirty(map);
        }

        public static GameObject GetOrCreateChild(Transform parent, string name)
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

        public static Tilemap GetOrCreateTilemap(GameObject go)
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

        public static void FocusTilemap(Tilemap tilemap)
        {
            if (tilemap == null)
            {
                return;
            }

            Selection.activeGameObject = tilemap.gameObject;
            SceneView.FrameLastActiveSceneView();
        }

        static string ToBinary(int mask)
        {
            return Convert.ToString(mask, 2).PadLeft(4, '0');
        }
    }
}
#endif
