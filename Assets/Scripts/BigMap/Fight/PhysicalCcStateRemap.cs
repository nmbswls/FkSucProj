using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My
{
    public enum EPhysicalCcKind
    {
        None = 0,
        Knockdown = 1,
        Knockfly = 2,
    }

    public readonly struct PhysicalCcStateRemapRule
    {
        public readonly string FromStateAttrId;
        public readonly EPhysicalCcKind Kind;
        public readonly string HRelayBuffId;

        public PhysicalCcStateRemapRule(string fromStateAttrId, EPhysicalCcKind kind, string hRelayBuffId)
        {
            FromStateAttrId = fromStateAttrId;
            Kind = kind;
            HRelayBuffId = hRelayBuffId;
        }
    }

    // 受击侧物理异常状态 remap：击倒/击飞 State -> UnitStagger，监听后挂 H 中继 Buff
    public static class PhysicalCcStateRemap
    {
        public const string HRelayBuffKnockdown = "h_cc_knockdown";
        public const string HRelayBuffKnockfly = "h_cc_knockfly";
        public const float HRelayBuffDurationSec = 1.2f;

        const string RemapAbilityName = "PhysicalCcRemap";
        const long KnockdownResourceThreshold = 100_000;

        public static readonly PhysicalCcStateRemapRule[] StateRules =
        {
            new PhysicalCcStateRemapRule(AttrIdConsts.Stun, EPhysicalCcKind.Knockdown, HRelayBuffKnockdown),
            new PhysicalCcStateRemapRule(AttrIdConsts.NpcFcked, EPhysicalCcKind.Knockfly, HRelayBuffKnockfly),
        };

        static readonly Dictionary<long, EPhysicalCcKind> PendingByEntity = new();

        static bool TryFindStateRule(string attrId, out PhysicalCcStateRemapRule rule)
        {
            rule = default;
            if (string.IsNullOrEmpty(attrId))
            {
                return false;
            }

            for (int i = 0; i < StateRules.Length; i++)
            {
                if (StateRules[i].FromStateAttrId == attrId)
                {
                    rule = StateRules[i];
                    return true;
                }
            }

            return false;
        }

        public static ModSourceKey MakeRemapSourceKey(long entityId)
        {
            return new ModSourceKey
            {
                entityId = entityId,
                buffId = 0,
                abilityName = RemapAbilityName,
            };
        }

        static bool ShouldSkipRemap(IEntityAttributeOwner owner)
        {
            if (owner == null)
            {
                return true;
            }

            return owner.CheckHasState(AttrIdConsts.ImmuneKnock)
                   || owner.CheckHasState(AttrIdConsts.SuperArmor);
        }

        static void ApplyStaggerState(IEntityAttributeOwner owner, EPhysicalCcKind kind, string logContext)
        {
            if (owner == null || kind == EPhysicalCcKind.None)
            {
                return;
            }

            var src = MakeRemapSourceKey(owner.Id);
            owner.ExpireModifierBySource(src);
            owner.AddAttrModifier(src, AttrIdConsts.UnitStagger, 1);
            PendingByEntity[owner.Id] = kind;
            Debug.Log($"[PhysicalCcRemap] {logContext} entity={owner.Id} kind={kind} -> {AttrIdConsts.UnitStagger}");
        }

        public static bool TryRemapIncomingState(long entityId, IEntityAttributeOwner owner, string attrId, long val, out string remappedAttrId)
        {
            remappedAttrId = attrId;
            if (val <= 0 || owner == null || owner.Id != entityId)
            {
                return false;
            }

            if (!TryFindStateRule(attrId, out var rule))
            {
                return false;
            }

            if (ShouldSkipRemap(owner))
            {
                Debug.Log($"[PhysicalCcRemap] skipped_immune entity={entityId} attr={attrId}");
                return false;
            }

            remappedAttrId = AttrIdConsts.UnitStagger;
            ApplyStaggerState(owner, rule.Kind, $"remap_state from={attrId}");
            return true;
        }

        public static bool IsKnockdownResource(string resourceId)
        {
            return resourceId == AttrIdConsts.UnitKnockDown || resourceId == AttrIdConsts.PlayerKnockDown;
        }

        public static bool TryConsumeKnockdownResource(IEntityAttributeOwner owner, string resourceId, long delta)
        {
            if (owner == null || delta <= 0 || !IsKnockdownResource(resourceId))
            {
                return false;
            }

            if (ShouldSkipRemap(owner))
            {
                Debug.Log($"[PhysicalCcRemap] skipped_immune entity={owner.Id} res={resourceId}");
                return false;
            }

            ApplyStaggerState(owner, EPhysicalCcKind.Knockdown, $"remap_resource res={resourceId} delta={delta}");
            return true;
        }

        public static void TriggerKnockdownThreshold(BaseUnitLogicEntity unit, string resourceId)
        {
            if (unit == null)
            {
                return;
            }

            if (ShouldSkipRemap(unit))
            {
                Debug.Log($"[PhysicalCcRemap] skipped_immune entity={unit.Id} threshold res={resourceId}");
                return;
            }

            unit.ForceSetResource(resourceId, 0);
            ApplyStaggerState(unit, EPhysicalCcKind.Knockdown, $"knockdown_threshold res={resourceId}");
        }

        public static bool TryResolveHRelayBuff(long entityId, out string buffId, out EPhysicalCcKind kind)
        {
            buffId = null;
            kind = EPhysicalCcKind.None;
            if (!PendingByEntity.TryGetValue(entityId, out kind) || kind == EPhysicalCcKind.None)
            {
                return false;
            }

            PendingByEntity.Remove(entityId);
            for (int i = 0; i < StateRules.Length; i++)
            {
                if (StateRules[i].Kind == kind)
                {
                    buffId = StateRules[i].HRelayBuffId;
                    return !string.IsNullOrEmpty(buffId);
                }
            }

            return false;
        }

        public static void ClearPending(long entityId)
        {
            PendingByEntity.Remove(entityId);
        }

        public static long KnockdownThreshold => KnockdownResourceThreshold;
    }
}
