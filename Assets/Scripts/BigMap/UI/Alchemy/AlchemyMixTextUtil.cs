using System.Collections.Generic;
using System.Text;
using cfg.demo;
using My.Config;

namespace My.UI.Alchemy
{
    public static class AlchemyMixTextUtil
    {
        public static string BuildVirtueSummary(IReadOnlyDictionary<int, int> virtues)
        {
            if (virtues == null || virtues.Count == 0)
            {
                return "功效：-";
            }

            var sb = new StringBuilder("功效：");
            bool first = true;
            foreach (var pair in virtues)
            {
                if (pair.Key <= 0 || pair.Value == 0)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append("  ");
                }

                first = false;
                var def = AlchemyCatalog.GetVirtueDef(pair.Key);
                sb.Append(def?.DisplayName ?? pair.Key.ToString());
                sb.Append(' ');
                sb.Append(pair.Value);
            }

            return first ? "功效：-" : sb.ToString();
        }

        public static string BuildAspectSummary(IReadOnlyDictionary<int, int> aspects)
        {
            if (aspects == null || aspects.Count == 0)
            {
                return "属性：-";
            }

            var sb = new StringBuilder("属性：");
            bool first = true;
            foreach (var pair in aspects)
            {
                if (pair.Key <= 0 || pair.Value == 0)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append("  ");
                }

                first = false;
                var def = AlchemyCatalog.GetAspectDef(pair.Key);
                sb.Append(def?.DisplayName ?? pair.Key.ToString());
                sb.Append(' ');
                sb.Append(pair.Value);
            }

            return first ? "属性：-" : sb.ToString();
        }

        public static string BuildVirtueSummaryFromValues(IReadOnlyList<AlchemyVirtueValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return "功效：-";
            }

            var sb = new StringBuilder("功效：");
            bool first = true;
            for (int i = 0; i < values.Count; i++)
            {
                var entry = values[i];
                if (entry == null || entry.VirtueId <= 0 || entry.Value == 0)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append("  ");
                }

                first = false;
                var def = AlchemyCatalog.GetVirtueDef(entry.VirtueId);
                sb.Append(def?.DisplayName ?? entry.VirtueId.ToString());
                sb.Append(' ');
                sb.Append(entry.Value);
            }

            return first ? "功效：-" : sb.ToString();
        }

        public static string BuildAspectSummaryFromValues(IReadOnlyList<AlchemyAspectValue> values)
        {
            if (values == null || values.Count == 0)
            {
                return "属性：-";
            }

            var sb = new StringBuilder("属性：");
            bool first = true;
            for (int i = 0; i < values.Count; i++)
            {
                var entry = values[i];
                if (entry == null || entry.AspectId <= 0 || entry.Value == 0)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append("  ");
                }

                first = false;
                var def = AlchemyCatalog.GetAspectDef(entry.AspectId);
                sb.Append(def?.DisplayName ?? entry.AspectId.ToString());
                sb.Append(' ');
                sb.Append(entry.Value);
            }

            return first ? "属性：-" : sb.ToString();
        }
    }
}
