using System;
using System.Collections.Generic;
using My.Map.Entity;

namespace My.Map.Fight
{
    [Flags]
    public enum FightAttrCaptureKind
    {
        None = 0,
        Level = 1 << 0,
        HPower = 1 << 1,
        XiXue = 1 << 2,
        AllCombatSource = Level | HPower | XiXue,
    }

    public static class FightAttrCapture
    {
        public static void CaptureInto(IFightAttrProvider src, Dictionary<string, long> extraAttrs, FightAttrCaptureKind kinds)
        {
            if (src == null || extraAttrs == null || kinds == FightAttrCaptureKind.None)
            {
                return;
            }

            if (kinds.HasFlag(FightAttrCaptureKind.Level) && src.TryGetUnitLevel(out var level))
            {
                extraAttrs[AttrIdConsts.SrcLevel_Pipeline] = level;
            }

            if (kinds.HasFlag(FightAttrCaptureKind.HPower) && src.TryGetAttr(AttrIdConsts.HPower, out var hPower))
            {
                extraAttrs[AttrIdConsts.HPower_Pipeline] = hPower;
            }

            if (kinds.HasFlag(FightAttrCaptureKind.XiXue) && src.TryGetAttr(AttrIdConsts.DamageXiXue, out var xixue))
            {
                extraAttrs[AttrIdConsts.XiXue_Pipeline] = xixue;
            }
        }
    }
}
