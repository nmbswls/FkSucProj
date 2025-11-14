using System.Collections.Generic;
using Config;
using Config.Map;
using Config.Unit;
using DG.Tweening;
using Map.Logic.Events;
using My.Map.Entity;
using My.Map.Logic;
using UnityEngine;
using static GameLogicManager;

namespace My.Map
{
    public class MonsterUnitLogicEntity : BaseUnitLogicEntity
    {
        public MapMonsterConfig cacheCfg;

        public MonsterUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            // get meta info

            cacheCfg = MapMonsterConfigLoader.Get(CfgId);
            this.unitCfg = cacheCfg;
        }

        public bool IsSummoned;

        // summon info
        public long SummonerId;


        public override EEntityType Type => EEntityType.Monster;


        public override void Tick(float dt)
        {
            base.Tick(dt);

            
        }

        protected override void InitAttribute()
        {
            base.InitAttribute();
            moveSpeed = cacheCfg.MoveSpeed;
        }

        protected override void InitAiBrain()
        {
            base.InitAiBrain();
        }

        protected override void InitAbilityController()
        {
            abilityController = new(this);


            List<string> defaultSkillList = new List<string>()
            {
                "default_enemy_qinfan",
            };

            foreach (var skill in defaultSkillList)
            {
                var conf = AbilityLibrary.GetAbilityConfig(skill);
                abilityController.RegisterAbility(conf);
            }
        }
    }
}








