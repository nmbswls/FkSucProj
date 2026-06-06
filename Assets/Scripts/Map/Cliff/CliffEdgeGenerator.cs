using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Cliff
{
    public static class CliffEdgeGenerator
    {
        // View 格：有砖且正下方无砖
        public static bool IsSouthEdgeCell(Tilemap view, Vector3Int cell)
        {
            var thisCell = view.GetTile(cell);
            var downCell = view.GetTile(cell + Vector3Int.down);
            return view != null
                && thisCell != null
                && downCell == null;
        }

        public struct CliffPlacement
        {
            public Vector3Int CliffCell;
            public Vector3Int ViewSouthEdgeCell;
            public bool IsEdgeRow;
            public ColumnAttrs Attrs;
        }

        public struct ColumnAttrs
        {
            public int X;
            public int SouthEdgeY;
            public int TopAnchorY;
            public int DepthDelta;
            public CliffSpanRole Span;
            public CliffDepthJunction DepthJunction;
            public CliffCornerShape Corner;
            public bool LeftOpen;
            public bool RightOpen;
            public bool LeftCliffWall;
            public bool RightCliffWall;
        }

        public struct SouthEdgeColumn
        {
            public int X;
            public int Y;
            public int Z;
        }

        public static HashSet<Vector3Int> CollectSouthEdgeCells(Tilemap view)
        {
            var set = new HashSet<Vector3Int>();
            if (view == null)
            {
                return set;
            }

            view.CompressBounds();
            foreach (var cell in view.cellBounds.allPositionsWithin)
            {
                if (IsSouthEdgeCell(view, cell))
                {
                    set.Add(cell);
                }
            }

            return set;
        }

        // 水平 |dx|=1,|dy|<=height；同列岩壁 |dx|=0,|dy|=1
        public static List<List<SouthEdgeColumn>> GroupSouthEdgeSegments(HashSet<Vector3Int> southEdge, int height)
        {
            var segments = new List<List<SouthEdgeColumn>>();
            if (southEdge == null || southEdge.Count == 0)
            {
                return segments;
            }

            int maxHorizontalDy = Mathf.Max(1, height);
            var visited = new HashSet<Vector3Int>();
            foreach (var start in southEdge.OrderBy(c => c.x).ThenBy(c => c.y))
            {
                if (!visited.Add(start))
                {
                    continue;
                }

                var segment = new List<SouthEdgeColumn>();
                var queue = new Queue<Vector3Int>();
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    segment.Add(new SouthEdgeColumn { X = cell.x, Y = cell.y, Z = cell.z });

                    foreach (var next in EnumerateSegmentNeighbors(cell, southEdge, maxHorizontalDy))
                    {
                        if (visited.Add(next))
                        {
                            queue.Enqueue(next);
                        }
                    }
                }

                segment.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
                segments.Add(segment);
            }

            segments.Sort((a, b) =>
            {
                int cmp = a[0].X.CompareTo(b[0].X);
                return cmp != 0 ? cmp : a[0].Y.CompareTo(b[0].Y);
            });

            return segments;
        }

        static IEnumerable<Vector3Int> EnumerateSegmentNeighbors(
            Vector3Int cell,
            HashSet<Vector3Int> southEdge,
            int maxHorizontalDy)
        {
            for (int dy = -1; dy <= 1; dy += 2)
            {
                var vertical = new Vector3Int(cell.x, cell.y + dy, cell.z);
                if (southEdge.Contains(vertical))
                {
                    yield return vertical;
                }
            }

            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dy = -maxHorizontalDy; dy <= maxHorizontalDy; dy++)
                {
                    var n = new Vector3Int(cell.x + dx, cell.y + dy, cell.z);
                    if (southEdge.Contains(n))
                    {
                        yield return n;
                    }
                }
            }
        }

        // 同列多格南缘 = 岩壁立面
        static bool IsCliffWallColumn(HashSet<Vector3Int> southEdge, int x, int z)
        {
            int count = 0;
            foreach (var c in southEdge)
            {
                if (c.x == x && c.z == z)
                {
                    count++;
                    if (count > 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 分段后：左端 x-1、右端 x+1，View 有砖则把该格并入南缘（贴岩壁延长 1 格）
        static void ExtendSegmentEndsAtCliffView(Tilemap view, HashSet<Vector3Int> southEdge, int height)
        {
            if (view == null || southEdge == null || southEdge.Count == 0)
            {
                return;
            }

            foreach (var segment in GroupSouthEdgeSegments(southEdge, height))
            {
                if (segment.Count == 0)
                {
                    continue;
                }

                int minX = segment[0].X;
                int maxX = segment[0].X;
                for (int i = 1; i < segment.Count; i++)
                {
                    if (segment[i].X < minX)
                    {
                        minX = segment[i].X;
                    }

                    if (segment[i].X > maxX)
                    {
                        maxX = segment[i].X;
                    }
                }

                foreach (var col in segment)
                {
                    if (col.X == minX)
                    {
                        var left = new Vector3Int(col.X - 1, col.Y, col.Z);
                        if (view.GetTile(left) != null)
                        {
                            southEdge.Add(left);
                        }
                    }

                    if (col.X == maxX)
                    {
                        var right = new Vector3Int(col.X + 1, col.Y, col.Z);
                        if (view.GetTile(right) != null)
                        {
                            southEdge.Add(right);
                        }
                    }
                }
            }
        }

        public static void LogSouthEdgeSegments(IReadOnlyList<List<SouthEdgeColumn>> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                Debug.Log("[CliffEdgeGenerator] South edge segments: (empty)");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("[CliffEdgeGenerator] South edge segments: ").Append(segments.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                sb.Append("\n  seg").Append(i).Append(": ");
                foreach (var col in segments[i])
                {
                    sb.Append('(').Append(col.X).Append(',').Append(col.Y).Append(") ");
                }
            }

            Debug.Log(sb.ToString());
        }

        public static int Generate(
            Tilemap view,
            Tilemap cliff,
            CliffTileSet tileSet,
            int height,
            bool clearBeforeGenerate)
        {
            if (view == null || cliff == null || tileSet == null || height < 1)
            {
                return 0;
            }

            if (clearBeforeGenerate)
            {
                cliff.ClearAllTiles();
            }

            var southEdge = CollectSouthEdgeCells(view);
            ExtendSegmentEndsAtCliffView(view, southEdge, height);
            var segments = GroupSouthEdgeSegments(southEdge, height);
            LogSouthEdgeSegments(segments);

            if (segments.Count == 0)
            {
                return 0;
            }

            int placed = 0;
            var ownerSouthEdgeY = new Dictionary<Vector3Int, int>();
            foreach (var segment in segments)
            {
                foreach (var col in segment)
                {
                    var attrs = ClassifyColumn(view, col, segment, southEdge);
                    placed += PlaceColumn(cliff, tileSet, col.X, attrs, height, ownerSouthEdgeY);
                }
            }

            return placed;
        }

        public static IEnumerable<CliffPlacement> EnumerateCliffPlacements(Tilemap view, int height)
        {
            if (view == null || height < 1)
            {
                yield break;
            }

            var southEdge = CollectSouthEdgeCells(view);
            ExtendSegmentEndsAtCliffView(view, southEdge, height);
            var ownerSouthEdgeY = new Dictionary<Vector3Int, int>();
            foreach (var segment in GroupSouthEdgeSegments(southEdge, height))
            {
                foreach (var col in segment)
                {
                    var attrs = ClassifyColumn(view, col, segment, southEdge);
                    int rowCount = ResolveRowCount(height, attrs.SouthEdgeY, attrs.TopAnchorY);
                    for (int r = 0; r < rowCount; r++)
                    {
                        var cliffCell = CliffDualGridMapping.ResolveCliffCell(col.X, attrs.TopAnchorY, r, col.Z);
                        if (ownerSouthEdgeY.TryGetValue(cliffCell, out int existing)
                            && existing > attrs.SouthEdgeY)
                        {
                            continue;
                        }

                        ownerSouthEdgeY[cliffCell] = attrs.SouthEdgeY;
                        yield return new CliffPlacement
                        {
                            CliffCell = cliffCell,
                            ViewSouthEdgeCell = new Vector3Int(col.X, attrs.SouthEdgeY, col.Z),
                            IsEdgeRow = r == 0,
                            Attrs = attrs,
                        };
                    }
                }
            }
        }

        public static Vector3 ResolveGizmoCliffNorthEdgeWorld(
            Tilemap cliff,
            CliffPlacement placement,
            Vector3 cellSize)
        {
            return CliffDualGridMapping.ResolveCliffNorthEdgeWorld(cliff, placement.CliffCell, cellSize);
        }

        public static Vector3 ResolveGizmoViewSouthEdgeWorld(Tilemap view, CliffPlacement placement)
        {
            return CliffDualGridMapping.ResolveViewSouthEdgeWorld(view, placement.ViewSouthEdgeCell);
        }

        // 邻接由 GroupSouthEdgeSegments 分段时已确定；此处只在本段 x±1 列取代表格，不再筛 |Δy|
        static bool TryGetSegmentNeighbor(
            IReadOnlyList<SouthEdgeColumn> segment,
            SouthEdgeColumn col,
            int dx,
            out SouthEdgeColumn neighbor)
        {
            int nx = col.X + dx;
            SouthEdgeColumn? best = null;
            int bestDy = int.MaxValue;

            foreach (var c in segment)
            {
                if (c.X != nx || c.Z != col.Z)
                {
                    continue;
                }

                int dy = Mathf.Abs(c.Y - col.Y);
                if (dy < bestDy)
                {
                    bestDy = dy;
                    best = c;
                }
            }

            if (best.HasValue)
            {
                neighbor = best.Value;
                return true;
            }

            neighbor = default;
            return false;
        }

        static bool SegmentHasColumnAtX(IReadOnlyList<SouthEdgeColumn> segment, int x)
        {
            foreach (var c in segment)
            {
                if (c.X == x)
                {
                    return true;
                }
            }

            return false;
        }

        static ColumnAttrs ClassifyColumn(
            Tilemap view,
            SouthEdgeColumn col,
            IReadOnlyList<SouthEdgeColumn> segment,
            HashSet<Vector3Int> southEdge)
        {
            int leftX = col.X - 1;
            int rightX = col.X + 1;

            bool hasSegLeft = SegmentHasColumnAtX(segment, leftX);
            bool hasSegRight = SegmentHasColumnAtX(segment, rightX);

            SouthEdgeColumn left = default;
            SouthEdgeColumn right = default;
            bool segLeft = hasSegLeft && TryGetSegmentNeighbor(segment, col, -1, out left);
            bool segRight = hasSegRight && TryGetSegmentNeighbor(segment, col, 1, out right);

            bool leftCliffWall = IsCliffWallColumn(southEdge, leftX, col.Z);
            bool rightCliffWall = IsCliffWallColumn(southEdge, rightX, col.Z);

            // 硬边：本段该侧 x 列不再延伸，且该侧不是岩壁列（左右同一规则）
            bool leftOpen = !hasSegLeft && !leftCliffWall && view.GetTile(new Vector3Int(leftX, col.Y, col.Z)) == null;
            bool rightOpen = !hasSegRight && !rightCliffWall;

            CliffSpanRole span;
            if (leftOpen && rightOpen)
            {
                span = CliffSpanRole.Single;
            }
            else if (leftOpen)
            {
                span = CliffSpanRole.LeftEnd;
            }
            else if (rightOpen)
            {
                span = CliffSpanRole.RightEnd;
            }
            else
            {
                span = CliffSpanRole.Mid;
            }

            CliffDepthJunction depth = CliffDepthJunction.None;
            int depthDelta = 0;
            if (segLeft && segRight)
            {
                int leftY = left.Y;
                int rightY = right.Y;
                if (leftY > col.Y && rightY > col.Y)
                {
                    Debug.LogError(
                        $"[CliffEdgeGenerator] Invalid south edge at ({col.X},{col.Y}): "
                        + $"leftY={leftY}, rightY={rightY}.");
                }
                else if (leftY > col.Y)
                {
                    depth = CliffDepthJunction.Left;
                    depthDelta = leftY - col.Y;
                }
                else if (rightY > col.Y)
                {
                    depth = CliffDepthJunction.Right;
                    depthDelta = rightY - col.Y;
                }
            }

            CliffCornerShape corner = depth != CliffDepthJunction.None
                ? CliffCornerShape.None
                : ResolveConvexCorner(view, col, hasSegLeft, hasSegRight, leftCliffWall, rightCliffWall);

            int topAnchorY = col.Y;
            if (segLeft)
            {
                topAnchorY = Mathf.Max(topAnchorY, left.Y);
            }

            if (segRight)
            {
                topAnchorY = Mathf.Max(topAnchorY, right.Y);
            }

            return new ColumnAttrs
            {
                X = col.X,
                SouthEdgeY = col.Y,
                TopAnchorY = topAnchorY,
                DepthDelta = depthDelta,
                Span = span,
                DepthJunction = depth,
                Corner = corner,
                LeftOpen = leftOpen,
                RightOpen = rightOpen,
                LeftCliffWall = leftCliffWall,
                RightCliffWall = rightCliffWall,
            };
        }

        static CliffCornerShape ResolveConvexCorner(
            Tilemap view,
            SouthEdgeColumn col,
            bool hasSegLeft,
            bool hasSegRight,
            bool leftCliffWall,
            bool rightCliffWall)
        {
            bool filled(int dx, int dy) => view.GetTile(new Vector3Int(col.X + dx, col.Y + dy, col.Z)) != null;

            if (!filled(-1, -1) && (hasSegLeft || leftCliffWall))
            {
                return CliffCornerShape.ConvexLeft;
            }

            if (!filled(1, -1) && (hasSegRight || rightCliffWall))
            {
                return CliffCornerShape.ConvexRight;
            }

            return CliffCornerShape.None;
        }

        // 顶行在 TopAnchorY（本格与左右邻接 Y 的最大值），共 Height + 1 + (TopAnchorY - SouthEdgeY) 行
        static int ResolveRowCount(int height, int southEdgeY, int topAnchorY)
        {
            return height + 1 + (topAnchorY - southEdgeY);
        }

        static int PlaceColumn(
            Tilemap cliff,
            CliffTileSet tileSet,
            int cliffX,
            ColumnAttrs attrs,
            int height,
            Dictionary<Vector3Int, int> ownerSouthEdgeY)
        {
            int rowCount = ResolveRowCount(height, attrs.SouthEdgeY, attrs.TopAnchorY);
            int placed = 0;
            for (int r = 0; r < rowCount; r++)
            {
                var cliffCell = CliffDualGridMapping.ResolveCliffCell(cliffX, attrs.TopAnchorY, r, 0);
                if (ownerSouthEdgeY.TryGetValue(cliffCell, out int existing)
                    && existing > attrs.SouthEdgeY)
                {
                    continue;
                }

                var span = ResolvePlacementSpan(r, attrs);
                var depthJunction = attrs.DepthJunction;
                var corner = r == 0 ? attrs.Corner : CliffCornerShape.None;
                if (depthJunction != CliffDepthJunction.None)
                {
                    corner = CliffCornerShape.None;
                }

                var key = ResolveVariant(r, rowCount, span, corner, depthJunction, attrs.DepthDelta);
                var tile = tileSet.GetTile(key);
                if (tile == null)
                {
                    Debug.LogWarning(
                        $"[CliffEdgeGenerator] Missing tile at cliff ({cliffX},{attrs.TopAnchorY - r}), "
                        + $"key={key.Row}/{key.Span}/depth={key.DepthJunction}");
                    continue;
                }

                ownerSouthEdgeY[cliffCell] = attrs.SouthEdgeY;
                cliff.SetTile(cliffCell, tile);
                placed++;
            }

            return placed;
        }

        static CliffSpanRole ResolvePlacementSpan(int rowIndex, ColumnAttrs attrs)
        {
            var span = attrs.Span;
            if (rowIndex == 0 && span == CliffSpanRole.Mid)
            {
                return CliffSpanRole.Single;
            }

            return span;
        }

        static CliffVariantKey ResolveVariant(
            int rowIndex,
            int rowCount,
            CliffSpanRole span,
            CliffCornerShape corner,
            CliffDepthJunction depthJunction,
            int depthDelta)
        {
            if (depthJunction != CliffDepthJunction.None)
            {
                int delta = Mathf.Max(1, depthDelta);
                int junctionRow = rowCount - 1 - delta;
                var depthSideSpan = depthJunction == CliffDepthJunction.Left
                    ? CliffSpanRole.LeftEnd
                    : CliffSpanRole.RightEnd;

                // 最底行：接地角
                if (rowIndex == rowCount - 1)
                {
                    return new CliffVariantKey(CliffRowRole.Bottom, depthSideSpan, CliffCornerShape.None);
                }

                // 深度交界行：随左右高度差上移（差1→倒数第二，差2→倒数第三…）
                if (rowIndex == junctionRow)
                {
                    return new CliffVariantKey(
                        CliffRowRole.Bottom, CliffSpanRole.Mid, CliffCornerShape.None, depthJunction);
                }

                // 交界与角点之间：岩壁端
                if (rowIndex > junctionRow && rowIndex < rowCount - 1)
                {
                    return new CliffVariantKey(CliffRowRole.Body, depthSideSpan, CliffCornerShape.None);
                }

                return new CliffVariantKey(
                    CliffRowRole.Body, CliffSpanRole.Mid, CliffCornerShape.None, depthJunction);
            }

            if (rowCount == 1)
            {
                return new CliffVariantKey(CliffRowRole.Body, span, corner);
            }

            if (rowIndex == rowCount - 1)
            {
                return new CliffVariantKey(CliffRowRole.Bottom, span, CliffCornerShape.None);
            }

            if (rowIndex == 0)
            {
                return new CliffVariantKey(CliffRowRole.Body, span, corner);
            }

            return new CliffVariantKey(CliffRowRole.Body, span, CliffCornerShape.None);
        }
    }
}
