using System;

namespace My.Player
{
    public sealed class BodyPartProgressionProvider : IProgressionSource
    {
        readonly PlayerBodyPartSystem _bodyPartSystem;

        public event Action<IProgressionSource> OnStatsChanged;

        public EProgressionModule ModuleName => EProgressionModule.BodyPart;

        public BodyPartProgressionProvider(PlayerBodyPartSystem bodyPartSystem)
        {
            _bodyPartSystem = bodyPartSystem;
        }

        public void EvaluateStats(StatMap targetMap)
        {
            _bodyPartSystem?.AccumulateGlobalBonuses(targetMap);
        }

        public void NotifyChanged()
        {
            OnStatsChanged?.Invoke(this);
        }
    }
}
