using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Dungeon
{
    public static class DungeonFloorPatternStamper
    {
        private static readonly Vector2Int Size2x2 = new Vector2Int(2, 2);

        public static bool Apply(DungeonGenerationResult result, DungeonFloorTileset tileset, WorldAreaRoot root)
        {
            var basePatterns = tileset.ResolveBasePatterns();
            if (basePatterns.Count == 0)
            {
                Debug.LogError("DungeonFloorPatternStamper: no base patterns available");
                return false;
            }

            var baseMap = root.TileGrounds[0];
            var uncovered = new HashSet<Vector3Int>(result.WalkableCells);
            var walkable = result.WalkableCells;

            var patterns2x2 = FilterPatternsBySize(basePatterns, Size2x2);
            var patterns1x2 = FilterPatterns1x2(basePatterns);
            var patterns1x1 = tileset.Allow1x1Patterns ? FilterPatternsBySize(basePatterns, Vector2Int.one) : null;

            RunPhase2x2GridPass(baseMap, uncovered, walkable, patterns2x2, tileset.BaseGridPhase, result.Seed);
            RunPhase1x2Pass(baseMap, uncovered, walkable, patterns1x2, result.Seed);

            if (patterns1x1 != null && patterns1x1.Count > 0)
            {
                RunPhase1x1Pass(baseMap, uncovered, walkable, patterns1x1, result.Seed);
            }

            FillOrphanCells(baseMap, uncovered, patterns2x2, basePatterns, result.Seed);

            if (root.TileGrounds.Length > 1 && root.TileGrounds[1] != null)
            {
                ApplyAccentPass(result, tileset, root.TileGrounds[0], root.TileGrounds[1]);
            }

            baseMap.CompressBounds();
            if (root.TileGrounds.Length > 1 && root.TileGrounds[1] != null)
            {
                root.TileGrounds[1].CompressBounds();
            }

            return true;
        }

        private static void RunPhase2x2GridPass(
            Tilemap map,
            HashSet<Vector3Int> uncovered,
            HashSet<Vector3Int> walkable,
            List<DungeonFloorPattern> patterns,
            Vector2Int gridPhase,
            int seed)
        {
            if (patterns.Count == 0)
            {
                return;
            }

            foreach (var anchor in BuildSortedAnchors(uncovered))
            {
                if (!uncovered.Contains(anchor))
                {
                    continue;
                }

                if (!IsGridAligned(anchor, gridPhase))
                {
                    continue;
                }

                var rng = new DungeonRng(DungeonRng.DeriveSeed(seed, "phase2x2", anchor.x, anchor.y));
                TryPickAndStamp(map, anchor, patterns, uncovered, walkable, rng);
            }
        }

        private static void RunPhase1x2Pass(
            Tilemap map,
            HashSet<Vector3Int> uncovered,
            HashSet<Vector3Int> walkable,
            List<DungeonFloorPattern> patterns,
            int seed)
        {
            if (patterns.Count == 0)
            {
                return;
            }

            foreach (var anchor in BuildSortedAnchors(uncovered))
            {
                if (!uncovered.Contains(anchor))
                {
                    continue;
                }

                var rng = new DungeonRng(DungeonRng.DeriveSeed(seed, "phase1x2", anchor.x, anchor.y));
                TryPickAndStamp(map, anchor, patterns, uncovered, walkable, rng);
            }
        }

        private static void RunPhase1x1Pass(
            Tilemap map,
            HashSet<Vector3Int> uncovered,
            HashSet<Vector3Int> walkable,
            List<DungeonFloorPattern> patterns,
            int seed)
        {
            foreach (var anchor in BuildSortedAnchors(uncovered))
            {
                if (!uncovered.Contains(anchor))
                {
                    continue;
                }

                var rng = new DungeonRng(DungeonRng.DeriveSeed(seed, "phase1x1", anchor.x, anchor.y));
                TryPickAndStamp(map, anchor, patterns, uncovered, walkable, rng);
            }
        }

        private static void FillOrphanCells(
            Tilemap map,
            HashSet<Vector3Int> uncovered,
            List<DungeonFloorPattern> patterns2x2,
            List<DungeonFloorPattern> allPatterns,
            int seed)
        {
            if (uncovered.Count == 0)
            {
                return;
            }

            var sliceSource = patterns2x2.Count > 0 ? patterns2x2 : allPatterns;
            if (sliceSource.Count == 0)
            {
                Debug.LogWarning("DungeonFloorPatternStamper: orphan cells remain but no pattern available for slice fill");
                return;
            }

            foreach (var cell in BuildSortedAnchors(uncovered))
            {
                if (!uncovered.Contains(cell))
                {
                    continue;
                }

                var rng = new DungeonRng(DungeonRng.DeriveSeed(seed, "orphan", cell.x, cell.y));
                var pattern = PickWeighted(sliceSource, rng);
                var tile = PickSliceTile(pattern, rng);
                if (tile == null)
                {
                    continue;
                }

                map.SetTile(cell, tile);
                uncovered.Remove(cell);
            }
        }

        private static bool TryPickAndStamp(
            Tilemap map,
            Vector3Int anchor,
            List<DungeonFloorPattern> patterns,
            HashSet<Vector3Int> uncovered,
            HashSet<Vector3Int> walkable,
            DungeonRng rng)
        {
            var candidates = new List<DungeonFloorPattern>();
            foreach (var pattern in patterns)
            {
                if (CanPlaceBase(pattern, anchor, uncovered, walkable))
                {
                    candidates.Add(pattern);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var placed = PickWeighted(candidates, rng);
            StampPattern(map, anchor, placed);
            RemoveFromUncovered(anchor, placed, uncovered);
            return true;
        }

        private static TileBase PickSliceTile(DungeonFloorPattern pattern, DungeonRng rng)
        {
            if (pattern == null || pattern.Cells == null || pattern.Cells.Count == 0)
            {
                return null;
            }

            int idx = rng.NextInt(pattern.Cells.Count);
            return pattern.Cells[idx].Tile;
        }

        private static bool IsGridAligned(Vector3Int anchor, Vector2Int gridPhase)
        {
            return Mod2(anchor.x - gridPhase.x) == 0 && Mod2(anchor.y - gridPhase.y) == 0;
        }

        private static int Mod2(int value)
        {
            int mod = value % 2;
            return mod < 0 ? mod + 2 : mod;
        }

        private static List<DungeonFloorPattern> FilterPatternsBySize(
            List<DungeonFloorPattern> patterns,
            Vector2Int size)
        {
            var result = new List<DungeonFloorPattern>();
            foreach (var pattern in patterns)
            {
                if (pattern != null && pattern.SizeCells == size)
                {
                    result.Add(pattern);
                }
            }

            return result;
        }

        private static List<DungeonFloorPattern> FilterPatterns1x2(List<DungeonFloorPattern> patterns)
        {
            var result = new List<DungeonFloorPattern>();
            foreach (var pattern in patterns)
            {
                if (pattern == null)
                {
                    continue;
                }

                bool horizontal = pattern.SizeCells.x == 2 && pattern.SizeCells.y == 1;
                bool vertical = pattern.SizeCells.x == 1 && pattern.SizeCells.y == 2;
                if (horizontal || vertical)
                {
                    result.Add(pattern);
                }
            }

            return result;
        }

        private static void ApplyAccentPass(
            DungeonGenerationResult result,
            DungeonFloorTileset tileset,
            Tilemap baseMap,
            Tilemap accentMap)
        {
            var accentPatterns = tileset.ResolveAccentPatterns();
            if (accentPatterns.Count == 0 || tileset.AccentDensity <= 0f)
            {
                return;
            }

            var priorities = GetDistinctPrioritiesSortedDesc(accentPatterns);
            var accentOccupied = new HashSet<Vector3Int>();
            var anchors = BuildSortedAnchors(result.WalkableCells);

            foreach (var anchor in anchors)
            {
                if (baseMap.GetTile(anchor) == null)
                {
                    continue;
                }

                var rng = new DungeonRng(DungeonRng.DeriveSeed(result.Seed, "accent", anchor.x, anchor.y));
                if (rng.NextFloat() >= tileset.AccentDensity)
                {
                    continue;
                }

                TryPickAndStampAccent(accentMap, anchor, accentPatterns, priorities, baseMap, accentOccupied, result.WalkableCells, rng);
            }
        }

        private static bool TryPickAndStampAccent(
            Tilemap accentMap,
            Vector3Int anchor,
            List<DungeonFloorPattern> patterns,
            List<int> priorities,
            Tilemap baseMap,
            HashSet<Vector3Int> accentOccupied,
            HashSet<Vector3Int> walkable,
            DungeonRng rng)
        {
            foreach (var priority in priorities)
            {
                var candidates = new List<DungeonFloorPattern>();
                foreach (var pattern in patterns)
                {
                    if (pattern.SizePriority != priority)
                    {
                        continue;
                    }

                    if (CanPlaceAccent(pattern, anchor, baseMap, accentOccupied, walkable))
                    {
                        candidates.Add(pattern);
                    }
                }

                if (candidates.Count == 0)
                {
                    continue;
                }

                var picked = PickWeighted(candidates, rng);
                StampPattern(accentMap, anchor, picked);
                MarkAccentOccupied(anchor, picked, accentOccupied);
                return true;
            }

            return false;
        }

        private static bool CanPlaceBase(
            DungeonFloorPattern pattern,
            Vector3Int anchor,
            HashSet<Vector3Int> uncovered,
            HashSet<Vector3Int> walkable)
        {
            foreach (var worldCell in pattern.EnumerateWorldCells(anchor))
            {
                if (!walkable.Contains(worldCell) || !uncovered.Contains(worldCell))
                {
                    return false;
                }
            }

            return pattern.Cells != null && pattern.Cells.Count > 0;
        }

        private static bool CanPlaceAccent(
            DungeonFloorPattern pattern,
            Vector3Int anchor,
            Tilemap baseMap,
            HashSet<Vector3Int> accentOccupied,
            HashSet<Vector3Int> walkable)
        {
            foreach (var worldCell in pattern.EnumerateWorldCells(anchor))
            {
                if (!walkable.Contains(worldCell))
                {
                    return false;
                }

                if (baseMap.GetTile(worldCell) == null)
                {
                    return false;
                }

                if (accentOccupied.Contains(worldCell))
                {
                    return false;
                }
            }

            return pattern.Cells != null && pattern.Cells.Count > 0;
        }

        private static void StampPattern(Tilemap map, Vector3Int anchor, DungeonFloorPattern pattern)
        {
            if (pattern.Cells == null)
            {
                return;
            }

            foreach (var cell in pattern.Cells)
            {
                if (cell.Tile == null)
                {
                    continue;
                }

                var world = new Vector3Int(
                    anchor.x + cell.LocalOffset.x - pattern.Anchor.x,
                    anchor.y + cell.LocalOffset.y - pattern.Anchor.y,
                    0);
                map.SetTile(world, cell.Tile);
            }
        }

        private static void RemoveFromUncovered(Vector3Int anchor, DungeonFloorPattern pattern, HashSet<Vector3Int> uncovered)
        {
            foreach (var worldCell in pattern.EnumerateWorldCells(anchor))
            {
                uncovered.Remove(worldCell);
            }
        }

        private static void MarkAccentOccupied(Vector3Int anchor, DungeonFloorPattern pattern, HashSet<Vector3Int> accentOccupied)
        {
            foreach (var worldCell in pattern.EnumerateWorldCells(anchor))
            {
                accentOccupied.Add(worldCell);
            }
        }

        private static DungeonFloorPattern PickWeighted(List<DungeonFloorPattern> candidates, DungeonRng rng)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            int total = 0;
            foreach (var candidate in candidates)
            {
                total += Mathf.Max(1, candidate.Weight);
            }

            int roll = rng.NextInt(total);
            int acc = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += Mathf.Max(1, candidates[i].Weight);
                if (roll < acc)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static List<Vector3Int> BuildSortedAnchors(HashSet<Vector3Int> cells)
        {
            var list = new List<Vector3Int>(cells);
            list.Sort((a, b) =>
            {
                int cmp = a.x.CompareTo(b.x);
                return cmp != 0 ? cmp : a.y.CompareTo(b.y);
            });
            return list;
        }

        private static List<int> GetDistinctPrioritiesSortedDesc(List<DungeonFloorPattern> patterns)
        {
            var set = new HashSet<int>();
            foreach (var pattern in patterns)
            {
                set.Add(pattern.SizePriority);
            }

            var list = new List<int>(set);
            list.Sort((a, b) => b.CompareTo(a));
            return list;
        }
    }
}
