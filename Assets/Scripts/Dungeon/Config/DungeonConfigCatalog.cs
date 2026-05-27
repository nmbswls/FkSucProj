using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
    public static class DungeonConfigCatalog
    {
        private static readonly Dictionary<string, DungeonDef> _byId = new();
        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _byId.Clear();

            var defs = Resources.LoadAll<DungeonDef>("Config/Dungeon");
            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.DungeonId))
                {
                    continue;
                }

                _byId[def.DungeonId] = def;
            }
        }

        public static DungeonDef GetOrDefault(string dungeonId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(dungeonId))
            {
                return null;
            }

            _byId.TryGetValue(dungeonId, out var def);
            if (def == null && dungeonId == "test_cave")
            {
                def = DungeonDefaultContent.GetOrCreateTestCave();
                _byId[dungeonId] = def;
            }

            return def;
        }
    }

    // 未配置 asset 时的 test_cave 内置回退
    internal static class DungeonDefaultContent
    {
        private static DungeonDef _cached;

        public static DungeonDef GetOrCreateTestCave()
        {
            if (_cached != null)
            {
                return _cached;
            }

            _cached = ScriptableObject.CreateInstance<DungeonDef>();
            _cached.DungeonId = "test_cave";
            _cached.MinRooms = 5;
            _cached.MaxRooms = 7;
            _cached.SlotStrideCells = 24;
            _cached.GraphRandomness = 0.35f;
            _cached.Branchiness = 0.5f;
            _cached.CorridorWidthCells = 2;
            _cached.DestroyObjCfgId = "obj_01";
            _cached.MonsterRoomRatio = 0.7f;
            _cached.MonsterCountMin = 2;
            _cached.MonsterCountMax = 4;
            _cached.DefaultMonsterCfgId = "slime_red";
            _cached.FloorTileset = CreateDefaultTileset();
            _cached.RoomTemplates = new List<DungeonRoomExportMeta>
            {
                BuildRoom("test_cave_start_12x10", EDungeonRoomRole.Start, new Vector2Int(12, 10), 1,
                    new[] { Door(EDungeonCardinalDir.South, 5, 0, 2) },
                    new[] { Slot("born", 6, 5) }),
                BuildRoom("test_cave_combat_16x14", EDungeonRoomRole.Combat, new Vector2Int(16, 14), 2,
                    new[] { Door(EDungeonCardinalDir.North, 7, 13, 2), Door(EDungeonCardinalDir.South, 7, 0, 2) },
                    new[] { Slot("obj_a", 8, 7) }),
                BuildRoom("test_cave_combat_20x12", EDungeonRoomRole.Combat, new Vector2Int(20, 12), 2,
                    new[] { Door(EDungeonCardinalDir.West, 0, 5, 2), Door(EDungeonCardinalDir.East, 19, 5, 2) },
                    new[] { Slot("obj_b", 10, 6) }),
                BuildRoom("test_cave_combat_14x16", EDungeonRoomRole.Combat, new Vector2Int(14, 16), 1,
                    new[] { Door(EDungeonCardinalDir.North, 6, 15, 2), Door(EDungeonCardinalDir.South, 6, 0, 2) },
                    new[] { Slot("obj_c", 7, 8) }),
            };

            return _cached;
        }

        private static DungeonFloorTileset CreateDefaultTileset()
        {
            var loaded = Resources.Load<DungeonFloorTileset>("Config/Dungeon/test_cave_floor");
            if (loaded != null)
            {
                return loaded;
            }

            var tileset = ScriptableObject.CreateInstance<DungeonFloorTileset>();
            tileset.TilesetId = "test_cave_floor";
            return tileset;
        }

        private static DungeonRoomExportMeta BuildRoom(
            string id,
            EDungeonRoomRole role,
            Vector2Int size,
            int weight,
            DungeonDoorSocketExport[] doors,
            DungeonEntitySlotExport[] slots)
        {
            var meta = ScriptableObject.CreateInstance<DungeonRoomExportMeta>();
            meta.TemplateId = id;
            meta.Role = role;
            meta.Weight = weight;
            meta.SizeCells = size;
            meta.DoorSockets = new List<DungeonDoorSocketExport>(doors);
            meta.EntitySlots = new List<DungeonEntitySlotExport>(slots);
            FillMask(meta);
            return meta;
        }

        private static void FillMask(DungeonRoomExportMeta meta)
        {
            meta.EnsureMaskSize();
            for (int y = 0; y < meta.SizeCells.y; y++)
            {
                for (int x = 0; x < meta.SizeCells.x; x++)
                {
                    bool border = x == 0 || y == 0 || x == meta.SizeCells.x - 1 || y == meta.SizeCells.y - 1;
                    meta.WalkableMask[y * meta.SizeCells.x + x] = (byte)(border ? 0 : 1);
                }
            }

            foreach (var door in meta.DoorSockets)
            {
                for (int i = 0; i < door.Width; i++)
                {
                    int cx = door.LocalCell.x;
                    int cy = door.LocalCell.y;
                    if (door.Direction == EDungeonCardinalDir.South || door.Direction == EDungeonCardinalDir.North)
                    {
                        cx = door.LocalCell.x + i;
                    }
                    else
                    {
                        cy = door.LocalCell.y + i;
                    }

                    if (cx >= 0 && cy >= 0 && cx < meta.SizeCells.x && cy < meta.SizeCells.y)
                    {
                        meta.WalkableMask[cy * meta.SizeCells.x + cx] = 1;
                    }
                }
            }
        }

        private static DungeonDoorSocketExport Door(EDungeonCardinalDir dir, int x, int y, int width)
        {
            return new DungeonDoorSocketExport { Direction = dir, LocalCell = new Vector2Int(x, y), Width = width };
        }

        private static DungeonEntitySlotExport Slot(string id, int x, int y)
        {
            return new DungeonEntitySlotExport { SlotId = id, LocalCell = new Vector2Int(x, y), FaceDir = Vector2.down };
        }
    }
}
