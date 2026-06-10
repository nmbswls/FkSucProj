using Config;
using Config.Map;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;

namespace My
{
    public static class MapInteractPointPersistUtil
    {
        public static bool ShouldPersistEntity(EEntityType entityType, string cfgId)
        {
            if (string.IsNullOrEmpty(cfgId))
            {
                return false;
            }

            switch (entityType)
            {
                case EEntityType.RemovableObstacle:
                    {
                        var cfg = MapRemovableObstacleLoader.Get(cfgId);
                        return cfg != null && cfg.PersistByUniqName;
                    }
                case EEntityType.InteractPoint:
                    {
                        var cfg = MapInteractPointLoader.Get(cfgId);
                        return cfg != null && cfg.PersistByUniqName;
                    }
                default:
                    return false;
            }
        }

        public static bool ShouldSkipMapRuntimeEntityRecord(LogicEntityRecord rec)
        {
            if (rec == null || string.IsNullOrEmpty(rec.SrcUniqName))
            {
                return false;
            }

            return ShouldPersistEntity(rec.EntityType, rec.CfgId);
        }

        public static string GetRemovableObstacleRemovedSwitchName(string cfgId)
        {
            var cfg = MapRemovableObstacleLoader.Get(cfgId);
            if (cfg == null || string.IsNullOrEmpty(cfg.RemovedLocalSwitch))
            {
                return LogicEntityRemovableObstacle.DefaultRemovedLocalSwitch;
            }

            return cfg.RemovedLocalSwitch;
        }
    }
}
