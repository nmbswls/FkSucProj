using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Ground
{
    [CreateAssetMenu(fileName = "MapLogicHeightConfig", menuName = "Map/Ground/Logic Height Config")]
    public class MapLogicHeightConfig : ScriptableObject
    {
        [Serializable]
        public class GroundTileEntry
        {
            public TileBase Tile;
            // 平地站立高度（离散平台）
            public float LogicY;
        }

        [Serializable]
        public class SlopeTileEntry
        {
            public TileBase Tile;
            // 北高南低：南缘（低）→ 北缘（高），格内 lerp
            public float SouthLogicY;
            public float NorthLogicY;
        }

        [Header("Ground Tiles (flat, discrete LogicY)")]
        public GroundTileEntry[] GroundTiles;

        [Header("Slope Tiles (north high / south low, continuous within cell)")]
        public SlopeTileEntry[] SlopeTiles;

        [Header("Resolver")]
        public float MaxLogicYDeltaPerSec = 20f;
        public float ProbeDownMaxDistance = 8f;

        Dictionary<TileBase, float> _cachedGroundLookup;
        Dictionary<TileBase, SlopeTileEntry> _cachedSlopeLookup;
        bool _lookupDirty = true;

        public bool TryGetGroundLogicY(TileBase tile, out float logicY)
        {
            logicY = 0f;
            if (tile == null || GroundTiles == null)
            {
                return false;
            }

            foreach (var entry in GroundTiles)
            {
                if (entry != null && entry.Tile == tile)
                {
                    logicY = entry.LogicY;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSlope(TileBase tile, out SlopeTileEntry slope)
        {
            slope = null;
            if (tile == null || SlopeTiles == null)
            {
                return false;
            }

            foreach (var entry in SlopeTiles)
            {
                if (entry != null && entry.Tile == tile)
                {
                    slope = entry;
                    return true;
                }
            }

            return false;
        }

        void OnEnable()
        {
            InvalidateRuntimeLookup();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            InvalidateRuntimeLookup();
        }
#endif

        public void InvalidateRuntimeLookup()
        {
            _lookupDirty = true;
        }

        public void GetRuntimeLookup(
            out Dictionary<TileBase, float> groundLookup,
            out Dictionary<TileBase, SlopeTileEntry> slopeLookup)
        {
            if (_lookupDirty || _cachedGroundLookup == null || _cachedSlopeLookup == null)
            {
                RebuildRuntimeLookupCache();
            }

            groundLookup = _cachedGroundLookup;
            slopeLookup = _cachedSlopeLookup;
        }

        void RebuildRuntimeLookupCache()
        {
            _cachedGroundLookup ??= new Dictionary<TileBase, float>();
            _cachedSlopeLookup ??= new Dictionary<TileBase, SlopeTileEntry>();
            _cachedGroundLookup.Clear();
            _cachedSlopeLookup.Clear();

            if (GroundTiles != null)
            {
                foreach (var entry in GroundTiles)
                {
                    if (entry?.Tile == null)
                    {
                        continue;
                    }

                    _cachedGroundLookup[entry.Tile] = entry.LogicY;
                }
            }

            if (SlopeTiles != null)
            {
                foreach (var entry in SlopeTiles)
                {
                    if (entry?.Tile == null)
                    {
                        continue;
                    }

                    _cachedSlopeLookup[entry.Tile] = entry;
                }
            }

            _lookupDirty = false;
        }
    }
}
