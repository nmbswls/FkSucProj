using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{

    public enum EDmgCategory
    {
        None = 0,
        Physics,

        Magic,
        H,
    }

    [Serializable]
    public class MapFightEffectApplyDamageCfg : MapFightEffectCfg
    {
        public long BaseDamage;

        public List<AttrKvPair> ExtraAttrs = new();
        public List<AttrKvPair> ExtraDamageRate = new();
        //public EDamageFlag Flags;
        public EDmgCategory DamageCategory;


        public int TargetType; // 在不同触发语境下 该值的含义不同
        public float KnockBackForce;

        public float HRate; // 覆盖hrate

    }
}

