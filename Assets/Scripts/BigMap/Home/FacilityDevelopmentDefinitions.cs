using System.Collections.Generic;
using cfg.demo;
using My.Config;

namespace My.Home
{
    public sealed class FacilityDevelopmentDefinition
    {
        public string LogicAreaId;
        public string FacilityId;
        public string DisplayName;
        public int SortOrder;
        public int MaxLevel;
        public string Icon;
    }

    public sealed class FacilityDevelopmentLevel
    {
        public string LogicAreaId;
        public string FacilityId;
        public int Level;
        public string DisplayName;
        public string Desc;
        public List<CommonCheckCond> UnlockConds;
        public List<TalentUnlockCost> UnlockCosts;
        public List<TalentUnlockCost> DailyOutputs;
    }

    public static class FacilityDevelopmentCatalog
    {
        public static FacilityDevelopmentDefinition GetDefinition(string logicAreaId, string facilityId)
        {
            var row = CfgMgr.Cfgs?.TbFacilityDevelopmentDefinition?.Get(logicAreaId, facilityId);
            if (row == null) return null;
            return new FacilityDevelopmentDefinition
            {
                LogicAreaId = row.LogicAreaId,
                FacilityId = row.FacilityId,
                DisplayName = row.DisplayName,
                SortOrder = row.SortOrder,
                MaxLevel = row.MaxLevel,
                Icon = row.Icon,
            };
        }

        public static FacilityDevelopmentLevel GetLevel(string logicAreaId, string facilityId, int level)
        {
            var row = CfgMgr.Cfgs?.TbFacilityDevelopmentLevel?.Get(logicAreaId, facilityId, level);
            if (row == null) return null;
            return new FacilityDevelopmentLevel
            {
                LogicAreaId = row.LogicAreaId,
                FacilityId = row.FacilityId,
                Level = row.Level,
                DisplayName = row.DisplayName,
                Desc = row.Desc,
                UnlockConds = row.UnlockConds,
                UnlockCosts = row.UnlockCosts,
                DailyOutputs = row.DailyOutputs,
            };
        }

        public static IReadOnlyList<FacilityDevelopmentDefinition> GetDefinitions(string logicAreaId)
        {
            var result = new List<FacilityDevelopmentDefinition>();
            var table = CfgMgr.Cfgs?.TbFacilityDevelopmentDefinition;
            if (table == null) return result;
            foreach (var row in table.DataList)
            {
                if (row == null || row.LogicAreaId != logicAreaId) continue;
                result.Add(new FacilityDevelopmentDefinition
                {
                    LogicAreaId = row.LogicAreaId,
                    FacilityId = row.FacilityId,
                    DisplayName = row.DisplayName,
                    SortOrder = row.SortOrder,
                    MaxLevel = row.MaxLevel,
                    Icon = row.Icon,
                });
            }
            result.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
            return result;
        }
    }
}
