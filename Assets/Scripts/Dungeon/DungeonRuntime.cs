using System.Threading.Tasks;
using cfg.demo;
using My.MapExport;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

namespace My.Dungeon
{
    // 地牢进图会话：seed 与生成结果在逻辑层/表现层之间传递
    public static class DungeonSession
    {
        public static int PendingSeed { get; private set; }
        public static bool HasPendingSeed { get; private set; }

        private static string _lastOverlayId = string.Empty;
        private static DungeonGenerationResult _lastResult;

        public static void SetPendingSeed(int seed)
        {
            PendingSeed = seed;
            HasPendingSeed = true;
        }

        public static int ConsumeSeed(string overlayId, int fallbackSeed)
        {
            if (HasPendingSeed)
            {
                HasPendingSeed = false;
                return PendingSeed;
            }

            return fallbackSeed;
        }

        public static void SetLastResult(string overlayId, DungeonGenerationResult result)
        {
            _lastOverlayId = overlayId ?? string.Empty;
            _lastResult = result;
        }

        public static DungeonGenerationResult GetLastResult(string overlayId = null)
        {
            if (!string.IsNullOrEmpty(overlayId) &&
                !string.Equals(_lastOverlayId, overlayId, System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return _lastResult;
        }

        public static void ClearLastResult()
        {
            _lastOverlayId = string.Empty;
            _lastResult = null;
        }
    }

    // 逻辑层：按 overlay 配置生成运行时 MapExportDatabase
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

    // 表现层：场景加载后贴图与 Nav 烘焙
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

            await BakeNavMeshAsync(dungeonResult, root).ConfigureAwait(true);
        }

        private static async Task BakeNavMeshAsync(DungeonGenerationResult result, WorldAreaRoot root)
        {
            if (root == null)
            {
                Debug.LogError("DungeonPresentation: WorldAreaRoot is null");
                return;
            }

            var surface = root.GetComponentInChildren<NavMeshSurface>();
            if (surface == null)
            {
                Debug.LogWarning("DungeonPresentation: NavMeshSurface not found on scene.");
                return;
            }

            FitNavSurfaceBounds(surface, result);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            AsyncOperation op = surface.BuildNavMeshAsync();
            while (op != null && !op.isDone)
            {
                await Task.Yield();
            }

            sw.Stop();
            int walkable = result?.WalkableCells != null ? result.WalkableCells.Count : 0;
            Debug.Log($"[DungeonNav] bake ms={sw.ElapsedMilliseconds} walkableCells={walkable}");
        }

        private static void FitNavSurfaceBounds(NavMeshSurface surface, DungeonGenerationResult result)
        {
            if (result?.WalkableCells == null || result.WalkableCells.Count == 0)
            {
                return;
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            foreach (var cell in result.WalkableCells)
            {
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
            }

            float padding = 2f;
            surface.center = new Vector3(
                (minX + maxX + 1f) * 0.5f,
                (minY + maxY + 1f) * 0.5f,
                0f);
            surface.size = new Vector3(
                maxX - minX + 1f + padding * 2f,
                maxY - minY + 1f + padding * 2f,
                4f);
        }
    }
}
