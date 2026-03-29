using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectGiveItemCfg : MapFightEffectCfg
    {
        public string ItemId;
        public int Count;
        public int SpecificBagId;
    }
}

