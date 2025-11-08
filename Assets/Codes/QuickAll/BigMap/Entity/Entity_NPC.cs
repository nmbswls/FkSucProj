using Config.Unit;
using Config;
using Map.Logic.Chunk;
using UnityEngine;


namespace Map.Entity
{
    public class NpcUnitLogicEntity : BaseUnitLogicEntity
    {
        public MapNpcConfig cacheCfg;

        public NpcUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheCfg = MapNpcConfigLoader.Get(CfgId);
            this.unitCfg = cacheCfg;
        }

        public override EEntityType Type => EEntityType.Npc;

        protected override void InitAbilityController()
        {
            abilityController = new DefaultNpcAbilityController(this);
        }

        protected override void InitAttribute()
        {
            var cacheCfg = MapNpcConfigLoader.Get(CfgId);
            moveSpeed = cacheCfg.MoveSpeed;

            attributeStore = new(this);
            // 数值类
            attributeStore.RegisterNumeric("Attack", initialBase: 100);
            attributeStore.RegisterNumeric("Strength", initialBase: 10);
            attributeStore.RegisterNumeric("HP.Max", initialBase: 1000);
            attributeStore.RegisterNumeric("RegenRate.HP", initialBase: 5);

            // 资源类
            attributeStore.RegisterResource("HP", "HP.Max", 100);

            attributeStore.Commit();
        }

        protected override void InitAiBrain()
        {
            base.InitAiBrain();
        }

    }
}

