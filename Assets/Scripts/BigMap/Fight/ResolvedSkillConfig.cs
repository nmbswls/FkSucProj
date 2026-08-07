using System.Collections.Generic;
using cfg.demo;

namespace My.Map.Entity
{
    // 鎶€鑳藉熀纭€琛?+ 绛夌骇琛ㄥ悎骞跺悗鐨勮繍琛屾椂瑙嗗浘
    public sealed class ResolvedSkillConfig
    {
        public readonly EntitySkillData Base;
        public readonly EntitySkillLevel Level;

        public ResolvedSkillConfig(EntitySkillData baseCfg, EntitySkillLevel levelCfg)
        {
            Base = baseCfg;
            Level = levelCfg;
        }

        public string SkillId => Base.SkillId;
        public string Desc => Base.Desc;
        public bool IsPassive => Base.IsPassive;
        public bool IsCombo => Base.IsCombo;
        public bool NeedHMode => Base.NeedHMode;
        public bool AllowInPeaceArea => Base.AllowInPeaceArea;
        public bool InterruptCombo => Base.InterruptCombo;
        public bool IsDerived => Base.IsDerived;
        public int StackCount => Base.StackCount;
        public string IconPath => Base.IconPath;
        public int Priority => Base.Priority;
        public float DesiredUseAngle => Base.DesiredUseAngle;
        public float DesiredUseDistance => Base.DesiredUseDistance;
        public float BufferCacheTime => Base.BufferCacheTime;
        public string PassiveBuffLevelVariableKey => Base.PassiveBuffLevelVariableKey;
        public IReadOnlyList<SkillCastCondition> CastConditions => Base.CastConditions;

        public string MainAbilityId =>
            Level != null && !string.IsNullOrEmpty(Level.MainAbilityId)
                ? Level.MainAbilityId
                : Base.MainAbilityId;

        public float CoolDown => Level?.CoolDown ?? 0f;

        public IReadOnlyList<SkillAbilityExtraPair> AbilityExtra =>
            Level?.AbilityExtra ?? EmptyAbilityExtra;

        static readonly List<SkillAbilityExtraPair> EmptyAbilityExtra = new();
    }
}
