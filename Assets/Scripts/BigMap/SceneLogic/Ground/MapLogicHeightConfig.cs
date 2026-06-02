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

        // 与 GridRoot/WalkGridRoot 下 Tilemap 的 GameObject 名一致；留空则采样全部 Walk 层
        [Header("Ground Tilemap Layer Names")]
        public string[] GroundLayerNames;

        [Header("Resolver")]
        public float MaxLogicYDeltaPerSec = 20f;
        public float ProbeDownMaxDistance = 8f;

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

        public void BuildRuntimeLookup(
            out Dictionary<TileBase, float> groundLookup,
            out Dictionary<TileBase, SlopeTileEntry> slopeLookup)
        {
            groundLookup = new Dictionary<TileBase, float>();
            slopeLookup = new Dictionary<TileBase, SlopeTileEntry>();

            if (GroundTiles != null)
            {
                foreach (var entry in GroundTiles)
                {
                    if (entry?.Tile == null)
                    {
                        continue;
                    }

                    groundLookup[entry.Tile] = entry.LogicY;
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
