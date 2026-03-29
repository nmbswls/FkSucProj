using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectSpawnEntityCfg : MapFightEffectCfg
    {
        public EEntityType EntityType;
        public string CfgId;
        public float LifeTime;

        public long Param1;
        public long Param2;
        public long Param3;
        public string Param4;
        public string Param5;
    }
}

