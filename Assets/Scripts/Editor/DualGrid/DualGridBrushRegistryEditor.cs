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
                "Data 层用普通 Tile 绘制；在此登记笔刷 Tile 与 TerrainId 的对应关系。",
                MessageType.Info);
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
