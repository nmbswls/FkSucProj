using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.SecretBase
{
    public enum ESecretBaseGiveGiftResult
    {
        Ok,
        InvalidArgs,
        NotInSecretBase,
        NoRegistry,
        DailyLimit,
        NoGiftItem,
        CostItemFailed,
        NoGiftDef,
        FavorUnsupported,
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

            if (!glm.IsInSecretBaseContext())
            {
                return ESecretBaseGiveGiftResult.NotInSecretBase;
            }

            var registry = glm.worldPersistState?.NpcCharacters;
            if (registry == null)
            {
                return ESecretBaseGiveGiftResult.NoRegistry;
            }

            var characterInfo = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(row.CharacterKey);
            if (characterInfo == null || !characterInfo.SupportsFavor)
            {
                return ESecretBaseGiveGiftResult.FavorUnsupported;
            }

            int giftsPerDay = characterInfo.GiftsPerDay > 0 ? characterInfo.GiftsPerDay : 1;
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

            var overrideRule = FindOverride(row.CharacterKey, itemId);
            int gain = overrideRule != null
                ? overrideRule.FavorValue
                : CalculateBaseGain(characterInfo, giftDef);

            registry.AddFavorValue(row.CharacterKey, gain);
            registry.RecordGiftGiven(row.CharacterKey, settlementDay);
            favorGained = gain;
            if (overrideRule != null && !string.IsNullOrEmpty(overrideRule.ReceiveDialogId))
            {
                MainGameManager.Instance?.PlayDialog(overrideRule.ReceiveDialogId);
            }
            return ESecretBaseGiveGiftResult.Ok;
        }

        static CharacterGiftRule FindOverride(string characterKey, string itemId)
        {
            var rules = CfgMgr.Cfgs?.TbCharacterGiftRule?.DataList;
            if (rules == null)
            {
                return null;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule != null && rule.CharacterKey == characterKey && rule.ItemId == itemId)
                {
                    return rule;
                }
            }

            return null;
        }

        static int CalculateBaseGain(cfg.demo.CharacterInfo characterInfo, ItemGift giftDef)
        {
            int baseValue = giftDef.BaseFavorValue;
            if (HasTagOverlap(giftDef.GiftTags, characterInfo.DislikedGiftTags))
            {
                return -Mathf.Abs(baseValue);
            }

            if (HasTagOverlap(giftDef.GiftTags, characterInfo.PreferredGiftTags))
            {
                return Mathf.RoundToInt(baseValue * PreferredTagBonusMultiplier);
            }

            return baseValue;
        }

        static bool HasTagOverlap(
            System.Collections.Generic.List<EGiftTag> itemTags,
            System.Collections.Generic.List<EGiftTag> characterTags)
        {
            if (itemTags == null || characterTags == null)
            {
                return false;
            }

            for (int i = 0; i < characterTags.Count; i++)
            {
                if (itemTags.Contains(characterTags[i]))
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
