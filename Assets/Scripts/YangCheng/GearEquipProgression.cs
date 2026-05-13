using System;
using System.Collections.Generic;

namespace My.Player
{
    // 单件装备对养成 StatMap 的加成（与 Luban 表扩展对接；当前可无词条）
    public sealed class GearEquipProgressionProvider : IProgressionSource
    {
        public event Action<IProgressionSource> OnStatsChanged;

        readonly List<StatPair> _bonuses;
        public string DebugItemId { get; }

        public EProgressionModule ModuleName => EProgressionModule.Gear;

        public GearEquipProgressionProvider(string itemId, IReadOnlyList<StatPair> bonuses)
        {
            DebugItemId = itemId;
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

        public void NotifyChanged() => OnStatsChanged?.Invoke(this);
    }
}
