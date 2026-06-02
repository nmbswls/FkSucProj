#if UNITY_EDITOR
using My.Map.Ground;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldAreaRoot))]
public class WorldAreaRootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var root = (WorldAreaRoot)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "TileGrounds 由 MapLogicHeightConfig.GroundLayerNames 装配（仅地面层）。\n" +
            "请配置 LogicHeightConfig 与 Grid，点击下方按钮刷新。",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(root.LogicHeightConfig == null))
        {
            if (GUILayout.Button("从 LogicHeightConfig 装配地面层"))
            {
                if (root.ApplyTileGroundsFromLogicHeightConfig())
                {
                    EditorUtility.SetDirty(root);
                }
            }
        }

        if (root.LogicHeightConfig != null &&
            (root.LogicHeightConfig.GroundLayerNames == null || root.LogicHeightConfig.GroundLayerNames.Length == 0))
        {
            EditorGUILayout.HelpBox("LogicHeightConfig.GroundLayerNames 为空。", MessageType.Warning);
        }

        if (root.TileGrounds != null && root.TileGrounds.Length > 0)
        {
            EditorGUILayout.LabelField("当前地面层", EditorStyles.boldLabel);
            foreach (var tm in root.TileGrounds)
            {
                if (tm != null)
                {
                    EditorGUILayout.LabelField($"  • {tm.name}");
                }
            }
        }
    }
}
#endif
