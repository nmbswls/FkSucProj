using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;
using static My.Map.Fight.FightStruct;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectAddResourceCfg : MapFightEffectCfg
    {
        public string ResourceId;
        public long AddValue;
        public string AddValueFromAttrId;
        public bool IsEnmity;
        public EDmgFlag Flags;

        public List<AttrKvPair> ExtraAttrInfos;

        public bool IsSelf;
    }
}
