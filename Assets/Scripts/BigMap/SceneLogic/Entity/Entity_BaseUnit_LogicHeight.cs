using My.Map.Entity;
using My.Map.Ground;
using UnityEngine;

namespace My.Map
{
    public abstract partial class BaseUnitLogicEntity
    {
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

        protected override void OnAfterPositionTeleport()
        {
            ResolveLogicYAtPosition(LogicYSetReason.Teleport);
        }

        void TickLogicHeight(float dt)
        {
            if (!IsMovable() || IsDead || MarkDestroyed)
            {
                return;
            }

            if (CheckHasState(AttrIdConsts.Ghost))
            {
                return;
            }

            if (MotorSystem != null && MotorSystem.IgnoreGround)
            {
                return;
            }

            ResolveLogicYAtPosition(LogicYSetReason.Probe, dt);
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

            float maxDelta = reason == LogicYSetReason.Teleport || reason == LogicYSetReason.JumpDown
                ? 0f
                : config.MaxLogicYDeltaPerSec;

            SetLogicY(result.LogicY, reason, maxDelta, dt);
            ActiveSupportLogicY = result.LogicY;
        }
    }
}
