using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectRemoveBuffCfg : MapFightEffectCfg
    {
        public string BuffId;
        public int Layer;
        public int TargetType;
    }
}
