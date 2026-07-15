using System.Collections.Generic;
using cfg.demo;
using My.Home;
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
            return FacilityDevelopmentCatalog.GetDefinitions(logicAreaId).Count > 0;
        }

        public static IReadOnlyList<string> GetDistinctLogicAreaIdsWithDevelopableFacilities()
        {
            _logicAreaIds.Clear();
            var table = CfgMgr.Cfgs?.TbFacilityDevelopmentDefinition;
            if (table == null) return _logicAreaIds;
            foreach (var row in table.DataList)
            {
                if (row == null || string.IsNullOrEmpty(row.LogicAreaId)) continue;
                if (!_logicAreaIds.Contains(row.LogicAreaId)) _logicAreaIds.Add(row.LogicAreaId);
            }
            return _logicAreaIds;
        }
    }
}
