using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    [CreateAssetMenu(fileName = "DualGridTile", menuName = "Map/Dual Grid/Display Tile", order = 3)]
    public class DualGridTile : TileBase
    {
        public DualGridTilePalette Palette;
        public Sprite DefaultSprite;
        public Tile.ColliderType Collider = Tile.ColliderType.Sprite;
        public TileFlags Flags = TileFlags.LockColor;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = DefaultSprite;
            tileData.colliderType = Collider;
            tileData.flags = Flags;
            tileData.transform = Matrix4x4.identity;
            tileData.color = Color.white;

            if (Palette == null)
            {
                return;
            }

            if (tilemap == null)
            {
                return;
            }

            var viewTilemap = tilemap.GetComponent<Tilemap>();
            if (viewTilemap == null)
            {
                return;
            }

            var map = viewTilemap.GetComponentInParent<DualTileMap>();
            if (map == null || map.DataTilemap == null || map.BrushRegistry == null)
            {
                return;
            }

            byte terrainId = Palette.TerrainId;
            int mask = DualGridCore.ComputeCornerMask(
                map.DataTilemap,
                map.BrushRegistry,
                position,
                terrainId);

            int seed = DualGridCore.StableHash(position);
            var sprite = Palette.GetSprite(mask, seed);
            if (sprite != null)
            {
                tileData.sprite = sprite;
            }
        }

        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            return true;
        }
    }
}
