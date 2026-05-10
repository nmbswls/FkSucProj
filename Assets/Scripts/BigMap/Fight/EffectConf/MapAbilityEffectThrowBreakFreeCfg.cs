using System;
using UnityEngine;

namespace My.Map.Entity
{
    // 终止当前投技（受害者挣脱）：ThrowEndReason.QteBreakFree，走 ThrowStart 上 OnQteBreakFreeEffects
    [Serializable]
    public class MapAbilityEffectThrowBreakFreeCfg : MapFightEffectCfg
    {
    }
}
