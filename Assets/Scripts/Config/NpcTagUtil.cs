using System;
using System.Collections.Generic;
using cfg.demo;

namespace My.Config
{
    public static class NpcTagUtil
    {
        public static bool HasTag(UnitNpc npc, string tag)
        {
            if (npc == null || string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            tag = tag.Trim();
            if (npc.NpcTags != null)
            {
                for (int i = 0; i < npc.NpcTags.Count; i++)
                {
                    if (string.Equals(npc.NpcTags[i]?.Trim(), tag, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return string.Equals(npc.MindTag?.Trim(), tag, StringComparison.Ordinal);
        }

        public static HashSet<string> BuildTagSet(UnitNpc npc)
        {
            var tags = new HashSet<string>(StringComparer.Ordinal);
            if (npc == null)
            {
                return tags;
            }

            AddTags(tags, npc.NpcTags);
            if (!string.IsNullOrWhiteSpace(npc.MindTag))
            {
                tags.Add(npc.MindTag.Trim());
            }

            return tags;
        }

        private static void AddTags(HashSet<string> tags, List<string> source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                {
                    tags.Add(source[i].Trim());
                }
            }
        }
    }
}
