using System;
using UnityEngine;

namespace My.Map.Cliff
{
    public enum CliffRowRole
    {
        Body = 0,
        Bottom = 1,
    }

    public enum CliffSpanRole
    {
        Single = 0,
        LeftEnd = 1,
        Mid = 2,
        RightEnd = 3,
    }

    // 外凸角（仅首行崖体）
    public enum CliffCornerShape
    {
        None = 0,
        ConvexLeft = 1,
        ConvexRight = 2,
    }

    // 前后岩壁深度差 / 内凹交界（整列 height 行均使用）
    public enum CliffDepthJunction
    {
        None = 0,
        Left = 1,
        Right = 2,
    }

    [Serializable]
    public struct CliffVariantKey : IEquatable<CliffVariantKey>
    {
        public CliffRowRole Row;
        public CliffSpanRole Span;
        public CliffCornerShape Corner;
        public CliffDepthJunction DepthJunction;

        public CliffVariantKey(
            CliffRowRole row,
            CliffSpanRole span,
            CliffCornerShape corner,
            CliffDepthJunction depthJunction = CliffDepthJunction.None)
        {
            Row = row;
            Span = span;
            Corner = corner;
            DepthJunction = depthJunction;
        }

        public bool Equals(CliffVariantKey other) =>
            Row == other.Row
            && Span == other.Span
            && Corner == other.Corner
            && DepthJunction == other.DepthJunction;

        public override bool Equals(object obj) => obj is CliffVariantKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Row, Span, Corner, DepthJunction);
    }
}
