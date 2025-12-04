using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Fight;
using UnityEngine;


namespace My.Map.Entity
{

    [Serializable]
    public class MapAbilityEffectSpawnBulletCfg : MapFightEffectCfg
    {
        public string BulletId;
        /// <summary>
        /// 运动轨迹
        /// </summary>
        [SerializeReference]
        public MotionDataBase MotionData;

        public enum ESpawnPos
        {
            TriggerPos,
            CastPos,
        }
        public ESpawnPos SpawnPos = ESpawnPos.TriggerPos;

        public enum ESpawnDir
        {
            NoDir = 0,
            ToCastPos,
            ToTriggerPos,
            Random,
        }
        public ESpawnDir SpawnDir = ESpawnDir.NoDir;

        /// <summary>
        /// 追踪相关
        /// </summary>
        public bool isHoming; // 是否制导
        public FightStruct.ESelectPolicy homingSelectPolicy;
        public float homingTime = 999; // 制导时间

        public bool showRangeWarn;

        // 最长时间
        public float lifeTime;
        public FightStruct.Shape BulletShape;


        [SerializeReference]
        public List<MapFightEffectCfg> HitEffects = new();

        public bool TriggerOnLifeEnd;
        public bool TriggerOnCollide;

        public bool lockViewAngle = true;
    }
}
