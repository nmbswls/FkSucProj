using Bag;
using Config;
using Config.Map;
using Config.Unit;
using DG.Tweening;
using Map.Entity;
using Map.Entity.AI;
using Map.Logic.Chunk;
using Map.Logic.Events;
using SuperScrollView;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static GameLogicManager;
using static MapEntityAbilityController;

namespace Map.Entity
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
        public float LifeTime;


        public override EEntityType Type => EEntityType.Monster;


        public override void Tick(float now, float dt)
        {
            base.Tick(now, dt);

            if (!MarkDead && LifeTime > 0)
            {
                LifeTime -= dt;
                if (LifeTime <= 0)
                {
                    // 死亡
                    LogicManager.AreaManager.RequestEntityDie(this.Id);

                    LogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
                    {
                        Name = "Death",
                        Param3 = this.Id,
                        Param4 = 3, // 3 时间到期
                    });


                    //EventOnDeath?.Invoke();
                }
            }
        }

        protected override void InitAttribute()
        {
            moveSpeed = cacheCfg.MoveSpeed;

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

        protected override void InitAbilityController()
        {
            abilityController = new(this);
            var fakeAbility = Resources.Load<MapAbilitySpecConfig>($"Config/Ability/Test/PuJi");
            //var fakeAbility = AbilityLibrary.CreateDefaultMonsterAttack();
            abilityController.RegisterAbility(fakeAbility);
        }
    }
}








