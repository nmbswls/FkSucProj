using Map.Entity;
using My.Map.Entity;
using System;
using UnityEngine;

namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectHumanWeaponHitCfg : MapFightEffectCfg
    {
        public int WeaponLevel;
        public long StunValue;
    }
}
