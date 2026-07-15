using System;
using System.Collections.Generic;

namespace My.Player
{
    // 图鉴兼容适配器：旧系统不再向玩家属性或技能贡献调精效果。
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
        }

        public void CollectContributedSkills(HashSet<string> applied, List<(string skillId, int level)> output)
        {
        }

        public void NotifyChanged()
        {
            OnStatsChanged?.Invoke(this);
        }
    }
}
