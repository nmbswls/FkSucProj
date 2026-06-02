#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Ground
{
    [CustomEditor(typeof(MapLogicHeightConfig))]
    public class MapLogicHeightConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Ground Layer Names：填 GridRoot prefab 里 Tilemap 节点名（如 Ground、Building_01_Ground）。\n" +
                "留空则运行时采样 WalkGrid 下全部 Walk 层。",
                MessageType.Info);

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

            if (GUILayout.Button("从 GridRoot prefab 填充 LayerName（Main_Area_01）"))
            {
                TryFillLayerNamesFromGridRootPrefab(config);
            }
        }

        static void TryFillLayerNamesFromGridRootPrefab(MapLogicHeightConfig config)
        {
            const string gridRootPath = "Assets/Resources/MapChunk/Main_Area_01/Prefabs/GridRoot.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(gridRootPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Fill LayerName", $"Prefab not found:\n{gridRootPath}", "OK");
                return;
            }

            config.GroundLayerNames = prefab.GetComponentsInChildren<Tilemap>(true)
                .Select(t => t.name)
                .Where(n => n != "Hole")
                .Distinct()
                .OrderBy(n => n)
                .ToArray();

            EditorUtility.SetDirty(config);
            Debug.Log($"[MapLogicHeightConfig] Filled {config.GroundLayerNames.Length} layer name(s) from GridRoot prefab.");
        }
    }
}
#endif
