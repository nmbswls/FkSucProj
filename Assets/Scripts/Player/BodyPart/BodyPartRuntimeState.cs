using cfg.demo;
using My.Config;

namespace My.Player
{
    public sealed class BodyPartRuntimeState
    {
        public EBodyPart PartId;
        public int Level;
        public long Exp;
        public StatMap LocalStats = new StatMap();

        public void RebuildLocalStats(PlayerEquipmentManager equipment = null)
        {
            LocalStats = BodyPartCatalog.BuildLocalStats(PartId, Level);
            BodyPartCatalog.AccumulateEquippedGearLocalBonuses(PartId, equipment, LocalStats);
        }

        public void RebuildInnerInfo()
        {
        }
    }
}
