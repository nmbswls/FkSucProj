using My.Map;

namespace My.Map.Entity
{
    public class SkillProxyAbilityExecutor : MapEntityAbilityExecutor
    {
        readonly SkillProxyLogicEntity _proxy;

        public SkillProxyAbilityExecutor(SkillProxyLogicEntity proxy, BaseUnitLogicEntity owner)
            : base(owner)
        {
            _proxy = proxy;
        }

        protected override ILogicEntity GetSpatialHost() => _proxy;

        protected override ILogicEntity GetCombatSource() => EntityOwner;

        protected override bool IsDetachedHost() => true;

        protected override IEntityBuffOwner GetBuffCheckOwner() => _proxy;

        public override GameLogicManager.LogicFightEffectContext GenerateEfffectContextByAbility(AbilityRunningContext abilityCtx)
        {
            var ctx = base.GenerateEfffectContextByAbility(abilityCtx);
            ctx.TriggerPos = _proxy.PendingBulletSpawnPos;

            if (_proxy.PendingParabolaLaunchZ > 0f)
            {
                ctx.RunningStorage[SkillProxyOrbLayout.ParabolaLaunchZStorageKey] =
                    (long)(_proxy.PendingParabolaLaunchZ * 1000f);
            }

            return ctx;
        }
    }
}
