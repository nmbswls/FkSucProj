using System;

namespace My.UI.Home
{
    public enum FacilityFunctionType
    {
        None = 0,
        Tavern = 1,
    }

    public static class FacilityFunctionTypeResolver
    {
        public static FacilityFunctionType Resolve(string facilityId)
        {
            return string.Equals(facilityId, "tavern", StringComparison.OrdinalIgnoreCase)
                ? FacilityFunctionType.Tavern
                : FacilityFunctionType.None;
        }
    }
}
