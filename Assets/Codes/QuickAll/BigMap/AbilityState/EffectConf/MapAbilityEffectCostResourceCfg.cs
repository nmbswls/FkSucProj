using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectCostResourceCfg : MapFightEffectCfg
    {
        public string ResourceId;
        public long CostValue;
        public int Flags;

        public int TargetType; // 在不同触发语境下 该值的含义不同

        public List<AttrKvPair> ExtraAttrInfos;
    }
}

