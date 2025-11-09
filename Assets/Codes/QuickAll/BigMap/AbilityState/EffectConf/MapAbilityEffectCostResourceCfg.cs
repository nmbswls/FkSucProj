using Map.Entity;
using Map.Entity.Attr;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
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

