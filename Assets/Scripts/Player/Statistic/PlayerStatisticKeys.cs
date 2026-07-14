using cfg.demo;

namespace My.Player
{
    // Statistic 稳定 key：{stat_type}:{arg0}:{arg1}
    public static class PlayerStatisticKeys
    {
        public static string MakeKey(EStatType type, string arg0 = null, string arg1 = null)
        {
            return ((int)type) + ":" + (arg0 ?? string.Empty) + ":" + (arg1 ?? string.Empty);
        }
    }
}
