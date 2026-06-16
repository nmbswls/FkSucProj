using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.VisualScripting.Metadata;


namespace My.Player
{
    /// <summary>
    /// 
    /// </summary>
    public enum EYCAttribute
    {
        None,
        SecretSlot,
        InnerCharm,
        StaticCharm,

        InnerArm,
        StaticArm,

        ExtraJingYuanSlot,

        TiaoJingSlotCap,

        FixDmgReduceFinal,

        FixFallenAdd,

        PartGearPoint_Mouth = 10,
        PartGearPoint_Breast = 11,
        PartGearPoint_Womb = 12,
        PartGearPoint_Tail = 13,
        PartGearPoint_Wing = 14,
        PartGearPoint_Skin = 15,
    }

    // 2. 极简存储结构（用于叶子节点存储数据，省内存）
    [System.Serializable]
    public struct StatPair
    {
        public int ID;
        public long Value;

        public StatPair(int id, long val)
        {
            ID = id;
            Value = val;
        }
    }


    public class StatMap
    {
        // 只有在聚合节点才真正创建 Dictionary
        private Dictionary<int, long> _map = new Dictionary<int, long>();

        // 获取值，没有则返回 0
        public long Get(int id)
        {
            return _map.TryGetValue(id, out long val) ? val : 0;
        }

        // 设置/添加
        public void Add(int id, long value)
        {
            if (_map.ContainsKey(id))
                _map[id] += value;
            else
                _map[id] = value;
        }

        // 清空（复用 Dictionary，避免 GC）
        public void Clear()
        {
            _map.Clear();
        }

        // 核心优化：将另一个 Map 合并进来（用于中间节点聚合）
        public void MergeFrom(StatMap otherMap)
        {
            foreach (var kvp in otherMap._map)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        // 核心优化：将紧凑列表合并进来（用于叶子节点聚合）
        // 这是处理稀疏数据的关键，遍历 List 比遍历大数组快得多
        public void MergeFrom(List<StatPair> sparseList)
        {
            if (sparseList == null) return;
            // List 为空时循环不会执行，开销极低
            for (int i = 0; i < sparseList.Count; i++)
            {
                Add(sparseList[i].ID, sparseList[i].Value);
            }
        }

        // 调试用
        public Dictionary<int, long> GetRawDict() => _map;

        // 新增：暴露迭代器供外部遍历
        // 注意：尽量避免直接暴露 Dictionary，防止外部修改
        public Dictionary<int, long>.Enumerator GetEnumerator()
        {
            return _map.GetEnumerator();
        }
    }


    public interface IProgressionSource
    {

        EProgressionModule ModuleName { get; }

        event Action<IProgressionSource> OnStatsChanged;

        void EvaluateStats(StatMap targetMap);
    }

    public enum EProgressionModule
    {
        None,
        Basic,
        Level,
        Gear,
        Talent,
        BodyPart,
        JingYuanCodex,

        Aggregator
    }


    /// <summary>
    /// 聚合节点
    /// </summary>
    public class ProgressionAggregator : IProgressionSource
    {
        public event Action<IProgressionSource> OnStatsChanged;
        public string NodeName { get; private set; }

        public EProgressionModule ModuleName => EProgressionModule.Aggregator;

        private List<IProgressionSource> _children = new List<IProgressionSource>();
        private StatMap _cache = new StatMap();
        private bool _isDirty = true;

        public ProgressionAggregator(string name) => NodeName = name;

        public void AddChild(IProgressionSource source)
        {
            if (!_children.Contains(source))
            {
                _children.Add(source);
                source.OnStatsChanged += OnChildChanged;
                MarkDirty();
            }
        }

        public void RemoveChild(IProgressionSource source)
        {
            if (_children.Contains(source))
            {
                source.OnStatsChanged -= OnChildChanged;
                _children.Remove(source);
                MarkDirty();
            }
        }

        private void OnChildChanged(IProgressionSource source) => MarkDirty();

        protected void MarkDirty()
        {
            if (_isDirty) return;
            _isDirty = true;
            OnStatsChanged?.Invoke(this);
        }

        // 外部子系统（如装备 Buff 无 StatMap 条目）仍需通知养成根节点刷新缓存
        public void ForceDirty() => MarkDirty();


        public void EvaluateStats(StatMap targetMap)
        {
            // 外部请求数据时，确保缓存是最新的
            RebuildCache();
            targetMap.MergeFrom(_cache);
        }

        //// 仅用于调试或根节点查询
        //public float GetValue(int id)
        //{
        //    if (_isDirty)
        //    {
        //        // 触发内部重算但不导出到外部
        //        StatMap temp = new StatMap();
        //        EvaluateStats(temp);
        //    }
        //    return _cache.Get(id);
        //}

        // 2. 优化 GetValue：不再产生 GC
        public long GetValue(int id)
        {
            RebuildCache(); // 确保脏标记被清除，缓存最新
            return _cache.Get(id);
        }

        // 1. 提取重算逻辑
        private void RebuildCache()
        {
            if (!_isDirty) return;

            _cache.Clear();
            foreach (var child in _children)
            {
                child.EvaluateStats(_cache);
            }
            _isDirty = false;
            // 这里计算完后，_cache 里就是最新的全量数据
        }


        // 3. 新增：允许外部直接访问缓存（只读引用）
        // 对于“大地图”这种需要遍历所有属性的系统，这个最关键
        public StatMap GetRawCache()
        {
            RebuildCache();
            return _cache;
        }
    }
}
