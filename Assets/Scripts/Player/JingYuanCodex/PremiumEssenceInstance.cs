using System.Collections.Generic;
using cfg.demo;

namespace My.Player
{
    public enum PremiumEssenceStorageState
    {
        Temporary = 0,
        Warehouse = 1,
        Equipped = 2,
    }

    [System.Serializable]
    public sealed class PremiumEssenceInstance
    {
        public long InstanceId;
        public EJingYuanType TypeId;
        public int Concentration;
        public int DropLevel;
        public int QualityTier;
        public string SourceItemId;
        public List<string> ExtraAffixIds = new();
        public int RemainingShelfLifeDays;
        public int RenewalCount;
        public string SourceType;
        public PremiumEssenceStorageState StorageState;
    }
}
