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

        // 相邻列 |dy|<=height 且 X+1 列竖直射线通；同列岩壁 |dx|=0,|dy|=1
        public static List<List<SouthEdgeColumn>> GroupSouthEdgeSegments(
            Tilemap view,
            HashSet<Vector3Int> southEdge,
            int height)
        {
            var segments = new List<List<SouthEdgeColumn>>();
            if (view == null || southEdge == null || southEdge.Count == 0)
            {
                return segments;
            }

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

                    foreach (var next in EnumerateSegmentNeighbors(view, cell, southEdge, height))
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

        // 归一化 left=(x,ya), right=(x+1,yb)；相邻列连通看 X+1 列
        static bool AreSouthEdgeCellsConnected(
            Tilemap view,
            Vector3Int a,
            Vector3Int b,
            int height)
        {
            if (view == null || a.z != b.z)
            {
                return false;
            }

            if (a.x == b.x)
            {
                return Mathf.Abs(a.y - b.y) == 1;
            }

            if (Mathf.Abs(a.x - b.x) != 1)
            {
                return false;
            }

            var left = a.x < b.x ? a : b;
            var right = a.x < b.x ? b : a;
            int ya = left.y;
            int yb = right.y;
            int xRight = right.x;
            int z = left.z;
            int dy = yb - ya;

            if (Mathf.Abs(dy) > height)
            {
                return false;
            }

            if (dy == 0)
            {
                return true;
            }

            int yMin = Mathf.Min(ya, yb);
            int yMax = Mathf.Max(ya, yb);

            // 情况 A：X+1 列在 [yMin,yMax] 整段竖条全满（缓坡 / 立面）
            if (IsViewColumnStripFilled(view, xRight, yMin, yMax, z))
            {
                return true;
            }

            // 情况 B：外凸角阶 |dy|==1，右列南缘高度 yb 处有砖即可衔接（(x+1,ya) 可空）
            if (Mathf.Abs(dy) == 1 && view.GetTile(new Vector3Int(xRight, yb, z)) != null)
            {
                return true;
            }

            return false;
        }

        static bool IsViewColumnStripFilled(Tilemap view, int x, int yMin, int yMax, int z)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                if (view.GetTile(new Vector3Int(x, y, z)) == null)
                {
                    return false;
                }
            }

            return true;
        }

        static IEnumerable<Vector3Int> EnumerateSegmentNeighbors(
            Tilemap view,
            Vector3Int cell,
            HashSet<Vector3Int> southEdge,
            int height)
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
                for (int dy = -height; dy <= height; dy++)
                {
                    var n = new Vector3Int(cell.x + dx, cell.y + dy, cell.z);
                    if (!southEdge.Contains(n))
                    {
                        continue;
                    }

                    if (AreSouthEdgeCellsConnected(view, cell, n, height))
                    {
                        yield return n;
                    }
                }
            }
        }

        // 分段后：左端 x-1、右端 x+1，View 有砖则把该格并入南缘并追加到本段
        static void ExtendSegmentEndsAtCliffView(
            Tilemap view,
            List<List<SouthEdgeColumn>> segments,
            HashSet<Vector3Int> southEdge)
        {
            if (view == null || segments == null || southEdge == null)
            {
                return;
            }

            foreach (var segment in segments)
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

                var appended = new List<SouthEdgeColumn>();
                foreach (var col in segment)
                {
                    if (col.X == minX)
                    {
                        TryAppendExtendedSouthEdge(view, appended, southEdge, col.X - 1, col.Y, col.Z);
                    }

                    if (col.X == maxX)
                    {
                        TryAppendExtendedSouthEdge(view, appended, southEdge, col.X + 1, col.Y, col.Z);
                    }
                }

                if (appended.Count == 0)
                {
                    continue;
                }

                segment.AddRange(appended);
                segment.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            }
        }

        static void TryAppendExtendedSouthEdge(
            Tilemap view,
            List<SouthEdgeColumn> appended,
            HashSet<Vector3Int> southEdge,
            int x,
            int y,
            int z)
        {
            var cell = new Vector3Int(x, y, z);
            if (view.GetTile(cell) == null || southEdge.Contains(cell))
            {
                return;
            }

            southEdge.Add(cell);
            appended.Add(new SouthEdgeColumn { X = x, Y = y, Z = z });
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
            var segments = GroupSouthEdgeSegments(view, southEdge, height);
            ExtendSegmentEndsAtCliffView(view, segments, southEdge);
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
                    var attrs = ClassifyColumn(view, col, segment, height);
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
            var segments = GroupSouthEdgeSegments(view, southEdge, height);
            ExtendSegmentEndsAtCliffView(view, segments, southEdge);
            var ownerSouthEdgeY = new Dictionary<Vector3Int, int>();
            foreach (var segment in segments)
            {
                foreach (var col in segment)
                {
                    var attrs = ClassifyColumn(view, col, segment, height);
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

        // 段内 x±1 连通邻格（与 GroupSouthEdgeSegments 同规则）；同行优先，否则 |Δy| 最小
        static bool TryGetSegmentConnectedNeighbor(
            Tilemap view,
            IReadOnlyList<SouthEdgeColumn> segment,
            SouthEdgeColumn col,
            int dx,
            int height,
            out SouthEdgeColumn neighbor)
        {
            int nx = col.X + dx;
            var colCell = new Vector3Int(col.X, col.Y, col.Z);
            SouthEdgeColumn? best = null;
            int bestDy = int.MaxValue;
            bool bestSameRow = false;

            foreach (var candidate in segment)
            {
                if (candidate.X != nx || candidate.Z != col.Z)
                {
                    continue;
                }

                var candidateCell = new Vector3Int(candidate.X, candidate.Y, candidate.Z);
                if (!AreSouthEdgeCellsConnected(view, colCell, candidateCell, height))
                {
                    continue;
                }

                bool sameRow = candidate.Y == col.Y;
                int absDy = Mathf.Abs(candidate.Y - col.Y);
                if (best == null
                    || (sameRow && !bestSameRow)
                    || (sameRow == bestSameRow && absDy < bestDy))
                {
                    best = candidate;
                    bestSameRow = sameRow;
                    bestDy = absDy;
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

        static ColumnAttrs ClassifyColumn(
            Tilemap view,
            SouthEdgeColumn col,
            IReadOnlyList<SouthEdgeColumn> segment,
            int height)
        {
            int leftX = col.X - 1;
            int rightX = col.X + 1;

            bool hasSegLeft = TryGetSegmentConnectedNeighbor(view, segment, col, -1, height, out var left);
            bool hasSegRight = TryGetSegmentConnectedNeighbor(view, segment, col, 1, height, out var right);

            bool leftOpen = !hasSegLeft
                && view.GetTile(new Vector3Int(leftX, col.Y, col.Z)) == null;
            bool rightOpen = !hasSegRight
                && view.GetTile(new Vector3Int(rightX, col.Y, col.Z)) == null;

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
            if (hasSegLeft && hasSegRight)
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
                : ResolveConvexCorner(view, col, hasSegLeft, hasSegRight);

            int topAnchorY = col.Y;
            if (hasSegLeft)
            {
                topAnchorY = Mathf.Max(topAnchorY, left.Y);
            }

            if (hasSegRight)
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
            };
        }

        static CliffCornerShape ResolveConvexCorner(
            Tilemap view,
            SouthEdgeColumn col,
            bool hasSegLeft,
            bool hasSegRight)
        {
            bool filled(int dx, int dy) => view.GetTile(new Vector3Int(col.X + dx, col.Y + dy, col.Z)) != null;

            if (!filled(-1, -1) && hasSegLeft)
            {
                return CliffCornerShape.ConvexLeft;
            }

            if (!filled(1, -1) && hasSegRight)
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

