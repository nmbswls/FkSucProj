
using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace My.Saving
{
    public static class SaveSystem
    {
        public const string DefaultSaveFileName = "mysave.json";

        // Resources 下 TextAsset，路径不含扩展名；只读
        public const string BundledTestSaveResourcePath = "Saves/test_save";

        public static bool IsBusy { get; private set; }

        public static string GetFullPath(string saveFileName)
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }

        public static bool SaveFileLooksValid(string fileName)
        {
            string fullPath = GetFullPath(fileName);
            if (!File.Exists(fullPath)) return false;
            try
            {
                return new FileInfo(fullPath).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static string GetPath(string saveFileName) => GetFullPath(saveFileName);

        public static SaveData DeserializeSaveData(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[SaveSystem] DeserializeSaveData: json is empty");
                return null;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<SaveData>(json);
                if (data == null)
                {
                    Debug.LogError("[SaveSystem] DeserializeSaveData: result is null");
                    return null;
                }

                SaveData.EnsureHydrated(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] DeserializeSaveData failed: {e.Message}");
                return null;
            }
        }

        public static SaveData LoadBundledSaveFromResources(string resourcePathWithoutExtension)
        {
            var asset = Resources.Load<TextAsset>(resourcePathWithoutExtension);
            if (asset == null)
            {
                Debug.LogError($"[SaveSystem] Bundled save TextAsset not found: Resources/{resourcePathWithoutExtension}.json");
                return null;
            }

            return DeserializeSaveData(asset.text);
        }

        public static async Task SaveAsync(string fileName, SaveData data)
        {
            if (IsBusy) return;
            if (data == null)
            {
                Debug.LogError("[SaveSystem] SaveAsync: data is null");
                return;
            }

            IsBusy = true;
            string fullPath = GetPath(fileName);

            await Task.Run(() =>
            {
                try
                {
                    string json = JsonConvert.SerializeObject(data, Formatting.None);
                    File.WriteAllText(fullPath, json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
                }
            });

            IsBusy = false;
            Debug.Log($"[SaveSystem] Save completed: {fullPath}");
        }

        public static async Task<SaveData> LoadAsync(string fileName)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[SaveSystem] LoadAsync skipped: another save IO is in progress");
                return null;
            }

            IsBusy = true;
            string fullPath = GetPath(fileName);
            if (!File.Exists(fullPath))
            {
                IsBusy = false;
                Debug.LogWarning("[SaveSystem] Save file not found");
                return null;
            }

            SaveData result = await Task.Run(() =>
            {
                try
                {
                    string json = File.ReadAllText(fullPath);
                    var parsed = JsonConvert.DeserializeObject<SaveData>(json);
                    if (parsed == null)
                    {
                        Debug.LogError("[SaveSystem] Deserialize returned null (empty or invalid JSON)");
                        return null;
                    }

                    SaveData.EnsureHydrated(parsed);
                    return parsed;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                    return null;
                }
            });

            IsBusy = false;
            return result;
        }
    }
}
