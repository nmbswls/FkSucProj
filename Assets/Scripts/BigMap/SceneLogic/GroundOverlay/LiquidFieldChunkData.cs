using System;

namespace My
{
    public class LiquidFieldChunkData
    {
        public const int CoreSize = LiquidFieldConstants.ChunkTexSize;
        public const int CoreCellCount = CoreSize * CoreSize;

        public readonly byte[] Intensities = new byte[CoreCellCount];
        public readonly byte[] Types = new byte[CoreCellCount];
        public readonly float[] ExpireTimes = new float[CoreCellCount];

        public bool HasVisibleContent()
        {
            for (int i = 0; i < CoreCellCount; i++)
            {
                if (Intensities[i] > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            Array.Clear(Intensities, 0, CoreCellCount);
            Array.Clear(Types, 0, CoreCellCount);
            Array.Clear(ExpireTimes, 0, CoreCellCount);
        }

        public static int ToIndex(int tx, int ty)
        {
            return ty * CoreSize + tx;
        }
    }
}
