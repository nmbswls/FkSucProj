using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectAddResourceCfg : MapAbilityEffectCfg
    {
        public string ResourceId;
        public long AddValue;
        public int Flags;
    }
}

