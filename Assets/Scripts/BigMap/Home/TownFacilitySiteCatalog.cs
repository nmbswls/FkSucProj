using System.Collections.Generic;
using cfg.demo;
using My.Config;

namespace My.Home
{
    // 地图设施站点配表查询（town_facility_site）
    public static class TownFacilitySiteCatalog
    {
        public static TownFacilitySiteConfig Get(int siteId)
        {
            if (siteId <= 0)
            {
                return null;
            }

            return CfgMgr.Cfgs?.TbTownFacilitySite?.GetOrDefault(siteId);
        }

        public static TownFacilitySiteConfig FindByMapAndFacility(string mapId, string facilityCfgId)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(facilityCfgId))
            {
                return null;
            }

            var table = CfgMgr.Cfgs?.TbTownFacilitySite?.DataList;
            if (table == null)
            {
                return null;
            }

            foreach (var row in table)
            {
                if (row == null)
                {
                    continue;
                }

                if (row.MapId == mapId && row.FacilityCfgId == facilityCfgId)
                {
                    return row;
                }
            }

            return null;
        }

        public static TownFacilitySiteConfig FindByMapAndSlot(string mapId, string slot)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(slot))
            {
                return null;
            }

            var table = CfgMgr.Cfgs?.TbTownFacilitySite?.DataList;
            if (table == null)
            {
                return null;
            }

            foreach (var row in table)
            {
                if (row != null && row.MapId == mapId && row.Slot == slot)
                {
                    return row;
                }
            }

            return null;
        }

        public static IReadOnlyList<FacilityDevelopmentDefinition> GetDefinitionsForMap(string mapId)
        {
            var result = new List<FacilityDevelopmentDefinition>();
            foreach (var site in GetSitesForMap(mapId))
            {
                var def = FacilityDevelopmentCatalog.GetDefinition(site.FacilityCfgId);
                if (def != null)
                {
                    result.Add(def);
                }
            }

            return result;
        }

        public static IReadOnlyList<TownFacilitySiteConfig> GetSitesForMap(string mapId)
        {
            var result = new List<TownFacilitySiteConfig>();
            var table = CfgMgr.Cfgs?.TbTownFacilitySite?.DataList;
            if (table == null || string.IsNullOrEmpty(mapId))
            {
                return result;
            }

            foreach (var row in table)
            {
                if (row != null && row.MapId == mapId)
                {
                    result.Add(row);
                }
            }

            result.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
            return result;
        }

        public static bool HasSites(string mapId)
        {
            var table = CfgMgr.Cfgs?.TbTownFacilitySite?.DataList;
            if (table == null || string.IsNullOrEmpty(mapId))
            {
                return false;
            }

            foreach (var row in table)
            {
                if (row != null && row.MapId == mapId)
                {
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyList<string> GetDistinctMapIds()
        {
            var result = new List<string>();
            var table = CfgMgr.Cfgs?.TbTownFacilitySite?.DataList;
            if (table == null)
            {
                return result;
            }

            foreach (var row in table)
            {
                if (row == null || string.IsNullOrEmpty(row.MapId))
                {
                    continue;
                }

                if (!result.Contains(row.MapId))
                {
                    result.Add(row.MapId);
                }
            }

            return result;
        }
    }
}
