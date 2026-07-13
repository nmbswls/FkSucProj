using System.Collections.Generic;
using cfg.demo;

namespace My.Config
{
    public static class ItemTagCatalog
    {
        public static bool HasTag(ItemData def, EItemTag tag)
        {
            if (def?.Tags == null || tag == EItemTag.None)
            {
                return false;
            }

            return def.Tags.Contains(tag);
        }

        public static bool HasTag(string itemId, EItemTag tag)
        {
            return HasTag(ItemCatalog.GetItemDef(itemId), tag);
        }

        public static bool HasAnyTag(ItemData def, params EItemTag[] tags)
        {
            if (def == null || tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Length; i++)
            {
                if (HasTag(def, tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAnyTag(ItemData def, IEnumerable<EItemTag> tags)
        {
            if (def == null || tags == null)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (HasTag(def, tag))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RequiresInstance(ItemData def)
        {
            if (def?.Tags == null)
            {
                return false;
            }

            for (int i = 0; i < def.Tags.Count; i++)
            {
                if (RequiresInstance(def.Tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RequiresInstance(EItemTag tag)
        {
            return GetTagDef(tag)?.RequiresInstance ?? false;
        }

        public static IReadOnlyList<string> GetVisibleTagDisplayNames(ItemData def)
        {
            var result = new List<string>();
            if (def?.Tags == null)
            {
                return result;
            }

            var added = new HashSet<EItemTag>();
            for (int i = 0; i < def.Tags.Count; i++)
            {
                var tag = def.Tags[i];
                if (tag == EItemTag.None || !added.Add(tag) || IsHidden(tag))
                {
                    continue;
                }

                result.Add(GetDisplayName(tag));
            }

            return result;
        }

        public static bool IsHidden(EItemTag tag)
        {
            return GetTagDef(tag)?.Hidden ?? false;
        }

        public static string GetDisplayName(EItemTag tag)
        {
            var def = GetTagDef(tag);
            return !string.IsNullOrEmpty(def?.DisplayName)
                ? def.DisplayName
                : tag.ToString();
        }

        static ItemTagInfo GetTagDef(EItemTag tag)
        {
            if (tag == EItemTag.None)
            {
                return null;
            }

            return CfgMgr.Cfgs?.TbItemTagInfo?.GetOrDefault(tag);
        }
    }
}
