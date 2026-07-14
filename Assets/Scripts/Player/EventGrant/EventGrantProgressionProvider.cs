using System;
using System.Collections.Generic;

namespace My.Player
{
    // EventGrant AssemblePassive：属性 + 被动技能贡献
    public sealed class EventGrantProgressionProvider : IProgressionSource, IProgressionSkillSource
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

        public void CollectContributedSkills(HashSet<string> applied, List<(string skillId, int level)> output)
        {
            _owner?.CollectQualifiedPassiveSkills(applied, output);
        }

        public void NotifyChanged()
        {
            OnStatsChanged?.Invoke(this);
        }
    }
}
