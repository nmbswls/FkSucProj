using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapFightEffectAddLiquidCfg : MapFightEffectCfg
    {
        public EGroundLiquidType ElementType = EGroundLiquidType.None;
        public float Range = 1.2f;
        public float Duration = 5.0f;

        public float OffsetRange = 0;
    }
}

