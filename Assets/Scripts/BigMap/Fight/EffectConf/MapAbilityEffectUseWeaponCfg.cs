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
        // true 时 Duration 取当前 Executing 阶段时长（与 Phase 自动对齐）
        public bool BindPhaseDuration;
        public int MaxHit;
        public string AnimName;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects;
    }
}
