using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
    [CreateAssetMenu(fileName = "DungeonFloorTileset", menuName = "Dungeon/Floor Tileset")]
    public class DungeonFloorTileset : ScriptableObject
    {
        public string TilesetId = string.Empty;
        public List<DungeonFloorPattern> BasePatterns = new();
        public List<DungeonFloorPattern> AccentPatterns = new();

        [Range(0f, 1f)]
        public float AccentDensity = 0.12f;

        public Vector2Int BaseGridPhase = Vector2Int.zero;
        public bool Allow1x1Patterns = false;

        public List<DungeonFloorPattern> ResolveBasePatterns()
        {
            var result = new List<DungeonFloorPattern>();
            if (BasePatterns == null)
            {
                return result;
            }

            foreach (var pattern in BasePatterns)
            {
                if (pattern != null)
                {
                    result.Add(pattern);
                }
            }

            return result;
        }

        public List<DungeonFloorPattern> ResolveAccentPatterns()
        {
            var result = new List<DungeonFloorPattern>();
            if (AccentPatterns == null)
            {
                return result;
            }

            foreach (var pattern in AccentPatterns)
            {
                if (pattern != null)
                {
                    result.Add(pattern);
                }
            }

            return result;
        }
    }
}
