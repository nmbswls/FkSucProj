
using System.Collections.Generic;
using System;
using cfg.demo;

namespace My.Player
{
    /// <summary>
    /// 养成提供器
    /// </summary>
    public class BasicProgressionProvider : IProgressionSource
    {
        public BasicProgressionProvider()
        {
            
        }

        public EProgressionModule ModuleName => EProgressionModule.Basic;

        public event Action<IProgressionSource> OnStatsChanged;

        public void EvaluateStats(StatMap targetMap)
        {
            targetMap.Add((int)EYCAttribute.StaticCharm, 10);
            targetMap.Add((int)EYCAttribute.SecretSlot, 3);

            targetMap.Add((int)EYCAttribute.FixDmgReduceFinal, 5_000);

            // 负重：各随身特殊背包重量折算率初始值 10000 = 100%
            targetMap.Add((int)EYCAttribute.MainBagWeightRatio, 10_000);
            targetMap.Add((int)EYCAttribute.SecretBagWeightRatio, 10_000);
            targetMap.Add((int)EYCAttribute.PlantBagWeightRatio, 10_000);
            targetMap.Add((int)EYCAttribute.KeyBagWeightRatio, 10_000);
            targetMap.Add((int)EYCAttribute.PotionBagWeightRatio, 10_000);
            targetMap.Add((int)EYCAttribute.BigBagWeightRatio, 10_000);
            targetMap.Add((int)EYCAttribute.MindBagWeightRatio, 10_000);
            targetMap.Add((int)EYCAttribute.ImportantBagWeightRatio, 10_000);

            // 负重：基础负重与加成初始值
            targetMap.Add((int)EYCAttribute.CarryWeightBase, 300);
            targetMap.Add((int)EYCAttribute.CarryWeightExtraFlat, 0);
            targetMap.Add((int)EYCAttribute.CarryWeightExtraPercent, 0);
        }
    }


    /// <summary>
    /// 养成提供器
    /// </summary>
    public class LevelProgressionProvider : IProgressionSource
    {
        public event Action<IProgressionSource> OnStatsChanged;

        private int _level = 1;
        // 模拟配置表数据：每升一级加多少属性
        private Dictionary<int, long> _growthRates = new();
        private Dictionary<int, long> _baseStats = new();

        public EProgressionModule ModuleName => EProgressionModule.Level;

        public LevelProgressionProvider()
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
                long growth = _growthRates.ContainsKey(pair.Key) ? _growthRates[pair.Key] : 0;
                long total = pair.Value + ((_level - 1) * growth);
                targetMap.Add(pair.Key, total);
            }
        }
    }
}
