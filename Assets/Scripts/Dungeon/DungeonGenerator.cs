using System;
using System.Collections.Generic;
using My.MapExport;
using UnityEngine;

namespace My.Dungeon
{
    public static class DungeonGenerator
    {
        private const int MinSlotGapCells = 2;

        private struct GraphNode
        {
            public int Id;
            public int ParentId;
            public Vector2Int SlotCoord;
            public EDungeonRoomRole Role;
            public EDungeonRoomContentType ContentType;
            public int MonsterCount;
            public DungeonRoomExportMeta Template;
        }

        private struct GraphEdge
        {
            public int A;
            public int B;
        }

        public static DungeonGenerationResult Generate(string dungeonId, int seed)
        {
            var def = DungeonConfigCatalog.GetOrDefault(dungeonId);
            if (def == null)
            {
                Debug.LogError($"DungeonGenerator: dungeon def not found: {dungeonId}");
                return null;
            }

            if (def.RoomTemplates == null || def.RoomTemplates.Count == 0)
            {
                Debug.LogError($"DungeonGenerator: no room templates for {dungeonId}");
                return null;
            }

            var rng = new DungeonRng(seed);
            int roomCount = rng.NextInt(def.MinRooms, def.MaxRooms + 1);
            roomCount = Mathf.Clamp(roomCount, 2, 32);
            int slotStride = ResolveSlotStride(def);

            var nodes = BuildGraph(roomCount, def, rng);
            AssignRoles(nodes, rng);
            AssignContentTypes(nodes, def, rng);
            AssignTemplates(nodes, def, rng);
            EmbedOnGrid(nodes, def, rng);

            var edges = CollectEdges(nodes);
            var result = new DungeonGenerationResult
            {
                Seed = seed,
                DungeonId = dungeonId,
            };

            foreach (var node in nodes)
            {
                result.Rooms.Add(new PlacedRoom
                {
                    GraphNodeId = node.Id,
                    TemplateId = node.Template != null ? node.Template.TemplateId : string.Empty,
                    Role = node.Role,
                    ContentType = node.ContentType,
                    MonsterCount = node.MonsterCount,
                    GridOriginCells = new Vector2Int(node.SlotCoord.x * slotStride, node.SlotCoord.y * slotStride),
                    SlotCoord = node.SlotCoord,
                    Meta = node.Template,
                });
            }

            BuildWalkableCells(result, def);
            BuildCorridors(result, edges, def, rng);
            result.RuntimeMapData = DungeonRuntimeMapBuilder.Build(result, def);

            return result;
        }

        private static int ResolveSlotStride(DungeonDef def)
        {
            int maxW = 0;
            int maxH = 0;
            foreach (var template in def.RoomTemplates)
            {
                if (template == null)
                {
                    continue;
                }

                maxW = Mathf.Max(maxW, template.SizeCells.x);
                maxH = Mathf.Max(maxH, template.SizeCells.y);
            }

            int minStride = Mathf.Max(maxW, maxH) + MinSlotGapCells;
            if (def.SlotStrideCells < minStride)
            {
                Debug.LogWarning($"DungeonGenerator: SlotStrideCells {def.SlotStrideCells} < {minStride}, clamped.");
                return minStride;
            }

            return def.SlotStrideCells;
        }

        private static List<GraphNode> BuildGraph(int roomCount, DungeonDef def, DungeonRng rng)
        {
            var nodes = new List<GraphNode>(roomCount);
            var occupied = new HashSet<Vector2Int> { Vector2Int.zero };

            nodes.Add(new GraphNode { Id = 0, ParentId = -1, SlotCoord = Vector2Int.zero });

            var frontier = new List<Vector2Int> { Vector2Int.zero };
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (nodes.Count < roomCount && frontier.Count > 0)
            {
                int fi = PickFrontierIndex(frontier, def.GraphRandomness, rng);
                var baseSlot = frontier[fi];
                int parentId = FindNodeAt(nodes, baseSlot);
                Vector2Int incoming = Vector2Int.zero;
                if (parentId >= 0 && nodes[parentId].ParentId >= 0)
                {
                    incoming = baseSlot - nodes[nodes[parentId].ParentId].SlotCoord;
                }

                bool expanded = false;
                OrderDirsByBranchiness(dirs, incoming, def.Branchiness, rng);
                foreach (var d in dirs)
                {
                    var next = baseSlot + d;
                    if (occupied.Contains(next))
                    {
                        continue;
                    }

                    occupied.Add(next);
                    int id = nodes.Count;
                    nodes.Add(new GraphNode
                    {
                        Id = id,
                        ParentId = parentId,
                        SlotCoord = next,
                    });
                    frontier.Add(next);
                    expanded = true;
                    break;
                }

                if (!expanded)
                {
                    frontier.RemoveAt(fi);
                }
            }

            return nodes;
        }

