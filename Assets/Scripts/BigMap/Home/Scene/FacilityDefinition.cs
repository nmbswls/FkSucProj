using System;
using My.Config;

namespace My
{
    [Flags]
    public enum FacilityCapability
    {
        None = 0,
        Interaction = 1 << 0,
        Storage = 1 << 1,
        Trade = 1 << 2,
        Workforce = 1 << 3,
        Operation = 1 << 4,
        Development = 1 << 5,
        DailyOutput = 1 << 6,
    }

    public sealed class FacilityDefinition
    {
        public string FacilityId;
        public string DisplayName;
        public FacilityCapability Capabilities;
        public int MaxWorkforce;
        public string PresentationPrefab;
        public string InteractionHandlerId;
    }

    public static class FacilityDefinitionCatalog
    {
        public static FacilityDefinition Get(string facilityId)
        {
            if (string.IsNullOrEmpty(facilityId)) return null;
            var row = CfgMgr.Cfgs?.TbFacilityDefinition?.GetOrDefault(facilityId);
            if (row == null) return null;
            return new FacilityDefinition
            {
                FacilityId = row.FacilityId,
                DisplayName = row.DisplayName,
                Capabilities = (FacilityCapability)row.Capabilities,
                MaxWorkforce = row.MaxWorkforce,
                PresentationPrefab = row.PresentationPrefab,
                InteractionHandlerId = row.InteractionHandlerId,
            };
        }
    }
}
