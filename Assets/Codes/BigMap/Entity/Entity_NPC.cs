using Config.Unit;
using Config;
using UnityEngine;
using My.Map.Logic;
using My.Map.Entity;
using System.Collections.Generic;


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

        // 预设程序生成行为
        public List<string> DefaultSkillList = new List<string>()
        {
            "player_shoot",
            "default_weapon",
            "default_enemy_qinfan",
        };



        protected override void InitAbility()
        {
            base.InitAbility();
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

        public override bool CanWatch()
        {
            return true;
        }
    }
}

