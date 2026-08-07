using System;
using System.Collections.Generic;

namespace My.Player
{
    public enum ESkillModifierType
    {
        PhaseDurationMultiplier,
        ChargeThresholdTimeMultiplier,
    }

    public sealed class SkillModifierSpec
    {
        public string SkillId;
        public string AbilityId;
        public string PhaseName;
        public ESkillModifierType ModifierType;
        public float Value;
        public string SourceId;
    }

    public interface IProgressionSkillModifierSource
    {
        void CollectSkillModifiers(List<SkillModifierSpec> output);
    }

    public static class SkillModifierUtil
    {
        public static float ResolvePhaseDurationMultiplier(
            IReadOnlyList<SkillModifierSpec> modifiers,
            string skillId,
            string abilityId,
            string phaseName)
        {
            return ResolveMultiplier(modifiers, skillId, abilityId, phaseName, ESkillModifierType.PhaseDurationMultiplier);
        }

        public static float ResolveChargeThresholdTimeMultiplier(
            IReadOnlyList<SkillModifierSpec> modifiers,
            string skillId,
            string abilityId,
            string phaseName)
        {
            return ResolveMultiplier(modifiers, skillId, abilityId, phaseName, ESkillModifierType.ChargeThresholdTimeMultiplier);
        }

        static float ResolveMultiplier(
            IReadOnlyList<SkillModifierSpec> modifiers,
            string skillId,
            string abilityId,
            string phaseName,
            ESkillModifierType modifierType)
        {
            float result = 1f;
            if (modifiers == null)
            {
                return result;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null
                    || modifier.ModifierType != modifierType
                    || !Matches(modifier, skillId, abilityId, phaseName))
                {
                    continue;
                }

                result *= modifier.Value > 0f ? modifier.Value : 1f;
            }

            return result;
        }

        static bool Matches(SkillModifierSpec modifier, string skillId, string abilityId, string phaseName)
        {
            return (string.IsNullOrEmpty(modifier.SkillId) || string.Equals(modifier.SkillId, skillId, StringComparison.Ordinal))
                && (string.IsNullOrEmpty(modifier.AbilityId) || string.Equals(modifier.AbilityId, abilityId, StringComparison.Ordinal))
                && (string.IsNullOrEmpty(modifier.PhaseName) || string.Equals(modifier.PhaseName, phaseName, StringComparison.Ordinal));
        }
    }
}
