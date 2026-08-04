using System;
using System.Collections.Generic;
using cfg.demo;
using My.Player;

namespace My.Config
{
    public static class PlayerBagCatalog
    {
        public const int MaxBigBagCapacity = 4;

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
            if (def.YcAttributeId != EYCAttribute.None)
            {
                attribute = player?.ProgressionSystem?.GetFinalAttribute((int)def.YcAttributeId) ?? 0;
            }

            int capacity = Math.Max(0, def.BaseCapacity + (int)attribute);
            if (def.BagId == (int)EPlayerBagId.Big)
            {
                capacity = Math.Min(MaxBigBagCapacity, capacity);
            }

            return capacity;
        }

        public static int ResolveExtraCapacity(PlayerBagDef def, int fallback)
        {
            return def != null ? Math.Max(0, def.ExtraCapacity) : fallback;
        }

        // 负重：背包配置的基础重量折算率，10000 = 100%。
        public static int ResolveBagWeightRatio(PlayerBagDef def)
        {
            if (def == null)
            {
                return WeightRatioBasis;
            }

            return def.WeightRatio > 0 ? def.WeightRatio : 0;
        }

        public const int WeightRatioBasis = 10000;

        // 负重：玩家养成属性对该背包重量折算率的修正，10000 = 100%。
        public static int ResolveBagWeightRatioAttribute(PlayerBagDef def, PlayerSystemManager player, int fallback = WeightRatioBasis)
        {
            if (def == null || def.YcWeightRatioAttrId == EYCAttribute.None)
            {
                return fallback;
            }

            long value = player?.ProgressionSystem?.GetFinalAttribute((int)def.YcWeightRatioAttrId) ?? fallback;
            return (int)Math.Max(0, value);
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
