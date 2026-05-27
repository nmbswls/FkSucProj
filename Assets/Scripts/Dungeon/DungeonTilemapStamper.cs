using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Dungeon
{
    public static class DungeonTilemapStamper
    {
        public static bool Apply(DungeonGenerationResult result, WorldAreaRoot root)
        {
            if (result == null || root == null)
            {
                Debug.LogError("DungeonTilemapStamper.Apply: null result or root");
                return false;
            }

            var def = DungeonConfigCatalog.GetOrDefault(result.DungeonId);
            if (def == null)
            {
                Debug.LogError($"DungeonTilemapStamper: missing def for {result.DungeonId}");
                return false;
            }

            if (def.FloorTileset == null)
            {
                Debug.LogError("DungeonTilemapStamper: FloorTileset not configured");
                return false;
            }

            if (root.TileGrounds == null || root.TileGrounds.Length == 0 || root.TileGrounds[0] == null)
            {
                Debug.LogError("DungeonTilemapStamper: TileGrounds not configured on WorldAreaRoot");
                return false;
            }

            if (!HasBasePatterns(def.FloorTileset))
            {
                Debug.LogError("DungeonTilemapStamper: no BasePatterns configured on FloorTileset");
                return false;
            }

            foreach (var groundLayer in root.TileGrounds)
            {
                if (groundLayer == null)
                {
                    continue;
                }

                groundLayer.ClearAllTiles();
            }

            if (root.TileHole != null)
            {
                root.TileHole.ClearAllTiles();
            }

            return DungeonFloorPatternStamper.Apply(result, def.FloorTileset, root);
        }

        private static bool HasBasePatterns(DungeonFloorTileset tileset)
        {
            if (tileset.BasePatterns == null || tileset.BasePatterns.Count == 0)
            {
                return false;
            }

            foreach (var pattern in tileset.BasePatterns)
            {
                if (pattern != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
