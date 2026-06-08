namespace My
{
    public static class LiquidFieldConstants
    {
        public const float GridSize = 0.2f;
        public const int ChunkGridSize = 32;
        public const int SubCellsPerGrid = 5;
        public const int ChunkTexSize = 160;
        public const int ChunkHaloTexels = 1;
        public const int ChunkTexSizeWithHalo = 162;

        public const float SubCellWorldSize = GridSize / SubCellsPerGrid;
        public const float ChunkWorldSize = ChunkGridSize * GridSize;

        public const byte LiquidIntensityThreshold = 50;
        public const byte PeakIntensity = 255;
        public const float FadeSpeedPerSecond = 180f;
        public const float SourceFadeDelaySeconds = 2f;

        public static int FloorDiv(int value, int divisor)
        {
            if (divisor <= 0)
            {
                return 0;
            }

            if (value >= 0)
            {
                return value / divisor;
            }

            return (value - divisor + 1) / divisor;
        }
    }
}
