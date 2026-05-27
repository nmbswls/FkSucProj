#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace My.Dungeon.Editor
{
    public class DungeonPreviewWindow : EditorWindow
    {
        private const string DefaultDungeonId = "test_cave";
        private int _seed = 12345;
        private string _lastSummary = string.Empty;

        [MenuItem("Tools/Dungeon/Preview Generation Window...")]
        public static void Open()
        {
            var window = GetWindow<DungeonPreviewWindow>("Dungeon Preview");
            window.minSize = new Vector2(360f, 200f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Editor 内可视化预览", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "会在 Main_Dungeon_TestCave 场景中铺地面 Tilemap，并在 Scene 视图叠加房间框、出生点与 DestroyObj 位置。",
                MessageType.Info);

            _seed = EditorGUILayout.IntField("Seed", _seed);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("生成并预览", GUILayout.Height(28f)))
            {
                RunPreview();
            }

            if (GUILayout.Button("清除叠加", GUILayout.Height(28f), GUILayout.Width(88f)))
            {
                DungeonEditorPreview.Clear();
                _lastSummary = string.Empty;
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_lastSummary))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(_lastSummary, EditorStyles.wordWrappedLabel, GUILayout.Height(40f));
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("图例", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("绿框 = Start 房间");
            EditorGUILayout.LabelField("黄框 = Combat 房间");
            EditorGUILayout.LabelField("蓝点 = 出生点 (PlayerBornPos)");
            EditorGUILayout.LabelField("青圈 = DestroyObj 插槽");
        }

        private void RunPreview()
        {
            if (DungeonEditorPreview.TryShow(DefaultDungeonId, _seed, out var summary))
            {
                _lastSummary = summary;
                EditorUtility.DisplayDialog(
                    "Dungeon Preview",
                    summary + "\n\n已打开测试场景并在 Scene 视图显示布局。\n请查看 Tilemap_Ground 与彩色线框。",
                    "OK");
            }
            else
            {
                _lastSummary = summary;
                EditorUtility.DisplayDialog("Dungeon Preview Failed", summary, "OK");
            }
        }
    }
}
#endif
