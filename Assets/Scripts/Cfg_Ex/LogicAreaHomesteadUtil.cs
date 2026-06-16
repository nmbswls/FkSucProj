using System.Collections.Generic;
using cfg.demo;
using My.Map;
using My.Map.Logic;

namespace My.Config
{
    // 逻辑区域家园收编相关配置查询
    public static class LogicAreaHomesteadUtil
    {
        static readonly List<HomesteadBuilding> _buildingQueryBuffer = new();
        static readonly List<string> _logicAreaIdQueryBuffer = new();
        public static string ResolveLogicAreaId(AreaOverlayStateInfo overlay)
        {
            if (overlay == null)
            {
                return null;
            }

            var logicAreaId = overlay.BelongVariantInfo?.LogicAreaId;
            if (!string.IsNullOrEmpty(logicAreaId))
            {
                return logicAreaId;
            }

            return overlay.VarId;
        }

        public static string ResolveCurrentLogicAreaId(GameLogicAreaManager areaManager)
        {
            return ResolveLogicAreaId(areaManager?.cacheMapOverlayCfg);
        }

        public static bool SupportsControlDegree(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return false;
            }

            return CfgMgr.Cfgs.TbLogicAreaHomesteadReq.GetOrDefault(logicAreaId) != null;
        }

        public static LogicAreaHomesteadReq GetHomesteadReq(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return null;
            }

            return CfgMgr.Cfgs.TbLogicAreaHomesteadReq.GetOrDefault(logicAreaId);
        }

        public static bool CanAnnexAsHomestead(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return false;
            }

            var info = CfgMgr.Cfgs.TbLogicAreaInfo.GetOrDefault(logicAreaId);
            return info != null && info.CanAnnexHomestead;
        }

        public static HomesteadBuilding GetBuildingDef(string logicAreaId, string buildingId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(buildingId))
            {
                return null;
            }

            return CfgMgr.Cfgs?.TbHomesteadBuilding?.Get(logicAreaId, buildingId);
        }

        public static HomesteadBuildingUpgrade GetBuildingUpgradeDef(string logicAreaId, string buildingId, int level)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(buildingId) || level <= 0)
            {
                return null;
            }

            return CfgMgr.Cfgs?.TbHomesteadBuildingUpgrade?.Get(logicAreaId, buildingId, level);
        }

        public static IReadOnlyList<HomesteadBuilding> GetBuildingDefsForArea(string logicAreaId)
        {
            _buildingQueryBuffer.Clear();
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return _buildingQueryBuffer;
            }

            var table = CfgMgr.Cfgs?.TbHomesteadBuilding;
            if (table == null)
            {
                return _buildingQueryBuffer;
            }

            foreach (var row in table.DataList)
            {
                if (row != null && row.LogicAreaId == logicAreaId)
                {
                    _buildingQueryBuffer.Add(row);
                }
            }

            _buildingQueryBuffer.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return _buildingQueryBuffer;
        }

        public static bool HasManageableBuildings(string logicAreaId)
        {
            return GetBuildingDefsForArea(logicAreaId).Count > 0;
        }

        public static IReadOnlyList<string> GetDistinctLogicAreaIdsWithBuildings()
        {
            _logicAreaIdQueryBuffer.Clear();
            var table = CfgMgr.Cfgs?.TbHomesteadBuilding;
            if (table == null)
            {
                return _logicAreaIdQueryBuffer;
            }

            foreach (var row in table.DataList)
            {
                if (row == null || string.IsNullOrEmpty(row.LogicAreaId))
                {
                    continue;
                }

                if (!_logicAreaIdQueryBuffer.Contains(row.LogicAreaId))
                {
                    _logicAreaIdQueryBuffer.Add(row.LogicAreaId);
                }
            }

            return _logicAreaIdQueryBuffer;
        }
    }
}
