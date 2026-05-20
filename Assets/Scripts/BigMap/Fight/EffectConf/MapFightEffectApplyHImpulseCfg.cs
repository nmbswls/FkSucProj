using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapFightEffectApplyHImpulseCfg : MapFightEffectCfg
    {
        public long BaseVal;
        public List<AttrKvPair> ExtraDamageRate = new();
    }
}

