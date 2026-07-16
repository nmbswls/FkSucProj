using System;
using cfg.demo;
using My.Config;

namespace My.Player.Bag
{
    // 负重：统计玩家随身各背包的有效负重（仓库不计入）。
    public static class PlayerCarryWeightCalculator
    {
        public static long CalculateTotalCarryWeight(PlayerInventorySystem inventory, PlayerSystemManager player)
        {
            if (inventory == null)
            {
                return 0;
            }

            long total = 0;
            AccumulateBagWeight(inventory.MainBag, player, ref total);
            AccumulateBagWeight(inventory.MindFacetBag, player, ref total);
            AccumulateBagWeight(inventory.ImportantItemBag, player, ref total);

            if (inventory.SpeBags != null)
            {
                foreach (var pair in inventory.SpeBags)
                {
                    AccumulateBagWeight(pair.Value, player, ref total);
                }
            }

            return total;
        }

        // 负重：养成属性合成有效负重上限。
        public static long CalculateCarryWeightLimit(PlayerSystemManager player)
        {
            var progression = player?.ProgressionSystem;
            if (progression == null)
            {
                return 0;
            }

            long baseWeight = progression.GetFinalAttribute((int)EYCAttribute.CarryWeightBase);
            long extraFlat = progression.GetFinalAttribute((int)EYCAttribute.CarryWeightExtraFlat);
            long extraPercent = progression.GetFinalAttribute((int)EYCAttribute.CarryWeightExtraPercent);
            if (baseWeight < 0)
            {
                baseWeight = 0;
            }

            if (extraFlat < 0)
            {
                extraFlat = 0;
            }

            long core = baseWeight + extraFlat;
            long scaled = core * (PlayerBagCatalog.WeightRatioBasis + Math.Max(0, extraPercent))
                          / PlayerBagCatalog.WeightRatioBasis;
            return Math.Max(0, scaled);
        }

        static void AccumulateBagWeight(PlayerBag bag, PlayerSystemManager player, ref long total)
        {
            if (bag == null || bag.BagId == EPlayerBagId.Storage || bag.BagId == EPlayerBagId.FurnitureStorage)
            {
                return;
            }

            var def = PlayerBagCatalog.GetDef(bag.BagId);
            int bagWeightRatio = PlayerBagCatalog.ResolveBagWeightRatio(def);
            if (bagWeightRatio <= 0)
            {
                return;
            }

            int attrWeightRatio = PlayerBagCatalog.ResolveBagWeightRatioAttribute(def, player, PlayerBagCatalog.WeightRatioBasis);
            long effectiveBagRatio = (long)bagWeightRatio * attrWeightRatio / PlayerBagCatalog.WeightRatioBasis;
            if (effectiveBagRatio <= 0)
            {
                return;
            }

            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                total += CalculateStackWeight(bag.NormalSlots[i], effectiveBagRatio);
            }

            for (int i = 0; i < bag.ExtraSlots.Count; i++)
            {
                total += CalculateStackWeight(bag.ExtraSlots[i], effectiveBagRatio);
            }
        }

        static long CalculateStackWeight(ItemStack stack, long effectiveBagRatio)
        {
            if (stack == null || stack.IsEmpty)
            {
                return 0;
            }

            long unitWeight = ItemCatalog.GetItemUnitWeight(stack.ItemID);
            if (unitWeight <= 0 || stack.Count <= 0)
            {
                return 0;
            }

            // 负重：单件重量 * 数量 * 背包折算率
            return unitWeight * stack.Count * effectiveBagRatio / PlayerBagCatalog.WeightRatioBasis;
        }
    }
}
