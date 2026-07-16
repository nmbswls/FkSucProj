using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Logic;

namespace My.Config
{
    // 区域：logic map 的上一层概念；初版每个 region 仅对应一个可控制 home 城镇
    public static class GameRegionUtil
    {
        const string DefaultRegionKey = "default";

        static readonly Dictionary<string, string> _homeLogicAreaByRegion = new();

        public static string ResolveRegionKey(AreaOverlayStateInfo overlay)
        {
            if (overlay == null)
            {
                return DefaultRegionKey;
            }

            return ResolveRegionKeyByVarId(overlay.VarId);
        }

        public static string ResolveRegionKeyByVarId(string varId)
        {
            if (string.IsNullOrEmpty(varId))
            {
                return DefaultRegionKey;
            }

            var table = CfgMgr.Cfgs?.TbWorldMapBigMapLayer;
            if (table != null)
            {
                foreach (var row in table.DataList)
                {
                    if (row != null && row.AreaVarId == varId && !string.IsNullOrEmpty(row.RegionKey))
                    {
                        return row.RegionKey;
                    }
                }
            }

            return DefaultRegionKey;
        }

        public static string ResolveHomeLogicAreaId(string regionKey)
        {
            if (string.IsNullOrEmpty(regionKey))
            {
                regionKey = DefaultRegionKey;
            }

            if (_homeLogicAreaByRegion.TryGetValue(regionKey, out var cached)
                && !string.IsNullOrEmpty(cached))
            {
                return cached;
            }

            var overlays = CfgMgr.Cfgs?.TbAreaOverlayStateInfo?.DataList;
            if (overlays != null)
            {
                foreach (var overlay in overlays)
                {
                    if (overlay == null || !overlay.IsHome)
                    {
                        continue;
                    }

                    if (ResolveRegionKey(overlay) != regionKey)
                    {
                        continue;
                    }

                    var logicAreaId = TownFacilityUtil.ResolveLogicAreaId(overlay);
                    if (!string.IsNullOrEmpty(logicAreaId))
                    {
                        _homeLogicAreaByRegion[regionKey] = logicAreaId;
                        return logicAreaId;
                    }
                }
            }

            _homeLogicAreaByRegion[regionKey] = "homestead_01";
            return "homestead_01";
        }

        public static string ResolveHomeLogicAreaForOverlay(AreaOverlayStateInfo overlay)
        {
            return ResolveHomeLogicAreaId(ResolveRegionKey(overlay));
        }

        public static void ClearCache()
        {
            _homeLogicAreaByRegion.Clear();
        }
    }
}
