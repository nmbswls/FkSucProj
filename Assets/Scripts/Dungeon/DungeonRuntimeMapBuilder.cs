using System.Collections.Generic;
using My.MapExport;
using UnityEngine;

namespace My.Dungeon
{
    public static class DungeonRuntimeMapBuilder
    {
        private static int _nextStaticId = 1000;

        public static MapExportDatabase Build(DungeonGenerationResult result, DungeonDef def)
        {
            _nextStaticId = 1000;
            var db = ScriptableObject.CreateInstance<MapExportDatabase>();
            db.AreaId = result.DungeonId;
            db.NamedPoints = new List<NamedPoint>();
            db.EntityRefreshInfo = new List<DynamicEntityRefreshInfo>();
            db.Buckets = new List<MapExportDatabase.ChunkExportItem>();
            db.NamedPaths = new List<NamedPath>();
            db.PortalNetworks = new List<PortalNetworkExport>();

            AddBornPoint(result, def, db);
            AddDestroyObjSlots(result, def, db);
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
                        BindRoomId = room.GraphNodeId.ToString(),
                    };

                    db.EntityRefreshInfo.Add(new DynamicEntityRefreshInfo
                    {
                        StaticId = staticId,
                        UniqName = $"dungeon_{result.DungeonId}_{room.GraphNodeId}_{slot.SlotId}",
                        WillRespawn = false,
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
