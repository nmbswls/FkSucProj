using System.Collections.Generic;
using My.Map;
using My.Map.Fight;
using UnityEngine;

namespace My.Map.Entity
{
    public static class SkillCostUtil
    {
        public static bool CanPay(IEntityAttributeOwner payer, IReadOnlyList<SkillCostEntry> costs)
        {
            if (payer == null || costs == null || costs.Count == 0)
            {
                return true;
            }

            foreach (var cost in costs)
            {
                if (cost == null || cost.CostType == ESkillCostType.None || cost.Amount <= 0)
                {
                    continue;
                }

                switch (cost.CostType)
                {
                    case ESkillCostType.Resource:
                        {
                            if (string.IsNullOrEmpty(cost.ResourceId))
                            {
                                Debug.LogWarning("[SkillCostUtil] Resource cost missing resource id.");
                                return false;
                            }

                            if (payer.GetAttr(cost.ResourceId) < cost.Amount)
                            {
                                return false;
                            }
                        }
                        break;
                }
            }

            return true;
        }

        public static bool Pay(IEntityAttributeOwner payer, long? srcEntityId, IReadOnlyList<SkillCostEntry> costs)
        {
            if (payer == null || costs == null || costs.Count == 0)
            {
                return true;
            }

            if (!CanPay(payer, costs))
            {
                return false;
            }

            foreach (var cost in costs)
            {
                if (cost == null || cost.CostType == ESkillCostType.None || cost.Amount <= 0)
                {
                    continue;
                }

                switch (cost.CostType)
                {
                    case ESkillCostType.Resource:
                        {
                            payer.ApplyResourceChange(
                                cost.ResourceId,
                                -cost.Amount,
                                false,
                                FightStruct.EDmgFlag.None,
                                srcEntityId);
                        }
                        break;
                }
            }

            if (payer is LogicEntityBase logicEntity)
            {
                logicEntity.ForceCommitAttribute();
            }

            return true;
        }

        public static IReadOnlyList<SkillCostEntry> ResolveAbilityCastCosts(MapAbilitySpecConfig abilityCfg)
        {
            if (abilityCfg?.CastCosts == null || abilityCfg.CastCosts.Count == 0)
            {
                return null;
            }

            return abilityCfg.CastCosts;
        }
    }
}
