using System.Collections.Generic;
using cfg.demo;
using My.Config;

namespace My.Home
{
    public sealed class FacilityDevelopmentDefinition
    {
        public string FacilityId;
        public string DisplayName;
        public int SortOrder;
        public int MaxLevel;
        public string Icon;
    }

    public sealed class FacilityDevelopmentLevel
    {
        public string FacilityId;
        public int Level;
        public string DisplayName;
        public string Desc;
        public List<CommonCheckCond> UnlockConds;
        public List<TalentUnlockCost> UnlockCosts;
        public int OutputInterval = 1;
        public List<TalentUnlockCost> OutputItems;
        public List<FacilityEffect> Effects;
    }

    public static class FacilityDevelopmentCatalog
    {
        public static FacilityDevelopmentDefinition GetDefinition(string facilityId)
        {
            var row = CfgMgr.Cfgs?.TbFacilityDevelopmentDefinition?.Get(facilityId);
            if (row == null)
            {
                return null;
            }

            return new FacilityDevelopmentDefinition
            {
                FacilityId = row.FacilityId,
                DisplayName = row.DisplayName,
                SortOrder = row.SortOrder,
                MaxLevel = row.MaxLevel,
                Icon = row.Icon,
            };
        }

        public static FacilityDevelopmentLevel GetLevel(string facilityId, int level)
        {
            var row = CfgMgr.Cfgs?.TbFacilityDevelopmentLevel?.Get(facilityId, level);
            if (row == null)
            {
                return null;
            }

            return new FacilityDevelopmentLevel
            {
                FacilityId = row.FacilityId,
                Level = row.Level,
                DisplayName = row.DisplayName,
                Desc = row.Desc,
                UnlockConds = row.UnlockConds,
                UnlockCosts = row.UnlockCosts,
                OutputInterval = row.OutputInterval > 0 ? row.OutputInterval : 1,
                OutputItems = row.OutputItems,
                Effects = TownFacilityEffectCatalog.MapList(row.Effects),
            };
        }

        public static IReadOnlyList<FacilityDevelopmentDefinition> GetDefinitions(string logicAreaId)
        {
            return TownFacilitySiteCatalog.GetDefinitionsForMap(logicAreaId);
        }
    }
}
