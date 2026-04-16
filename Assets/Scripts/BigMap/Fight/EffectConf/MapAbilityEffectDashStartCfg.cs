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
            InputDir,
        }

        public EDirMode DirMode;
        public string DashWeaponName = string.Empty;

        public float DashSpeed;
        public float DashDuration;
        public float MaxDistance;

        public float DashOverrideHitRadius = 0;

        public bool IsGhost = true;
        public bool EndOnHitUnit;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects;

        public bool StopOnWall;

        public bool EndAbilityPhaseWhenEnds = true; // 只有当来自技能时 该值才可为true
    }
}
