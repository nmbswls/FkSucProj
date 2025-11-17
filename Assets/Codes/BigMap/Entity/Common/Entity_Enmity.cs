using Config;
using Map.Logic.Events;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

namespace My.Map.Entity
{

    public interface IWithEnmity
    {
        bool CheckIsEmnity();
    }

    /// <summary>
    /// 敌对行为类型
    /// </summary>
    [Serializable]
    public enum EEnmityBehaveType
    {
        Invalid,
        Notice, // 看见即产生敌对
        Loot, // 偷窃
        EnterRoom,
        DevilAct,
    }



    /// <summary>
    /// 敌对行为
    /// </summary>
    [Serializable]
    public class UnitEnmityBehave
    {
        public EEnmityBehaveType EnmityType;
        public long Param1;
        public long Param2;
        public long Param3;
        public long Param4;
        public string Param5;
        public string Param6;
    }

    [Serializable]
    public class UnitEnmityConf
    {
        public string Id;
        public float BaseEnmity;
        public List<UnitEnmityBehave> Behaves = new();
        public bool WillShare = true;
        public float ShareEnmityRange = 5.0f;
    }


    public static class UnitEnmityConfLoader
    {
        public static Dictionary<string, UnitEnmityConf> _infos;

        public static UnitEnmityConf Get(string id)
        {
            if (_infos == null)
            {
                _infos = new();

                {
                    var conf = new UnitEnmityConf();
                    conf.Id = "default_npc";
                    conf.BaseEnmity = 0;
                    conf.Behaves = new List<UnitEnmityBehave>()
                    {
                        new UnitEnmityBehave()
                        {
                            EnmityType = EEnmityBehaveType.Loot,
                            Param1 = 40
                        },

                        new UnitEnmityBehave()
                        {
                            EnmityType = EEnmityBehaveType.EnterRoom,
                            Param1 = 20,
                        },
                    };

                    _infos[conf.Id] = conf;
                }

                {
                    var conf = new UnitEnmityConf();
                    conf.Id = "default_monster";
                    conf.BaseEnmity = 0;
                    conf.Behaves = new List<UnitEnmityBehave>()
                    {
                        new UnitEnmityBehave()
                        {
                            EnmityType = EEnmityBehaveType.Notice,
                            Param1 = 40
                        },
                    };

                    _infos[conf.Id] = conf;
                }
            }

            _infos.TryGetValue(id, out var ret);
            return ret;
        }
    }

    /// <summary>
    /// 敌意组件 
    /// 限定仅针对玩家所在的阵营
    /// </summary>
    public class UnitEnmityComp : IWithEnmity
    {
        public BaseUnitLogicEntity UnitEntity;
        public UnitEnmityConf enmityConf;

        public float LastTriggerEnmityTime;
        public float CurrEnmityVal;
        public bool IsEnmityState; // 有可能出现敌意没满 但敌对状态的情况 例如受到其他人传播

        public void Initialize(BaseUnitLogicEntity unit)
        {
            this.UnitEntity = unit;

            if(unit is NpcUnitLogicEntity npcEntity)
            {
                enmityConf = UnitEnmityConfLoader.Get("default_npc");
            }
            else if (unit is MonsterUnitLogicEntity monsterEntity)
            {
                enmityConf = UnitEnmityConfLoader.Get("default_monster");
            }
        }

        public void Tick(float dt)
        {
            TryApplyShareEnmity();
        }


        /// <summary>
        /// 监听地图事件
        /// </summary>
        /// <param name="ev"></param>
        public void OnMapLogicEvent(IMapLogicEvent ev)
        {
            var srcEntity = ev.Ctx.SourceEntity;
            bool changed = false;
            switch (ev)
            {
                case MLECommonGameEvent commonEv:
                    {
                        if (commonEv.Name == "Loot")
                        {
                            EFactionId lootFaction = (EFactionId)commonEv.Param3;
                            if (lootFaction != UnitEntity.FactionId)
                            {
                                break;
                            }

                            Debug.Log("check loot if same faction");
                            if (enmityConf.Behaves != null)
                            {
                                foreach (var behav in enmityConf.Behaves)
                                {
                                    if (behav.EnmityType == EEnmityBehaveType.Loot)
                                    {
                                        CurrEnmityVal += behav.Param1;
                                        changed = true;
                                    }
                                }
                            }

                        }
                    }
                    break;
            }

            if (changed)
            {
                if (CurrEnmityVal >= 100.0f && !IsEnmityState)
                {
                    IsEnmityState = true;
                }

                // 更新最后更新时间
                LastTriggerEnmityTime = LogicTime.time;
            }
        }


        protected float _shareTimer;

        /// <summary>
        /// 传播敌对状态
        /// </summary>
        public void TryApplyShareEnmity()
        {
            if (enmityConf == null)
            {
                return;
            }

            if (!enmityConf.WillShare)
            {
                return;
            }

            _shareTimer -= LogicTime.deltaTime;
            if (_shareTimer > 0) return;
            _shareTimer = 1.0f;


            foreach (var behave in enmityConf.Behaves)
            {
                if (behave.EnmityType == EEnmityBehaveType.Notice)
                {
                    CurrEnmityVal = 100.0f;
                    if (CurrEnmityVal >= 100.0f && !IsEnmityState)
                    {
                        IsEnmityState = true;
                    }

                    // 更新最后更新时间
                    LastTriggerEnmityTime = LogicTime.time;
                }
            }

            // 只有当自身是传播者 且处于敌意状态 才会传播敌意
            if (this.CurrEnmityVal > 100.0f && IsEnmityState)
            {
                var radius = enmityConf.ShareEnmityRange;
                var retEntities = UnitEntity.LogicManager.visionSenser.OverlapCircleAllEntity(UnitEntity.Pos, radius, new EntityFilterParam()
                {
                    CampFilterType = ECampFilterType.OnlySelf,
                    SelfCampId = UnitEntity.FactionId,
                });

                foreach (var entity in retEntities)
                {
                    if (entity == null) continue;
                    if (entity is not BaseUnitLogicEntity unitEntity)
                    {
                        continue;
                    }

                    unitEntity.EnmityComp?.ShareEnmityValues(this);
                }
            }
        }

        /// <summary>
        /// 分享敌对状态
        /// </summary>
        /// <param name="src"></param>
        public void ShareEnmityValues(UnitEnmityComp src)
        {
            if(!src.IsEnmityState)
            {
                return;
            }
            if(src.CurrEnmityVal < 100.0f)
            {
                return;
            }

            IsEnmityState = true;
            LastTriggerEnmityTime = LogicTime.time;
        }


        public bool CheckIsEmnity()
        {
            return IsEnmityState;
        }
    }


}

