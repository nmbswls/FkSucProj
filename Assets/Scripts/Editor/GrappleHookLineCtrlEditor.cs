#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrappleHookLineCtrl))]
public class GrappleHookLineCtrlEditor : UnityEditor.Editor
{
    static GrappleHookLineCtrl _playingCtrl;
    static double _lastTickTime;
    static bool _playCycle;

    void OnDisable()
    {
        StopPreviewPlay();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        var ctrl = (GrappleHookLineCtrl)target;
        if (ctrl == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space();
        DrawDerivedInfo(ctrl);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 模式：按 E 施放钩爪。", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新预览"))
            {
                Undo.RecordObject(ctrl, "Refresh Grapple Preview");
                var lengthProp = serializedObject.FindProperty("editorPreviewLength");
                float length = lengthProp != null ? lengthProp.floatValue : 3f;
                ctrl.ApplyEditorPreview(length);
                MarkDirty(ctrl);
            }

            if (GUILayout.Button("清除预览"))
            {
                Undo.RecordObject(ctrl, "Clear Grapple Preview");
                ctrl.ClearEditorPreview();
                MarkDirty(ctrl);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (_playCycle && _playingCtrl == ctrl)
            {
                if (GUILayout.Button("停止动画"))
                {
                    StopPreviewPlay();
                    ctrl.ClearEditorPreview();
                    MarkDirty(ctrl);
                }
            }
            else if (GUILayout.Button("播放出钩动画"))
            {
                StopPreviewPlay();
                Undo.RecordObject(ctrl, "Play Grapple Preview");
                ctrl.ResetEditorPreviewCycle();
                _playingCtrl = ctrl;
                _playCycle = true;
                _lastTickTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += EditorPreviewUpdate;
                MarkDirty(ctrl);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    static void DrawDerivedInfo(GrappleHookLineCtrl ctrl)
    {
        var previewLen = serializedObject.FindProperty("editorPreviewLength")?.floatValue ?? 3f;
        float linkLen = ctrl.LinkLengthWorld;
        float repeats = previewLen / Mathf.Max(linkLen, 0.01f);

        EditorGUILayout.HelpBox(
            "链绳外观\n" +
            $"• 每节长度：{linkLen:0.###} m（沿绳重复一次贴图的距离）\n" +
            $"• 绳粗：{ctrl.RopeThicknessWorld:0.###} m\n" +
            $"• 预览绳长 {previewLen:0.###} m → 约 {repeats:0.##} 节\n\n" +
            "改贴图：勾「从材质同步」并 Reimport 贴图，或取消勾选后手改每节长度/绳粗。",
            MessageType.None);
    }

    static void EditorPreviewUpdate()
    {
        if (!_playCycle || _playingCtrl == null)
        {
            StopPreviewPlay();
            return;
        }

        if (Application.isPlaying)
        {
            StopPreviewPlay();
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - _lastTickTime);
        _lastTickTime = now;

        if (dt <= 0f)
        {
            return;
        }

        dt = Mathf.Min(dt, 0.05f);

        if (!_playingCtrl.StepEditorPreview(dt))
        {
            StopPreviewPlay();
        }

        MarkDirty(_playingCtrl);
    }

    static void StopPreviewPlay()
    {
        _playCycle = false;
        _playingCtrl = null;
        EditorApplication.update -= EditorPreviewUpdate;
    }

    static void MarkDirty(GrappleHookLineCtrl ctrl)
    {
        EditorUtility.SetDirty(ctrl);
        SceneView.RepaintAll();
    }
}
#endif