        private static int PickFrontierIndex(List<Vector2Int> frontier, float graphRandomness, DungeonRng rng)
        {
            if (frontier.Count <= 1)
            {
                return 0;
            }

            graphRandomness = Mathf.Clamp01(graphRandomness);
            if (graphRandomness <= 0f)
            {
                return frontier.Count - 1;
            }

            if (graphRandomness >= 1f)
            {
                return rng.NextInt(frontier.Count);
            }

            if (rng.NextFloat() > graphRandomness)
            {
                return frontier.Count - 1;
            }

            return rng.NextInt(frontier.Count);
        }

        private static void OrderDirsByBranchiness(Vector2Int[] dirs, Vector2Int incoming, float branchiness, DungeonRng rng)
        {
            ShuffleDirs(dirs, rng);
            if (incoming == Vector2Int.zero)
            {
                return;
            }

            branchiness = Mathf.Clamp01(branchiness);
            Array.Sort(dirs, (a, b) => ScoreDirection(b, incoming, branchiness).CompareTo(ScoreDirection(a, incoming, branchiness)));
        }

        private static float ScoreDirection(Vector2Int dir, Vector2Int incoming, float branchiness)
        {
            if (dir == incoming)
            {
                return 1f - branchiness;
            }

            if (dir == -incoming)
            {
                return 0f;
            }

            return branchiness;
        }

        private static void ShuffleDirs(Vector2Int[] dirs, DungeonRng rng)
        {
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }
        }

