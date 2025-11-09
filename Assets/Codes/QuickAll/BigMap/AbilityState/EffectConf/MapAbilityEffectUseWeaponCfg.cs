using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectUseWeaponCfg : MapFightEffectCfg
    {
        public string WeaponName;
        public float Duration;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects;
    }
}
