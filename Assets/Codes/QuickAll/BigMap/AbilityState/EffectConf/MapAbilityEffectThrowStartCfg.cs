using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectThrowStartCfg : MapAbilityEffectCfg
    {
        public int Priority;
        public float Duration;
        public string ThrowSelfBuffId;
        public string ThrowMainBuffId;
    }
}


