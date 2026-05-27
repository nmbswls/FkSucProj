using cfg.demo;
using My.MapExport;

namespace My.Dungeon
{
    public static class DungeonMapLoader
    {
        public static bool TryLoad(
            AreaOverlayStateInfo cfg,
            string overlayId,
            out MapExportDatabase mapDb,
            out DungeonGenerationResult genResult)
        {
            mapDb = null;
            genResult = null;

            if (cfg == null || string.IsNullOrEmpty(cfg.ProceduralDefId))
            {
                return false;
            }

            int fallbackSeed = DungeonRng.DeriveSeed(overlayId.GetHashCode(), 1);
            int seed = DungeonSession.ConsumeSeed(overlayId, fallbackSeed);
            genResult = DungeonGenerator.Generate(cfg.ProceduralDefId, seed);
            if (genResult == null)
            {
                return false;
            }

            DungeonSession.SetLastResult(overlayId, genResult);
            mapDb = genResult.RuntimeMapData;
            return true;
        }
    }
}
