namespace My.Dungeon
{
    public static class DungeonSession
    {
        public static int PendingSeed { get; private set; }
        public static bool HasPendingSeed { get; private set; }

        private static string _lastOverlayId = string.Empty;
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

        public static void SetLastResult(string overlayId, DungeonGenerationResult result)
        {
            _lastOverlayId = overlayId ?? string.Empty;
            _lastResult = result;
        }

        public static DungeonGenerationResult GetLastResult(string overlayId = null)
        {
            if (!string.IsNullOrEmpty(overlayId) &&
                !string.Equals(_lastOverlayId, overlayId, System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return _lastResult;
        }

        public static void ClearLastResult()
        {
            _lastOverlayId = string.Empty;
            _lastResult = null;
        }
    }
}
