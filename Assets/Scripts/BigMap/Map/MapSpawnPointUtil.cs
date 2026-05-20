using My.Config;
using cfg.demo;

namespace My.Map
{
    // 卧室出击「真身潜入」落点：优先 BornPoint 表，否则 default 命名点（切图时由 GameLogicManager_Map 再 fallback BornPos）
    public static class MapSpawnPointUtil
    {
        public static string ResolveMapInitialSpawnPoint(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return null;
            }

            var table = CfgMgr.Cfgs?.TbBornPoint;
            if (table?.DataList != null)
            {
                foreach (var row in table.DataList)
                {
                    if (row != null && row.MapName == mapId && !string.IsNullOrEmpty(row.NamedPoint))
                    {
                        return row.NamedPoint;
                    }
                }
            }

            return "default";
        }
    }
}
