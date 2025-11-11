using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;


namespace My.Map.Entity
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

