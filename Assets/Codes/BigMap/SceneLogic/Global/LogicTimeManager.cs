using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{

    public class PauseController
    {
        // key: 来源名；value: 该来源当前持有的暂停次数
        private readonly Dictionary<string, int> _sources = new Dictionary<string, int>(StringComparer.Ordinal);

        // 是否处于暂停（任意来源计数 > 0）
        public bool IsPaused => _sources.Count > 0;

        // 请求暂停（同一来源可多次调用，计数累加）
        public void RequestPause(string source)
        {
            if (string.IsNullOrEmpty(source)) source = "Unknown";
            if (_sources.TryGetValue(source, out var count))
                _sources[source] = count + 1;
            else
                _sources[source] = 1;
        }

        // 释放暂停（计数减一，归零后移除来源）
        public void ReleasePause(string source)
        {
            if (string.IsNullOrEmpty(source)) source = "Unknown";
            if (_sources.TryGetValue(source, out var count))
            {
                count -= 1;
                if (count <= 0) _sources.Remove(source);
                else _sources[source] = count;
            }
            // 没找到也不抛错，容忍重复释放
        }

        // 清空某来源的所有暂停（防泄漏/容错）
        public void Clear(string source)
        {
            if (string.IsNullOrEmpty(source)) source = "Unknown";
            _sources.Remove(source);
        }

        // 清空全部来源（紧急复位）
        public void ClearAll()
        {
            _sources.Clear();
        }

        // 调试查看当前来源与计数
        public IReadOnlyDictionary<string, int> ActiveSources => _sources;
    }

    public static class LogicTime
    {
        private const string DefaultDomain = "Game";
        private static LogicDomainState D => LogicTimeManager.Instance.GetOrCreate(DefaultDomain);

        public static float time => D.Elapsed;
        public static float deltaTime => D.Delta;
        public static float timeScale { get => D.TimeScale; set => D.TimeScale = value; }
        public static bool paused => D.Paused;

        // 直接用字符串管理暂停
        public static void RequestPause(string source) => D.Pause.RequestPause(source);
        public static void ReleasePause(string source) => D.Pause.ReleasePause(source);
        public static void ClearPauseSource(string source) => D.Pause.Clear(source);
        public static void ClearAllPauses() => D.Pause.ClearAll();
    }


    public class LogicDomainState
    {
        public float Elapsed;
        public float Delta;
        public float TimeScale = 1f;
        public readonly PauseController Pause = new PauseController();
        public bool Paused => Pause.IsPaused;
    }


    public class LogicTimeManager : MonoBehaviour
    {
        public static LogicTimeManager Instance { get; private set; }
        private readonly Dictionary<string, LogicDomainState> _domains = new Dictionary<string, LogicDomainState>();

        void Awake() => Instance = this;

        public LogicDomainState GetOrCreate(string name)
        {
            if (!_domains.TryGetValue(name, out var d))
            {
                d = new LogicDomainState();
                _domains[name] = d;
            }
            return d;
        }

        void Update()
        {
            float realDt = Time.unscaledDeltaTime;
            foreach (var d in _domains.Values)
            {
                if (d.Paused) { d.Delta = 0f; continue; }
                float logicDt = realDt * Mathf.Max(d.TimeScale, 0f);
                d.Delta = logicDt;
                d.Elapsed += logicDt;
            }
        }
    }

    //public class MapUnitController : MonoBehaviour, ILogicUpdatable
    //{
    //    public string DomainName = "BigMap";

    //    private LogicTimeDomain _domain;

    //    private void OnEnable()
    //    {
    //        _domain = LogicTimeManager.Instance.GetOrCreate(DomainName);
    //        _domain.Add(this);
    //    }

    //    private void OnDisable()
    //    {
    //        _domain?.Remove(this);
    //    }

    //    public void OnLogicUpdate(float dt)
    //    {
    //        // 这里使用 dt（逻辑时间）进行移动、计时等
    //        // Example: transform.position += velocity * dt;
    //    }
    //}
}