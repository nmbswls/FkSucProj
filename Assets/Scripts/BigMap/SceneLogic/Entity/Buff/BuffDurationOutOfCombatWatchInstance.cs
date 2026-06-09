using My.Map.Entity;

namespace My.Map.Entity
{
    // 脱战计时：仅负责在 !IsInCombat 时按间隔触发 Tick，恢复等行为由 TriggerList 配置
    internal sealed class BuffDurationOutOfCombatWatchInstance : BuffDurationInstanceBase
    {
        readonly float _interval;
        float _timer;

        public BuffDurationOutOfCombatWatchInstance(BuffDurationEffet cfg)
        {
            _interval = cfg.ParamFloat1 > 0f ? cfg.ParamFloat1 : 3f;
        }

        public override void OnBuffInfoChanged(BuffInstance inst)
        {
        }

        public override void OnDetached(BuffInstance inst)
        {
        }

        public override void OnTick(BuffInstance inst, float dt)
        {
            if (!inst.EffectsEnabled || inst.BuffOwner is not BaseUnitLogicEntity unit)
            {
                return;
            }

            if (unit.IsDead || unit.MarkDestroyed || unit.IsInCombat)
            {
                _timer = 0f;
                return;
            }

            _timer -= dt;
            if (_timer > 0f)
            {
                return;
            }

            _timer = _interval;
            inst.DoBuffTrigger(ETriggerType.Tick);
        }
    }
}
