using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


 namespace My.Map.Entity
{
    public enum EAbilityEffectType
    {
        None,
        ApplyBuff,
        FakeDamage,
        DashStart,
        DashEnd,
        HitBox,
        RemoveBuff,
        SpawnBullet,
        UseItem,
        UseWeapon,
        OpenLock
    }

    [Serializable]
    public abstract class MapFightEffectCfg
    {
        public EAbilityEffectType EffectType;
    }
}

