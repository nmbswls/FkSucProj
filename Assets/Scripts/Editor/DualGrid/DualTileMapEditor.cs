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
        Vector3Int _probeLogicCell;
        bool _useLogicProbe = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var map = (DualTileMap)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Grid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DataTilemap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BrushRegistry"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ViewTilemap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AutoRefreshInEditor"));

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "只在 Brush Registry 配笔刷与 Palette。\n" +
                "画 Data，View 自动拼贴；无需 Display Tile 资产。",
                MessageType.Info);

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
                EditorGUILayout.LabelField("Coordinate Probe", EditorStyles.boldLabel);
                _useLogicProbe = EditorGUILayout.Toggle("Probe Logic Cell", _useLogicProbe);
                if (_useLogicProbe)
                {
                    _probeLogicCell = EditorGUILayout.Vector3IntField("Logic Cell", _probeLogicCell);
                    DrawProbe(map, _probeLogicCell);
                }
            }

            serializedObject.ApplyModifiedProperties();
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

        static void DrawProbe(DualTileMap map, Vector3Int viewCell)
        {
            var reg = map.BrushRegistry;
            EditorGUILayout.LabelField($"View corner: {viewCell}", EditorStyles.miniLabel);

            if (reg.TryResolveViewCorner(map.DataTilemap, viewCell, out byte win, out int winMask))
            {
                EditorGUILayout.LabelField(
                    $"显示 T{win} mask={winMask} ({ToBinary(winMask)})",
                    EditorStyles.miniLabel);
            }

            if (reg.Terrains == null)
            {
                return;
            }

            for (int i = 0; i < reg.Terrains.Length; i++)
            {
                var style = reg.Terrains[i];
                if (style == null || style.TerrainId == 0)
                {
                    continue;
                }

                int mask = DualGridCore.ComputeCornerMask(
                    map.DataTilemap,
                    reg,
                    viewCell,
                    style.TerrainId);
                EditorGUILayout.LabelField(
                    $"T{style.TerrainId} mask={mask} ({ToBinary(mask)})",
                    EditorStyles.miniLabel);
            }
        }

        static string ToBinary(int mask)
        {
            return Convert.ToString(mask, 2).PadLeft(4, '0');
        }
    }
}
#endif
