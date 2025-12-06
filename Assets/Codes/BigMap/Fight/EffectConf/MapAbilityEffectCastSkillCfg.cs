using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectCastSkillCfg : MapFightEffectCfg
    {
        public bool UseTargetAsTarget;
        public bool UseTargetAsCastVec;
        public bool UseSrcAsTarget;
        public string SkillId;
    }
}

