using System.Collections.Generic;
using UnityEngine;

namespace My.Dungeon
{
    [CreateAssetMenu(fileName = "DungeonDef", menuName = "Dungeon/Dungeon Def")]
    public class DungeonDef : ScriptableObject
    {
        public string DungeonId = "test_cave";
        public int MinRooms = 5;
        public int MaxRooms = 8;
        public int SlotStrideCells = 24;

        [Range(0f, 1f)]
        public float GraphRandomness = 0.35f;

        [Range(0f, 1f)]
        public float Branchiness = 0.5f;

        public int CorridorWidthCells = 2;
        public string DestroyObjCfgId = "obj_01";

        [Range(0f, 1f)]
        public float MonsterRoomRatio = 0.7f;

        public int MonsterCountMin = 2;
        public int MonsterCountMax = 4;
        public string DefaultMonsterCfgId = "slime_red";

        public DungeonFloorTileset FloorTileset;
        public List<DungeonRoomExportMeta> RoomTemplates = new();
    }
}
