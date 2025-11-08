using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectIfBranchCfg : MapAbilityEffectCfg
    {
        public enum ECheckType
        {
            HasBuff,
            AttrGreater
        }

        public ECheckType CheckType;
        public string Param1;
        public string Param2;
        public int Param3;


        [SerializeReference]
        public List<MapAbilityEffectCfg> TrueBranchEffects = new();

        [SerializeReference]
        public List<MapAbilityEffectCfg> FalseBranchEffects = new();
    }
}


