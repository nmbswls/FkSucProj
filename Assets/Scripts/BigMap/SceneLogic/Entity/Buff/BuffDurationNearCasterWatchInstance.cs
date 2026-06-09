using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Entity
{
    internal sealed class BuffDurationNearCasterWatchInstance : BuffDurationInstanceBase
    {
        const float DefaultTriggerRadius = 0.35f;

        readonly float _triggerRadiusSqr;
        readonly float _checkInterval;
        bool _triggered;
        float _checkTimer;

        public BuffDurationNearCasterWatchInstance(BuffDurationEffet cfg)
        {
            var radius = cfg.ParamFloat1 > 0f ? cfg.ParamFloat1 : DefaultTriggerRadius;
            _triggerRadiusSqr = radius * radius;
            _checkInterval = cfg.ParamFloat2 > 0f ? cfg.ParamFloat2 : 0f;
        }

        public override void OnBuffInfoChanged(BuffInstance inst)
        {
        }

        public override void OnDetached(BuffInstance inst)
        {
        }

        public override void OnTick(BuffInstance inst, float dt)
        {
            if (_triggered || inst.BuffOwner == null || inst.BuffOwner is not BaseUnitLogicEntity unit)
            {
                return;
            }

            if (unit.IsDead || unit.MarkDestroyed)
            {
                return;
            }

            if (_checkInterval > 0f)
            {
                _checkTimer -= dt;
                if (_checkTimer > 0f)
                {
                    return;
                }

                _checkTimer = _checkInterval;
            }

            if (!TryResolveLiveCasterPos(inst, unit, out var casterPos))
            {
                inst.MarkedForRemove = true;
                return;
            }

            var delta = casterPos - unit.Pos;
            if (delta.sqrMagnitude > _triggerRadiusSqr)
            {
                return;
            }

            _triggered = true;
            inst.DoBuffTrigger(ETriggerType.NearCaster);
        }

        static bool TryResolveLiveCasterPos(BuffInstance inst, BaseUnitLogicEntity unit, out Vector2 casterPos)
        {
            casterPos = default;
            if (inst.CasterId == 0 || unit.LogicManager == null)
            {
                return false;
            }

            var caster = unit.LogicManager.GetLogicEntity(inst.CasterId, false);
            if (caster == null)
            {
                return false;
            }

            if (caster is LogicEntityBase le && le.MarkDestroyed)
            {
                return false;
            }

            casterPos = caster.Pos;
            return true;
        }
    }
}
