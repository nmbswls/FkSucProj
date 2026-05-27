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

    public struct DungeonRng
    {
        private ulong _state;

        public DungeonRng(int seed)
        {
            _state = (ulong)(uint)seed;
            if (_state == 0)
            {
                _state = 0x9E3779B97F4A7C15UL;
            }
        }

        public static int DeriveSeed(int rootSeed, params int[] parts)
        {
            unchecked
            {
                int h = rootSeed;
                for (int i = 0; i < parts.Length; i++)
                {
                    h = h * 31 + parts[i];
                    h ^= (h << 13);
                    h ^= (h >> 17);
                    h ^= (h << 5);
                }

                return h;
            }
        }

        public static int DeriveSeed(int rootSeed, string salt, params int[] parts)
        {
            int saltHash = string.IsNullOrEmpty(salt) ? 0 : salt.GetHashCode();
            if (parts == null || parts.Length == 0)
            {
                return DeriveSeed(rootSeed, saltHash);
            }

            var merged = new int[parts.Length + 1];
            merged[0] = saltHash;
            for (int i = 0; i < parts.Length; i++)
            {
                merged[i + 1] = parts[i];
            }

            return DeriveSeed(rootSeed, merged);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            var range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public int NextInt(int maxExclusive)
        {
            return NextInt(0, maxExclusive);
        }

        public float NextFloat()
        {
            return NextUInt() / (float)uint.MaxValue;
        }

        private uint NextUInt()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return (uint)((x * 2685821657736338717UL) >> 32);
        }
    }
}
