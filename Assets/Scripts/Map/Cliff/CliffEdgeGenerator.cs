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
            return view != null
                && view.GetTile(cell) != null
                && view.GetTile(cell + Vector3Int.down) == null;
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

        // 水平 |dx|=1,|dy|<=1；同列岩壁 |dx|=0,|dy|=1
        public static List<List<SouthEdgeColumn>> GroupSouthEdgeSegments(HashSet<Vector3Int> southEdge)
        {
            var segments = new List<List<SouthEdgeColumn>>();
            if (southEdge == null || southEdge.Count == 0)
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

                    foreach (var next in EnumerateSegmentNeighbors(cell, southEdge))
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

        static IEnumerable<Vector3Int> EnumerateSegmentNeighbors(Vector3Int cell, HashSet<Vector3Int> southEdge)
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
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dy == 0)
                    {
                        var n = new Vector3Int(cell.x + dx, cell.y, cell.z);
                        if (southEdge.Contains(n))
                        {
                            yield return n;
                        }

                        continue;
                    }

                    var nd = new Vector3Int(cell.x + dx, cell.y + dy, cell.z);
                    if (southEdge.Contains(nd))
                    {
                        yield return nd;
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
            var segments = GroupSouthEdgeSegments(southEdge);
            LogSouthEdgeSegments(segments);

            int rowCount = ResolveRowCount(height);
            Debug.Log($"[CliffEdgeGenerator] Row count = {rowCount} (Height={height}), cliff Y from southEdgeY+1 downward.");

            if (segments.Count == 0)
            {
                return 0;
            }

            int placed = 0;
            foreach (var segment in segments)
            {
                foreach (var col in segment)
                {
                    var attrs = ClassifyColumn(view, col, segment, southEdge);
                    placed += PlaceColumn(cliff, tileSet, col.X, attrs, height);
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
            foreach (var segment in GroupSouthEdgeSegments(southEdge))
            {
                foreach (var col in segment)
                {
                    var attrs = ClassifyColumn(view, col, segment, southEdge);
                    int rowCount = ResolveRowCount(height);
                    for (int r = 0; r < rowCount; r++)
                    {
                        yield return new CliffPlacement
                        {
                            CliffCell = CliffDualGridMapping.ResolveCliffCell(col.X, attrs.SouthEdgeY, r, col.Z),
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
                if (c.X != nx)
                {
                    continue;
                }

                int dy = Mathf.Abs(c.Y - col.Y);
                if (dy > 1)
                {
                    continue;
                }

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
            bool leftOpen = !hasSegLeft && !leftCliffWall;
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
                }
                else if (rightY > col.Y)
                {
                    depth = CliffDepthJunction.Right;
                }
            }

            CliffCornerShape corner = depth != CliffDepthJunction.None
                ? CliffCornerShape.None
                : ResolveConvexCorner(view, col, hasSegLeft, hasSegRight, leftCliffWall, rightCliffWall);

            return new ColumnAttrs
            {
                X = col.X,
                SouthEdgeY = col.Y,
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

        // 首行在 southEdgeY+1，共 Height+2 行（补顶 + 南缘行 + Height 体）
        static int ResolveRowCount(int height) => height + 2;

        static int PlaceColumn(
            Tilemap cliff,
            CliffTileSet tileSet,
            int cliffX,
            ColumnAttrs attrs,
            int height)
        {
            int rowCount = ResolveRowCount(height);
            int placed = 0;
            for (int r = 0; r < rowCount; r++)
            {
                var cliffCell = CliffDualGridMapping.ResolveCliffCell(cliffX, attrs.SouthEdgeY, r, 0);
                var span = ResolvePlacementSpan(r, attrs);
                var depthJunction = attrs.DepthJunction;
                var corner = r == 0 ? attrs.Corner : CliffCornerShape.None;
                if (depthJunction != CliffDepthJunction.None)
                {
                    corner = CliffCornerShape.None;
                }

                var key = ResolveVariant(r, rowCount, span, corner, depthJunction);
                var tile = tileSet.GetTile(key);
                if (tile == null)
                {
                    Debug.LogWarning(
                        $"[CliffEdgeGenerator] Missing tile at cliff ({cliffX},{attrs.SouthEdgeY + 1 - r}), "
                        + $"key={key.Row}/{key.Span}/depth={key.DepthJunction}");
                    continue;
                }

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
            int height,
            CliffSpanRole span,
            CliffCornerShape corner,
            CliffDepthJunction depthJunction)
        {
            if (depthJunction != CliffDepthJunction.None)
            {
                if (rowIndex == height - 1)
                {
                    var bottomSpan = depthJunction == CliffDepthJunction.Left
                        ? CliffSpanRole.LeftEnd
                        : CliffSpanRole.RightEnd;
                    return new CliffVariantKey(CliffRowRole.Bottom, bottomSpan, CliffCornerShape.None);
                }

                if (height >= 2 && rowIndex == height - 2)
                {
                    return new CliffVariantKey(
                        CliffRowRole.Bottom, CliffSpanRole.Mid, CliffCornerShape.None, depthJunction);
                }

                return new CliffVariantKey(
                    CliffRowRole.Body, CliffSpanRole.Mid, CliffCornerShape.None, depthJunction);
            }

            if (height == 1)
            {
                return new CliffVariantKey(CliffRowRole.Body, span, corner);
            }

            if (rowIndex == height - 1)
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
