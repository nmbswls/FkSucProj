#if UNITY_EDITOR
using System.Linq;
using My.MapExport;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(MapChunkEditorRoot))]
public class MapChunkEditorRootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var root = (MapChunkEditorRoot)target;

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Fill Ground Layers From GridRoot"))
            {
                FillGroundLayerNamesFromGridRoot(root);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Paint Background", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap Paint Rect"))
            {
                Undo.RecordObject(root, "Snap Paint Rect");
                root.PaintWorldRect = MapChunkUtility.SnapWorldRectToChunkGrid(
                    root.PaintWorldRect,
                    root.ChunkOrigin,
                    root.ChunkWorldSize);
                EditorUtility.SetDirty(root);
            }

            if (GUILayout.Button("Refresh Scene Preview"))
            {
                var mapName = string.IsNullOrEmpty(root.MapVariantSceneName)
                    ? root.gameObject.scene.name
                    : root.MapVariantSceneName;
                var result = MapPaintBackgroundPreview.SyncToScene(root, mapName);
                if (!result.Success)
                {
                    EditorUtility.DisplayDialog("Paint Preview", result.Message, "OK");
                }
            }

            if (GUILayout.Button("Open Paint Window"))
            {
                EditorApplication.ExecuteMenuItem("Window/Map Paint Background");
            }
        }
    }

    public static void FillGroundLayerNamesFromGridRoot(MapChunkEditorRoot root)
    {
        if (root == null)
        {
            return;
        }

        var gridRoot = MapChunkEditorTilemapResolver.TryGetGridRoot(root);
        if (gridRoot == null)
        {
            EditorUtility.DisplayDialog("Ground Layers", "GridRoot not found under MapVariantRoot.", "OK");
            return;
        }

        root.GroundLayerNames = gridRoot.GetComponentsInChildren<Tilemap>(true)
            .Select(t => t.name)
            .Where(n => n != "Hole")
            .Distinct()
            .OrderBy(n => n)
            .ToArray();

        EditorUtility.SetDirty(root);
        Debug.Log($"[MapChunkEditorRoot] Filled {root.GroundLayerNames.Length} ground layer name(s).");
    }
}
#endif
