using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{

    [Serializable]
    public class MapAbilityEffectSpawnBulletCfg : MapFightEffectCfg
    {
        public enum ETargetType
        {
            Dir,
            Pos,
        }

        public EMotionType motionType;
        public ETargetType targetType;

        public float lifeTime;
        public float speed;
        public int param1;
        public int param2;

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
