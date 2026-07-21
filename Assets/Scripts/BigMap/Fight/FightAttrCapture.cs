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
        HTechnique = 1 << 1,
        XiXue = 1 << 2,
        AllCombatSource = Level | HTechnique | XiXue,
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

            if (kinds.HasFlag(FightAttrCaptureKind.HTechnique) && src.TryGetAttr(AttrIdConsts.HTechnique, out var hTechnique))
            {
                extraAttrs[AttrIdConsts.HTechnique_Pipeline] = hTechnique;
            }

            if (kinds.HasFlag(FightAttrCaptureKind.XiXue) && src.TryGetAttr(AttrIdConsts.DamageXiXue, out var xixue))
            {
                extraAttrs[AttrIdConsts.XiXue_Pipeline] = xixue;
            }
        }
    }
}
