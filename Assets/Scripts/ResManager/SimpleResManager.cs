using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My
{
    public static class SimpleResManager
    {
        // 资源缓存字典
        private static Dictionary<string, Object> cache = new Dictionary<string, Object>();

        /// <summary>
        /// 加载资源（带缓存）
        /// </summary>
        public static T Load<T>(string path) where T : Object
        {
            // 1. 如果缓存中存在，直接返回
            if (cache.TryGetValue(path, out Object obj))
            {
                if (obj != null) return obj as T;
            }

            // 2. 缓存中没有，从 Resources 加载
            T res = Resources.Load<T>(path);
            if (res != null)
            {
                cache[path] = res; // 存入缓存
            }
            else
            {
                Debug.LogError($"[ResManager] 资源加载失败，路径: {path}");
            }

            return res;
        }

        /// <summary>
        /// 卸载单个资源
        /// </summary>
        public static void Unload(string path)
        {
            if (cache.TryGetValue(path, out Object obj))
            {
                cache.Remove(path); // 移除缓存引用

                // Unity规定：GameObject和Component不能使用 UnloadAsset 卸载
                if (!(obj is GameObject) && !(obj is Component))
                {
                    // 仅适用于 Texture, AudioClip, TextAsset 等独立资源
                    Resources.UnloadAsset(obj);
                }
            }
        }

        /// <summary>
        /// 卸载所有未使用资源（通常在切场景时调用）
        /// </summary>
        public static void UnloadAllUnused()
        {
            // 清空整个字典的引用
            cache.Clear();

            // 触发Unity底层异步卸载没有被引用的资源（包括被移出字典的 GameObject 预制体）
            Resources.UnloadUnusedAssets();
        }
    }

}


