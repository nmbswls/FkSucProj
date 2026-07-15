using System.Collections.Generic;
using cfg.demo;
using My.Map;
using My.Map.Logic;

namespace My.Config
{
    public static class TownFacilityUtil
    {
        static readonly List<string> _logicAreaIds = new();

        public static string ResolveLogicAreaId(AreaOverlayStateInfo overlay)
        {
            if (overlay == null) return null;
            return !string.IsNullOrEmpty(overlay.BelongVariantInfo?.LogicAreaId)
                ? overlay.BelongVariantInfo.LogicAreaId
                : overlay.VarId;
        }

        public static string ResolveCurrentLogicAreaId(GameLogicAreaManager areaManager)
        {
            return ResolveLogicAreaId(areaManager?.cacheMapOverlayCfg);
        }

        public static bool SupportsControlDegree(string logicAreaId)
        {
            return !string.IsNullOrEmpty(logicAreaId)
                && CfgMgr.Cfgs.TbLogicAreaHomesteadReq.GetOrDefault(logicAreaId) != null;
        }

        public static LogicAreaHomesteadReq GetHomesteadReq(string logicAreaId)
        {
            return string.IsNullOrEmpty(logicAreaId)
                ? null
                : CfgMgr.Cfgs.TbLogicAreaHomesteadReq.GetOrDefault(logicAreaId);
        }

        public static bool CanAnnexAsHomestead(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId)) return false;
            var info = CfgMgr.Cfgs.TbLogicAreaInfo.GetOrDefault(logicAreaId);
            return info != null && info.CanAnnexHomestead;
        }

        public static bool HasDevelopableFacilities(string logicAreaId)
        {
            return My.Home.TownFacilitySiteCatalog.HasSites(logicAreaId);
        }

        public static IReadOnlyList<string> GetDistinctLogicAreaIdsWithDevelopableFacilities()
        {
            return My.Home.TownFacilitySiteCatalog.GetDistinctMapIds();
        }
    }
}
