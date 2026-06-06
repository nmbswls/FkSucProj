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
        [Tooltip("南缘格左侧有更高台地（向内凹陷）时，该列所有 Body/Bottom 行优先使用")]
        public TileBase Body_DepthJunctionLeft;
        [Tooltip("南缘格右侧有更高台地（向内凹陷）时，该列所有 Body/Bottom 行优先使用")]
        public TileBase Body_DepthJunctionRight;

        [Header("Body — 外凸角（仅首行崖体）")]
        public TileBase Body_ConvexLeft;
        public TileBase Body_ConvexRight;

        [Header("Bottom — 接地行")]
        public TileBase Bottom_Mid;
        public TileBase Bottom_LeftEnd;
        public TileBase Bottom_RightEnd;
        [Tooltip("接地左深度交界砖（ground_grasss_59）")]
        public TileBase Bottom_DepthJunctionLeft;
        [Tooltip("接地右深度交界砖（ground_grasss_60）")]
        public TileBase Bottom_DepthJunctionRight;

        public TileBase GetTile(CliffVariantKey key)
        {
            var direct = GetDirect(key);
            if (direct != null)
            {
                return direct;
            }

            if (key.DepthJunction != CliffDepthJunction.None && key.Row == CliffRowRole.Body)
            {
                var bodyJunction = key.DepthJunction switch
                {
                    CliffDepthJunction.Left => Body_DepthJunctionLeft,
                    CliffDepthJunction.Right => Body_DepthJunctionRight,
                    _ => null,
                };
                if (bodyJunction != null)
                {
                    return bodyJunction;
                }
            }

            if (key.DepthJunction != CliffDepthJunction.None && key.Row == CliffRowRole.Bottom)
            {
                var bottomJunction = ResolveBottomDepthJunction(key.DepthJunction);
                if (bottomJunction != null)
                {
                    return bottomJunction;
                }
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

        TileBase GetDirect(CliffVariantKey key)
        {
            if (key.DepthJunction != CliffDepthJunction.None)
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

        // 有专用 Bottom 深度交界砖时与 Body 同侧；未配时回退对侧 Span（GridRoot：Body 84 列 → Bottom 26）
        TileBase ResolveBottomDepthJunction(CliffDepthJunction depthJunction)
        {
            return depthJunction switch
            {
                CliffDepthJunction.Left => Bottom_DepthJunctionLeft != null
                    ? Bottom_DepthJunctionLeft
                    : Bottom_RightEnd,
                CliffDepthJunction.Right => Bottom_DepthJunctionRight != null
                    ? Bottom_DepthJunctionRight
                    : Bottom_LeftEnd,
                _ => null,
            };
        }
    }
}
