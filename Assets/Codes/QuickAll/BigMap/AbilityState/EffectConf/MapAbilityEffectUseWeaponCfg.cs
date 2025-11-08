using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectUseWeaponCfg : MapAbilityEffectCfg
    {
        public string WeaponName;
        public float Duration;

        [SerializeReference]
        public List<MapAbilityEffectCfg> OnHitEffects;
    }
}
