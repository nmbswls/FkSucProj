using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;

namespace My.Map
{
    // Buff 模拟摇杆：聚合多 Buff 方向贡献并写入 Motor.SimulatedMoveInput
    public sealed class BuffSteerInputCoordinator
    {
        struct Contribution
        {
            public Vector2 Dir;
            public float SpeedRate;
        }

        readonly BaseUnitLogicEntity _unit;
        readonly Dictionary<long, Contribution> _contributions = new();

        public BuffSteerInputCoordinator(BaseUnitLogicEntity unit)
        {
            _unit = unit;
        }

        public bool HasActiveContribution => _contributions.Count > 0;

        public void SetContribution(long buffInstanceId, Vector2 dir, float speedRate)
        {
            if (buffInstanceId == 0)
            {
                return;
            }

            var wasEmpty = _contributions.Count == 0;
            _contributions[buffInstanceId] = new Contribution
            {
                Dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero,
                SpeedRate = Mathf.Max(0.01f, speedRate),
            };

            if (wasEmpty)
            {
                _unit.StopMove();
            }

            RefreshMotor();
        }

        public void RemoveContribution(long buffInstanceId)
        {
            if (!_contributions.Remove(buffInstanceId))
            {
                return;
            }

            RefreshMotor();
        }

        void RefreshMotor()
        {
            if (_contributions.Count == 0)
            {
                _unit.InternalClearSimulatedMoveInput();
                return;
            }

            var sum = Vector2.zero;
            var maxRate = 0.01f;
            foreach (var kv in _contributions)
            {
                if (kv.Value.Dir.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                sum += kv.Value.Dir;
                if (kv.Value.SpeedRate > maxRate)
                {
                    maxRate = kv.Value.SpeedRate;
                }
            }

            if (sum.sqrMagnitude < 0.0001f)
            {
                _unit.InternalClearSimulatedMoveInput();
                return;
            }

            _unit.InternalSetSimulatedMoveInput(sum, maxRate);
        }
    }

    public abstract partial class BaseUnitLogicEntity
    {
        BuffSteerInputCoordinator _buffSteerInput;

        public BuffSteerInputCoordinator BuffSteerInput => _buffSteerInput;

        void EnsureBuffSteerInputCoordinator()
        {
            _buffSteerInput ??= new BuffSteerInputCoordinator(this);
        }

        internal void InternalSetSimulatedMoveInput(Vector2 dir, float speedRate)
        {
            MotorSystem?.SetSimulatedMoveInput(dir, speedRate);
        }

        internal void InternalClearSimulatedMoveInput()
        {
            MotorSystem?.ClearSimulatedMoveInput();
        }

        public void BuffSteerSetContribution(long buffInstanceId, Vector2 dir, float speedRate)
        {
            EnsureBuffSteerInputCoordinator();
            _buffSteerInput.SetContribution(buffInstanceId, dir, speedRate);
        }

        public void BuffSteerRemoveContribution(long buffInstanceId)
        {
            if (_buffSteerInput == null)
            {
                return;
            }

            _buffSteerInput.RemoveContribution(buffInstanceId);
        }
    }
}
