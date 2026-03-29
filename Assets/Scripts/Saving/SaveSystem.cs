
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace My.Saving
{

    public static class SaveSystem
    {
        // 防止短时间内重复触发读写
        public static bool IsBusy { get; private set; }

        // 获取存档路径 (Application.persistentDataPath 只能在主线程访问，所以建议在初始化时缓存或从外部传入)
        private static string GetPath(string saveFileName)
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }

        /// <summary>
        /// 异步保存
        /// </summary>
        /// <param name="fileName">文件名 (如 save.json)</param>
        /// <param name="data">准备好的纯数据对象 (DTO)</param>
        public static async Task SaveAsync(string fileName, SaveData data)
        {
            if (IsBusy) return;
            IsBusy = true;

            // 1. 获取路径 (必须在主线程完成)
            string fullPath = GetPath(fileName);

            // 2. 切换到后台线程执行繁重工作
            await Task.Run(() =>
            {
                try
                {
                    // A. 序列化 (CPU 密集型操作)
                    // Formatting.None 可以减小文件体积，调试时可用 Indented
                    string json = JsonConvert.SerializeObject(data, Formatting.None);

                    // B. 写入文件 (IO 密集型操作)
                    // 可以在这里插入 AES 加密逻辑
                    File.WriteAllText(fullPath, json);
                }
                catch (System.Exception e)
                {
                    // 捕获异常，防止 Task 默默失败
                    Debug.LogError($"[SaveSystem] 保存失败: {e.Message}");
                }
            });

            IsBusy = false;
            Debug.Log($"[SaveSystem] 异步保存完成: {fullPath}");
        }

        /// <summary>
        /// 异步读取
        /// </summary>
        public static async Task<SaveData> LoadAsync(string fileName)
        {
            if (IsBusy) return null;
            IsBusy = true;

            string fullPath = GetPath(fileName);
            if (!File.Exists(fullPath))
            {
                IsBusy = false;
                Debug.LogWarning("[SaveSystem] 存档文件不存在");
                return null;
            }

            // 切换到后台线程
            SaveData result = await Task.Run(() =>
            {
                try
                {
                    // A. 读取文件 (IO)
                    string json = File.ReadAllText(fullPath);

                    // B. 反序列化 (CPU) - 这是最耗时的部分！
                    // 可以在这里插入 AES 解密逻辑
                    return JsonConvert.DeserializeObject<SaveData>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SaveSystem] 读取失败: {e.Message}");
                    return null;
                }
            });

            IsBusy = false;
            return result;
        }
    }
}
