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
        // 仅表示 HAct 派生的纯扣血；禁止经 ApplyDamage 等外界效果直接创建
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

        // 空则扣 HP；buff 等对 NPCHVal 等资源时指定
        public string ResourceId;
        public bool IsEnmity = true;

        public int TargetType; // 在不同触发语境下 该值的含义不同
        public float KnockBackForce;

        public float HRate; // 覆盖hrate

    }
}

