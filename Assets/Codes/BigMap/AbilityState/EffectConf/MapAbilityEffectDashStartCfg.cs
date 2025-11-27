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
        public bool IsTimeMode;
        public float DashDuration;

        public bool IsFixPointMode;
        public float DashOverrideHitRadius = 0;

        public bool IsTargetDir;
        public bool IsCastDir;

        public float DashSpeed;
        public bool IsGhost;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects;
    }
}
