using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using UnityEngine;

namespace My
{
    public partial class GameLogicManager
    {
        public void RestoreNamedNpcDesireCrystal(string characterKey)
        {
            worldPersistState?.NpcCharacters?.RestoreNamedNpcDesireCrystal(characterKey);
        }

        public bool TryHarvestDesireCrystalFromNpc(long npcEntityId)
        {
            var npc = AreaManager.GetLogicEntiy(npcEntityId, false) as NpcUnitLogicEntity;
            if (npc == null)
            {
                return false;
            }

            var rec = npc.NpcRecord;
            if (string.IsNullOrEmpty(rec.AttachedDesireCrystalTypeId))
            {
                return false;
            }

            var def = CfgMgr.Cfgs.TbDesireCrystalDef.GetOrDefault(rec.AttachedDesireCrystalTypeId);
            if (def == null || string.IsNullOrEmpty(def.ItemId))
            {
                Debug.LogWarning($"TryHarvestDesireCrystalFromNpc: missing DesireCrystalDef for {rec.AttachedDesireCrystalTypeId}");
                return false;
            }

            long put = playerDataManager.TryGiveItem(def.ItemId, 1, 0);
            if (put <= 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(rec.CharacterKey))
            {
                worldPersistState?.NpcCharacters?.SetDesireCrystalTaken(rec.CharacterKey, true);
            }

            rec.AttachedDesireCrystalTypeId = string.Empty;
            return true;
        }
    }
}
