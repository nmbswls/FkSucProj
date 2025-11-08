using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unit.Ability.Effect
{

    [Serializable]
    public class MapAbilityEffectSpawnBulletCfg : MapAbilityEffectCfg
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
    }
}
