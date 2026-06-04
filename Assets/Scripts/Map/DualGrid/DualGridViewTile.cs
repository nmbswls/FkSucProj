using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    [CreateAssetMenu(fileName = "DualGridViewTile", menuName = "Map/Dual Grid/View Tile", order = 3)]
    public class DualGridViewTile : TileBase
    {
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
            return tm != null ? tm.GetComponentInParent<DualTileMap>() : null;
        }
    }
}
