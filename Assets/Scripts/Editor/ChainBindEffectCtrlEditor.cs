#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChainBindEffectCtrl))]
public class ChainBindEffectCtrlEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();

        var ctrl = (ChainBindEffectCtrl)target;
        if (ctrl == null)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Actions", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Editor preview is disabled during Play Mode.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Preview"))
            {
                Undo.RegisterFullObjectHierarchyUndo(ctrl.gameObject, "Refresh Chain Preview");
                ctrl.RebuildEditorPreview();
                EditorUtility.SetDirty(ctrl);
            }

            if (GUILayout.Button("Clear Preview"))
            {
                Undo.RegisterFullObjectHierarchyUndo(ctrl.gameObject, "Clear Chain Preview");
                ctrl.ClearEditorPreview();
                EditorUtility.SetDirty(ctrl);
            }

            if (GUILayout.Button("Randomize Seed"))
            {
                Undo.RecordObject(ctrl, "Randomize Chain Preview Seed");
                ctrl.RandomizePreviewSeed();
                EditorUtility.SetDirty(ctrl);
            }
        }

        serializedObject.ApplyModifiedProperties();

        var previewProp = serializedObject.FindProperty("previewInEditor");
        if (previewProp != null && !previewProp.boolValue)
        {
            EditorGUILayout.HelpBox("Enable Preview In Editor to auto-refresh chains when parameters change.", MessageType.None);
        }
    }
}
#endif
