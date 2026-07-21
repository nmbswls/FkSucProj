using System;

namespace My.Map.Entity
{
    // 技能侧唯一合法的 H 行为入口：结算 HAct（冲击 + 可选派生 H 伤害）
    [Serializable]
    public class MapFightEffectApplyHActCfg : MapFightEffectCfg
    {
        public int ActId = HActResolver.DefaultHAttackActId;
        public float Intensity = 1f;
        public bool ApplyHpDamage = true;
        public float KnockBackForce;
    }
}
