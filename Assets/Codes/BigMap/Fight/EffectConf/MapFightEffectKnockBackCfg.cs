using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{

    [Serializable]
    public class MapFightEffectKnockBackCfg : MapFightEffectCfg
    {
        public int TargetType;

        public enum EKnockBackType
        {
            None,
            CastDir,
            Random,
        }
        public EKnockBackType DirType; // 在不同触发语境下 该值的含义不同
        public float KnockBackForce;
    }
}

