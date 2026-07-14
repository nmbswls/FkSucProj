using System;
using System.Collections.Generic;

namespace My.Player
{
    // 图鉴：等级属性 + 调精装备被动技能
    public sealed class JingYuanCodexProgressionProvider : IProgressionSource, IProgressionSkillSource
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

        public void CollectContributedSkills(HashSet<string> applied, List<(string skillId, int level)> output)
        {
            _owner?.CollectEquippedTunePassiveSkills(applied, output);
        }

        public void NotifyChanged()
        {
            OnStatsChanged?.Invoke(this);
        }
    }
}
