using System;
using System.Collections.Generic;

namespace My.Player
{
    // 武器快捷栏 itemId → 运行时绑定技能（初版字面量；后续可迁 Luban）
    public static class WeaponQuickBarSkillBinding
    {
        static readonly Dictionary<string, string> ItemIdToSkillV1 = new(StringComparer.Ordinal)
        {
            { "small_knife", "shoot_knife" },
        };

        public static bool TryResolveSkillId(string itemId, out string skillId)
        {
            skillId = null;
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            if (ItemIdToSkillV1.TryGetValue(itemId, out var mapped) && !string.IsNullOrEmpty(mapped))
            {
                skillId = mapped;
                return true;
            }

            return false;
        }
    }
}
