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

        public enum EDirMode
        {
            CastDir,
            LookDir,
            TmpLookDir,
        }

        public EDirMode DirMode;
        public string DashWeaponName = string.Empty;

        public float DashSpeed;
        public float DashDuration;
        public float MaxDistance;

        public float DashOverrideHitRadius = 0;

        public bool IsGhost = true;
        public bool NextPhaseOnHit;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects;
    }
}
