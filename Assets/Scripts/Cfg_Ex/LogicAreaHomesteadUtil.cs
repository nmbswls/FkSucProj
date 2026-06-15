using cfg.demo;
using My.Map;
using My.Map.Logic;

namespace My.Config
{
    // 逻辑区域家园收编相关配置查询
    public static class LogicAreaHomesteadUtil
    {
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
    }
}
