using Map.Entity;
using My.Map.Entity;
using System;
using UnityEngine;

namespace My.Map.Entity
{
    [Serializable]
    public class MapFightEffectAddMistCfg : MapFightEffectCfg
    {
        public EGroundMistType ElementType = EGroundMistType.None;
        public float Range = 1.2f;
        public float Duration = 5.0f;
        public float OffsetRange = 0;
    }
}
