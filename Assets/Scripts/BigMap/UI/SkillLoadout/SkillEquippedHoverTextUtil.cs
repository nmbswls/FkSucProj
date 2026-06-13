using cfg.demo;
using My.Map.Entity;

namespace My.UI.SkillLoadout
{
    public static class SkillEquippedHoverTextUtil
    {
        public static string ResolveDisplayName(EntitySkillData cfg, string skillId)
        {
            if (cfg != null && !string.IsNullOrEmpty(cfg.Desc))
            {
                return cfg.Desc;
            }

            return string.IsNullOrEmpty(skillId) ? string.Empty : skillId;
        }

        public static string ResolveSummary(EntitySkillData cfg)
        {
            if (cfg == null)
            {
                return string.Empty;
            }

            if (cfg.IsPassive)
            {
                return "被动技能，装备后持续生效";
            }

            return "主动技能，可在战斗中施放";
        }

        public static string ResolveStateText(string skillId, EntitySkillData cfg)
        {
            if (string.IsNullOrEmpty(skillId) || cfg == null)
            {
                return string.Empty;
            }

            if (cfg.IsPassive)
            {
                return "已装备";
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                return "已装备";
            }

            if (SkillCastConditionUtil.TryEvaluateReadiness(
                    player,
                    player.ablilityManager,
                    skillId,
                    out _))
            {
                return "可施放";
            }

            return "暂不可施放";
        }

        public static string ResolveHintText(string skillId, EntitySkillData cfg)
        {
            if (string.IsNullOrEmpty(skillId) || cfg == null)
            {
                return string.Empty;
            }

            if (cfg.IsPassive)
            {
                return "被动技能自动生效";
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                return string.Empty;
            }

            if (SkillCastConditionUtil.TryEvaluateReadiness(
                    player,
                    player.ablilityManager,
                    skillId,
                    out var denyMessage))
            {
                return "满足施放条件";
            }

            return string.IsNullOrEmpty(denyMessage) ? "暂不满足施放条件" : denyMessage;
        }
    }
}
