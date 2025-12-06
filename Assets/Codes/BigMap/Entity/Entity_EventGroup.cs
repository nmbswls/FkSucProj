using Config.Unit;
using Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using Map.Logic;
using My.Map.Logic;
using static My.GameLogicManager;
using System.Linq;
using System.Security.Principal;
using My.Map.Entity;


namespace My.Map
{
    public class EventGroupLogicEntity : InteractPointLogic
    {

        public EventGroupConfig cacheCfg;

        public LogicEntityRecord4EventGroup RealRecord { get { return (LogicEntityRecord4EventGroup)BindingRecord; } }

        public EventGroupLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheCfg = MapEventGroupCfgLoader.Get(cfgId);
        }

        ///// <summary>
        ///// 缓存
        ///// 危险 logicentity可能被创建为另一个
        ///// </summary>
        //public Dictionary<int, ILogicEntity> GroupMemberDict = new();

        private bool TriggerEnmity = false;

        private bool AllDeadFlag = false;
        private float _lastCheckTimer;

        public override EEntityType Type => EEntityType.AreaEffect;

        public override void Initialize()
        {
            base.Initialize();

            cacheCfg = MapEventGroupCfgLoader.Get(CfgId);


            foreach(var kv in RealRecord.MemberEntityMap)
            {
                var entity = LogicManager.GetLogicEntity(kv.Value);
                if(entity is BaseUnitLogicEntity unitEntity)
                {
                    unitEntity.EventOnEnmityBehave += () =>
                    {
                        // 有敌意行为 尝试切换状态
                        TriggerEnmity = true;
                    };
                }
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            TickAllMemberStatus();
        }

        public bool CanInteractEnable()
        {
            if(!CheckAllRequiredEntityCleared())
            {
                return false;
            }
            return true;
        }

        public void TickAllMemberStatus()
        {
            if(LogicTime.time < _lastCheckTimer)
            {
                return;
            }

            _lastCheckTimer = LogicTime.time + 0.5f;

            if(!AllDeadFlag)
            {
                if(CheckAllRequiredEntityCleared())
                {
                    AllDeadFlag = true;
                    //RealRecord.CustomFlags.Add("Finish_01");
                }
            }
        }


        public bool CheckAllRequiredEntityCleared()
        {
            bool allDead = false;
            foreach(var i in cacheCfg.InteractCondDeadList)
            {
                RealRecord.MemberEntityMap.TryGetValue(i, out long eId);
                var entity = LogicManager.GetLogicEntity(eId);
                if(entity == null || entity.MarkDead)
                {
                    continue;
                }

                allDead = true;
            }
            
            return allDead;
        }

        /// <summary>
        /// 激活沉睡成员
        /// </summary>
        public void ActivateSleepyMembers()
        {
            foreach(var id in RealRecord.SleepMemberIds)
            {
                
            }

            RealRecord.SleepMemberIds.Clear();
        }
    }



}

