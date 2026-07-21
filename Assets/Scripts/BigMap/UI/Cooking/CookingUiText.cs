using cfg.demo;

namespace My.UI.Cooking
{
    public static class CookingUiText
    {
        public static string Rarity(ECookingRarity rarity) => rarity switch
        {
            ECookingRarity.Common => "常见",
            ECookingRarity.Uncommon => "少见",
            ECookingRarity.Rare => "稀有",
            ECookingRarity.Precious => "珍贵",
            ECookingRarity.Limited => "有限",
            _ => rarity.ToString(),
        };

        public static string PrimaryType(ECookingPrimaryType type) => type switch
        {
            ECookingPrimaryType.Staple => "主食",
            ECookingPrimaryType.Main => "主菜",
            ECookingPrimaryType.Soup => "汤羹",
            ECookingPrimaryType.Dessert => "甜点",
            ECookingPrimaryType.Drink => "饮品",
            ECookingPrimaryType.Platter => "拼盘",
            _ => type.ToString(),
        };

        public static string Style(ECookingStyleTag style) => style switch
        {
            ECookingStyleTag.Homestyle => "家常",
            ECookingStyleTag.Hearty => "丰盛",
            ECookingStyleTag.Sweet => "甜味",
            ECookingStyleTag.Refreshing => "清爽",
            ECookingStyleTag.Refined => "精致",
            ECookingStyleTag.Festive => "节庆",
            ECookingStyleTag.Exotic => "异域",
            ECookingStyleTag.Nostalgic => "怀旧",
            _ => string.Empty,
        };
    }
}
