
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My
{
    public class PreviewTilemapController : MonoBehaviour
    {
        public Tilemap previewTilemap;
        public TileBase validTile;
        public TileBase invalidTile;

        public void DrawCells(IEnumerable<Vector3Int> cells, bool valid)
        {
            Clear();
            foreach (var c in cells)
            {
                previewTilemap.SetTile(c, valid ? validTile : invalidTile);
            }
        }

        public void Clear()
        {
            previewTilemap.ClearAllTiles();
        }
    }
}