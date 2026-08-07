using System;
using System.Collections;
using System.Collections.Generic;
using cfg.demo;
using UnityEngine;


 namespace My.Map.Entity
{
    public enum EBuffLayerScaleUsage
    {
        Ignore = 0,
        MultiplyNumeric = 1,
        Custom = 2,
    }

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
        public EBuffLayerScaleUsage LayerScaleUsage = EBuffLayerScaleUsage.Ignore;
    }

    [Serializable]
    public class MapAbilityEffectNextPhaseCfg : MapFightEffectCfg
    {
        public string MatchSkill;
        public string MatchPhase;
    }

    [Serializable]
    public class MapFightEffectDestroyEntityCfg : MapFightEffectCfg
    {
        public enum ETarget
        {
            Source,
            Target,
        }

        public ETarget Target = ETarget.Source;
        public string Reason = "fight_effect";
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

    // 公开场合通缉事件：叠加对应罪类通道、半径内和平 NPC 临时敌意 + 邪恶警戒
    [Serializable]
    public class MapFightEffectWantedIncidentBroadcastCfg : MapFightEffectCfg
    {
        public EWantedBehaveType Behave;

        public float Radius = 8f;

        public float TempEnmityAmount = 25f;

        public float EvilAlertDuration = 6f;

        public bool OnlyPeaceNpc = true;
    }

    // 玩家蹲伏偷袭：结算成功率（依目标 PhysicalForm）与成功/失败后果
    [Serializable]
    public class MapAbilityEffectSneakBackstabResolveCfg : MapFightEffectCfg
    {
        
    }
}

