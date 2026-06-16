using System.Collections.Generic;
using cfg.demo;
using My.Map.Entity;
using My.Map.Fight;
using UnityEngine;

namespace My.Map.Entity
{
    public static class SkillCastConditionUtil
    {
        public static bool CheckAll(BaseUnitLogicEntity entity, IReadOnlyList<SkillCastCondition> conditions)
        {
            return TryCheckAll(entity, conditions, out _);
        }

        public static bool CheckSkill(BaseUnitLogicEntity entity, EntitySkillData skillConfig)
        {
            return TryCheckSkill(entity, skillConfig, out _);
        }

        public static bool TryCheckSkill(BaseUnitLogicEntity entity, EntitySkillData skillConfig, out string denyMessage)
        {
            denyMessage = null;
            if (skillConfig == null)
            {
                return true;
            }

            return TryCheckAll(entity, skillConfig.CastConditions, out denyMessage);
        }

        public static bool TryEvaluateReadiness(
            BaseUnitLogicEntity entity,
            MapEntitySkillManager skillMgr,
            EntitySkillData skillConfig,
            out string denyMessage)
        {
            denyMessage = null;
            if (entity == null || skillConfig == null)
            {
                denyMessage = "无法使用";
                return false;
            }

            if (skillConfig.IsPassive)
            {
                denyMessage = "被动技能无法施放";
                return false;
            }

            if (skillMgr != null
                && skillMgr.SkillRuntimes.TryGetValue(skillConfig.SkillId, out var skillRuntime)
                && skillRuntime != null
                && skillRuntime.cooldown > 0)
            {
                denyMessage = "技能冷却中";
                return false;
            }

            if (!skillConfig.NeedHMode)
            {
                if (entity.IsInHBehaveMode())
                {
                    denyMessage = "当前状态无法使用";
                    return false;
                }
            }
            else if (!entity.IsInHBehaveMode())
            {
                denyMessage = "需在特殊状态下使用";
                return false;
            }

            if (FightEffectInterceptors.TryGetSkillCastDeny(entity, out denyMessage))
            {
                return false;
            }

            return TryCheckAll(entity, skillConfig.CastConditions, out denyMessage);
        }

        public static bool TryEvaluateReadiness(
            BaseUnitLogicEntity entity,
            MapEntitySkillManager skillMgr,
            string skillId,
            out string denyMessage)
        {
            denyMessage = null;
            if (string.IsNullOrEmpty(skillId))
            {
                denyMessage = "无法使用";
                return false;
            }

            var skillConfig = SkillLibrary.GetSkillConfig(skillId);
            if (skillConfig == null)
            {
                denyMessage = "技能不存在";
                return false;
            }

            return TryEvaluateReadiness(entity, skillMgr, skillConfig, out denyMessage);
        }

        static bool TryCheckAll(
            BaseUnitLogicEntity entity,
            IReadOnlyList<SkillCastCondition> conditions,
            out string denyMessage)
        {
            denyMessage = null;
            if (entity == null || conditions == null || conditions.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < conditions.Count; i++)
            {
                if (!TryCheckSingle(entity, conditions[i], out denyMessage))
                {
                    return false;
                }
            }

            return true;
        }

        static bool TryCheckSingle(
            BaseUnitLogicEntity entity,
            SkillCastCondition condition,
            out string denyMessage)
        {
            denyMessage = null;
            if (condition == null || condition.Type == ESkillCastConditionType.None)
            {
                return true;
            }

            if (CheckSingle(entity, condition))
            {
                return true;
            }

            denyMessage = ResolveDenyMessage(condition);
            return false;
        }

        static bool CheckSingle(BaseUnitLogicEntity entity, SkillCastCondition condition)
        {
            if (condition == null || condition.Type == ESkillCastConditionType.None)
            {
                return true;
            }

            switch (condition.Type)
            {
                case ESkillCastConditionType.HMode:
                    return entity.IsInHBehaveMode();

                case ESkillCastConditionType.QueenMode:
                    return entity is PlayerLogicEntity queenPlayer && queenPlayer.IsQueenMode;

                case ESkillCastConditionType.NoQueenMode:
                    return entity is not PlayerLogicEntity noQueenPlayer || !noQueenPlayer.IsQueenMode;

                case ESkillCastConditionType.NotInBattle:
                    return !entity.IsInCombat;

                case ESkillCastConditionType.InBattle:
                    return entity.IsInCombat;

                default:
                    Debug.LogWarning($"[SkillCastConditionUtil] Unknown cast condition type: {condition.Type}");
                    return true;
            }
        }

        static string ResolveDenyMessage(SkillCastCondition condition)
        {
            if (!string.IsNullOrEmpty(condition.Param3))
            {
                return condition.Param3;
            }

            return condition.Type switch
            {
                ESkillCastConditionType.HMode => "需在特殊状态下使用",
                ESkillCastConditionType.QueenMode => "需在女王形态下使用",
                ESkillCastConditionType.NoQueenMode => "女王形态下无法使用",
                ESkillCastConditionType.NotInBattle => "战斗中无法使用",
                ESkillCastConditionType.InBattle => "需在战斗中使用",
                _ => "无法使用",
            };
        }
    }
}
