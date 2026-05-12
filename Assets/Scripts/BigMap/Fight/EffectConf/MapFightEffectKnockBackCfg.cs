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
        // 若两者均为 false，执行器按历史行为视为仅对目标生效（等价 ApplyTarget=true）
        public bool ApplyTarget = true;
        public bool ApplySelf;

        public enum EKnockBackType
        {
            None,
            CastDir,
            AwayFromSrc,
            AwayFromTarget, // 仅在 ApplySelf 时合法：受力者远离 TargetId
            Random,
        }

        public EKnockBackType DirType; // 在不同触发语境下 该值的含义不同

        public float KnockBackForce = 0.5f;
    }
}

