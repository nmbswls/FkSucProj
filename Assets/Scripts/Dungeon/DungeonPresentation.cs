using System.Threading.Tasks;
using cfg.demo;
using UnityEngine;

namespace My.Dungeon
{
    public static class DungeonPresentation
    {
        public const string TestCaveOverlayId = "dungeon_test_cave";

        public static bool IsProceduralOverlay(AreaOverlayStateInfo cfg)
        {
            return cfg != null && !string.IsNullOrEmpty(cfg.ProceduralDefId);
        }

        public static async Task ApplyAsync(string overlayId, WorldAreaRoot root)
        {
            if (string.IsNullOrEmpty(overlayId) || root == null)
            {
                Debug.LogError("DungeonPresentation.ApplyAsync: invalid overlayId or root");
                return;
            }

            var dungeonResult = DungeonSession.GetLastResult(overlayId);
            if (dungeonResult == null)
            {
                Debug.LogError($"DungeonPresentation.ApplyAsync: dungeon result missing for {overlayId}");
                return;
            }

            if (!DungeonTilemapStamper.Apply(dungeonResult, root))
            {
                Debug.LogError("DungeonPresentation.ApplyAsync: DungeonTilemapStamper.Apply failed");
                return;
            }

            await DungeonNavMeshBaker.BakeAsync(dungeonResult, root).ConfigureAwait(true);
        }
    }
}
