using System;
using System.Collections.Generic;
using My.Map.Logic;
using UnityEngine;

namespace My.MapExport
{
    [CreateAssetMenu(fileName = "MapChunkDatabase", menuName = "MapExport/Map Chunk Database")]
    public class MapChunkDatabase : ScriptableObject
    {
        public string AreaId;
        public string SceneName;
        public float ChunkWorldSize = 32f;
        public float TexturePPU = 32f;
        public Vector2 ChunkOrigin;
        public int SourceTextureWidth;
        public int SourceTextureHeight;

        // Editor 导出的完整可走 Tilemap（Resources 路径，不含扩展名）
        public string WalkGridKey;

        // Editor 导出的 LogicHeight 配置 Resources 路径（不含扩展名，可选）
        public string LogicHeightConfigKey;

        public List<MapChunkExportItem> Chunks = new List<MapChunkExportItem>();

        // 地图逻辑活动范围（世界坐标）；留空则运行时由 Chunks 推算
        public Rect LogicWorldRect;

        [NonSerialized] Dictionary<(int x, int y), MapChunkExportItem> _lookup;

        public bool HasLogicWorldRect => LogicWorldRect.width > 0f && LogicWorldRect.height > 0f;

        public int SlicePixelSize => MapChunkUtility.ComputeSlicePixelSize(ChunkWorldSize, TexturePPU);

        public MapChunkExportItem GetChunkItem(int x, int y)
        {
            BuildLookup();
            _lookup.TryGetValue((x, y), out var item);
            return item;
        }

        public MapChunkExportItem GetChunkItem(ChunkCoord coord)
        {
            return GetChunkItem(coord.X, coord.Y);
        }

        public bool HasChunkContent => Chunks != null && Chunks.Count > 0;

        public bool HasWalkGrid => !string.IsNullOrEmpty(WalkGridKey);

        public Rect ResolveLogicWorldRect()
        {
            if (HasLogicWorldRect)
            {
                return LogicWorldRect;
            }

            return ComputeBoundsFromChunks(Chunks, ChunkOrigin, ChunkWorldSize);
        }

        public bool IsChunkInLogicBounds(ChunkCoord coord)
        {
            var rect = ResolveLogicWorldRect();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return true;
            }

            return MapChunkUtility.IsChunkInsideWorldRect(coord, rect, ChunkOrigin, ChunkWorldSize);
        }

        public static Rect ComputeBoundsFromChunks(
            List<MapChunkExportItem> chunks,
            Vector2 chunkOrigin,
            float chunkWorldSize)
        {
            if (chunks == null || chunks.Count == 0 || chunkWorldSize <= 0f)
            {
                return default;
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                if (chunk == null)
                {
                    continue;
                }

                minX = Math.Min(minX, chunk.X);
                minY = Math.Min(minY, chunk.Y);
                maxX = Math.Max(maxX, chunk.X);
                maxY = Math.Max(maxY, chunk.Y);
            }

            if (minX == int.MaxValue)
            {
                return default;
            }

            var min = MapChunkUtility.ChunkWorldMin(new ChunkCoord(minX, minY), chunkOrigin, chunkWorldSize);
            var maxCorner = MapChunkUtility.ChunkWorldMin(
                new ChunkCoord(maxX + 1, maxY + 1),
                chunkOrigin,
                chunkWorldSize);
            return Rect.MinMaxRect(min.x, min.y, maxCorner.x, maxCorner.y);
        }

        public void BuildLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<(int x, int y), MapChunkExportItem>();
            if (Chunks == null)
            {
                return;
            }

            foreach (var chunk in Chunks)
            {
                if (chunk == null)
                {
                    continue;
                }

                _lookup[(chunk.X, chunk.Y)] = chunk;
            }
        }

        public void InvalidateLookup()
        {
            _lookup = null;
        }

        void OnEnable()
        {
            InvalidateLookup();
        }
    }

    [Serializable]
    public class MapChunkExportItem
    {
        public int X;
        public int Y;
        public string BackgroundKey;
        public string TilemapKey;
    }

    // MapVariant 级地图资源：按 Unity 场景名（AreaVariantInfo.scene_name）索引
    public static class MapVariantMapResources
    {
        public const string MapChunkFolder = "MapChunk";
        public const string MapExportFolder = "MapExport";

        public static string ResolveMapChunkKey(cfg.demo.AreaOverlayStateInfo overlay)
        {
            if (overlay?.BelongVariantInfo == null)
            {
                return null;
            }

            return ResolveMapChunkKey(overlay.BelongVariantInfo.SceneName);
        }

        public static string ResolveMapChunkKey(string unitySceneName)
        {
            if (string.IsNullOrWhiteSpace(unitySceneName))
            {
                return null;
            }

            return unitySceneName.Trim();
        }

        public static string BuildMapChunkDatabasePath(string sceneName)
        {
            var key = ResolveMapChunkKey(sceneName);
            return string.IsNullOrEmpty(key) ? null : $"{MapChunkFolder}/{key}";
        }

        public static MapChunkDatabase LoadMapChunkDatabase(cfg.demo.AreaOverlayStateInfo overlay)
        {
            var path = BuildMapChunkDatabasePath(ResolveMapChunkKey(overlay));
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return Resources.Load<MapChunkDatabase>(path);
        }

        public static MapChunkDatabase LoadMapChunkDatabaseBySceneName(string unitySceneName)
        {
            var path = BuildMapChunkDatabasePath(unitySceneName);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return Resources.Load<MapChunkDatabase>(path);
        }
    }
}
