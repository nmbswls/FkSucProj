using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectIfBranchCfg : MapFightEffectCfg
    {
        public enum ECheckType
        {
            HasBuff,
            AttrGreater,

            HasTarget,

            BodyVsWin,
        }

        public ECheckType CheckType;
        public string Param1;
        public string Param2;
        public int Param3;

        public long Param5;
        public long Param6;


        [SerializeReference]
        public List<MapFightEffectCfg> TrueBranchEffects = new();

        [SerializeReference]
        public List<MapFightEffectCfg> FalseBranchEffects = new();
    }
}


