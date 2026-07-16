using System.Collections.Generic;

namespace My.Player
{
    [System.Serializable]
    public sealed class HumanWeaponAffixDef
    {
        public string AffixId;
        public string DisplayName;
        public string Description;
        public string GroupId;
        public int Weight = 100;
        public int BasePrice;
        // Kept for save/config compatibility with the first market implementation.
        public int MarketValue;
        public int ExclusiveGroup;
        public List<string> AllowedSubtypes = new();
    }

    [System.Serializable]
    public sealed class HumanWeaponAffixTier
    {
        public string AffixId;
        public int Tier;
        public int Weight;
        public string ValueText;
    }

    [System.Serializable]
    public sealed class HumanWeaponAffixLinkPrice
    {
        public string LinkId;
        public List<string> AffixIds = new();
        public int ExtraPrice;
        public string DisplayName;
    }
}
