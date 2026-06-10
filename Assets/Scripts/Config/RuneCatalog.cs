using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Player;

namespace My.Config
{
    public static class RuneCatalog
    {
        static ERuneEquipSlot[] _equipSlots =
        {
            ERuneEquipSlot.Core,
            ERuneEquipSlot.Hand,
            ERuneEquipSlot.Foot,
        };

        public static IReadOnlyList<ERuneEquipSlot> EquipSlots => _equipSlots;

        public static void RebuildEquipSlotOrder()
        {
            var table = CfgMgr.Cfgs?.TbRuneEquipSlot;
            if (table?.DataList == null || table.DataList.Count == 0)
            {
                return;
            }

            _equipSlots = table.DataList
                .Where(x => x != null && x.EquipSlot != ERuneEquipSlot.None)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => (int)x.EquipSlot)
                .Select(x => x.EquipSlot)
                .ToArray();
        }

        public static RuneData GetOrDefault(string runeId)
        {
            if (string.IsNullOrEmpty(runeId) || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbRuneData.GetOrDefault(runeId);
        }

        public static RuneEquipSlotInfo GetEquipSlotDef(ERuneEquipSlot slot)
        {
            if (slot == ERuneEquipSlot.None || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbRuneEquipSlot.GetOrDefault(slot);
        }

        public static string GetSlotDisplayName(ERuneEquipSlot slot)
        {
            var def = GetEquipSlotDef(slot);
            if (def != null && !string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return slot switch
            {
                ERuneEquipSlot.Core => "???",
                ERuneEquipSlot.Hand => "???",
                ERuneEquipSlot.Foot => "???",
                _ => string.Empty,
            };
        }

        public static string GetEquipSlotLockHint(ERuneEquipSlot slot, PlayerRuneSystem runeSystem)
        {
            if (slot == ERuneEquipSlot.None)
            {
                return string.Empty;
            }

            if (runeSystem != null && runeSystem.IsEquipSlotOpen(slot))
            {
                return string.Empty;
            }

            var def = GetEquipSlotDef(slot);
            if (def != null && !string.IsNullOrEmpty(def.LockHint))
            {
                return def.LockHint;
            }

            return $"{GetSlotDisplayName(slot)}???????";
        }
    }
}
