using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;

namespace My.Home
{
    public sealed class FacilityRenovationDefinition
    {
        public string FacilityId;
        public string RenovationId;
        public string DisplayName;
        public string Desc;
        public int MinLevel;
        public int SortOrder;
        public List<CommonCheckCond> UnlockConds;
        public List<TalentUnlockCost> LearnCosts;
        public int OutputInterval = 1;
        public List<TalentUnlockCost> OutputItems;
        public List<FacilityEffect> Effects;
    }

    public static class FacilityRenovationCatalog
    {
        public static FacilityRenovationDefinition Get(string facilityId, string renovationId)
        {
            var row = CfgMgr.Cfgs?.TbFacilityRenovation?.Get(facilityId, renovationId);
            return row == null ? null : Map(row);
        }

        public static List<FacilityRenovationDefinition> GetRenovationsForFacility(string facilityId)
        {
            var result = new List<FacilityRenovationDefinition>();
            var table = CfgMgr.Cfgs?.TbFacilityRenovation;
            if (table?.DataList == null)
            {
                return result;
            }

            foreach (var row in table.DataList)
            {
                if (row == null || row.FacilityId != facilityId)
                {
                    continue;
                }

                result.Add(Map(row));
            }

            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }

        public static bool CanLearn(
            FacilityRenovationDefinition renovation,
            int facilityLevel,
            GameLogicManager glm,
            out string failReason)
        {
            failReason = null;
            if (renovation == null)
            {
                failReason = "invalid_renovation";
                return false;
            }

            if (facilityLevel < renovation.MinLevel)
            {
                failReason = "renovation_level_too_low";
                return false;
            }

            if (renovation.UnlockConds != null && renovation.UnlockConds.Count > 0
                && (glm == null || !glm.CheckCommonCondsAll(renovation.UnlockConds)))
            {
                failReason = "renovation_cond_fail";
                return false;
            }

            var pdm = glm?.playerDataManager;
            if (pdm != null && renovation.LearnCosts != null)
            {
                foreach (var cost in renovation.LearnCosts)
                {
                    if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                    {
                        continue;
                    }

                    if (!pdm.CheckHaveItem(cost.ItemId, cost.Count))
                    {
                        failReason = "not_enough_item";
                        return false;
                    }
                }
            }

            return true;
        }

        static FacilityRenovationDefinition Map(FacilityRenovationConfig row)
        {
            return new FacilityRenovationDefinition
            {
                FacilityId = row.FacilityId,
                RenovationId = row.RenovationId,
                DisplayName = row.DisplayName,
                Desc = row.Desc,
                MinLevel = row.MinLevel,
                SortOrder = row.SortOrder,
                UnlockConds = row.UnlockConds,
                LearnCosts = row.LearnCosts,
                OutputInterval = row.OutputInterval > 0 ? row.OutputInterval : 1,
                OutputItems = row.OutputItems,
                Effects = TownFacilityEffectCatalog.MapList(row.Effects),
            };
        }
    }
}
