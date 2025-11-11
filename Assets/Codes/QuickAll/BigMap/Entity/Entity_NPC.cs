using Config.Unit;
using Config;
using UnityEngine;
using My.Map.Logic.Chunk;


namespace My.Map
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

        public override void Initialize()
        {
            base.Initialize();

            if (UnitBaseRecord.Unsensored)
            {
                LogicManager.globalBuffManager.RequestAddBuff(Id, "unsensored");
            }
        }

        protected override void InitAttribute()
        {
            var cacheCfg = MapNpcConfigLoader.Get(CfgId);
            moveSpeed = cacheCfg.MoveSpeed;

            base.InitAttribute();

        }

        protected override void InitAiBrain()
        {
            base.InitAiBrain();
        }

    }
}

