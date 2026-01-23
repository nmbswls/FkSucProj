using Config;
using Map.Logic.Events;
using My.Map.Entity;
using My.Map.Unit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Unit
{

    public interface IWithEnmity
    {
        bool IsEnmityWith(BaseUnitLogicEntity otherUnit);
    }

    /// <summary>
    /// 敌对行为类型
    /// </summary>
    [Serializable]
    public enum EEnmityBehaveType
    {
        Invalid,
        Loot, // 偷窃
        EnterRoom,
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
    public class UnitEnmity4PlayerCfg
    {
        public string CfgId;

        public List<UnitEnmityBehave> Behaves = new();
        public bool AlwaysEnmity = false; // 永久敌对
        public bool WantedEnmity = false; // 针对通缉敌对

    }


    public static class UnitEnmityCfgLoader
    {
        public static Dictionary<string, UnitEnmity4PlayerCfg> _infos;

        public static UnitEnmity4PlayerCfg Get(string id)
        {
            if (_infos == null)
            {
                _infos = new();

                {
                    // 默认的基础敌对
                    var conf = new UnitEnmity4PlayerCfg();
                    conf.CfgId = "default";
                    _infos[conf.CfgId] = conf;
                }

                {
                    // 默认的基础敌对
                    var conf = new UnitEnmity4PlayerCfg();
                    conf.CfgId = "city_bad";
                    conf.AlwaysEnmity = true;
                    _infos[conf.CfgId] = conf;
                }
                

                {
                    var conf = new UnitEnmity4PlayerCfg();
                    conf.CfgId = "default_npc";
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

                    _infos[conf.CfgId] = conf;
                }

                {
                    var conf = new UnitEnmity4PlayerCfg();
                    conf.CfgId = "default_monster";
                    conf.AlwaysEnmity = true;
                    
                    _infos[conf.CfgId] = conf;
                }

                {
                    var conf = new UnitEnmity4PlayerCfg();
                    conf.CfgId = "default_guard";
                    conf.AlwaysEnmity = true;

                    _infos[conf.CfgId] = conf;
                }
            }

            _infos.TryGetValue(id, out var ret);
            return ret;
        }
    }

    /// <summary>
    /// 单位敌意组件 
    /// </summary>
    public class UnitEnmitySystem : IWithEnmity
    {
        private BaseUnitLogicEntity UnitEntity { get; set; }
        public UnitEnmity4PlayerCfg enmityConf;

        // 临时敌意仅保存针对player的
        public float LastTriggerEnmityTime;
        public float CurrEnmityVal;

        public UnitEnmitySystem(BaseUnitLogicEntity unit)
        {
            this.UnitEntity = unit;

            string enmityCfgId = unit.unitCfg.EnmityCfgId;
            if (string.IsNullOrEmpty(enmityCfgId))
            {
                enmityCfgId = "default";
            }

            enmityConf = UnitEnmityCfgLoader.Get(enmityCfgId);
        }


        /// <summary>
        /// 检查是否与目标敌对
        ///   1.阵营敌对
        ///     1.1 默认矩阵
        ///     1.2 阵营动态敌意
        ///   2.个体敌对
        ///   3.临时敌对
        /// </summary>
        /// <returns></returns>
        public bool IsEnmityWith(BaseUnitLogicEntity otherUnit)
        {

            if(CheckIsEmnityFaction(otherUnit.FactionId))
            {
                return true;
            }

            // 目前只针对player有特殊处理
            if(otherUnit is not PlayerLogicEntity playerEntity)
            {
                return false;
            }

            // 自身h模式下 对主角特殊敌对
            if (UnitEntity is NpcUnitLogicEntity npcUnit && npcUnit.IsInHMode())
            {
                return true;
            }

            // 面对女王模式下的主角 始终敌对
            if (playerEntity.IsQueenMode)
            {
                return true;
            }

            return false;
        }


        public void Tick(float dt)
        {
            
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
                // 更新最后更新时间
                LastTriggerEnmityTime = LogicTime.time;
            }
        }


        /// <summary>
        /// 1.检查默认阵营敌意矩阵
        /// 2.去logic manager中拉取动态阵营敌意
        /// </summary>
        /// <param name="factionId"></param>
        /// <returns></returns>
        public bool CheckIsEmnityFaction(EFactionId factionId)
        {
            // 配置化
            if(UnitEntity.FactionId == EFactionId.HSprite && factionId == EFactionId.Player)
            {
                return true;
            }
            if (UnitEntity.FactionId == EFactionId.Beast && factionId == EFactionId.Player)
            {
                return true;
            }
            return false;
        }
    }

    

}


namespace My.Map
{
    public abstract partial class BaseUnitLogicEntity
    {
        public UnitEnmitySystem EnmitySystem { get; set; }

        public void InitEnmitySystem()
        {
            EnmitySystem = new(this);
        }

        public bool IsEnmityWith(BaseUnitLogicEntity otherUnit)
        {
            return EnmitySystem.IsEnmityWith(otherUnit);
        }
    }
}
