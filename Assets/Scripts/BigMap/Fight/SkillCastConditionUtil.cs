using System.Collections.Generic;
using cfg.demo;
using UnityEngine;

namespace My.Map.Entity
{
    public static class SkillCastConditionUtil
    {
        public static bool CheckAll(BaseUnitLogicEntity entity, IReadOnlyList<SkillCastCondition> conditions)
        {
            if (entity == null || conditions == null || conditions.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < conditions.Count; i++)
            {
                if (!CheckSingle(entity, conditions[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckSkill(BaseUnitLogicEntity entity, EntitySkillData skillConfig)
        {
            if (skillConfig == null)
            {
                return true;
            }

            return CheckAll(entity, skillConfig.CastConditions);
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
    }
}
