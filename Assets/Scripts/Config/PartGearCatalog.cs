using cfg.demo;
using My.Config;

namespace My.Player
{
    // 部位装备细节表 TbPartGear
    public static class PartGearCatalog
    {
        public static PartGear GetOrDefault(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            return CfgMgr.Cfgs?.TbPartGear?.GetOrDefault(itemId);
        }

        public static bool HasDetail(string itemId) => GetOrDefault(itemId) != null;

        public static bool MeetsPartLevel(PartGear def, BodyPartRuntimeState state)
        {
            if (def == null || def.MinPartLevel <= 0)
            {
                return true;
            }

            return state != null && state.Level >= def.MinPartLevel;
        }

        public static bool MeetsPartLevel(string itemId, BodyPartRuntimeState state)
        {
            return MeetsPartLevel(GetOrDefault(itemId), state);
        }
    }
}
