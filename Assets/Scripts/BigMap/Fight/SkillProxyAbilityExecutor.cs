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
    }
}
