using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Cliff
{
    public static class CliffEdgeGenerator
    {
        // 南边缘：源层有砖，且正下方（-Y）一格无砖。
        // 凸/凹台地时同一 X 的不同 Y 行可各自成为南边缘，不做「每列只取一条」合并。
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
            public Vector3Int PlatformEdgeCell;
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
            public int DataWidth => XRight - XLeft + 1;
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
                    Span = ResolveSpan(cell, southEdge),
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
                int cliffXLeft = dualGridPlateau ? segment.XLeft - 1 : segment.XLeft;
                int cliffXRight = segment.XRight;

                for (int cliffX = cliffXLeft; cliffX <= cliffXRight; cliffX++)
                {
                    if (!TryResolveColumnContext(
                            cliffX, segment, dualGridPlateau, edgeInfoMap,
                            out bool isWestCap, out EdgeInfo edge))
                    {
                        continue;
                    }

                    for (int r = 0; r < height; r++)
                    {
                        var cliffCell = new Vector3Int(cliffX, segment.Y - 1 - r, segment.Z);
                        var span = ResolvePlacementSpan(r, edge.Span, isWestCap);
                        var corner = r == 0 && !isWestCap ? edge.Corner : CliffCornerShape.None;
                        var depthJunction = isWestCap ? CliffDepthJunction.None : edge.DepthJunction;
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
                int cliffXLeft = dualGridPlateau ? segment.XLeft - 1 : segment.XLeft;
                int cliffXRight = segment.XRight;
                for (int cliffX = cliffXLeft; cliffX <= cliffXRight; cliffX++)
                {
                    if (!TryResolveColumnContext(
                            cliffX, segment, dualGridPlateau, edgeInfoMap,
                            out bool isWestCap, out EdgeInfo edge))
                    {
                        continue;
                    }

                    var platformEdge = new Vector3Int(cliffX, segment.Y, segment.Z);

                    for (int r = 0; r < height; r++)
                    {
                        yield return new CliffPlacement
                        {
                            CliffCell = new Vector3Int(cliffX, segment.Y - 1 - r, segment.Z),
                            PlatformEdgeCell = platformEdge,
                            IsEdgeRow = r == 0,
                            IsWestCapColumn = isWestCap,
                            Edge = edge,
                        };
                    }
                }
            }
        }

        public static Vector3 ResolveGizmoEdgeWorld(Tilemap cliff, CliffPlacement placement, Vector3 cellSize)
        {
            var cliffCenter = cliff.GetCellCenterWorld(placement.CliffCell);
            return cliffCenter + new Vector3(0f, cellSize.y * 0.5f, 0f);
        }

        // Dual Grid 西扩列 = View 最西侧填充列，用 Mid；Data 列仍按 edge.Span 区分 Left/Mid/Right
        static bool TryResolveColumnContext(
            int cliffX,
            SouthEdgeSegment segment,
            bool dualGridPlateau,
            Dictionary<Vector3Int, EdgeInfo> edgeInfoMap,
            out bool isWestCap,
            out EdgeInfo edge)
        {
            isWestCap = dualGridPlateau && cliffX < segment.XLeft;
            if (isWestCap)
            {
                edge = new EdgeInfo
                {
                    PlatformCell = new Vector3Int(cliffX, segment.Y, segment.Z),
                    Span = CliffSpanRole.Mid,
                    Corner = CliffCornerShape.None,
                    DepthJunction = CliffDepthJunction.None,
                };
                return true;
            }

            var platformCell = new Vector3Int(cliffX, segment.Y, segment.Z);
            return edgeInfoMap.TryGetValue(platformCell, out edge);
        }

        // 首行 Single；西扩列 Mid 填充；其余行按 Data 南缘 Span
        static CliffSpanRole ResolvePlacementSpan(int rowIndex, CliffSpanRole segmentSpan, bool isWestCap)
        {
            if (isWestCap)
            {
                return CliffSpanRole.Mid;
            }

            if (rowIndex == 0)
            {
                return CliffSpanRole.Single;
            }

            return segmentSpan;
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
                    Span = ResolveSpan(cell, southEdge),
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

        static CliffSpanRole ResolveSpan(Vector3Int cell, HashSet<Vector3Int> southEdge)
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

            if (filled(-1, 0) && !edge(-1, 0) && filled(-1, -1))
            {
                return CliffDepthJunction.Left;
            }

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
