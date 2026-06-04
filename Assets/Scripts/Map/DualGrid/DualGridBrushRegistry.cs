using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    [CreateAssetMenu(fileName = "DualGridBrushRegistry", menuName = "Map/Dual Grid/Brush Registry", order = 1)]
    public class DualGridBrushRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public TileBase Brush;
            public byte TerrainId;
        }

        public Entry[] Entries = Array.Empty<Entry>();

        public bool TryGetTerrainId(TileBase brush, out byte terrainId)
        {
            terrainId = 0;
            if (brush == null || Entries == null)
            {
                return false;
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                var e = Entries[i];
                if (e != null && e.Brush == brush && e.TerrainId != 0)
                {
                    terrainId = e.TerrainId;
                    return true;
                }
            }

            return false;
        }
    }
}
