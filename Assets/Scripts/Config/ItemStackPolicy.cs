using System;
using System.Collections.Generic;
using cfg.demo;
using My;

namespace My.Config
{
    // 容器堆叠：专属 item 覆盖 + 按 tag 比率；配置来自 TbContainerItemStackOverride / TbContainerStackTagRule
    public static class ItemStackPolicy
    {
        struct TagRuleRow
        {
            public int Priority;
            public EItemTag[] Tags;
            public float Ratio;
        }

        static Dictionary<(EContainerType Container, string ItemId), int> _overrideAbsMax =
            new Dictionary<(EContainerType, string), int>();

        static Dictionary<EContainerType, List<TagRuleRow>> _tagRulesByContainer =
            new Dictionary<EContainerType, List<TagRuleRow>>();

        public static void RebuildCaches()
        {
            _overrideAbsMax = new Dictionary<(EContainerType, string), int>();
            _tagRulesByContainer = new Dictionary<EContainerType, List<TagRuleRow>>();

            if (CfgMgr.Cfgs == null)
            {
                return;
            }

            foreach (var row in CfgMgr.Cfgs.TbContainerItemStackOverride.DataList)
            {
                var ct = (EContainerType)(int)row.ContainerType;
                _overrideAbsMax[(ct, row.ItemId)] = row.MaxStack;
            }

            foreach (var row in CfgMgr.Cfgs.TbContainerStackTagRule.DataList)
            {
                var ct = (EContainerType)(int)row.ContainerType;
                if (!_tagRulesByContainer.TryGetValue(ct, out var list))
                {
                    list = new List<TagRuleRow>();
                    _tagRulesByContainer[ct] = list;
                }

                EItemTag[] tags = Array.Empty<EItemTag>();
                if (row.Tags != null && row.Tags.Count > 0)
                {
                    var t = new List<EItemTag>();
                    foreach (var tag in row.Tags)
                    {
                        if (tag != EItemTag.None)
                        {
                            t.Add(tag);
                        }
                    }

                    tags = t.Count > 0 ? t.ToArray() : Array.Empty<EItemTag>();
                }

                list.Add(new TagRuleRow
                {
                    Priority = row.Priority,
                    Tags = tags,
                    Ratio = row.StackRatio <= 0f ? 1f : row.StackRatio,
                });
            }

            foreach (var kv in _tagRulesByContainer)
            {
                kv.Value.Sort((a, b) =>
                {
                    int c = b.Priority.CompareTo(a.Priority);
                    if (c != 0)
                    {
                        return c;
                    }

                    int la = a.Tags?.Length ?? 0;
                    int lb = b.Tags?.Length ?? 0;
                    return lb.CompareTo(la);
                });
            }
        }

        public static bool TryGetAbsoluteOverride(EContainerType container, string itemId, out int maxStack)
        {
            return _overrideAbsMax.TryGetValue((container, itemId), out maxStack);
        }

        public static float ResolveStackRatio(EContainerType container, ItemData def)
        {
            if (!_tagRulesByContainer.TryGetValue(container, out var rules) || rules == null || rules.Count == 0)
            {
                return 1f;
            }

            var itemTags = BuildItemTagSet(def);

            foreach (var rule in rules)
            {
                if (rule.Tags == null || rule.Tags.Length == 0)
                {
                    return rule.Ratio;
                }

                bool allHit = true;
                for (int i = 0; i < rule.Tags.Length; i++)
                {
                    if (!itemTags.Contains(rule.Tags[i]))
                    {
                        allHit = false;
                        break;
                    }
                }

                if (allHit)
                {
                    return rule.Ratio;
                }
            }

            return 1f;
        }

        static HashSet<EItemTag> BuildItemTagSet(ItemData def)
        {
            var set = new HashSet<EItemTag>();
            if (def?.Tags == null)
            {
                return set;
            }

            foreach (var tag in def.Tags)
            {
                if (tag != EItemTag.None)
                {
                    set.Add(tag);
                }
            }

            return set;
        }
    }
}
