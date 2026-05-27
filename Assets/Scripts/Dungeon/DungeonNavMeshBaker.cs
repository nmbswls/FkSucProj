using System.Threading.Tasks;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

namespace My.Dungeon
{
    public static class DungeonNavMeshBaker
    {
        public static async Task BakeAsync(DungeonGenerationResult result, WorldAreaRoot root)
        {
            if (root == null)
            {
                Debug.LogError("DungeonNavMeshBaker: WorldAreaRoot is null");
                return;
            }

            var surface = root.GetComponentInChildren<NavMeshSurface>();
            if (surface == null)
            {
                Debug.LogWarning("DungeonNavMeshBaker: NavMeshSurface not found on Main_Dungeon_TestCave scene.");
                return;
            }

            FitSurfaceBounds(surface, result);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            AsyncOperation op = surface.BuildNavMeshAsync();
            while (op != null && !op.isDone)
            {
                await Task.Yield();
            }

            sw.Stop();
            int walkable = result?.WalkableCells != null ? result.WalkableCells.Count : 0;
            UnityEngine.Debug.Log($"[DungeonNav] bake ms={sw.ElapsedMilliseconds} walkableCells={walkable}");
        }

        private static void FitSurfaceBounds(NavMeshSurface surface, DungeonGenerationResult result)
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
            var center = new Vector3(
                (minX + maxX + 1f) * 0.5f,
                (minY + maxY + 1f) * 0.5f,
                0f);
            var size = new Vector3(
                maxX - minX + 1f + padding * 2f,
                maxY - minY + 1f + padding * 2f,
                4f);

            surface.center = center;
            surface.size = size;
        }
    }
}
