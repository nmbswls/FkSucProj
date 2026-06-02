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
        public class GroundLevelHeightEntry
        {
            public int Level;
            public float LogicY;
        }

        [Serializable]
        public class GroundTileEntry
        {
            public TileBase Tile;
            public int GroundLevel;
        }

        [Serializable]
        public class SlopeTileEntry
        {
            public TileBase Tile;
            public int FromLevel;
            public int ToLevel;
        }

        [Serializable]
        public class GroundLayerEntry
        {
            public Tilemap LayerReference;
            public string LayerName;
        }

        [Header("Level Heights (0-3)")]
        public GroundLevelHeightEntry[] LevelHeights = new GroundLevelHeightEntry[4];

        [Header("Ground Tiles")]
        public GroundTileEntry[] GroundTiles;

        [Header("Slope Tiles (North high, South low)")]
        public SlopeTileEntry[] SlopeTiles;

        [Header("Ground Tilemap Layers")]
        public GroundLayerEntry[] GroundLayers;

        [Header("Resolver")]
        public float MaxLogicYDeltaPerSec = 20f;
        public float ProbeDownMaxDistance = 8f;

        public float GetLevelHeight(int level)
        {
            if (LevelHeights == null)
            {
                return 0f;
            }

            foreach (var entry in LevelHeights)
            {
                if (entry != null && entry.Level == level)
                {
                    return entry.LogicY;
                }
            }

            return 0f;
        }

        public bool TryGetGroundLevel(TileBase tile, out int level)
        {
            level = -1;
            if (tile == null || GroundTiles == null)
            {
                return false;
            }

            foreach (var entry in GroundTiles)
            {
                if (entry != null && entry.Tile == tile)
                {
                    level = entry.GroundLevel;
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

        public void BuildRuntimeLookup(
            out Dictionary<TileBase, int> groundLookup,
            out Dictionary<TileBase, SlopeTileEntry> slopeLookup)
        {
            groundLookup = new Dictionary<TileBase, int>();
            slopeLookup = new Dictionary<TileBase, SlopeTileEntry>();

            if (GroundTiles != null)
            {
                foreach (var entry in GroundTiles)
                {
                    if (entry?.Tile == null)
                    {
                        continue;
                    }

                    groundLookup[entry.Tile] = entry.GroundLevel;
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

                    slopeLookup[entry.Tile] = entry;
                }
            }
        }
    }
}
