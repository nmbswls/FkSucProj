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

        public enum EDashMode
        {
            FixTime, 
            FixDistance,
            ToTarget,
        }

        public EDashMode DashMode;

        public bool UseTempDir = false;
        public string DashWeaponName = string.Empty;

        public float DashSpeed;
        public float DashDuration;
        public float MaxDistance;

        public float DashOverrideHitRadius = 0;

        public bool IsGhost;
        public bool NextPhaseOnHit;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects;
    }
}
