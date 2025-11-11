using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectOpenClickWindowCfg : MapFightEffectCfg
    {
        public string WindowType;
        public float Duration;
        public int MaxCount;
        public string AddBuffById;
    }
}


