using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Cliff
{
    public static class CliffEdgeGenerator
    {
        // 南边缘：Data 有砖且正下方（-Y）无砖
        public static bool IsSouthEdgeCell(Tilemap source, Vector3Int platformCell)
        {
            if (source == null || !IsFilled(source, platformCell))
            {
                return false;
            }

            return !IsFilled(source, platformCell + Vector3Int.down);
        }

        public static HashSet<Vector3Int> CollectSouthEdgeCells(Tilemap source)
        {
            return BuildSouthEdgeSet(source);
        }

        public struct CliffPlacement
        {
            public Vector3Int CliffCell;
            public Vector3Int DataSouthEdgeCell;
            public bool HasDataSouthEdgeCell;
            public bool IsEdgeRow;
            public bool IsWestCapColumn;
            public EdgeInfo Edge;
        }

        public struct EdgeInfo
        {
            public Vector3Int PlatformCell;
            public CliffSpanRole Span;
            public CliffCornerShape Corner;
            public CliffDepthJunction DepthJunction;
        }

        struct SouthEdgeSegment
        {
            public int XLeft;
            public int XRight;
            public int Y;
            public int Z;
        }

        public static List<EdgeInfo> CollectSouthEdges(Tilemap source)
        {
            var southEdge = BuildSouthEdgeSet(source);
            var edges = new List<EdgeInfo>();
            foreach (var cell in southEdge)
            {
                var depthJunction = ResolveDepthJunction(cell, source, southEdge);
                edges.Add(new EdgeInfo
                {
                    PlatformCell = cell,
                    Span = ResolveDataSouthEdgeSpan(cell, southEdge),
                    Corner = depthJunction != CliffDepthJunction.None
                        ? CliffCornerShape.None
                        : ResolveConvexCorner(cell, source, southEdge),
                    DepthJunction = depthJunction,
                });
            }

            return edges;
        }

        public static CliffVariantKey ResolveVariant(
            int rowIndex,
            int height,
            CliffSpanRole span,
            CliffCornerShape corner,
            CliffDepthJunction depthJunction)
        {
            if (height <= 0)
            {
                return new CliffVariantKey(CliffRowRole.Body, CliffSpanRole.Mid, CliffCornerShape.None);
            }

            if (depthJunction != CliffDepthJunction.None)
            {
                // 深度交界砖与 Span 无关，避免 RightEnd 参与回退查表
                span = CliffSpanRole.Mid;
                if (height == 1 || rowIndex < height - 1)
                {
                    return new CliffVariantKey(CliffRowRole.Body, span, CliffCornerShape.None, depthJunction);
                }

                return new CliffVariantKey(CliffRowRole.Bottom, span, CliffCornerShape.None, depthJunction);
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

        public static int Generate(
            Tilemap source,
            Tilemap cliff,
            CliffTileSet tileSet,
            int height,
            bool clearBeforeGenerate,
            bool dualGridPlateau = false)
        {
            if (source == null || cliff == null || tileSet == null || height < 1)
            {
                return 0;
            }

            if (clearBeforeGenerate)
            {
                cliff.ClearAllTiles();
            }

            var southEdge = BuildSouthEdgeSet(source);
            var edgeInfoMap = BuildEdgeInfoMap(source, southEdge);
            var segments = GroupSouthEdgeSegments(southEdge);
            int placed = 0;

            foreach (var segment in segments)
            {
                var range = CliffDualGridMapping.ResolveColumnRange(
                    segment.XLeft, segment.XRight, segment.Y, segment.Z, dualGridPlateau);
                edgeInfoMap.TryGetValue(
                    new Vector3Int(segment.XLeft, segment.Y, segment.Z),
                    out EdgeInfo segmentHeadEdge);

                for (int cliffX = range.CliffXLeft; cliffX <= range.CliffXRight; cliffX++)
                {
                    if (!TryResolveColumnContext(cliffX, range, edgeInfoMap, out bool isWestCap, out EdgeInfo edge))
                    {
                        continue;
                    }

                    for (int r = 0; r < height; r++)
                    {
                        var cliffCell = CliffDualGridMapping.ResolveCliffCell(cliffX, segment.Y, r, segment.Z);
                        var span = ResolvePlacementSpan(r, cliffX, range.CliffXLeft, range.CliffXRight);
                        var depthJunction = ResolvePlacementDepthJunction(cliffX, range, edgeInfoMap);
                        var corner = ResolvePlacementCorner(
                            r, cliffX, isWestCap, dualGridPlateau, segment.XLeft, edge, segmentHeadEdge);
                        if (depthJunction != CliffDepthJunction.None)
                        {
                            corner = CliffCornerShape.None;
                        }
                        var key = ResolveVariant(r, height, span, corner, depthJunction);
                        var tile = tileSet.GetTile(key);
                        if (tile == null)
                        {
                            continue;
                        }

                        cliff.SetTile(cliffCell, tile);
                        placed++;
                    }
                }
            }

            return placed;
        }

        public static IEnumerable<CliffPlacement> EnumerateCliffPlacements(
            Tilemap source,
            bool dualGridPlateau,
            int height)
        {
            if (source == null || height < 1)
            {
                yield break;
            }

            var southEdge = BuildSouthEdgeSet(source);
            var edgeInfoMap = BuildEdgeInfoMap(source, southEdge);
            foreach (var segment in GroupSouthEdgeSegments(southEdge))
            {
                var range = CliffDualGridMapping.ResolveColumnRange(
                    segment.XLeft, segment.XRight, segment.Y, segment.Z, dualGridPlateau);

                for (int cliffX = range.CliffXLeft; cliffX <= range.CliffXRight; cliffX++)
                {
                    if (!TryResolveColumnContext(cliffX, range, edgeInfoMap, out bool isWestCap, out EdgeInfo edge))
                    {
                        continue;
                    }

                    bool hasDataCell = CliffDualGridMapping.TryResolveDataSouthEdgeCell(
                        cliffX, segment.XLeft, segment.Y, segment.Z, dualGridPlateau, out var dataCell);
                    var depthJunction = ResolvePlacementDepthJunction(cliffX, range, edgeInfoMap);

                    for (int r = 0; r < height; r++)
                    {
                        yield return new CliffPlacement
                        {
                            CliffCell = CliffDualGridMapping.ResolveCliffCell(cliffX, segment.Y, r, segment.Z),
                            DataSouthEdgeCell = dataCell,
                            HasDataSouthEdgeCell = hasDataCell,
                            IsEdgeRow = r == 0,
                            IsWestCapColumn = isWestCap,
                            Edge = new EdgeInfo
                            {
                                PlatformCell = hasDataCell ? dataCell : new Vector3Int(cliffX, segment.Y, segment.Z),
                                Span = edge.Span,
                                Corner = edge.Corner,
                                DepthJunction = depthJunction,
                            },
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

        public static Vector3 ResolveGizmoDataSouthEdgeWorld(Tilemap data, CliffPlacement placement)
        {
            if (!placement.HasDataSouthEdgeCell)
            {
                return Vector3.zero;
            }

            return CliffDualGridMapping.ResolveDataSouthEdgeWorld(data, placement.DataSouthEdgeCell);
        }

        static bool TryResolveColumnContext(
            int cliffX,
            CliffDualGridMapping.CliffColumnRange range,
            Dictionary<Vector3Int, EdgeInfo> edgeInfoMap,
            out bool isWestCap,
            out EdgeInfo edge)
        {
            isWestCap = CliffDualGridMapping.IsWestCapColumn(cliffX, range.DataXLeft, range.DualGrid);
            if (isWestCap)
            {
                edge = new EdgeInfo
                {
                    PlatformCell = new Vector3Int(cliffX, range.DataSouthEdgeY, range.Z),
                    Span = CliffSpanRole.Mid,
                    Corner = CliffCornerShape.None,
                    DepthJunction = CliffDepthJunction.None,
                };
                return true;
            }

            var dataCell = new Vector3Int(cliffX, range.DataSouthEdgeY, range.Z);
            return edgeInfoMap.TryGetValue(dataCell, out edge);
        }

        // 首行：中间列 Single 衔接草缘；段左右端列仍用 LeftEnd/RightEnd
        static CliffSpanRole ResolvePlacementSpan(
            int rowIndex,
            int cliffX,
            int cliffXLeft,
            int cliffXRight)
        {
            if (rowIndex == 0)
            {
                var edgeSpan = ResolveCliffSpan(cliffX, cliffXLeft, cliffXRight);
                return edgeSpan == CliffSpanRole.Mid ? CliffSpanRole.Single : edgeSpan;
            }

            return ResolveCliffSpan(cliffX, cliffXLeft, cliffXRight);
        }

        static CliffSpanRole ResolveCliffSpan(int cliffX, int cliffXLeft, int cliffXRight)
        {
            if (cliffXLeft == cliffXRight)
            {
                return CliffSpanRole.Single;
            }

            if (cliffX == cliffXLeft)
            {
                return CliffSpanRole.LeftEnd;
            }

            if (cliffX == cliffXRight)
            {
                return CliffSpanRole.RightEnd;
            }

            return CliffSpanRole.Mid;
        }

        // Dual 西扩列承接段首 Data 格的 ConvexLeft；该角不再画在 Data 列
        static CliffCornerShape ResolvePlacementCorner(
            int rowIndex,
            int cliffX,
            bool isWestCap,
            bool dualGridPlateau,
            int dataXLeft,
            EdgeInfo edge,
            EdgeInfo segmentHeadEdge)
        {
            if (rowIndex != 0)
            {
                return CliffCornerShape.None;
            }

            if (isWestCap)
            {
                return segmentHeadEdge.Corner == CliffCornerShape.ConvexLeft
                    ? CliffCornerShape.ConvexLeft
                    : CliffCornerShape.None;
            }

            if (dualGridPlateau
                && cliffX == dataXLeft
                && edge.Corner == CliffCornerShape.ConvexLeft)
            {
                return CliffCornerShape.None;
            }

            return edge.Corner;
        }

        // Dual：Data 左交界 → 西侧 View 列 (dataX-1)；Data 右交界 → dataX 列
        static CliffDepthJunction ResolvePlacementDepthJunction(
            int cliffX,
            CliffDualGridMapping.CliffColumnRange range,
            Dictionary<Vector3Int, EdgeInfo> edgeInfoMap)
        {
            if (range.DualGrid)
            {
                var leftSource = new Vector3Int(cliffX + 1, range.DataSouthEdgeY, range.Z);
                if (edgeInfoMap.TryGetValue(leftSource, out EdgeInfo leftEdge)
                    && leftEdge.DepthJunction == CliffDepthJunction.Left)
                {
                    return CliffDepthJunction.Left;
                }

                var rightSource = new Vector3Int(cliffX, range.DataSouthEdgeY, range.Z);
                if (edgeInfoMap.TryGetValue(rightSource, out EdgeInfo rightEdge)
                    && rightEdge.DepthJunction == CliffDepthJunction.Right)
                {
                    return CliffDepthJunction.Right;
                }

                return CliffDepthJunction.None;
            }

            var dataCell = new Vector3Int(cliffX, range.DataSouthEdgeY, range.Z);
            return edgeInfoMap.TryGetValue(dataCell, out EdgeInfo edge)
                ? edge.DepthJunction
                : CliffDepthJunction.None;
        }

        static Dictionary<Vector3Int, EdgeInfo> BuildEdgeInfoMap(Tilemap source, HashSet<Vector3Int> southEdge)
        {
            var map = new Dictionary<Vector3Int, EdgeInfo>();
            foreach (var cell in southEdge)
            {
                var depthJunction = ResolveDepthJunction(cell, source, southEdge);
                map[cell] = new EdgeInfo
                {
                    PlatformCell = cell,
                    Span = ResolveDataSouthEdgeSpan(cell, southEdge),
                    Corner = depthJunction != CliffDepthJunction.None
                        ? CliffCornerShape.None
                        : ResolveConvexCorner(cell, source, southEdge),
                    DepthJunction = depthJunction,
                };
            }

            return map;
        }

        static List<SouthEdgeSegment> GroupSouthEdgeSegments(HashSet<Vector3Int> southEdge)
        {
            var segments = new List<SouthEdgeSegment>();
            var byRow = southEdge
                .GroupBy(c => (c.y, c.z))
                .OrderBy(g => g.Key.y)
                .ThenBy(g => g.Key.z);

            foreach (var rowGroup in byRow)
            {
                var sorted = rowGroup.OrderBy(c => c.x).ToList();
                int i = 0;
                while (i < sorted.Count)
                {
                    int xLeft = sorted[i].x;
                    int xRight = xLeft;
                    int y = sorted[i].y;
                    int z = sorted[i].z;
                    i++;
                    while (i < sorted.Count && sorted[i].x == xRight + 1 && sorted[i].y == y)
                    {
                        xRight = sorted[i].x;
                        i++;
                    }

                    segments.Add(new SouthEdgeSegment { XLeft = xLeft, XRight = xRight, Y = y, Z = z });
                }
            }

            return segments;
        }

        static HashSet<Vector3Int> BuildSouthEdgeSet(Tilemap source)
        {
            var southEdge = new HashSet<Vector3Int>();
            if (source == null)
            {
                return southEdge;
            }

            foreach (var cell in source.cellBounds.allPositionsWithin)
            {
                if (IsSouthEdgeCell(source, cell))
                {
                    southEdge.Add(cell);
                }
            }

            return southEdge;
        }

        static bool IsFilled(Tilemap source, Vector3Int cell) => source.GetTile(cell) != null;

        static CliffSpanRole ResolveDataSouthEdgeSpan(Vector3Int cell, HashSet<Vector3Int> southEdge)
        {
            bool left = southEdge.Contains(cell + Vector3Int.left);
            bool right = southEdge.Contains(cell + Vector3Int.right);

            if (!left && !right)
            {
                return CliffSpanRole.Single;
            }

            if (!left && right)
            {
                return CliffSpanRole.LeftEnd;
            }

            if (left && !right)
            {
                return CliffSpanRole.RightEnd;
            }

            return CliffSpanRole.Mid;
        }

        static CliffDepthJunction ResolveDepthJunction(
            Vector3Int cell,
            Tilemap source,
            HashSet<Vector3Int> southEdge)
        {
            bool filled(int dx, int dy) => IsFilled(source, cell + new Vector3Int(dx, dy, 0));
            bool edge(int dx, int dy) => southEdge.Contains(cell + new Vector3Int(dx, dy, 0));

            // Left：西侧 (x-1,y) 更高台地（非南缘），且 (x-1,y-1) 有砖
            if (filled(-1, 0) && !edge(-1, 0) && filled(-1, -1))
            {
                return CliffDepthJunction.Left;
            }

            // Right：东侧 (x+1,y) 更高台地（非南缘），且 (x+1,y-1) 有砖
            if (filled(1, 0) && !edge(1, 0) && filled(1, -1))
            {
                return CliffDepthJunction.Right;
            }

            return CliffDepthJunction.None;
        }

        static CliffCornerShape ResolveConvexCorner(
            Vector3Int cell,
            Tilemap source,
            HashSet<Vector3Int> southEdge)
        {
            bool filled(int dx, int dy) => IsFilled(source, cell + new Vector3Int(dx, dy, 0));
            bool edge(int dx, int dy) => southEdge.Contains(cell + new Vector3Int(dx, dy, 0));

            if (!filled(-1, -1) && edge(-1, 0))
            {
                return CliffCornerShape.ConvexLeft;
            }

            if (!filled(1, -1) && edge(1, 0))
            {
                return CliffCornerShape.ConvexRight;
            }

            return CliffCornerShape.None;
        }
    }
}
