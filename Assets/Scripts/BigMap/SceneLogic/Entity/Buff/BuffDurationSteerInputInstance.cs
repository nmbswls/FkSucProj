using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Entity
{
    internal sealed class BuffDurationSteerInputInstance : BuffDurationInstanceBase
    {
        const float DefaultTowardStopRadius = 0.35f;

        readonly EBuffMoveSteerMode _steerMode;
        readonly float _speedRate;
        readonly float _directionChangeInterval;
        readonly float _towardStopRadiusSqr;
        readonly Vector2 _fixedDirection;

        Vector2? _lastCasterPos;
        Vector2 _randomDir = Vector2.right;
        float _randomDirTimer;

        public BuffDurationSteerInputInstance(BuffDurationEffet cfg)
        {
            _speedRate = cfg.ParamFloat1 > 0f ? cfg.ParamFloat1 : 1f;
            _directionChangeInterval = cfg.ParamFloat2 > 0f ? cfg.ParamFloat2 : 0.2f;
            _towardStopRadiusSqr = DefaultTowardStopRadius * DefaultTowardStopRadius;

            if (!TryParseSteerMode(cfg.ParamStr1, out _steerMode))
            {
                _steerMode = EBuffMoveSteerMode.AwayFromCaster;
            }

            _fixedDirection = ParseFixedDirection(cfg.ParamStr2);
            if (_steerMode == EBuffMoveSteerMode.FixedDirection && _fixedDirection.sqrMagnitude < 0.0001f)
            {
                _fixedDirection = ParseFixedDirection(cfg.ParamStr1);
            }
        }

        public override void OnBuffConfigureChanged(BuffInstance inst)
        {
            PushSteer(inst);
        }

        public override void OnDetached(BuffInstance inst)
        {
            if (inst.BuffOwner is BaseUnitLogicEntity unit)
            {
                unit.BuffSteerRemoveContribution(inst.InstanceId);
            }
        }

        public override void OnTick(BuffInstance inst, float dt)
        {
            if (inst.BuffOwner == null || inst.BuffOwner is not BaseUnitLogicEntity unit)
            {
                return;
            }

            if (unit.IsDead || unit.MarkDestroyed || unit.CheckHasState(AttrIdConsts.Unmovable))
            {
                unit.BuffSteerRemoveContribution(inst.InstanceId);
                return;
            }

            if (IsImmuneSteer(unit))
            {
                unit.BuffSteerRemoveContribution(inst.InstanceId);
                return;
            }

            if (unit.controlledMoveCtx != null)
            {
                return;
            }

            if (_steerMode == EBuffMoveSteerMode.Random)
            {
                _randomDirTimer -= dt;
                if (_randomDirTimer <= 0f)
                {
                    _randomDirTimer = _directionChangeInterval;
                    var rnd = Random.insideUnitCircle;
                    _randomDir = rnd.sqrMagnitude > 0.0001f ? rnd.normalized : Vector2.right;
                }
            }

            PushSteer(inst);
        }

        bool IsImmuneSteer(BaseUnitLogicEntity unit)
        {
            if (unit.CheckHasState(AttrIdConsts.ImmuneSteerInput))
            {
                return true;
            }

            switch (_steerMode)
            {
                case EBuffMoveSteerMode.AwayFromCaster:
                    return unit.CheckHasState(AttrIdConsts.ImmuneFear);
                case EBuffMoveSteerMode.TowardCaster:
                    return unit.CheckHasState(AttrIdConsts.ImmuneLured);
                default:
                    return false;
            }
        }

        void PushSteer(BuffInstance inst)
        {
            if (inst.BuffOwner is not BaseUnitLogicEntity unit)
            {
                return;
            }

            var dir = ComputeDirection(inst, unit);
            unit.BuffSteerSetContribution(inst.InstanceId, dir, _speedRate);
        }

        Vector2 ComputeDirection(BuffInstance inst, BaseUnitLogicEntity unit)
        {
            switch (_steerMode)
            {
                case EBuffMoveSteerMode.TowardCaster:
                    return DirectionToCaster(inst, unit, toward: true);
                case EBuffMoveSteerMode.AwayFromCaster:
                    return DirectionToCaster(inst, unit, toward: false);
                case EBuffMoveSteerMode.Random:
                    return _randomDir;
                case EBuffMoveSteerMode.FixedDirection:
                    return _fixedDirection.sqrMagnitude > 0.0001f ? _fixedDirection.normalized : Vector2.zero;
                default:
                    return Vector2.zero;
            }
        }

        Vector2 DirectionToCaster(BuffInstance inst, BaseUnitLogicEntity unit, bool toward)
        {
            var casterPos = ResolveCasterPos(inst, unit);
            if (casterPos == null)
            {
                return _randomDir;
            }

            var delta = casterPos.Value - unit.Pos;
            if (toward && delta.sqrMagnitude <= _towardStopRadiusSqr)
            {
                return Vector2.zero;
            }

            if (delta.sqrMagnitude < 0.0004f)
            {
                var fallback = Random.insideUnitCircle;
                return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector2.right;
            }

            var away = -delta.normalized;
            return toward ? -away : away;
        }

        Vector2? ResolveCasterPos(BuffInstance inst, BaseUnitLogicEntity unit)
        {
            if (inst.CasterId != 0 && unit.LogicManager != null)
            {
                var caster = unit.LogicManager.GetLogicEntity(inst.CasterId, false);
                if (caster != null)
                {
                    if (caster is LogicEntityBase le && le.MarkDestroyed)
                    {
                        return _lastCasterPos;
                    }

                    _lastCasterPos = caster.Pos;
                    return caster.Pos;
                }
            }

            return _lastCasterPos;
        }

        static bool TryParseSteerMode(string raw, out EBuffMoveSteerMode mode)
        {
            mode = EBuffMoveSteerMode.AwayFromCaster;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            return System.Enum.TryParse(raw, true, out mode);
        }

        static Vector2 ParseFixedDirection(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return Vector2.zero;
            }

            var parts = raw.Split(',');
            if (parts.Length < 2)
            {
                return Vector2.zero;
            }

            if (float.TryParse(parts[0], out var x) && float.TryParse(parts[1], out var y))
            {
                return new Vector2(x, y);
            }

            return Vector2.zero;
        }
    }
}
