using My.Map.Entity;

namespace My.Map.Entity
{
    internal sealed class BuffDurationContactWatchInstance : BuffDurationInstanceBase
    {
        readonly float _radius;
        readonly float _interval;
        float _timer;

        public BuffDurationContactWatchInstance(BuffDurationEffet cfg)
        {
            _radius = cfg.ParamFloat1 > 0f ? cfg.ParamFloat1 : 0.65f;
            _interval = cfg.ParamFloat2 > 0f ? cfg.ParamFloat2 : 0.5f;
        }

        public override void OnBuffInfoChanged(BuffInstance inst)
        {
        }

        public override void OnDetached(BuffInstance inst)
        {
        }

        public override void OnTick(BuffInstance inst, float dt)
        {
            if (!inst.EffectsEnabled || inst.BuffOwner is not BaseUnitLogicEntity owner
                || owner.IsDead || owner.MarkDestroyed)
            {
                return;
            }

            _timer -= dt;
            if (_timer > 0f)
            {
                return;
            }

            _timer = _interval;
            foreach (var entity in owner.FindEntityInRange(owner.Pos, _radius))
            {
                if (entity is not BaseUnitLogicEntity unit || unit.Id == owner.Id
                    || unit.IsDead || unit.MarkDestroyed
                    || unit.FactionId == owner.FactionId)
                {
                    continue;
                }

                inst.DoBuffTrigger(ETriggerType.Tick);
                return;
            }
        }
    }
}
