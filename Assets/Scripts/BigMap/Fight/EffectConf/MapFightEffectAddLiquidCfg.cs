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
        public EGroundElementType ElementType = EGroundElementType.None;
        public float Range = 1.2f;
        public float Duration = 5.0f;
    }
}

