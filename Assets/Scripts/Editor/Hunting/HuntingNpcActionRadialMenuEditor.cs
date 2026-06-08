#if UNITY_EDITOR
using My.Map.Hunting;
using UnityEditor;
using UnityEngine;

namespace My.Map.Hunting.Editor
{
    [CustomEditor(typeof(HuntingNpcActionRadialMenu))]
    public class HuntingNpcActionRadialMenuEditor : UnityEditor.Editor
    {
        enum EPreviewPreset
        {
            Hidden = 0,
            AllEnabled = 1,
            ExecuteDisabled = 2,
            ControlDisabled = 3,
            AllDisabled = 4,
        }

        SerializedProperty _menuRoot;
        SerializedProperty _executeButton;
        SerializedProperty _optionsContainer;
        SerializedProperty _optionButtonTemplate;
        SerializedProperty _optionRadius;
        SerializedProperty _optionStartAngleDeg;
        SerializedProperty _optionAngleStepDeg;

        EPreviewPreset _previewPreset = EPreviewPreset.Hidden;

        void OnEnable()
        {
            _menuRoot = serializedObject.FindProperty("MenuRoot");
            _executeButton = serializedObject.FindProperty("ExecuteButton");
            _optionsContainer = serializedObject.FindProperty("OptionsContainer");
            _optionButtonTemplate = serializedObject.FindProperty("OptionButtonTemplate");
            _optionRadius = serializedObject.FindProperty("OptionRadius");
            _optionStartAngleDeg = serializedObject.FindProperty("OptionStartAngleDeg");
            _optionAngleStepDeg = serializedObject.FindProperty("OptionAngleStepDeg");

            var menu = (HuntingNpcActionRadialMenu)target;
            _previewPreset = menu.IsEditorPreviewActive
                ? EPreviewPreset.AllEnabled
                : EPreviewPreset.Hidden;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_menuRoot);
            EditorGUILayout.PropertyField(_executeButton);
            EditorGUILayout.PropertyField(_optionsContainer);
            EditorGUILayout.PropertyField(_optionButtonTemplate);
            EditorGUILayout.PropertyField(_optionRadius);
            EditorGUILayout.PropertyField(_optionStartAngleDeg);
            EditorGUILayout.PropertyField(_optionAngleStepDeg);

            serializedObject.ApplyModifiedProperties();

            var menu = (HuntingNpcActionRadialMenu)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "选择预览预设后会在 Scene 视图生成假轮盘。调整 OptionRadius / 角度参数时布局会实时刷新。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _previewPreset = (EPreviewPreset)EditorGUILayout.EnumPopup("Preview Preset", _previewPreset);
            bool presetChanged = EditorGUI.EndChangeCheck();

            if (presetChanged)
            {
                ApplyPreviewPreset(menu, _previewPreset);
            }
            else if (menu.IsEditorPreviewActive && GUI.changed)
            {
                ApplyPreviewPreset(menu, _previewPreset);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Preview"))
            {
                ApplyPreviewPreset(menu, _previewPreset);
            }

            if (GUILayout.Button("Hide Preview"))
            {
                _previewPreset = EPreviewPreset.Hidden;
                menu.HideEditorPreview();
                EditorUtility.SetDirty(menu);
            }
            EditorGUILayout.EndHorizontal();

            if (menu.IsEditorPreviewActive)
            {
                EditorGUILayout.HelpBox("预览中：MenuRoot 置于本地原点，可在父节点下拖动整体位置对照布局。", MessageType.None);
            }
        }

        static void ApplyPreviewPreset(HuntingNpcActionRadialMenu menu, EPreviewPreset preset)
        {
            if (preset == EPreviewPreset.Hidden)
            {
                menu.HideEditorPreview();
                return;
            }

            bool canExecute;
            bool canControl;
            switch (preset)
            {
                case EPreviewPreset.ExecuteDisabled:
                    canExecute = false;
                    canControl = true;
                    break;
                case EPreviewPreset.ControlDisabled:
                    canExecute = true;
                    canControl = false;
                    break;
                case EPreviewPreset.AllDisabled:
                    canExecute = false;
                    canControl = false;
                    break;
                default:
                    canExecute = true;
                    canControl = true;
                    break;
            }

            menu.ShowEditorPreview(canExecute, canControl);
            EditorUtility.SetDirty(menu);
            SceneView.RepaintAll();
        }
    }
}
#endif
