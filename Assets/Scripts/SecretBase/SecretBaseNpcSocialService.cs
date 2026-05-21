using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.SecretBase
{
    public enum ESecretBaseGiveGiftResult
    {
        Ok,
        InvalidArgs,
        NoRegistry,
        DailyLimit,
        NoGiftItem,
        CostItemFailed,
        NoGiftDef,
    }

    public static class SecretBaseNpcSocialService
    {
        const float PreferredTagBonusMultiplier = 1.5f;

        public static bool TryTalk(SecretBaseCharacter row)
        {
            if (row == null || string.IsNullOrEmpty(row.DialogId))
            {
                return false;
            }

            return MainGameManager.Instance != null
                   && MainGameManager.Instance.PlayDialog(row.DialogId);
        }

        public static ESecretBaseGiveGiftResult TryGiveGift(
            GameLogicManager glm,
            SecretBaseCharacter row,
            string itemId,
            out int favorGained)
        {
            favorGained = 0;
            if (glm == null || row == null || string.IsNullOrEmpty(row.CharacterKey) || string.IsNullOrEmpty(itemId))
            {
                return ESecretBaseGiveGiftResult.InvalidArgs;
            }

            var registry = glm.worldPersistState?.NpcCharacters;
            if (registry == null)
            {
                return ESecretBaseGiveGiftResult.NoRegistry;
            }

            int giftsPerDay = row.GiftsPerDay > 0 ? row.GiftsPerDay : 1;
            int settlementDay = glm.SettlementDayIndex;
            if (!registry.CanGiveGiftToday(row.CharacterKey, giftsPerDay, settlementDay))
            {
                return ESecretBaseGiveGiftResult.DailyLimit;
            }

            var inv = glm.playerDataManager?.InventorySystem;
            if (inv == null || !SecretBaseGiftInventoryQuery.HasGiftItem(inv, itemId))
            {
                return ESecretBaseGiveGiftResult.NoGiftItem;
            }

            var giftDef = ItemCatalog.GetGiftDef(itemId);
            if (giftDef == null)
            {
                return ESecretBaseGiveGiftResult.NoGiftDef;
            }

            long left = inv.CostItem(itemId, 1);
            if (left > 0)
            {
                Debug.LogWarning($"SecretBaseNpcSocialService: CostItem failed itemId={itemId} left={left}");
                return ESecretBaseGiveGiftResult.CostItemFailed;
            }

            int perLevel = row.BaseFavorPerGiftLevel > 0 ? row.BaseFavorPerGiftLevel : 10;
            int gain = Mathf.Max(1, giftDef.GiftLevel * perLevel);
            if (HasPreferredTagOverlap(row, giftDef))
            {
                gain = Mathf.RoundToInt(gain * PreferredTagBonusMultiplier);
            }

            registry.AddFavorValue(row.CharacterKey, gain);
            registry.RecordGiftGiven(row.CharacterKey, settlementDay);
            favorGained = gain;
            return ESecretBaseGiveGiftResult.Ok;
        }

        static bool HasPreferredTagOverlap(SecretBaseCharacter row, ItemGift giftDef)
        {
            if (row?.PreferredGiftTags == null || row.PreferredGiftTags.Count == 0
                || giftDef?.GiftTags == null || giftDef.GiftTags.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < row.PreferredGiftTags.Count; i++)
            {
                if (giftDef.GiftTags.Contains(row.PreferredGiftTags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static string FormatGiveGiftError(ESecretBaseGiveGiftResult result)
        {
            return result switch
            {
                ESecretBaseGiveGiftResult.DailyLimit => "今日送礼次数已用完",
                ESecretBaseGiveGiftResult.NoGiftItem => "背包中没有该礼物",
                ESecretBaseGiveGiftResult.CostItemFailed => "扣除礼物失败",
                ESecretBaseGiveGiftResult.NoGiftDef => "该物品不是有效礼物",
                _ => "无法送礼",
            };
        }
    }
}
