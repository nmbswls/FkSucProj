using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectAddBuffCfg : MapAbilityEffectCfg
    {
        public string BuffId;
        public int Layer;
        public float Duration;
        public int TargetType; // 0 target 1 self 2 other
    }
}

