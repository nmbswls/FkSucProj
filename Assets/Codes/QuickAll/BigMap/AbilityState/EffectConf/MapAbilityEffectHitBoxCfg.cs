using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit.Ability.Effect
{
    [Serializable]
    public class MapAbilityEffectHitBoxCfg : MapFightEffectCfg
    {
        public enum EShape
        {
            None,
            Square,
            Circle,
        }
        public EShape Shape;

        public float Width;
        public float Length;
        public float Radius;

        public EEntityType TargetEntityType;

        public EntityFilterParam FilterParams;

        public bool IncludeEnmity;
        public bool IncludeFriendly;

        public float HitVal;

        [SerializeReference]
        public List<MapFightEffectCfg> OnHitEffects = new();
    }
}


