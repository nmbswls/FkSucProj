using System;
using System.Collections.Generic;
using cfg.demo;
using My.Player;

namespace My.Config
{
    public static class PlayerBagCatalog
    {
        static readonly List<PlayerBagDef> EmptyBagDefs = new();

        public static PlayerBagDef GetDef(EPlayerBagId bagId)
        {
            return CfgMgr.Cfgs?.TbPlayerBagDef?.GetOrDefault((int)bagId);
        }

        public static IReadOnlyList<PlayerBagDef> GetAutoGainBagDefs()
        {
            var rows = CfgMgr.Cfgs?.TbPlayerBagDef?.DataList;
            if (rows == null)
            {
                return EmptyBagDefs;
            }

            var result = new List<PlayerBagDef>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row != null && row.GainPriority > 0)
                {
                    result.Add(row);
                }
            }

            result.Sort((a, b) => a.GainPriority.CompareTo(b.GainPriority));
            return result;
        }

        public static EBagStorageLayout ResolveLayout(PlayerBagDef def, EBagStorageLayout fallback)
        {
            if (def == null || string.IsNullOrEmpty(def.Layout))
            {
                return fallback;
            }

            return string.Equals(def.Layout, nameof(EBagStorageLayout.Grid), StringComparison.OrdinalIgnoreCase)
                ? EBagStorageLayout.Grid
                : EBagStorageLayout.Compact;
        }

        public static int ResolveCapacity(PlayerBagDef def, PlayerSystemManager player, int fallback)
        {
            if (def == null)
            {
                return fallback;
            }

            long attribute = 0;
            if (def.YcAttributeId > 0)
            {
                attribute = player?.ProgressionSystem?.GetFinalAttribute(def.YcAttributeId) ?? 0;
            }

            return Math.Max(0, def.BaseCapacity + (int)attribute);
        }

        public static int ResolveExtraCapacity(PlayerBagDef def, int fallback)
        {
            return def != null ? Math.Max(0, def.ExtraCapacity) : fallback;
        }

        public static void ApplyAcceptedTags(PlayerBag bag, PlayerBagDef def)
        {
            if (bag == null)
            {
                return;
            }

            bag.SetAcceptedAnyTags(def?.AcceptedTags);
        }

        public static bool CanAutoCreate(PlayerBagDef def)
        {
            return def != null && def.AutoCreate;
        }
    }
}
