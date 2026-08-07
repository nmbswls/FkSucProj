using System;
using System;
using System.Collections.Generic;

namespace My.Player
{
    // 单个已解锁天赋节点：通过 IProgressionSource 累加固定属性（与 Luban TalentNode.stat_bonuses 对应）
    public class TalentNodeProgressionProvider : IProgressionSource, IProgressionSkillModifierSource
    {
        public event Action<IProgressionSource> OnStatsChanged;

        readonly List<StatPair> _bonuses;
        readonly List<SkillModifierSpec> _skillModifiers;

        public EProgressionModule ModuleName => EProgressionModule.Talent;

        public TalentNodeProgressionProvider(
            IReadOnlyList<StatPair> bonuses,
            IReadOnlyList<SkillModifierSpec> skillModifiers = null)
        {
            _bonuses = new List<StatPair>();
            _skillModifiers = new List<SkillModifierSpec>();
            if (bonuses != null)
            {
                _bonuses.AddRange(bonuses);
            }

            if (skillModifiers != null)
            {
                _skillModifiers.AddRange(skillModifiers);
            }
        }

        public void CollectSkillModifiers(List<SkillModifierSpec> output)
        {
            if (output == null)
            {
                return;
            }

            output.AddRange(_skillModifiers);
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
