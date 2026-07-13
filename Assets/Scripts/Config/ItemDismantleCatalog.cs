using System.Collections.Generic;
using cfg.demo;

namespace My.Config
{
    public readonly struct DismantleOutput
    {
        public DismantleOutput(string itemId, long count)
        {
            ItemId = itemId;
            Count = count;
        }

        public string ItemId { get; }
        public long Count { get; }
    }

    public static class ItemDismantleCatalog
    {
        public static ItemDismantle GetRule(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || CfgMgr.Cfgs?.TbItemDismantle == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbItemDismantle.GetOrDefault(itemId);
        }

        public static bool CanDismantle(string itemId)
        {
            var def = ItemCatalog.GetItemDef(itemId);
            var rule = GetRule(itemId);
            return def != null
                && def.CanDismantle
                && rule?.Outputs != null
                && rule.Outputs.Count > 0;
        }

        public static List<DismantleOutput> BuildOutputs(string itemId, long amount)
        {
            var result = new List<DismantleOutput>();
            var rule = GetRule(itemId);
            if (amount <= 0 || rule?.Outputs == null)
            {
                return result;
            }

            foreach (var output in rule.Outputs)
            {
                if (output == null || string.IsNullOrEmpty(output.ItemId) || output.Count <= 0)
                {
                    continue;
                }

                result.Add(new DismantleOutput(output.ItemId, checked(output.Count * amount)));
            }

            return result;
        }
    }
}
