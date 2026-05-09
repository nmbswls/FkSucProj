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

        public bool TryCreateDesireCrystalFromNpc(NpcUnitLogicEntity npc)
        {
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

            globalDropCollection.CreateDrop(def.ItemId, 1, npc.Pos + UnityEngine.Random.insideUnitCircle * 0.5f, true, npc.Pos);

            if (!string.IsNullOrEmpty(rec.CharacterKey))
            {
                worldPersistState?.NpcCharacters?.SetDesireCrystalTaken(rec.CharacterKey, true);
            }

            rec.AttachedDesireCrystalTypeId = string.Empty;
            return true;
        }
    }
}
