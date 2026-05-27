namespace My.Dungeon
{
    public static class DungeonSession
    {
        public static int PendingSeed { get; private set; }
        public static bool HasPendingSeed { get; private set; }

        private static DungeonGenerationResult _lastResult;

        public static void SetPendingSeed(int seed)
        {
            PendingSeed = seed;
            HasPendingSeed = true;
        }

        public static int ConsumeSeed(string overlayId, int fallbackSeed)
        {
            if (HasPendingSeed)
            {
                HasPendingSeed = false;
                return PendingSeed;
            }

            return fallbackSeed;
        }

        public static void SetLastResult(DungeonGenerationResult result)
        {
            _lastResult = result;
        }

        public static DungeonGenerationResult GetLastResult()
        {
            return _lastResult;
        }

        public static void ClearLastResult()
        {
            _lastResult = null;
        }
    }
}
