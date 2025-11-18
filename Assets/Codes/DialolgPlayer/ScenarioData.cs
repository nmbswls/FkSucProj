using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ScenarioData
{
    public string id;
    public List<StepData> steps = new List<StepData>();
}

[Serializable]
public class StepData
{
    public string label; // 可选，作为跳转锚点
    public List<CommandData> commands = new List<CommandData>();
}

[Serializable]
public class CommandData
{
    public string type; // 例如 "TypeText","ShowPortrait","CameraMove"...
    public bool wait = true;
    // 通用参数容器：可用键值字典或 JSON 可序列化的简单对象
    public SerializableDict<string, string> s; // 字符串参数（id、keys、names）
    public SerializableDict<string, float> f;  // 浮点参数（duration、fade、fov等）
    public SerializableDict<string, int> i;    // 整数参数（索引、计数）
    public List<SerializableDict<string, string>> listS; // 用于 options 等
}

// 简单可序列化字典（JsonUtility 不支持 Dictionary，可用自定义包装）
[Serializable]
public class SerializableDict<TKey, TValue>
{
    public List<TKey> keys = new List<TKey>();
    public List<TValue> values = new List<TValue>();
    public void Add(TKey k, TValue v) { keys.Add(k); values.Add(v); }
    public bool TryGetValue(TKey k, out TValue v)
    {
        int idx = keys.IndexOf(k);
        if (idx >= 0) { v = values[idx]; return true; }
        v = default; return false;
    }
}