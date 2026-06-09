#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace My.Map.Ground
{
    [CustomEditor(typeof(MapLogicHeightConfig))]
    public class MapLogicHeightConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var config = (MapLogicHeightConfig)target;
            if (config.SlopeTiles == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            foreach (var slope in config.SlopeTiles)
            {
                if (slope == null || slope.Tile == null)
                {
                    continue;
                }

                if (slope.NorthLogicY <= slope.SouthLogicY + 1e-5f)
                {
                    EditorGUILayout.HelpBox(
                        $"Slope '{slope.Tile.name}': NorthLogicY must be greater than SouthLogicY (north-high / south-low uphill).",
                        MessageType.Warning);
                }
            }
        }
    }
}
#endif
