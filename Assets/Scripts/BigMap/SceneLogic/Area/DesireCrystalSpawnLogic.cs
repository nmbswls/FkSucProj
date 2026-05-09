using My;
using My.Config;
using UnityEngine;

namespace My.Map.Logic
{
    public static class DesireCrystalSpawnLogic
    {
        public static void ApplyOnNpcBeforeSpawn(GameLogicManager gm, GameLogicAreaManager area, LogicEntityRecord4Npc rec)
        {
            if (rec == null || gm == null || area == null)
            {
                return;
            }

            if (area.cacheMapCfg == null || !area.cacheMapCfg.HuntingTarget)
            {
                return;
            }

            if (!string.IsNullOrEmpty(rec.AttachedDesireCrystalTypeId))
            {
                return;
            }

            if (!string.IsNullOrEmpty(rec.CharacterKey))
            {
                ApplyNamed(gm, rec);
                return;
            }

            if (area.Record2RefreshInfo.TryGetValue(rec.Id, out var sid) &&
                area.DesireCrystalFixedRefreshStaticIds != null &&
                area.DesireCrystalFixedRefreshStaticIds.Contains(sid))
            {
                rec.AttachedDesireCrystalTypeId = DesireCrystalRoller.RollWeightedCrystalTypeId();
                return;
            }

            if (!rec.IsFixed)
            {
                if (area.TryConsumeWalkerDesireCrystalRoll(out var cid))
                {
                    rec.AttachedDesireCrystalTypeId = cid;
                }
            }
        }

        static void ApplyNamed(GameLogicManager gm, LogicEntityRecord4Npc rec)
        {
            var named = CfgMgr.Cfgs.TbNamedNpcDesireCrystal.GetOrDefault(rec.CharacterKey);
            if (named == null || string.IsNullOrEmpty(named.CrystalTypeId))
            {
                return;
            }

            if (gm.worldPersistState?.NpcCharacters == null)
            {
                return;
            }

            if (gm.worldPersistState.NpcCharacters.IsDesireCrystalTaken(rec.CharacterKey))
            {
                return;
            }

            rec.AttachedDesireCrystalTypeId = named.CrystalTypeId;
        }
    }
}
