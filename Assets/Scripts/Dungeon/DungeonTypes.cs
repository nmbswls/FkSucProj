using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
    public enum EDungeonRoomRole
    {
        Start,
        Combat,
        Generic,
    }

    public enum EDungeonRoomContentType
    {
        Empty,
        Monster,
    }

    public enum EDungeonCardinalDir
    {
        South,
        North,
        West,
        East,
    }

    [Serializable]
    public struct DungeonDoorSocketExport
    {
        public EDungeonCardinalDir Direction;
        public Vector2Int LocalCell;
        public int Width;
    }

    [Serializable]
    public struct DungeonEntitySlotExport
    {
        public string SlotId;
        public Vector2Int LocalCell;
        public Vector2 FaceDir;
    }

    [Serializable]
    public struct PlacedRoom
    {
        public int GraphNodeId;
        public string TemplateId;
        public EDungeonRoomRole Role;
        public EDungeonRoomContentType ContentType;
        public int MonsterCount;
        public Vector2Int GridOriginCells;
        public Vector2Int SlotCoord;
        public DungeonRoomExportMeta Meta;
    }

    public class DungeonGenerationResult
    {
        public const int GeneratorVersion = 2;

        public int Seed;
        public string DungeonId = string.Empty;
        public List<PlacedRoom> Rooms = new();
        public List<Vector3Int> CorridorCells = new();
        public HashSet<Vector3Int> WalkableCells = new();
        public My.MapExport.MapExportDatabase RuntimeMapData;
    }

    public static class DungeonOverlayRegistry
    {
        public const string TestCaveOverlayId = "dungeon_test_cave";

        private static readonly Dictionary<string, string> OverlayToDungeon = new(StringComparer.OrdinalIgnoreCase)
        {
            { TestCaveOverlayId, "test_cave" },
        };

        public static bool TryGetDungeonId(string overlayId, out string dungeonId)
        {
            if (string.IsNullOrEmpty(overlayId))
            {
                dungeonId = string.Empty;
                return false;
            }

            return OverlayToDungeon.TryGetValue(overlayId, out dungeonId);
        }
    }
}
