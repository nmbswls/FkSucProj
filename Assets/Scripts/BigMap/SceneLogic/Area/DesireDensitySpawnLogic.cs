using cfg.demo;
using My.Config;

namespace My.Map.Logic
{
    public static class DesireDensitySpawnLogic
    {
        public static void ApplyOnNpcRecord(LogicEntityRecord4Npc rec)
        {
            if (rec == null || string.IsNullOrEmpty(rec.CfgId))
            {
                return;
            }

            var npcCfg = CfgMgr.Cfgs?.TbUnitNpc?.GetOrDefault(rec.CfgId);
            if (npcCfg == null || npcCfg.DesireDensityType == EDesireDensityType.None)
            {
                return;
            }

            if (rec.DesireDensityType != EDesireDensityType.None && rec.DesireDensity > 0)
            {
                return;
            }

            rec.DesireDensityType = npcCfg.DesireDensityType;
            rec.DesireDensity = DesireDensityUtil.RollInitialDensity(npcCfg.DesireDensityType);
        }
    }
}
