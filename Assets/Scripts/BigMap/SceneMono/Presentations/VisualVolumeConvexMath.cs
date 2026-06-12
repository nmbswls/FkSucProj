using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    // 面向 local 凸包：CCW 校验、Graham 凸包、包含与 clamp
    public static class VisualVolumeConvexMath
    {
        public const int MinHullPoints = 3;
        public const int MaxHullPoints = 12;
        const float Epsilon = 1e-5f;

        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        public static void ComputeAabb(IReadOnlyList<Vector2> points, out Vector2 center, out Vector2 halfExtents)
        {
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }

            center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            halfExtents = new Vector2((maxX - minX) * 0.5f, (maxY - minY) * 0.5f);
        }

        public static bool IsConvexCCW(IReadOnlyList<Vector2> points, bool allowCollinear = true)
        {
            if (points == null || points.Count < MinHullPoints)
            {
                return false;
            }

            int n = points.Count;
            int sign = 0;

            for (int i = 0; i < n; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % n];
                Vector2 c = points[(i + 2) % n];
                float cross = Cross(b - a, c - b);

                if (Mathf.Abs(cross) <= Epsilon)
                {
                    continue;
                }

                int curSign = cross > 0f ? 1 : -1;
                if (sign == 0)
                {
                    sign = curSign;
                }
                else if (sign != curSign)
                {
                    return false;
                }
            }

            if (sign == 0)
            {
                return allowCollinear;
            }

            return sign > 0;
        }

        public static bool ContainsConvexCCW(Vector2 point, IReadOnlyList<Vector2> hullCCW)
        {
            if (hullCCW == null || hullCCW.Count < MinHullPoints)
            {
                return false;
            }

            for (int i = 0; i < hullCCW.Count; i++)
            {
                Vector2 a = hullCCW[i];
                Vector2 b = hullCCW[(i + 1) % hullCCW.Count];
                if (Cross(b - a, point - a) < -Epsilon)
                {
                    return false;
                }
            }

            return true;
        }

        public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = Vector2.Dot(ab, ab);
            if (denom <= Epsilon)
            {
                return a;
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / denom);
            return a + ab * t;
        }

        public static Vector2 ClampToConvexCCW(Vector2 point, IReadOnlyList<Vector2> hullCCW)
        {
            if (hullCCW == null || hullCCW.Count < MinHullPoints)
            {
                return point;
            }

            if (ContainsConvexCCW(point, hullCCW))
            {
                return point;
            }

            Vector2 best = hullCCW[0];
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < hullCCW.Count; i++)
            {
                Vector2 a = hullCCW[i];
                Vector2 b = hullCCW[(i + 1) % hullCCW.Count];
                Vector2 proj = ClosestPointOnSegment(point, a, b);
                float distSq = (point - proj).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = proj;
                }
            }

            return best;
        }

        public static List<Vector2> BuildConvexHullGraham(IReadOnlyList<Vector2> source)
        {
            var points = new List<Vector2>();
            if (source == null)
            {
                return points;
            }

            for (int i = 0; i < source.Count; i++)
            {
                points.Add(source[i]);
            }

            if (points.Count <= 1)
            {
                return points;
            }

            int pivotIndex = 0;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 p = points[i];
                Vector2 pivot = points[pivotIndex];
                if (p.y < pivot.y - Epsilon || (Mathf.Abs(p.y - pivot.y) <= Epsilon && p.x < pivot.x))
                {
                    pivotIndex = i;
                }
            }

            Vector2 pivotPoint = points[pivotIndex];
            points.RemoveAt(pivotIndex);
            points.Sort((a, b) =>
            {
                float cross = Cross(a - pivotPoint, b - pivotPoint);
                if (Mathf.Abs(cross) <= Epsilon)
                {
                    float da = (a - pivotPoint).sqrMagnitude;
                    float db = (b - pivotPoint).sqrMagnitude;
                    return da.CompareTo(db);
                }

                return cross > 0f ? -1 : 1;
            });

            var hull = new List<Vector2> { pivotPoint };
            for (int i = 0; i < points.Count; i++)
            {
                while (hull.Count >= 2)
                {
                    Vector2 a = hull[hull.Count - 2];
                    Vector2 b = hull[hull.Count - 1];
                    if (Cross(b - a, points[i] - b) <= Epsilon)
                    {
                        hull.RemoveAt(hull.Count - 1);
                    }
                    else
                    {
                        break;
                    }
                }

                hull.Add(points[i]);
            }

            if (hull.Count > MaxHullPoints)
            {
                hull = ReduceHullVertices(hull, MaxHullPoints);
            }

            return hull;
        }

        static List<Vector2> ReduceHullVertices(List<Vector2> hull, int maxCount)
        {
            if (hull.Count <= maxCount)
            {
                return hull;
            }

            var reduced = new List<Vector2>(hull);
            while (reduced.Count > maxCount)
            {
                int removeIndex = 0;
                float minArea = float.MaxValue;

                for (int i = 0; i < reduced.Count; i++)
                {
                    Vector2 prev = reduced[(i - 1 + reduced.Count) % reduced.Count];
                    Vector2 cur = reduced[i];
                    Vector2 next = reduced[(i + 1) % reduced.Count];
                    float area = Mathf.Abs(Cross(cur - prev, next - cur));
                    if (area < minArea)
                    {
                        minArea = area;
                        removeIndex = i;
                    }
                }

                reduced.RemoveAt(removeIndex);
            }

            return reduced;
        }

        public static List<Vector2> BuildRectHull(Vector2 center, Vector2 halfExtents)
        {
            return new List<Vector2>
            {
                center + new Vector2(-halfExtents.x, -halfExtents.y),
                center + new Vector2(halfExtents.x, -halfExtents.y),
                center + new Vector2(halfExtents.x, halfExtents.y),
                center + new Vector2(-halfExtents.x, halfExtents.y),
            };
        }
    }
}
