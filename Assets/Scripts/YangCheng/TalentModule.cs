using System;
using System.Collections.Generic;

namespace My.Player
{
    // 单个已解锁天赋节点：通过 IProgressionSource 累加固定属性（与 Luban TalentNode.stat_bonuses 对应）
    public class TalentNodeProgressionProvider : IProgressionSource
    {
        public event Action<IProgressionSource> OnStatsChanged;

        readonly List<StatPair> _bonuses;

        public EProgressionModule ModuleName => EProgressionModule.Talent;

        public TalentNodeProgressionProvider(IReadOnlyList<StatPair> bonuses)
        {
            _bonuses = new List<StatPair>();
            if (bonuses != null)
            {
                _bonuses.AddRange(bonuses);
            }
        }

        public void EvaluateStats(StatMap targetMap)
        {
            for (int i = 0; i < _bonuses.Count; i++)
            {
                targetMap.Add(_bonuses[i].ID, _bonuses[i].Value);
            }
        }

        public void NotifyChanged()
        {
            OnStatsChanged?.Invoke(this);
        }
    }
}
