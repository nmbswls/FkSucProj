using Map.Entity;
using Map.Entity.Attr;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectAddResourceCfg : MapFightEffectCfg
    {
        public string ResourceId;
        public long AddValue;
        public int Flags;

        public List<AttrKvPair> ExtraAttrInfos;
    }
}

