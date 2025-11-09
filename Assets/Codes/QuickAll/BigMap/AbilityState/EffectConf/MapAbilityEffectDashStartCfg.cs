using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectDashStartCfg : MapFightEffectCfg
    {
        public bool IsTimeMode;
        public float DashDuration;
        public float DashSpeed;
    }
}
