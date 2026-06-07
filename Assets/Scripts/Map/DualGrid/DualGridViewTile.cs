using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    [CreateAssetMenu(fileName = "DualGridViewTile", menuName = "Map/Dual Grid/View Tile", order = 3)]
    public class DualGridViewTile : TileBase
    {
        static readonly Dictionary<int, DualTileMap> ViewTilemapOwners = new Dictionary<int, DualTileMap>();

        internal static void RegisterOwner(Tilemap viewTilemap, DualTileMap owner)
        {
            if (viewTilemap == null || owner == null)
            {
                return;
            }

            ViewTilemapOwners[viewTilemap.GetInstanceID()] = owner;
        }

        internal static void UnregisterOwner(Tilemap viewTilemap)
        {
            if (viewTilemap == null)
            {
                return;
            }

            ViewTilemapOwners.Remove(viewTilemap.GetInstanceID());
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = null;
            tileData.transform = Matrix4x4.identity;
            tileData.color = Color.white;
            tileData.flags = TileFlags.None;
            tileData.colliderType = Tile.ColliderType.None;

            var map = ResolveMap(tilemap);
            if (map != null && map.TryGetViewSprite(position, out var sprite) && sprite != null)
            {
                tileData.sprite = sprite;
            }
        }

        static DualTileMap ResolveMap(ITilemap tilemap)
        {
            if (tilemap == null)
            {
                return null;
            }

            var tm = tilemap.GetComponent<Tilemap>();
            if (tm != null && ViewTilemapOwners.TryGetValue(tm.GetInstanceID(), out var owner))
            {
                return owner;
            }

            return tm != null ? tm.GetComponentInParent<DualTileMap>() : null;
        }
    }
}
