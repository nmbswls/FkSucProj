using My.Map.Entity;
using My.Map.Ground;
using UnityEngine;

namespace My.Map
{
    public abstract partial class BaseUnitLogicEntity
    {
        const float PosChangeEpsilonSq = 1e-10f;
        // 移动中 LogicY 探测最小位移（坡面格内仍需周期性更新）
        const float LogicYProbeMoveThreshold = 0.05f;
        const float LogicYProbeMoveThresholdSq = LogicYProbeMoveThreshold * LogicYProbeMoveThreshold;

        Vector2 _lastLogicYProbePos;
        bool _logicYProbePosInitialized;

        // M2+：高台跳跃落地，v1 空实现
        public virtual bool CanJumpDownToLowerLevel(out int targetLevel, out float targetLogicY)
        {
            targetLevel = -1;
            targetLogicY = 0f;
            return false;
        }

        public virtual void TryJumpDown()
        {
        }

        public override void SetPosition(Vector2 pos)
        {
            bool posChanged = (pos - Pos).sqrMagnitude > PosChangeEpsilonSq;
            base.SetPosition(pos);
            if (posChanged)
            {
                TryResolveLogicYOnPositionChanged();
            }
        }

        protected override void OnAfterPositionTeleport()
        {
            ResolveLogicYAtPosition(LogicYSetReason.Teleport);
            _lastLogicYProbePos = Pos;
            _logicYProbePosInitialized = true;
        }

        bool CanResolveLogicYFromGround()
        {
            if (!IsMovable() || IsDead || MarkDestroyed)
            {
                return false;
            }

            if (CheckHasState(AttrIdConsts.Ghost))
            {
                return false;
            }

            if (MotorSystem != null && MotorSystem.IgnoreGround)
            {
                return false;
            }

            return true;
        }

        bool ShouldProbeLogicYThisMove()
        {
            if (!_logicYProbePosInitialized)
            {
                return true;
            }

            return (Pos - _lastLogicYProbePos).sqrMagnitude >= LogicYProbeMoveThresholdSq;
        }

        void TryResolveLogicYOnPositionChanged()
        {
            if (!CanResolveLogicYFromGround())
            {
                return;
            }

            if (!ShouldProbeLogicYThisMove())
            {
                return;
            }

            _lastLogicYProbePos = Pos;
            _logicYProbePosInitialized = true;
            ResolveLogicYAtPosition(LogicYSetReason.Probe);
        }

        void ResolveLogicYAtPosition(LogicYSetReason reason, float dt = 0f)
        {
            var areaRoot = WorldAreaManager.Instance?.currentRoot;
            var config = areaRoot?.LogicHeightConfig;
            if (config == null)
            {
                return;
            }

            var layers = areaRoot.ResolveGroundSamplingTilemaps();
            if (layers == null || layers.Length == 0)
            {
                return;
            }

            var input = new LogicHeightProbeInput
            {
                Pos = Pos,
                CurrentLogicY = LogicY,
                MaxDownSearch = config.ProbeDownMaxDistance,
                PreferredSupportLogicY = ActiveSupportLogicY,
                IsFlying = MotorSystem != null && MotorSystem.IgnoreGround,
            };

            var result = LogicHeightResolver.Instance.Probe(input, config, layers);
            if (!result.Found)
            {
                return;
            }

            float maxDelta = reason == LogicYSetReason.Teleport || reason == LogicYSetReason.JumpDown || reason == LogicYSetReason.Probe
                ? 0f
                : config.MaxLogicYDeltaPerSec;

            SetLogicY(result.LogicY, reason, maxDelta, dt);
            ActiveSupportLogicY = result.LogicY;

            if (reason == LogicYSetReason.Script
                || reason == LogicYSetReason.Teleport
                || reason == LogicYSetReason.JumpDown)
            {
                _lastLogicYProbePos = Pos;
                _logicYProbePosInitialized = true;
            }
        }
    }
}
