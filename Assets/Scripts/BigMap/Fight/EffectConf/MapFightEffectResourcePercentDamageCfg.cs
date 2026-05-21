using System;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Entity
{
    // 按目标资源上限万分比扣减（如中毒 0.5% MaxHP）
    [Serializable]
    public class MapFightEffectResourcePercentDamageCfg : MapFightEffectCfg
    {
        public string ResourceId;
        public long RateBp;
        public bool IsEnmity;
        public EDmgCategory DamageCategory;
        public Fight.FightStruct.EDmgFlag Flags;
    }
}
