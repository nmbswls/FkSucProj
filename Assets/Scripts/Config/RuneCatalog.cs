using System.Collections.Generic;
using System.Linq;
using cfg.demo;

namespace My.Config
{
    public static class RuneCatalog
    {
        public static readonly ERuneEquipSlot[] EquipSlots =
        {
            ERuneEquipSlot.Core,
            ERuneEquipSlot.Hand,
            ERuneEquipSlot.Foot,
        };

        public static RuneData GetOrDefault(string runeId)
        {
            if (string.IsNullOrEmpty(runeId) || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbRuneData.GetOrDefault(runeId);
        }


        public static string GetSlotDisplayName(ERuneEquipSlot slot)
        {
            return slot switch
            {
                ERuneEquipSlot.Core => "Core",
                ERuneEquipSlot.Hand => "Hand",
                ERuneEquipSlot.Foot => "Foot",
                _ => string.Empty,
            };
        }
    }
}
