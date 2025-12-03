using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectDashStartCfg : MapFightEffectCfg
    {
        public float DashDuration;

        public bool IsFixPointMode; // 冲刺到施放点就停下
        public float DashOverrideHitRadius = 0;

        public bool IsLockTarget;
        //public bool IsCastDir;

        public float DashSpeed;
        public bool IsGhost;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects;
    }
}
