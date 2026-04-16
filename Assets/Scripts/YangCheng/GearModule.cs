
using System.Collections.Generic;
using System;

namespace My
{
    public class PlayerGear : IProgressionSource
    {
        public event Action<IProgressionSource> OnStatsChanged;

        private int _level = 1;
        // 模拟配置表数据：每升一级加多少属性
        private Dictionary<int, float> _growthRates;
        private Dictionary<int, float> _baseStats;

        public EProgressionModule ModuleName => EProgressionModule.Level;

        public PlayerGear()
        {
            //_growthRates = new Dictionary<int, float> { { StatID.Health, 100 }, { StatID.Attack, 10 } };
            //_baseStats = new Dictionary<int, float> { { StatID.Health, 500 }, { StatID.Attack, 50 } };
        }

        public void SetLevel(int level)
        {
            _level = level;
            OnStatsChanged?.Invoke(this);
        }

        public void EvaluateStats(StatMap targetMap)
        {
            // 逻辑：基础值 + (等级-1 * 成长率)
            foreach (var pair in _baseStats)
            {
                float growth = _growthRates.ContainsKey(pair.Key) ? _growthRates[pair.Key] : 0;
                float total = pair.Value + ((_level - 1) * growth);
                targetMap.Add(pair.Key, total);
            }
        }
    }
}