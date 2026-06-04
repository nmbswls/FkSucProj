using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    [CreateAssetMenu(fileName = "DualGridBrushRegistry", menuName = "Map/Dual Grid/Brush Registry", order = 1)]
    public class DualGridBrushRegistry : ScriptableObject
    {
        [Serializable]
        public class TerrainStyle
        {
            public byte TerrainId;
            public DualGridTilePalette Palette;
        }

        [Serializable]
        public class Entry
        {
            public TileBase Brush;
            public byte TerrainId;
        }

        public TerrainStyle[] Terrains = Array.Empty<TerrainStyle>();
        [FormerlySerializedAs("Entries")]
        public Entry[] Brushes = Array.Empty<Entry>();

        public bool TryGetTerrainId(TileBase brush, out byte terrainId)
        {
            terrainId = 0;
            if (brush == null || Brushes == null)
            {
                return false;
            }

            for (int i = 0; i < Brushes.Length; i++)
            {
                var e = Brushes[i];
                if (e == null || e.Brush == null || e.TerrainId == 0)
                {
                    continue;
                }

                if (BrushMatches(e.Brush, brush))
                {
                    terrainId = e.TerrainId;
                    return true;
                }
            }

            return false;
        }

        static bool BrushMatches(TileBase registryBrush, TileBase paintedBrush)
        {
            if (registryBrush == paintedBrush)
            {
                return true;
            }

            if (registryBrush.name == paintedBrush.name)
            {
                return true;
            }

            if (registryBrush is Tile registryTile && paintedBrush is Tile paintedTile
                && registryTile.sprite != null && registryTile.sprite == paintedTile.sprite)
            {
                return true;
            }

#if UNITY_EDITOR
            string a = UnityEditor.AssetDatabase.GetAssetPath(registryBrush);
            string b = UnityEditor.AssetDatabase.GetAssetPath(paintedBrush);
            if (!string.IsNullOrEmpty(a) && a == b)
            {
                return true;
            }
#endif

            return false;
        }

        public DualGridTilePalette FindPalette(byte terrainId)
        {
            if (terrainId == 0 || Terrains == null)
            {
                return null;
            }

            for (int i = 0; i < Terrains.Length; i++)
            {
                var t = Terrains[i];
                if (t != null && t.TerrainId == terrainId && t.Palette != null)
                {
                    return t.Palette;
                }
            }

            return null;
        }

        // dataTilemap：必须是 DualTileMap/Data，不能传 View Tilemap
        // viewCell：View 层格子坐标（在 Data 某格落笔后，会刷新 viewCell 及 +1 偏移的共 4 个 View 格）
        public bool TryResolveViewCorner(Tilemap dataTilemap, Vector3Int viewCell, out byte terrainId, out int mask)
        {
            terrainId = 0;
            mask = 0;
            if (dataTilemap == null || Terrains == null || Terrains.Length == 0)
            {
                return false;
            }

            int bestMask = 0;
            byte bestTerrain = 0;

            for (int i = 0; i < Terrains.Length; i++)
            {
                var style = Terrains[i];
                if (style == null || style.TerrainId == 0 || style.Palette == null)
                {
                    continue;
                }

                if (FindPalette(style.TerrainId) == null)
                {
                    continue;
                }

                int m = DualGridCore.ComputeCornerMask(dataTilemap, this, viewCell, style.TerrainId);
                if (m != 0 && m > bestMask)
                {
                    bestMask = m;
                    bestTerrain = style.TerrainId;
                }
            }

            if (bestMask == 0)
            {
                return false;
            }

            terrainId = bestTerrain;
            mask = bestMask;
            return true;
        }
    }
}
