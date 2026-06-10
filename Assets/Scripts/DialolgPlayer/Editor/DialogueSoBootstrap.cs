using System.IO;
using UnityEditor;
using UnityEngine;

namespace My.Dialog
{
    // 从 output JSON 批量生成/刷新 DialogRawAsset SO
    public static class DialogueSoBootstrap
    {
        public const string OutputFolder = "Assets/Resources/Dialogue/output";
        public const string SoFolder = "Assets/DialogRawAsset";

        public static int BatchCreateFromOutputFolder(bool overwriteExisting = true)
        {
            if (!Directory.Exists(OutputFolder))
            {
                Debug.LogError($"Dialogue output folder not found: {OutputFolder}");
                return 0;
            }

            if (!Directory.Exists(SoFolder))
                Directory.CreateDirectory(SoFolder);

            var jsonFiles = Directory.GetFiles(OutputFolder, "*.json");
            int created = 0;

            foreach (var fullPath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(fullPath);
                if (string.IsNullOrEmpty(fileName))
                    continue;

                string assetPath = $"{SoFolder}/{fileName}.asset";
                if (File.Exists(assetPath) && !overwriteExisting)
                    continue;

                var so = ScriptableObject.CreateInstance<EditorDialogueData>();
                so.Steps = DialogueDataConverter.Deserialize(File.ReadAllText(fullPath)).Steps;
                so.LinkedJsonPath = $"{OutputFolder}/{fileName}.json";

                if (File.Exists(assetPath))
                    AssetDatabase.DeleteAsset(assetPath);

                AssetDatabase.CreateAsset(so, assetPath);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"DialogueSoBootstrap: created/updated {created} SO in {SoFolder}");
            return created;
        }

        public static EditorDialogueData CreateFromJsonFile(string fullPath, bool overwriteExisting = true)
        {
            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (!Directory.Exists(SoFolder))
                Directory.CreateDirectory(SoFolder);

            string assetPath = $"{SoFolder}/{fileName}.asset";
            if (File.Exists(assetPath) && !overwriteExisting)
                return AssetDatabase.LoadAssetAtPath<EditorDialogueData>(assetPath);

            var so = ScriptableObject.CreateInstance<EditorDialogueData>();
            so.Steps = DialogueDataConverter.Deserialize(File.ReadAllText(fullPath)).Steps;
            so.LinkedJsonPath = DialogueEditorMenus.ToProjectRelativePath(fullPath);

            if (File.Exists(assetPath))
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(so, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<EditorDialogueData>(assetPath);
        }

        // Unity -batchmode -executeMethod 入口（无对话框）
        public static void BatchCreateFromOutputFolderBatchMode()
        {
            int count = BatchCreateFromOutputFolder();
            if (count <= 0)
                EditorApplication.Exit(1);
        }
    }
}
