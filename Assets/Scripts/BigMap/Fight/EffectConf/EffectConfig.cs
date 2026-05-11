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
        public float PendingTime;

    }

    [Serializable]
    public class MapAbilityEffectNextPhaseCfg : MapFightEffectCfg
    {
        public string MatchSkill;
        public string MatchPhase;
    }


    [Serializable]
    public class MapFightEffectTriggerAlert : MapFightEffectCfg
    {
        public float AlertDuration;
        public long AlertPower;
    }

    [Serializable]
    public class MapFightEffectEasyEffect : MapFightEffectCfg
    {
        public string EffectText;
    }

    // 玩家蹲伏偷袭：结算成功率（依目标 PhysicalForm）与成功/失败后果
    [Serializable]
    public class MapAbilityEffectSneakBackstabResolveCfg : MapFightEffectCfg
    {
        
    }
}