        private static int FindNodeAt(List<GraphNode> nodes, Vector2Int slot)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].SlotCoord == slot)
                {
                    return nodes[i].Id;
                }
            }

            return -1;
        }

        private static void AssignRoles(List<GraphNode> nodes, DungeonRng rng)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                n.Role = i == 0 ? EDungeonRoomRole.Start : EDungeonRoomRole.Combat;
                nodes[i] = n;
            }
        }

        private static void AssignContentTypes(List<GraphNode> nodes, DungeonDef def, DungeonRng rng)
        {
            int minCount = Mathf.Max(0, def.MonsterCountMin);
            int maxCount = Mathf.Max(minCount, def.MonsterCountMax);
            float ratio = Mathf.Clamp01(def.MonsterRoomRatio);

            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n.Id == 0 || n.Role == EDungeonRoomRole.Start)
                {
                    n.ContentType = EDungeonRoomContentType.Empty;
                    n.MonsterCount = 0;
                }
                else if (rng.NextFloat() < ratio)
                {
                    n.ContentType = EDungeonRoomContentType.Monster;
                    n.MonsterCount = rng.NextInt(minCount, maxCount + 1);
                }
                else
                {
                    n.ContentType = EDungeonRoomContentType.Empty;
                    n.MonsterCount = 0;
                }

                nodes[i] = n;
            }
        }

        private static void AssignTemplates(List<GraphNode> nodes, DungeonDef def, DungeonRng rng)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                n.Template = PickTemplate(def, n.Role, rng);
                nodes[i] = n;
            }
        }

        private static DungeonRoomExportMeta PickTemplate(DungeonDef def, EDungeonRoomRole role, DungeonRng rng)
        {
            var pool = new List<DungeonRoomExportMeta>();
            var weights = new List<int>();
            foreach (var t in def.RoomTemplates)
            {
                if (t == null)
                {
                    continue;
                }

                if (t.Role != role && t.Role != EDungeonRoomRole.Generic)
                {
                    continue;
                }

                pool.Add(t);
                weights.Add(Mathf.Max(1, t.Weight));
            }

            if (pool.Count == 0)
            {
                foreach (var t in def.RoomTemplates)
                {
                    if (t != null)
                    {
                        return t;
                    }
                }

                return null;
            }

            int total = 0;
            foreach (var w in weights)
            {
                total += w;
            }

            int roll = rng.NextInt(total);
            int acc = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += weights[i];
                if (roll < acc)
                {
                    return pool[i];
                }
            }

            return pool[pool.Count - 1];
        }

        private static void EmbedOnGrid(List<GraphNode> nodes, DungeonDef def, DungeonRng rng)
        {
            // SlotCoord already assigned during graph growth
        }

        private static List<GraphEdge> CollectEdges(List<GraphNode> nodes)
        {
            var edges = new List<GraphEdge>();
            var slotToId = new Dictionary<Vector2Int, int>();
            foreach (var n in nodes)
            {
                slotToId[n.SlotCoord] = n.Id;
            }

            var dirs = new[] { Vector2Int.up, Vector2Int.right };
            foreach (var n in nodes)
            {
                foreach (var d in dirs)
                {
                    var neighbor = n.SlotCoord + d;
                    if (slotToId.TryGetValue(neighbor, out var otherId))
                    {
                        int a = Mathf.Min(n.Id, otherId);
                        int b = Mathf.Max(n.Id, otherId);
                        edges.Add(new GraphEdge { A = a, B = b });
                    }
                }
            }

            return edges;
        }

        private static void BuildWalkableCells(DungeonGenerationResult result, DungeonDef def)
        {
            result.WalkableCells.Clear();
            foreach (var room in result.Rooms)
            {
                if (room.Meta == null)
                {
                    continue;
                }

                var meta = room.Meta;
                for (int y = 0; y < meta.SizeCells.y; y++)
                {
                    for (int x = 0; x < meta.SizeCells.x; x++)
                    {
                        if (!meta.IsWalkableLocal(x, y))
                        {
                            continue;
                        }

                        var worldCell = new Vector3Int(room.GridOriginCells.x + x, room.GridOriginCells.y + y, 0);
                        result.WalkableCells.Add(worldCell);
                    }
                }
            }
        }

        private static void BuildCorridors(DungeonGenerationResult result, List<GraphEdge> edges, DungeonDef def, DungeonRng rng)
        {
            result.CorridorCells.Clear();
            var nodeMap = new Dictionary<int, PlacedRoom>();
            foreach (var room in result.Rooms)
            {
                nodeMap[room.GraphNodeId] = room;
            }

            int configuredWidth = Mathf.Max(1, def.CorridorWidthCells);
            foreach (var edge in edges)
            {
                if (!nodeMap.TryGetValue(edge.A, out var roomA) || !nodeMap.TryGetValue(edge.B, out var roomB))
                {
                    continue;
                }

                if (!TryPickDoorWorldCell(roomA, roomB, out var from, out var doorWidthA)
                    || !TryPickDoorWorldCell(roomB, roomA, out var to, out var doorWidthB))
                {
                    continue;
                }

                int width = Mathf.Max(configuredWidth, doorWidthA, doorWidthB);
                DrawCorridorThick(result, from, to, width);
            }
        }

        private static bool TryPickDoorWorldCell(PlacedRoom room, PlacedRoom other, out Vector3Int worldCell, out int doorWidth)
        {
            worldCell = default;
            doorWidth = 1;
            if (room.Meta == null || room.Meta.DoorSockets == null || room.Meta.DoorSockets.Count == 0)
            {
                return false;
            }

            Vector2 otherCenter = GetRoomCenterWorld(other);
            float best = float.MaxValue;
            bool found = false;

            foreach (var door in room.Meta.DoorSockets)
            {
                int cx = door.LocalCell.x + Mathf.Max(0, door.Width / 2);
                int cy = door.LocalCell.y;
                var wc = new Vector3Int(room.GridOriginCells.x + cx, room.GridOriginCells.y + cy, 0);
                float dist = Vector2.SqrMagnitude(new Vector2(wc.x, wc.y) - otherCenter);
                if (dist < best)
                {
                    best = dist;
                    worldCell = wc;
                    doorWidth = Mathf.Max(1, door.Width);
                    found = true;
                }
            }

            return found;
        }

        private static Vector2 GetRoomCenterWorld(PlacedRoom room)
        {
            if (room.Meta == null)
            {
                return room.GridOriginCells;
            }

            return new Vector2(
                room.GridOriginCells.x + room.Meta.SizeCells.x * 0.5f,
                room.GridOriginCells.y + room.Meta.SizeCells.y * 0.5f);
        }

        private static void DrawCorridorThick(DungeonGenerationResult result, Vector3Int from, Vector3Int to, int width)
        {
            var path = BuildBresenhamPath(from, to);
            if (path.Count == 0)
            {
                return;
            }

            for (int i = 0; i < path.Count; i++)
            {
                var cell = path[i];
                Vector3Int? prev = i > 0 ? path[i - 1] : null;
                Vector3Int? next = i < path.Count - 1 ? path[i + 1] : null;

                bool horizontal;
                if (next.HasValue)
                {
                    horizontal = next.Value.y == cell.y;
                }
                else if (prev.HasValue)
                {
                    horizontal = prev.Value.y == cell.y;
                }
                else
                {
                    horizontal = true;
                }

                StampCorridorCrossSection(result, cell.x, cell.y, width, horizontal);

                if (prev.HasValue && next.HasValue)
                {
                    bool prevHorizontal = prev.Value.y == cell.y;
                    bool nextHorizontal = next.Value.y == cell.y;
                    if (prevHorizontal != nextHorizontal)
                    {
                        StampCorridorCorner(result, cell.x, cell.y, width);
                    }
                }
            }
        }

        private static List<Vector3Int> BuildBresenhamPath(Vector3Int from, Vector3Int to)
        {
            var path = new List<Vector3Int>();
            int x0 = from.x;
            int y0 = from.y;
            int x1 = to.x;
            int y1 = to.y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                path.Add(new Vector3Int(x0, y0, 0));
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = err * 2;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return path;
        }

        private static void StampCorridorCrossSection(DungeonGenerationResult result, int cx, int cy, int width, bool horizontal)
        {
            int low = -(width - 1) / 2;
            int high = width / 2;
            for (int t = low; t <= high; t++)
            {
                int x = horizontal ? cx : cx + t;
                int y = horizontal ? cy + t : cy;
                AddCorridorCell(result, x, y);
            }
        }

        private static void StampCorridorCorner(DungeonGenerationResult result, int cx, int cy, int width)
        {
            int low = -(width - 1) / 2;
            int high = width / 2;
            for (int dy = low; dy <= high; dy++)
            {
                for (int dx = low; dx <= high; dx++)
                {
                    AddCorridorCell(result, cx + dx, cy + dy);
                }
            }
        }

        private static void AddCorridorCell(DungeonGenerationResult result, int x, int y)
        {
            var cell = new Vector3Int(x, y, 0);
            if (!result.WalkableCells.Contains(cell))
            {
                result.WalkableCells.Add(cell);
                result.CorridorCells.Add(cell);
            }
        }
    }

    public static class DungeonRuntimeMapBuilder
    {
        public static MapExportDatabase Build(DungeonGenerationResult result, DungeonDef def)
        {
            var db = ScriptableObject.CreateInstance<MapExportDatabase>();
            db.AreaId = result.DungeonId;
            db.NamedPoints = new List<NamedPoint>();
            db.EntityRefreshInfo = new List<DynamicEntityRefreshInfo>();
            db.Buckets = new List<MapExportDatabase.ChunkExportItem>();
            db.NamedPaths = new List<NamedPath>();
            db.PortalNetworks = new List<PortalNetworkExport>();

            AddBornPoint(result, def, db);
            AddDestroyObjSlots(result, def, db);
            AddMonsterSpawns(result, def, db);
            db.BuildRuntimeMap();
            return db;
        }

        private static void AddBornPoint(DungeonGenerationResult result, DungeonDef def, MapExportDatabase db)
        {
            PlacedRoom? startRoom = null;
            foreach (var room in result.Rooms)
            {
                if (room.Role == EDungeonRoomRole.Start)
                {
                    startRoom = room;
                    break;
                }
            }

            if (startRoom == null || startRoom.Value.Meta == null)
            {
                Debug.LogError("DungeonRuntimeMapBuilder: start room missing");
                return;
            }

            var bornLocal = startRoom.Value.Meta.GetBornLocalCell();
            var world = new Vector2(
                startRoom.Value.GridOriginCells.x + bornLocal.x + 0.5f,
                startRoom.Value.GridOriginCells.y + bornLocal.y + 0.5f);

            db.NamedPoints.Add(new NamedPoint
            {
                Name = "BornPos",
                PointType = ENamedPointType.BornPos,
                Position = world,
                Rotation = Quaternion.identity,
                Scale = Vector3.one,
            });
        }

        private static void AddDestroyObjSlots(DungeonGenerationResult result, DungeonDef def, MapExportDatabase db)
        {
            var sortedRooms = new List<PlacedRoom>(result.Rooms);
            sortedRooms.Sort((a, b) => a.GraphNodeId.CompareTo(b.GraphNodeId));

            foreach (var room in sortedRooms)
            {
                if (room.Meta == null || room.Meta.EntitySlots == null)
                {
                    continue;
                }

                var sortedSlots = new List<DungeonEntitySlotExport>(room.Meta.EntitySlots);
                sortedSlots.Sort((a, b) => string.CompareOrdinal(a.SlotId, b.SlotId));
                var spawnPolicy = room.GraphNodeId == 0
                    ? EDungeonSpawnPolicy.Immediate
                    : EDungeonSpawnPolicy.OnRoomEnter;

                foreach (var slot in sortedSlots)
                {
                    int staticId = MakeStaticId(result.Seed, room.GraphNodeId, slot.SlotId);
                    var pos = new Vector2(
                        room.GridOriginCells.x + slot.LocalCell.x + 0.5f,
                        room.GridOriginCells.y + slot.LocalCell.y + 0.5f);

                    var initInfo = new EntityInitInfo4DestroyObj
                    {
                        CfgId = def.DestroyObjCfgId,
                        Position = pos,
                        FaceDir = slot.FaceDir.sqrMagnitude > 0.01f ? slot.FaceDir.normalized : Vector2.down,
                    };

                    db.EntityRefreshInfo.Add(new DynamicEntityRefreshInfo
                    {
                        StaticId = staticId,
                        UniqName = $"dungeon_{result.DungeonId}_{room.GraphNodeId}_{slot.SlotId}",
                        WillRespawn = false,
                        DungeonNodeId = room.GraphNodeId,
                        SpawnPolicy = spawnPolicy,
                        InitInfo = initInfo,
                    });
                }
            }
        }

        private static void AddMonsterSpawns(DungeonGenerationResult result, DungeonDef def, MapExportDatabase db)
        {
            if (string.IsNullOrEmpty(def.DefaultMonsterCfgId))
            {
                return;
            }

            var sortedRooms = new List<PlacedRoom>(result.Rooms);
            sortedRooms.Sort((a, b) => a.GraphNodeId.CompareTo(b.GraphNodeId));

            foreach (var room in sortedRooms)
            {
                if (room.ContentType != EDungeonRoomContentType.Monster || room.MonsterCount <= 0)
                {
                    continue;
                }

                var rng = new DungeonRng(DungeonRng.DeriveSeed(result.Seed, room.GraphNodeId, 9001));
                var pool = DungeonRoomSpawnUtil.CollectInteriorSpawnCells(room);
                var picked = DungeonRoomSpawnUtil.PickRandomCells(pool, room.MonsterCount, rng);
                for (int i = 0; i < picked.Count; i++)
                {
                    string slotId = $"mob_{i}";
                    int staticId = MakeStaticId(result.Seed, room.GraphNodeId, slotId);
                    var pos = new Vector2(picked[i].x + 0.5f, picked[i].y + 0.5f);
                    var initInfo = new EntityInitInfo4Npc
                    {
                        CfgId = def.DefaultMonsterCfgId,
                        Position = pos,
                        FaceDir = Vector2.down,
                        IsPeace = false,
                        EnmityConfId = "default_monster",
                    };

                    db.EntityRefreshInfo.Add(new DynamicEntityRefreshInfo
                    {
                        StaticId = staticId,
                        UniqName = $"dungeon_{result.DungeonId}_{room.GraphNodeId}_{slotId}",
                        WillRespawn = false,
                        DungeonNodeId = room.GraphNodeId,
                        SpawnPolicy = EDungeonSpawnPolicy.OnRoomEnter,
                        InitInfo = initInfo,
                    });
                }
            }
        }

        private static int MakeStaticId(int seed, int graphNodeId, string slotId)
        {
            unchecked
            {
                int h = seed;
                h = h * 31 + graphNodeId;
                h = h * 31 + (slotId?.GetHashCode() ?? 0);
                h &= 0x7FFFFFFF;
                if (h < 1000)
                {
                    h += 1000;
                }

                return h;
            }
        }
    }
}
