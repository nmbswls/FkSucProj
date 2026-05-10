using System;
using UnityEngine;

namespace My.Map.Entity
{
    // 终止当前投技（受害者挣脱）：ThrowEndReason.PlayerBreakFree，走 ThrowStart 上 OnPlayerBreakFreeEffects
    [Serializable]
    public class MapAbilityEffectThrowBreakFreeCfg : MapFightEffectCfg
    {
    }
}
