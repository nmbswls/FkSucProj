#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace My.Map.Ground
{
    [CustomEditor(typeof(MapLogicHeightConfig))]
    public class MapLogicHeightConfigEditor : Editor
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

                if (slope.ToLevel != slope.FromLevel + 1)
                {
                    EditorGUILayout.HelpBox(
                        $"Slope tile '{slope.Tile.name}': ToLevel must be FromLevel + 1 (north-high / south-low uphill only).",
                        MessageType.Warning);
                }
            }
        }
    }
}
#endif
