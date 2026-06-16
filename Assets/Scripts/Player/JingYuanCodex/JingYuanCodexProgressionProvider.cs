using System;
using cfg.demo;

namespace My.Player
{
    // 图鉴等级累计永久属性
    public sealed class JingYuanCodexProgressionProvider : IProgressionSource
    {
        public event Action<IProgressionSource> OnStatsChanged;

        readonly PlayerJingYuanCodexSystem _owner;

        public EProgressionModule ModuleName => EProgressionModule.JingYuanCodex;

        public JingYuanCodexProgressionProvider(PlayerJingYuanCodexSystem owner)
        {
            _owner = owner;
        }

        public void EvaluateStats(StatMap targetMap)
        {
            _owner?.AccumulateProgressionStats(targetMap);
        }

        public void NotifyChanged()
        {
            OnStatsChanged?.Invoke(this);
        }
    }
}
