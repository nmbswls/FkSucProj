using System;
using System.Collections.Generic;
using My.Map.Logic;
using UnityEngine;

namespace My.MapExport
{
    public enum ChunkPaintSource
    {
        Generated = 0,
        UserPainted = 1,
    }

    [Serializable]
    public class MapPaintChunkInfo
    {
        public int X;
        public int Y;
        public ChunkPaintSource Source;
        public bool ResetOnExport;
        public float TileCoverageRatio;

        public ChunkCoord Coord => new ChunkCoord(X, Y);
    }

    [CreateAssetMenu(fileName = "MapPaintManifest", menuName = "MapExport/Map Paint Manifest")]
    public class MapPaintManifest : ScriptableObject
    {
        public string SceneName;
        public Rect PaintWorldRect;
        public Vector2 ChunkOrigin;
        public float ChunkWorldSize = 32f;
        public float TexturePPU = 32f;
        public float PaintExportPPU = 32f;
        public int SlicePixelSize;
        public int AtlasWidth;
        public int AtlasHeight;
        public Color MaskColor = Color.magenta;

        public int ExportRevision;
        public int OutlineSyncedRevision;

        public float ContextExpandRatio;

        public List<MapPaintChunkInfo> Chunks = new List<MapPaintChunkInfo>();

        [NonSerialized] Dictionary<(int x, int y), MapPaintChunkInfo> _lookup;

        public float EffectivePaintPpu => PaintExportPPU > 0f ? PaintExportPPU : TexturePPU;

        public MapPaintChunkInfo GetChunk(int x, int y)
        {
            BuildLookup();
            _lookup.TryGetValue((x, y), out var info);
            return info;
        }

        public MapPaintChunkInfo GetOrCreateChunk(ChunkCoord coord)
        {
            var info = GetChunk(coord.X, coord.Y);
            if (info != null)
            {
                return info;
            }

            info = new MapPaintChunkInfo { X = coord.X, Y = coord.Y };
            Chunks.Add(info);
            InvalidateLookup();
            return info;
        }

        public void BuildLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<(int x, int y), MapPaintChunkInfo>();
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
}
