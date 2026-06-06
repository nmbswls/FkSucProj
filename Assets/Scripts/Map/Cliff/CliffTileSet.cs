using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Cliff
{
    [CreateAssetMenu(fileName = "CliffTileSet", menuName = "Map/Cliff/Tile Set", order = 1)]
    public class CliffTileSet : ScriptableObject
    {
        public TileBase DefaultTile;

        [Header("Body — 直线段")]
        [Tooltip("最常用；LeftEnd/RightEnd/Single 未填时回退到此")]
        public TileBase Body_Mid;
        public TileBase Body_LeftEnd;
        public TileBase Body_RightEnd;

        [Header("Body — 深度交界（内凹，整列使用）")]
        [Tooltip("南缘格西侧有更高台地时，整列 Body 使用")]
        public TileBase Body_DepthJunctionLeft;
        [Tooltip("南缘格东侧有更高台地时，整列 Body 使用")]
        public TileBase Body_DepthJunctionRight;

        [Header("Body — 外凸角（仅首行崖体）")]
        public TileBase Body_ConvexLeft;
        public TileBase Body_ConvexRight;

        [Header("Bottom — 接地行")]
        public TileBase Bottom_Mid;
        public TileBase Bottom_LeftEnd;
        public TileBase Bottom_RightEnd;
        [Tooltip("ground_grasss_59")]
        public TileBase Bottom_DepthJunctionLeft;
        [Tooltip("ground_grasss_60")]
        public TileBase Bottom_DepthJunctionRight;

        public TileBase GetTile(CliffVariantKey key)
        {
            if (key.DepthJunction != CliffDepthJunction.None)
            {
                return ResolveDepthJunctionTile(key) ?? Body_Mid ?? DefaultTile;
            }

            var direct = GetDirect(key);
            if (direct != null)
            {
                return direct;
            }

            if (key.Row == CliffRowRole.Bottom)
            {
                var bodySpan = GetDirect(new CliffVariantKey(CliffRowRole.Body, key.Span, CliffCornerShape.None));
                if (bodySpan != null)
                {
                    return bodySpan;
                }
            }

            if (key.Row == CliffRowRole.Body && key.Corner != CliffCornerShape.None)
            {
                var spanFallback = GetDirect(new CliffVariantKey(CliffRowRole.Body, key.Span, CliffCornerShape.None));
                if (spanFallback != null)
                {
                    return spanFallback;
                }
            }

            if (Body_Mid != null)
            {
                return Body_Mid;
            }

            return DefaultTile;
        }

        TileBase ResolveDepthJunctionTile(CliffVariantKey key)
        {
            if (key.Row == CliffRowRole.Bottom)
            {
                return ResolveBottomDepthJunction(key.DepthJunction);
            }

            return key.DepthJunction switch
            {
                CliffDepthJunction.Left => Body_DepthJunctionLeft,
                CliffDepthJunction.Right => Body_DepthJunctionRight,
                _ => null,
            };
        }

        TileBase GetDirect(CliffVariantKey key)
        {
            if (key.DepthJunction != CliffDepthJunction.None)
            {
                return ResolveDepthJunctionTile(key);
            }

            if (key.Row == CliffRowRole.Bottom)
            {
                return key.Span switch
                {
                    CliffSpanRole.Single => Bottom_Mid,
                    CliffSpanRole.LeftEnd => Bottom_LeftEnd,
                    CliffSpanRole.Mid => Bottom_Mid,
                    CliffSpanRole.RightEnd => Bottom_RightEnd,
                    _ => null,
                };
            }

            if (key.Corner != CliffCornerShape.None)
            {
                return key.Corner switch
                {
                    CliffCornerShape.ConvexLeft => Body_ConvexLeft,
                    CliffCornerShape.ConvexRight => Body_ConvexRight,
                    _ => null,
                };
            }

            return key.Span switch
            {
                CliffSpanRole.Single => Body_Mid,
                CliffSpanRole.LeftEnd => Body_LeftEnd,
                CliffSpanRole.Mid => Body_Mid,
                CliffSpanRole.RightEnd => Body_RightEnd,
                _ => null,
            };
        }

        // Body 左/右交界与 Bottom 59/60 交叉配对（与 GridRoot 手铺一致：Body84 列 → 接地 60）
        TileBase ResolveBottomDepthJunction(CliffDepthJunction depthJunction)
        {
            return depthJunction switch
            {
                CliffDepthJunction.Left => Bottom_DepthJunctionRight != null
                    ? Bottom_DepthJunctionRight
                    : Bottom_RightEnd != null
                        ? Bottom_RightEnd
                        : Body_DepthJunctionLeft,
                CliffDepthJunction.Right => Bottom_DepthJunctionLeft != null
                    ? Bottom_DepthJunctionLeft
                    : Bottom_LeftEnd != null
                        ? Bottom_LeftEnd
                        : Body_DepthJunctionRight,
                _ => null,
            };
        }
    }
}
