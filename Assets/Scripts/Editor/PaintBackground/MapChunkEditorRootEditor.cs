#if UNITY_EDITOR
using My.MapExport;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapChunkEditorRoot))]
public class MapChunkEditorRootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var root = (MapChunkEditorRoot)target;
        var settings = MapChunkEditorSettings.GetOrCreate();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Chunk World Size", settings.EffectiveChunkWorldSize.ToString());
        EditorGUILayout.LabelField("Paint Slice Px", settings.PaintSlicePixelSize.ToString());
        EditorGUILayout.LabelField("Effective Paint PPU", settings.EffectivePaintExportPpu.ToString());
        if (GUILayout.Button("Open Map Chunk Editor Settings"))
        {
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
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
                var mapName = string.IsNullOrEmpty(root.SceneName) ? root.gameObject.scene.name : root.SceneName;
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
}
#endif
