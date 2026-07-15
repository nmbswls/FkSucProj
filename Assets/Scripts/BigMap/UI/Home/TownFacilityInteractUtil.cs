using My.Config;
using My.Home;
using My.Map;
using Config;

namespace My.UI.Home
{
    public static class TownFacilityInteractUtil
    {
        public const int SelectManageFacility = 9;

        public static void OpenDetailBySite(int siteId)
        {
            if (siteId <= 0)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var logicAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(glm?.AreaManager);
            var site = My.Home.TownFacilitySiteCatalog.Get(siteId);
            if (site == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(logicAreaId))
            {
                logicAreaId = site.MapId;
            }

            TownFacilityDetailPanel.Open(new TownFacilityDetailOpenArgs
            {
                SiteId = siteId,
                FacilityId = site.FacilityCfgId,
                LogicAreaId = logicAreaId,
            });
        }

        public static void OpenDetail(string facilityId, long instanceId = 0)
        {
            if (string.IsNullOrEmpty(facilityId))
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var logicAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(glm?.AreaManager);
            var site = !string.IsNullOrEmpty(logicAreaId)
                ? My.Home.TownFacilitySiteCatalog.FindByMapAndFacility(logicAreaId, facilityId)
                : null;
            if (site != null && instanceId == 0)
            {
                OpenDetailBySite(site.Id);
                return;
            }

            if (instanceId == 0)
            {
                var hm = glm?.homeDataManager;
                hm?.RefreshFixedFacilities();
                var found = hm?.FixedFacilities.Find(
                    f => !f.Removed && f.FacilityId == facilityId);
                if (found != null)
                {
                    instanceId = found.InstanceId;
                }
            }

            TownFacilityDetailPanel.Open(new TownFacilityDetailOpenArgs
            {
                InstanceId = instanceId,
                FacilityId = facilityId,
                LogicAreaId = logicAreaId,
            });
        }

        public static string ResolveFacilityIdFromRuin(string ruinCfgId)
        {
            if (string.IsNullOrEmpty(ruinCfgId))
            {
                return null;
            }

            var cfg = MapFixFacilityCfgLoader.Get(ruinCfgId);
            if (cfg == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(cfg.TargetFacilityId))
            {
                return cfg.TargetFacilityId;
            }

            return string.IsNullOrEmpty(cfg.PlacementId) ? null : cfg.PlacementId;
        }
    }
}
