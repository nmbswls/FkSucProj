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
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Paint Background", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Paint Slice Px", root.PaintSlicePixelSize.ToString());
        EditorGUILayout.LabelField("Effective Paint PPU", root.EffectivePaintExportPpu.ToString());

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

            if (GUILayout.Button("Open Paint Window"))
            {
                EditorApplication.ExecuteMenuItem("Window/Map Paint Background");
            }
        }
    }
}
#endif
