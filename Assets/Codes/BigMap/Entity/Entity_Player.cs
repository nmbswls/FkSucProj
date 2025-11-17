using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using My.Map.Logic;
using UnityEngine;


namespace My.Map
{
    public class PlayerLogicEntity : BaseUnitLogicEntity
    {

        public PlayerAbilityController PlayerAbilityController { get { return (PlayerAbilityController)abilityController; } }

        public PlayerLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {

        }
        

        public override EEntityType Type => EEntityType.Player;

        public bool IsEnabled { get; private set; } = true;

        protected override void InitAiBrain()
        {
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            //扣减值
            TickResourceCost();

            TickRefreshSpiritMonster();

            //TickMoveNoiseEffect(now, dt);
            TickAddAuraHVal(dt);
        }

        protected override void InitAttribute()
        {
            moveSpeed = 2.0f;
            // 数值类
            attributeStore.RegisterNumeric("HP.Max", initialBase: 1000);
            attributeStore.RegisterNumeric("RegenRate.HP", initialBase: 5);

            RegisterCommonStates();

            attributeStore.RegisterResource(AttrIdConsts.HP, AttrIdConsts.HP_MAX, 100);
            attributeStore.RegisterResource(AttrIdConsts.PlayerClothes, null, 100);
            attributeStore.RegisterResource(AttrIdConsts.PlayerKnockDown, null, 0);
            attributeStore.RegisterResource(AttrIdConsts.PlayerHunger, null, 100);
            attributeStore.RegisterResource(AttrIdConsts.PlayerNaiLi, null, 100);

            // 资源类
            attributeStore.RegisterResource(AttrIdConsts.UnitEnterHVal, null, 0);
            attributeStore.RegisterResource(AttrIdConsts.DeepZhaChance, null, 3);

            attributeStore.Commit();
        }



        public float applyHValTimer;

        protected override void InitAbilityController()
        {
            abilityController = new PlayerAbilityController(this);
        }

        public void TickResourceCost()
        {
            var baseGc = attributeStore.GetAttr(AttrIdConsts.PlayerHungerCost);

        }

        public override void OnStatusAttriChanged(string attrId, bool isOn)
        {
            base.OnStatusAttriChanged(attrId, isOn);
            switch (attrId)
            {
                case AttrIdConsts.HidingMask:
                    {
                        // 进入隐身时
                        if (isOn)
                        {
                            bool hasWatched = false;
                            var filterParam = new EntityFilterParam()
                            {
                                FilterParamLists = new() { EEntityType.Monster, EEntityType.Npc },
                                CampFilterType = ECampFilterType.NotSelf,
                                SelfCampId = EFactionId.Player,
                            };

                            var surrounds = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, 5, filterParam);
                            if (surrounds != null)
                            {
                                foreach (var one in surrounds)
                                {
                                    if (one is not BaseUnitLogicEntity unit)
                                    {
                                        continue;
                                    }

                                    // 敌对模式
                                    if (unit.Type == EEntityType.Monster)
                                    {
                                        if (!LogicManager.visionSenser.CanSee(unit.Pos, unit.FaceDir, this.Pos, 5f, 60))
                                        {
                                            continue;
                                        }

                                        hasWatched = true;
                                    }
                                }
                            }

                            if (hasWatched)
                            {
                                LogicManager.globalBuffManager.RequestAddBuff(this.Id, "hide_marked", 1);
                            }
                        }
                        else
                        {
                            // 脱战时同样需要清理该标记
                            LogicManager.globalBuffManager.RemoveAllBuffById(this.Id, "hide_marked");
                        }
                    }
                    break;
            }
        }


        private float lastRefreshSpiritTime; // 上次更新时间

        /// <summary>
        /// 检查精灵怪物
        /// </summary>
        protected void TickRefreshSpiritMonster()
        {
            
        }

        /// <summary>
        /// 为周围看着自己的打
        /// </summary>
        protected void TickAddAuraHVal(float dt)
        {
            applyHValTimer -= dt;
            if (applyHValTimer > 0)
            {
                return;
            }

            ApplyAuraHVal();
        }


        protected void ApplyAuraHVal()
        {
            float auraRange = 3.0f;
            // 
            var units = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, 3.0f, new EntityFilterParam()
            {
                FilterParamLists = new() { EEntityType.Monster, EEntityType.Npc },
                CampFilterType = ECampFilterType.NotSelf,
                SelfCampId = EFactionId.Player,
            });

            var effect = new MapAbilityEffectAddResourceCfg()
            {
                ResourceId = AttrIdConsts.UnitEnterHVal,
                AddValue = 2000,
                Flags = 100,
            };

            SourceKey srcKey = new SourceKey()
            {
                type = SourceType.Mechanism,
            };
            foreach (var unit in units)
            {
                GameLogicManager.LogicFightEffectContext ctx = new(LogicManager, srcKey)
                {
                    Actor = this,
                    Target = unit,
                };
                LogicManager.HandleLogicFightEffect(effect, ctx);
            }
        }

        #region 移动声音等


        #endregion

    }
}





