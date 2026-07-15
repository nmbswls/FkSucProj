using System;
using cfg.demo;

namespace My.Home
{
    public static class TownFacilityCondKeys
    {
        public const string SiteLevelPrefix = "town_facility_site:";
        public const string LevelPrefix = "town_facility_level:";

        public static string BuildSiteLevelCond(int siteId)
        {
            return $"{SiteLevelPrefix}{siteId}";
        }

        public static bool TryParseSiteLevelCond(string param5, out int siteId)
        {
            siteId = 0;
            if (string.IsNullOrEmpty(param5) || !param5.StartsWith(SiteLevelPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var body = param5.Substring(SiteLevelPrefix.Length);
            return int.TryParse(body, out siteId) && siteId > 0;
        }

        public static string BuildLevelCond(string logicAreaId, string facilityId)
        {
            return $"{LevelPrefix}{logicAreaId}:{facilityId}";
        }

        public static bool TryParseLevelCond(string param5, out string logicAreaId, out string facilityId)
        {
            logicAreaId = null;
            facilityId = null;
            if (string.IsNullOrEmpty(param5) || !param5.StartsWith(LevelPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var body = param5.Substring(LevelPrefix.Length);
            int split = body.IndexOf(':');
            if (split <= 0 || split >= body.Length - 1)
            {
                return false;
            }

            logicAreaId = body.Substring(0, split);
            facilityId = body.Substring(split + 1);
            return !string.IsNullOrEmpty(logicAreaId) && !string.IsNullOrEmpty(facilityId);
        }

        public static bool CondReferencesSite(CommonCheckCond cond, int siteId)
        {
            if (cond == null || siteId <= 0)
            {
                return false;
            }

            return TryParseSiteLevelCond(cond.Param5, out var parsedSiteId)
                   && parsedSiteId == siteId;
        }

        public static bool CondReferencesFacility(CommonCheckCond cond, string logicAreaId, string facilityId)
        {
            if (cond == null || string.IsNullOrEmpty(facilityId))
            {
                return false;
            }

            if (TryParseSiteLevelCond(cond.Param5, out var siteId))
            {
                var site = TownFacilitySiteCatalog.Get(siteId);
                return site != null
                       && site.FacilityCfgId == facilityId
                       && (string.IsNullOrEmpty(logicAreaId) || site.MapId == logicAreaId);
            }

            if (!TryParseLevelCond(cond.Param5, out var area, out var fid))
            {
                return false;
            }

            return string.Equals(fid, facilityId, StringComparison.Ordinal)
                   && (string.IsNullOrEmpty(logicAreaId) || string.Equals(area, logicAreaId, StringComparison.Ordinal));
        }
    }
}
