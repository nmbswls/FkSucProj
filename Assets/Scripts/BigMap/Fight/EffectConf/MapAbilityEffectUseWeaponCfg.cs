using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectUseWeaponCfg : MapFightEffectCfg
    {
        public string WeaponName;
        public float Duration;
        public int MaxHit;
        public string AnimName;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects;
    }
}
