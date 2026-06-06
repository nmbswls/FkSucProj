#if UNITY_EDITOR
using My.Map.Cliff;
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid.Editor
{
    [CustomEditor(typeof(DualTileMap))]
    public class DualTileMapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var map = (DualTileMap)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("DataTilemap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BrushRegistry"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ViewTilemap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AutoRefreshInEditor"));

            if (!map.IsConfigured(out var error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }

            EditorGUILayout.Space(4);
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

            var cliffGen = map.GetComponent<CliffPlateauGenerator>();
            if (cliffGen != null)
            {
                cliffGen.EnsureCliffChild();
                cliffGen.SyncDualGridOffset();
                EditorUtility.SetDirty(cliffGen);
            }

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
    }
}
#endif
