using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectThrowStartCfg : MapFightEffectCfg
    {
        public int Priority;
        public float Duration;
        public string ThrowSelfBuffId;
        public string ThrowMainBuffId;
    }
}


