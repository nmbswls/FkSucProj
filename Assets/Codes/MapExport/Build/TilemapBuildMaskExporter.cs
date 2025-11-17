

using UnityEngine.Tilemaps;
using UnityEngine;

namespace My 
{

    [System.Serializable]
    public struct BuildGrid
    {
        public int width;
        public int height;
        public int originX; // 对齐到 tilemap.cellBounds.xMin
        public int originY;

        // 位图：每 bit 表示一个格是否可建造（1=可建，0=不可）
        public byte[] bits;

        public bool IsBuildable(int x, int y)
        {
            int ix = y * width + x;
            int bi = ix >> 3;
            int mask = 1 << (ix & 7);
            return (bits[bi] & mask) != 0;
        }
        public void SetBuildable(int x, int y, bool value)
        {
            int ix = y * width + x;
            int bi = ix >> 3;
            int mask = 1 << (ix & 7);
            if (value) bits[bi] |= (byte)mask;
            else bits[bi] &= (byte)~mask;
        }
    }

    [System.Serializable]
    public struct OccupancyGrid
    {
        public int width;
        public int height;
        public int originX;
        public int originY;
        public byte[] bits; // 1=占用, 0=空

        public bool IsOccupied(int x, int y)
        {
            int ix = y * width + x;
            int bi = ix >> 3;
            int mask = 1 << (ix & 7);
            return (bits[bi] & mask) != 0;
        }
        public void SetOccupied(int x, int y, bool value)
        {
            int ix = y * width + x;
            int bi = ix >> 3;
            int mask = 1 << (ix & 7);
            if (value) bits[bi] |= (byte)mask;
            else bits[bi] &= (byte)~mask;
        }
    }


    public static class TilemapBuildMaskExporter
    {
        public struct ExportResult
        {
            public BuildGrid buildGrid;
            public OccupancyGrid occupancyGrid; // 可选，如果没传占用层则为空
        }

        // 传入：地面层、禁建层、水层、占用层（可选）
        public static ExportResult Export(
            Tilemap ground,
            Tilemap blocked = null,
            Tilemap water = null,
            Tilemap occupiedLayer = null // 如果你用一个Tilemap来表示当前占用
        )
        {
            // 统一边界：以 ground 的 cellBounds 为基准（可根据需要改为并集）
            BoundsInt bounds = ground.cellBounds;
            int W = bounds.size.x;
            int H = bounds.size.y;

            var build = new BuildGrid
            {
                width = W,
                height = H,
                originX = bounds.xMin,
                originY = bounds.yMin,
                bits = new byte[(W * H + 7) / 8]
            };

            var occ = new OccupancyGrid
            {
                width = W,
                height = H,
                originX = bounds.xMin,
                originY = bounds.yMin,
                bits = new byte[(W * H + 7) / 8]
            };

            // 扫描
            for (int ly = 0; ly < H; ly++)
            {
                for (int lx = 0; lx < W; lx++)
                {
                    var cell = new Vector3Int(bounds.xMin + lx, bounds.yMin + ly, 0);

                    bool hasGround = ground != null && ground.GetTile(cell) != null;
                    bool isBlocked = blocked != null && blocked.GetTile(cell) != null;
                    bool isWater = water != null && water.GetTile(cell) != null;
                    bool isOccupiedTile = occupiedLayer != null && occupiedLayer.GetTile(cell) != null;

                    // 可建造的逻辑：有地面，且不在禁建/水域/占用上
                    bool buildable = hasGround && !isBlocked && !isWater && !isOccupiedTile;
                    build.SetBuildable(lx, ly, buildable);

                    // 占用位图：如果你确实用一个层表示已占用
                    occ.SetOccupied(lx, ly, isOccupiedTile);
                }
            }

            return new ExportResult { buildGrid = build, occupancyGrid = occ };
        }

        // 如果没有单独的占用Tilemap，但你想在运行时根据对象位置标记占用：
        // 可用此方法将世界坐标列表映射为格子占用位图
        public static void MarkOccupancyFromWorldPositions(
            Grid grid,
            Vector3[] worldPositions,
            ref OccupancyGrid occ)
        {
            for (int i = 0; i < worldPositions.Length; i++)
            {
                Vector3Int cell = grid.WorldToCell(worldPositions[i]);
                int lx = cell.x - occ.originX;
                int ly = cell.y - occ.originY;
                if (lx >= 0 && ly >= 0 && lx < occ.width && ly < occ.height)
                {
                    occ.SetOccupied(lx, ly, true);
                }
            }
        }

        // 简易序列化为字节流（可用于存档/网络）
        public static byte[] SerializeBuildGrid(BuildGrid g)
        {
            // 格式：width,height,originX,originY, bitsLen, bits
            using (var ms = new System.IO.MemoryStream())
            using (var bw = new System.IO.BinaryWriter(ms))
            {
                bw.Write(g.width);
                bw.Write(g.height);
                bw.Write(g.originX);
                bw.Write(g.originY);
                bw.Write(g.bits.Length);
                bw.Write(g.bits);
                return ms.ToArray();
            }
        }

        public static BuildGrid DeserializeBuildGrid(byte[] data)
        {
            using (var ms = new System.IO.MemoryStream(data))
            using (var br = new System.IO.BinaryReader(ms))
            {
                BuildGrid g = new BuildGrid();
                g.width = br.ReadInt32();
                g.height = br.ReadInt32();
                g.originX = br.ReadInt32();
                g.originY = br.ReadInt32();
                int len = br.ReadInt32();
                g.bits = br.ReadBytes(len);
                return g;
            }
        }
    }
}