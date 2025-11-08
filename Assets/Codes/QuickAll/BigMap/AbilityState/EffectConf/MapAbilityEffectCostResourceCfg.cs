using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectCostResourceCfg : MapAbilityEffectCfg
    {
        public string ResourceId;
        public long CostValue;
        public int Flags;
    }
}

