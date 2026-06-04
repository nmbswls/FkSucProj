#if UNITY_EDITOR
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;

namespace My.Map.DualGrid.Editor
{
    [CustomEditor(typeof(DualGridBrushRegistry))]
    public class DualGridBrushRegistryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "Terrains：TerrainId + Palette（16 态显示），顺序为 View 角点优先级。\n" +
                "Brushes：Data 上画的普通 Tile → TerrainId。",
                MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Terrains"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Brushes"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
