using cfg.demo;

namespace My.Player
{
    // Statistic 稳定 key：{stat_type}:{arg0}:{arg1}
    // 配置里空参用 "-" 占位（Luban sep 会吞掉空字段）
    public static class PlayerStatisticKeys
    {
        public const string EmptyArgToken = "-";

        public static string NormalizeArg(string arg)
        {
            if (string.IsNullOrEmpty(arg) || arg == EmptyArgToken)
            {
                return string.Empty;
            }

            return arg;
        }

        public static string MakeKey(EStatType type, string arg0 = null, string arg1 = null)
        {
            return ((int)type) + ":" + NormalizeArg(arg0) + ":" + NormalizeArg(arg1);
        }
    }
}
