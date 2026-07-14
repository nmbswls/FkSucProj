using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;


namespace My.Map.Entity
{
    public enum ESummonLifetimeRule : byte
    {
        Independent = 0,
        WhileOwnerExists = 1,
        WhileOwnerInCombat = 2,
    }

    [Serializable]
    public class MapAbilityEffectSpawnEntityCfg : MapFightEffectCfg
    {
        public enum ESpawnCenter
        {
            CastPoint,
            Source,
        }

        public EEntityType EntityType;
        public string CfgId;
        public float LifeTime;
        public ESpawnCenter SpawnCenter = ESpawnCenter.CastPoint;
        [Min(1)] public int SpawnCount = 1;
        [Min(0f)] public float SpawnRadius;
        public string SummonGroup = string.Empty;
        [Min(0)] public int MaxAlivePerSource;
        public ESummonLifetimeRule SummonLifetimeRule = ESummonLifetimeRule.Independent;

        public long Param1;
        public long Param2;
        public long Param3;
        public string Param4;
        public string Param5;
    }
}

