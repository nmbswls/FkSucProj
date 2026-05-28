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
        public float ChunkWorldSize = 32f;
        public float TexturePPU = 32f;
        public Vector2 ChunkOrigin;
        public int SourceTextureWidth;
        public int SourceTextureHeight;

        // Editor 导出的完整可走 Tilemap（Resources 路径，不含扩展名）
        public string WalkGridKey;

        public List<MapChunkExportItem> Chunks = new List<MapChunkExportItem>();

        [NonSerialized] Dictionary<(int x, int y), MapChunkExportItem> _lookup;

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
}
