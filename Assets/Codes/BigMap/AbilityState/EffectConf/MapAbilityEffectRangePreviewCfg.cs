using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    

    [Serializable]
    public class MapAbilityEffectRangePreviewCfg : MapFightEffectCfg
    {
        public enum EShape
        {
            None = 0,
            Square,
            Circle,
        }
        public EShape Shape;

        public float Width;
        public float Length;
        public float Radius;

        public float PreviewDuration;
    }
}

