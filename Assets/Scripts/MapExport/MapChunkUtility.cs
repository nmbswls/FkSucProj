using System.Collections.Generic;
using My.Map.Logic;
using My.Map.Scene;
using UnityEngine;

namespace My.MapExport
{
    public static class MapChunkUtility
    {
        public static float DefaultChunkWorldSize => GameConsts.ChunkCellSize;

        public static ChunkCoord WorldToChunk(Vector2 worldPos, Vector2 chunkOrigin, float chunkWorldSize)
        {
            int cx = Mathf.FloorToInt((worldPos.x - chunkOrigin.x) / chunkWorldSize);
            int cy = Mathf.FloorToInt((worldPos.y - chunkOrigin.y) / chunkWorldSize);
            return new ChunkCoord(cx, cy);
        }

        public static ChunkCoord WorldToChunk(Vector3 worldPos, Vector2 chunkOrigin, float chunkWorldSize)
        {
            return WorldToChunk(new Vector2(worldPos.x, worldPos.y), chunkOrigin, chunkWorldSize);
        }

        public static Vector3 ChunkWorldMin(ChunkCoord coord, Vector2 chunkOrigin, float chunkWorldSize)
        {
            return new Vector3(
                chunkOrigin.x + coord.X * chunkWorldSize,
                chunkOrigin.y + coord.Y * chunkWorldSize,
                0f);
        }

        public static Rect ChunkWorldRect(ChunkCoord coord, Vector2 chunkOrigin, float chunkWorldSize)
        {
            var min = ChunkWorldMin(coord, chunkOrigin, chunkWorldSize);
            return Rect.MinMaxRect(min.x, min.y, min.x + chunkWorldSize, min.y + chunkWorldSize);
        }

        // chunk 完全落在 worldRect 内（用于逻辑范围 / PaintWorldRect 裁剪）
        public static bool IsChunkInsideWorldRect(
            ChunkCoord coord,
            Rect worldRect,
            Vector2 chunkOrigin,
            float chunkWorldSize)
        {
            if (worldRect.width <= 0f || worldRect.height <= 0f || chunkWorldSize <= 0f)
            {
                return true;
            }

            var chunkRect = ChunkWorldRect(coord, chunkOrigin, chunkWorldSize);
            return chunkRect.xMin >= worldRect.xMin - 1e-4f &&
                   chunkRect.yMin >= worldRect.yMin - 1e-4f &&
                   chunkRect.xMax <= worldRect.xMax + 1e-4f &&
                   chunkRect.yMax <= worldRect.yMax + 1e-4f;
        }

        public static Vector3 ClampOrthographicCenter(Rect worldRect, float orthoHalfHeight, float aspect, Vector3 center)
        {
            if (worldRect.width <= 0f || worldRect.height <= 0f || orthoHalfHeight <= 0f)
            {
                return center;
            }

            float halfW = orthoHalfHeight * Mathf.Max(aspect, 1e-4f);
            float halfH = orthoHalfHeight;

            if (worldRect.width <= halfW * 2f)
            {
                center.x = worldRect.center.x;
            }
            else
            {
                center.x = Mathf.Clamp(center.x, worldRect.xMin + halfW, worldRect.xMax - halfW);
            }

            if (worldRect.height <= halfH * 2f)
            {
                center.y = worldRect.center.y;
            }
            else
            {
                center.y = Mathf.Clamp(center.y, worldRect.yMin + halfH, worldRect.yMax - halfH);
            }

            return center;
        }

        public static int ComputeSlicePixelSize(float chunkWorldSize, float texturePpu)
        {
            return Mathf.Max(1, Mathf.RoundToInt(chunkWorldSize * texturePpu));
        }

        public static Rect TextureCropRect(ChunkCoord coord, int slicePixelSize, Vector2Int sourceTexSize)
        {
            int px = coord.X * slicePixelSize;
            int py = coord.Y * slicePixelSize;
            int w = Mathf.Min(slicePixelSize, sourceTexSize.x - px);
            int h = Mathf.Min(slicePixelSize, sourceTexSize.y - py);
            w = Mathf.Max(0, w);
            h = Mathf.Max(0, h);
            return new Rect(px, py, w, h);
        }

        public static void CollectChunkRing(ChunkCoord center, int ring, System.Collections.Generic.HashSet<ChunkCoord> output)
        {
            output.Clear();
            for (int dx = -ring; dx <= ring; dx++)
            {
                for (int dy = -ring; dy <= ring; dy++)
                {
                    output.Add(new ChunkCoord(center.X + dx, center.Y + dy));
                }
            }
        }

