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
        /// <summary>
        /// 运动轨迹
        /// </summary>
        [SerializeReference]
        public MotionDataBase MotionData;

        /// <summary>
        /// 追踪相关
        /// </summary>
        public bool isHoming; // 是否制导
        public FightStruct.ESelectPolicy homingSelectPolicy;
        public float homingTime = 999; // 制导时间
        public bool homingConstantSpeed = false;
        public float homingSterRate = 1.0f;  // 转向保留原速度
        public float homingSpeed = 12;

        // 最长时间
        public float lifeTime;
        public FightStruct.Shape BulletShape;

        /// <summary>
        /// 方向参数 多个
        /// </summary>
        public bool RandomDir;


        [SerializeReference]
        public List<MapFightEffectCfg> HitEffects = new();

        public bool TriggerOnLifeEnd;
        public bool TriggerOnCollide;
    }
}
