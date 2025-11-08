using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectRemoveBuffCfg : MapAbilityEffectCfg
    {
        public string BuffId;
        public int Layer;
        public int TargetType;
    }
}