        public static void IterateChunkCoordsForTexture(Vector2Int sourceTexSize, int slicePixelSize,
            System.Action<ChunkCoord> visit)
        {
            if (sourceTexSize.x <= 0 || sourceTexSize.y <= 0 || slicePixelSize <= 0)
            {
                return;
            }

            int cols = Mathf.CeilToInt(sourceTexSize.x / (float)slicePixelSize);
            int rows = Mathf.CeilToInt(sourceTexSize.y / (float)slicePixelSize);
            for (int cy = 0; cy < rows; cy++)
            {
                for (int cx = 0; cx < cols; cx++)
                {
                    visit(new ChunkCoord(cx, cy));
                }
            }
        }

        public static void IterateChunkCoordsForWorldRect(Rect worldRect, Vector2 chunkOrigin, float chunkWorldSize,
            System.Action<ChunkCoord> visit)
        {
            if (worldRect.width <= 0f || worldRect.height <= 0f || chunkWorldSize <= 0f || visit == null)
            {
                return;
            }

            var minCoord = WorldToChunk(new Vector2(worldRect.xMin, worldRect.yMin), chunkOrigin, chunkWorldSize);
            var maxCoord = WorldToChunk(
                new Vector2(worldRect.xMax - 1e-4f, worldRect.yMax - 1e-4f),
                chunkOrigin,
                chunkWorldSize);

            for (int cy = minCoord.Y; cy <= maxCoord.Y; cy++)
            {
                for (int cx = minCoord.X; cx <= maxCoord.X; cx++)
                {
                    visit(new ChunkCoord(cx, cy));
                }
            }
        }

        public static Rect SnapWorldRectToChunkGrid(Rect rect, Vector2 chunkOrigin, float chunkWorldSize)
        {
            if (rect.width <= 0f || rect.height <= 0f || chunkWorldSize <= 0f)
            {
                return rect;
            }

            var minCoord = WorldToChunk(new Vector2(rect.xMin, rect.yMin), chunkOrigin, chunkWorldSize);
            var maxCoord = WorldToChunk(
                new Vector2(rect.xMax - 1e-4f, rect.yMax - 1e-4f),
                chunkOrigin,
                chunkWorldSize);
            var min = ChunkWorldMin(minCoord, chunkOrigin, chunkWorldSize);
            var maxCorner = ChunkWorldMin(new ChunkCoord(maxCoord.X + 1, maxCoord.Y + 1), chunkOrigin, chunkWorldSize);
            return Rect.MinMaxRect(min.x, min.y, maxCorner.x, maxCorner.y);
        }

        public static void CollectChunkCoordsForWorldRect(Rect worldRect, Vector2 chunkOrigin, float chunkWorldSize,
            HashSet<ChunkCoord> output)
        {
            if (output == null)
            {
                return;
            }

            IterateChunkCoordsForWorldRect(worldRect, chunkOrigin, chunkWorldSize, coord => output.Add(coord));
        }
    }

    // 从 Collider2D 提取 FOV 用 Segment2D，仅 Editor Static Item 烘焙使用
    public static class SegmentColliderExtractor
    {
        public static int MapViewObcLayer => LayerMask.NameToLayer("MapViewObc");

        public static List<Segment2D> ExtractFromGameObject(GameObject root, bool includeInactive = false)
        {
            var result = new List<Segment2D>();
            if (root == null)
            {
                return result;
            }

            int segIdx = 0;
            var cols = root.GetComponentsInChildren<Collider2D>(includeInactive);
            ExtractFromColliders(cols, MapViewObcLayer, result, ref segIdx);
            return result;
        }

        public static void ExtractFromColliders(
            IEnumerable<Collider2D> colliders,
            int layerFilter,
            List<Segment2D> outList,
            ref int segIdx)
        {
            if (colliders == null || outList == null)
            {
                return;
            }

            foreach (var col in colliders)
            {
                if (col == null || col.gameObject.layer != layerFilter)
                {
                    continue;
                }

                switch (col)
                {
                    case BoxCollider2D box:
                        GetBoxWorldCorners(box, out var p0, out var p1, out var p2, out var p3);
                        outList.Add(new Segment2D(++segIdx, p0, p1));
                        outList.Add(new Segment2D(++segIdx, p1, p2));
                        outList.Add(new Segment2D(++segIdx, p2, p3));
                        outList.Add(new Segment2D(++segIdx, p3, p0));
                        break;

                    case CircleCollider2D circle:
                        GenerateCircleSegments(circle, 1e-1f, outList, ref segIdx);
                        break;

                    case PolygonCollider2D poly:
                        GeneratePolygonSegments(poly, outList, ref segIdx);
                        break;

                    case CompositeCollider2D comp:
                        GenerateCompositeSegments(comp, outList, ref segIdx);
                        break;
                }
            }
        }

