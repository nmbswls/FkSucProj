using cfg.demo;

namespace My.Player
{
    public sealed class BodyPartRuntimeState
    {
        public EBodyPart PartId;
        public int Level;
        public long Exp;
        public StatMap LocalStats = new StatMap();

        public void RebuildLocalStats()
        {
            LocalStats = My.Config.BodyPartCatalog.BuildLocalStats(PartId, Level);
        }

        public void RebuildInnerInfo()
        {

        }
    }
}
