using My.Map.Logic;
using UnityEngine;

namespace My.MapExport
{
    public static class MapChunkUtility
    {
        public static float DefaultChunkWorldSize => GameConsts.ChunkCellSize;

        public static ChunkCoord WorldToChunk(Vector2 worldPos, Vector2 chunkOrigin, float chunkWorldSize)
        {
            int cx = Mathf.FloorToInt((worldPos.x - chunkOrigin.x) / chunkWorldSize);
            int cy = Mathf.FloorToInt((worldPos.y - chunkOrigin.y) / chunkWorldSize);
            return new ChunkCoord(cx, cy);
        }

        public static ChunkCoord WorldToChunk(Vector3 worldPos, Vector2 chunkOrigin, float chunkWorldSize)
        {
            return WorldToChunk(new Vector2(worldPos.x, worldPos.y), chunkOrigin, chunkWorldSize);
        }

        public static Vector3 ChunkWorldMin(ChunkCoord coord, Vector2 chunkOrigin, float chunkWorldSize)
        {
            return new Vector3(
                chunkOrigin.x + coord.X * chunkWorldSize,
                chunkOrigin.y + coord.Y * chunkWorldSize,
                0f);
        }

        public static int ComputeSlicePixelSize(float chunkWorldSize, float texturePpu)
        {
            return Mathf.Max(1, Mathf.RoundToInt(chunkWorldSize * texturePpu));
        }

        public static Rect TextureCropRect(ChunkCoord coord, int slicePixelSize, Vector2Int sourceTexSize)
        {
            int px = coord.X * slicePixelSize;
            int py = coord.Y * slicePixelSize;
            int w = Mathf.Min(slicePixelSize, sourceTexSize.x - px);
            int h = Mathf.Min(slicePixelSize, sourceTexSize.y - py);
            w = Mathf.Max(0, w);
            h = Mathf.Max(0, h);
            return new Rect(px, py, w, h);
        }

        public static void CollectChunkRing(ChunkCoord center, int ring, System.Collections.Generic.HashSet<ChunkCoord> output)
        {
            output.Clear();
            for (int dx = -ring; dx <= ring; dx++)
            {
                for (int dy = -ring; dy <= ring; dy++)
                {
                    output.Add(new ChunkCoord(center.X + dx, center.Y + dy));
                }
            }
        }

        public static void IterateChunkCoordsForTexture(Vector2Int sourceTexSize, int slicePixelSize,
            System.Action<ChunkCoord> visit)
        {
            if (sourceTexSize.x <= 0 || sourceTexSize.y <= 0 || slicePixelSize <= 0)
            {
                return;
            }

            int cols = Mathf.CeilToInt(sourceTexSize.x / (float)slicePixelSize);
            int rows = Mathf.CeilToInt(sourceTexSize.y / (float)slicePixelSize);
            for (int cy = 0; cy < rows; cy++)
            {
                for (int cx = 0; cx < cols; cx++)
                {
                    visit(new ChunkCoord(cx, cy));
                }
            }
        }
    }
}