        public static void GetBoxWorldCorners(BoxCollider2D box, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3)
        {
            var tf = box.transform;
            var size = Vector2.Scale(box.size, tf.lossyScale);
            var offset = box.offset;
            var cx = tf.TransformPoint((Vector3)offset);
            var right = tf.right * (size.x * 0.5f);
            var up = tf.up * (size.y * 0.5f);
            Vector3 v0 = cx - right - up;
            Vector3 v1 = cx + right - up;
            Vector3 v2 = cx + right + up;
            Vector3 v3 = cx - right + up;
            p0 = v0;
            p1 = v1;
            p2 = v2;
            p3 = v3;
        }

        public static int CalcCircleSides(float radius, float maxWorldError, int minN = 12, int maxN = 64)
        {
            radius = Mathf.Max(radius, 1e-4f);
            maxWorldError = Mathf.Max(maxWorldError, 1e-6f);
            float nApprox = Mathf.PI * Mathf.Sqrt(radius / (2f * maxWorldError));
            int n = Mathf.CeilToInt(nApprox);
            n = Mathf.Clamp(n, minN, maxN);
            if ((n & 1) == 1)
            {
                n += 1;
            }

            return n;
        }

        public static void GenerateCircleSegments(CircleCollider2D circle, float maxError, List<Segment2D> outList, ref int segIdx)
        {
            var tf = circle.transform;
            float r = Mathf.Abs(circle.radius) * Mathf.Max(Mathf.Abs(tf.lossyScale.x), Mathf.Abs(tf.lossyScale.y));
            int sides = CalcCircleSides(r, Mathf.Max(1e-4f, maxError), 12, 64);

            Vector2 center = tf.TransformPoint(circle.offset);
            float angleStep = Mathf.PI * 2f / sides;

            Vector2 prev = center + new Vector2(r, 0f);
            for (int i = 1; i <= sides; i++)
            {
                float ang = i * angleStep;
                Vector2 cur = center + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
                outList.Add(new Segment2D(++segIdx, prev, cur));
                prev = cur;
            }
        }

        public static void GeneratePolygonSegments(PolygonCollider2D poly, List<Segment2D> outList, ref int segIdx)
        {
            var tf = poly.transform;
            int pathCount = poly.pathCount;
            for (int p = 0; p < pathCount; p++)
            {
                var path = poly.GetPath(p);
                int n = path.Length;
                if (n < 2)
                {
                    continue;
                }

                Vector2 prev = tf.TransformPoint(path[0] + poly.offset);
                for (int i = 1; i < n; i++)
                {
                    Vector2 cur = tf.TransformPoint(path[i] + poly.offset);
                    outList.Add(new Segment2D(++segIdx, prev, cur));
                    prev = cur;
                }

                Vector2 first = tf.TransformPoint(path[0] + poly.offset);
                outList.Add(new Segment2D(++segIdx, prev, first));
            }
        }

        public static void GenerateCompositeSegments(CompositeCollider2D comp, List<Segment2D> outList, ref int segIdx)
        {
            var tf = comp.transform;
            int pathCount = comp.pathCount;
            for (int p = 0; p < pathCount; p++)
            {
                int pointCount = comp.GetPathPointCount(p);
                if (pointCount < 2)
                {
                    continue;
                }

                var pts = new Vector2[pointCount];
                comp.GetPath(p, pts);
                Vector2 prev = tf.TransformPoint((Vector3)pts[0]);
                for (int i = 1; i < pointCount; i++)
                {
                    Vector2 cur = tf.TransformPoint((Vector3)pts[i]);
                    outList.Add(new Segment2D(++segIdx, prev, cur));
                    prev = cur;
                }

                Vector2 first = tf.TransformPoint((Vector3)pts[0]);
                outList.Add(new Segment2D(++segIdx, prev, first));
            }
        }
    }
}
