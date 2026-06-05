using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Ground
{
    public static class TallGrassQuery
    {
        public const string MaskLayerName = "TallGrassMask";

        static Tilemap _mask;

        public static void Bind(Tilemap mask) => _mask = mask;

        public static void Clear() => _mask = null;

        public static float SampleCoverStrength(Vector2 worldPos)
        {
            if (_mask == null)
            {
                return 0f;
            }

            var p = new Vector3(worldPos.x, worldPos.y, 0f);
            var cell = _mask.WorldToCell(p);
            var bl = _mask.CellToWorld(cell);
            var tr = _mask.CellToWorld(cell + Vector3Int.one);
            float tx = tr.x > bl.x ? Mathf.Clamp01(Mathf.InverseLerp(bl.x, tr.x, p.x)) : 0f;
            float ty = tr.y > bl.y ? Mathf.Clamp01(Mathf.InverseLerp(bl.y, tr.y, p.y)) : 0f;

            float s00 = CellHasMask(cell);
            float s10 = CellHasMask(cell + Vector3Int.right);
            float s01 = CellHasMask(cell + Vector3Int.up);
            float s11 = CellHasMask(cell + Vector3Int.one);

            return Mathf.Lerp(Mathf.Lerp(s00, s10, tx), Mathf.Lerp(s01, s11, tx), ty);
        }

        static float CellHasMask(Vector3Int cell) =>
            _mask.GetTile(cell) != null ? 1f : 0f;
    }
}
