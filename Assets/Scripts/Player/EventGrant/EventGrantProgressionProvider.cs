using System;

namespace My.Player
{
    // EventGrant AssemblePassive 的属性加成源
    public sealed class EventGrantProgressionProvider : IProgressionSource
    {
        public event Action<IProgressionSource> OnStatsChanged;

        readonly PlayerEventGrantSystem _owner;

        public EProgressionModule ModuleName => EProgressionModule.EventGrant;

        public EventGrantProgressionProvider(PlayerEventGrantSystem owner)
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
