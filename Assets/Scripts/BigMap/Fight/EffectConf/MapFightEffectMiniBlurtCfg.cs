using System;
using Map.Entity;

namespace My.Map.Entity
{
    [Serializable]
    public class MapFightEffectMiniBlurtCfg : MapFightEffectCfg
    {
        public float BaseSjAmount = 0.25f;
        public float FixedSjDamage = 0.4f;
    }
}
