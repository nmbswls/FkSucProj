using System;
using System.Collections.Generic;

namespace My.Map.Entity
{
    public enum ESkillCostType
    {
        None = 0,
        Resource = 1,
    }

    public enum ESkillCostTarget
    {
        Caster = 0,
        HostEntity = 1,
    }

    [Serializable]
    public class SkillCostEntry
    {
        public ESkillCostType CostType = ESkillCostType.Resource;
        public string ResourceId = string.Empty;
        public long Amount = 1;
        public ESkillCostTarget Target = ESkillCostTarget.Caster;
    }

    // MVP：技能表 Luban cast_costs 落地前的代码侧消耗配置
    public static class SkillCastCostLibrary
    {
        static readonly Dictionary<string, List<SkillCostEntry>> _bySkillId = new();

        public static void Register(string skillId, IReadOnlyList<SkillCostEntry> costs)
        {
            if (string.IsNullOrEmpty(skillId) || costs == null || costs.Count == 0)
            {
                return;
            }

            _bySkillId[skillId] = new List<SkillCostEntry>(costs);
        }

        public static IReadOnlyList<SkillCostEntry> GetCosts(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return null;
            }

            return _bySkillId.TryGetValue(skillId, out var list) ? list : null;
        }
    }
}
