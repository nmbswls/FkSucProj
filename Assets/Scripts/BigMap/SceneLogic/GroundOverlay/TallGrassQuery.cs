using My;
using UnityEngine;

namespace My.Map.Ground
{
    public static class TallGrassQuery
    {
        // Editor WalkGrid 烘焙仍可能引用该 Tilemap 名；运行时不再读取。
        public const string MaskLayerName = "TallGrassMask";

        static readonly Collider2D[] Hits = new Collider2D[8];
        static int _zoneMask = -1;

        static int ZoneMask
        {
            get
            {
                if (_zoneMask < 0)
                {
                    _zoneMask = LayerMask.GetMask("Zone");
                }

                return _zoneMask;
            }
        }

        public static float SampleCoverStrength(Vector2 worldPos)
        {
            if (ZoneMask == 0)
            {
                return 0f;
            }

            int count = Physics2D.OverlapPointNonAlloc(worldPos, Hits, ZoneMask);
            float best = 0f;

            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit == null)
                {
                    continue;
                }

                var zone = hit.GetComponentInParent<ZoneInfoProvider>();
                if (zone == null || !zone.HasTallGrass)
                {
                    continue;
                }

                best = Mathf.Max(best, zone.TallGrassCoverStrength);
            }

            return best;
        }
    }
}
