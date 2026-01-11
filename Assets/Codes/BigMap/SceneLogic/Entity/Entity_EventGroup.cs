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
    public class EventGroupLogicEntity : LogicEntityBase
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

        private float _lastCheckTimer;

        public class GroupEventTriggerState
        {
            public int TriggerIdx = 0;
            public int TriggerTimes = 0;

            public EventGroupConfig.GroupEventTrigger cacheTriggerConf;
        }
        protected List<GroupEventTriggerState> TriggerInfos = new();

        public override EEntityType Type => EEntityType.EventGroup;

        public override void Initialize()
        {
            base.Initialize();

            cacheCfg = MapEventGroupCfgLoader.Get(CfgId);

            foreach(var eventTrigger in cacheCfg.EventTriggers)
            {
                var info = new GroupEventTriggerState();
                info.cacheTriggerConf = eventTrigger;

                TriggerInfos.Add(info);
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            TickAllMemberStatus();
        }

        
        public void TickAllMemberStatus()
        {
            if(LogicTime.time < _lastCheckTimer)
            {
                return;
            }

            _lastCheckTimer = LogicTime.time + 0.5f;

            foreach (var t in TriggerInfos)
            {
                if (t.TriggerTimes > 0) continue;
                if(t.cacheTriggerConf.TriggerType == EventGroupConfig.GroupEventTrigger.ETriggerType.Cleared)
                {
                    // 死亡标记
                    var idStrs = t.cacheTriggerConf.Param3.Split(",");
                    bool allCleared = true;
                    foreach (var idStr in idStrs)
                    {
                        int.TryParse(idStr, out var memberId);
                        if (memberId == 0) continue;

                        if (!RealRecord.DestroyedMemberIds.Contains(memberId))
                        {
                            allCleared = false;
                            break;
                        }
                    }

                    if (allCleared)
                    {
                        t.TriggerTimes += 1;

                        HandleOutput(t.cacheTriggerConf);
                    }
                }
                else if(t.cacheTriggerConf.TriggerType != EventGroupConfig.GroupEventTrigger.ETriggerType.MemberIntStatus)
                {
                    int member = (int)t.cacheTriggerConf.Param1;
                    int status = (int)t.cacheTriggerConf.Param2;

                    RealRecord.MemberEntityMap.TryGetValue(member, out var entityId);
                    var intEntity = LogicManager.GetLogicEntity(entityId);
                    if (intEntity == null || intEntity is not LogicEntityInteractPoint intP)
                    {
                        Debug.Log("triger member int status not found");
                        break;
                    }

                    if(intP.CurrStatusId != status)
                    {
                        break;
                    }

                    t.TriggerTimes += 1;
                    HandleOutput(t.cacheTriggerConf);
                }

                
            }
        }

        public void HandleOutput(EventGroupConfig.GroupEventTrigger triggerCfg)
        {
            Debug.Log($"EventGroupLogicEntity HandleOutput {this.Id} for {triggerCfg.TriggerType}");

            if (triggerCfg.Outputs != null)
            {
                foreach (var output in triggerCfg.Outputs)
                {
                    switch (output.OutputType)
                    {
                        case EventGroupConfig.GroupEventOutput.EOutputType.UpdateInteractStatus:
                            {
                                int memberId = (int)output.Param1;
                                int status = (int)output.Param2;

                                RealRecord.MemberEntityMap.TryGetValue(memberId, out var entityId);
                                if(entityId == 0)
                                {
                                    Debug.Log("HandleOutput UpdateInteractStatus fail e");
                                    continue;
                                }

                                var intPoint = LogicManager.GetLogicEntity(entityId) as LogicEntityInteractPoint;
                                if (intPoint == null)
                                {
                                    Debug.Log("HandleOutput UpdateInteractStatus no entity e");
                                    continue;
                                }

                                intPoint.ChangeSelfStatus(status);
                            }
                            break;
                        case EventGroupConfig.GroupEventOutput.EOutputType.ActivateUnits:
                            {
                                var idStrs = output.Param3.Split(",");
                                bool allCleared = true;
                                foreach (var idStr in idStrs)
                                {
                                    int.TryParse(idStr, out var memberId);
                                    if (memberId == 0) continue;

                                    RealRecord.MemberEntityMap.TryGetValue(memberId, out var entityId);

                                    var unit = LogicManager.GetLogicEntity(entityId) as BaseUnitLogicEntity;
                                    // 
                                    Debug.Log($"HandleOutput ActivateUnits activate {entityId}");
                                    if(unit != null)
                                    {
                                        unit.IsActive = true;
                                    }
                                }
                            }
                            break;
                        case EventGroupConfig.GroupEventOutput.EOutputType.RemoveEntities:
                            {
                                var idStrs = output.Param3.Split(",");
                                bool allCleared = true;
                                foreach (var idStr in idStrs)
                                {
                                    int.TryParse(idStr, out var memberId);
                                    if (memberId == 0) continue;

                                    RealRecord.MemberEntityMap.TryGetValue(memberId, out var entityId);
                                    
                                    Debug.Log($"HandleOutput ActivateUnits activate {entityId}");
                                    LogicManager.AreaManager.RequestEntityDestroy(entityId, "event_group_remove");
                                }
                            }
                            break;
                    }
                }
            }
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

        public override void OnSpawn(LogicEntityRecord data)
        {
            base.OnSpawn(data);
            foreach (var kv in RealRecord.MemberEntityMap)
            {
                var member = LogicManager.GetLogicEntity(kv.Value);

                if(member is BaseUnitLogicEntity unitEntity)
                {
                    unitEntity.EventOnDie += OnMemberUnitDead;
                    unitEntity.EventOnEnmityBehave += OnMemberEntityEnmityBehaved;
                }
            }
        }

        public override void OnDespawn(out LogicEntityRecord? snapshot)
        {
            base.OnDespawn(out snapshot);

            foreach (var kv in RealRecord.MemberEntityMap)
            {
                var member = LogicManager.GetLogicEntity(kv.Value);

                if (member is BaseUnitLogicEntity unitEntity)
                {
                    unitEntity.EventOnDie -= OnMemberUnitDead;
                    unitEntity.EventOnEnmityBehave -= OnMemberEntityEnmityBehaved;
                }
            }
        }

        protected void OnMemberEntityEnmityBehaved(long deadEntityId)
        {
            foreach (var t in TriggerInfos)
            {
                if (t.cacheTriggerConf.TriggerType != EventGroupConfig.GroupEventTrigger.ETriggerType.AnyEnmity)
                {
                    continue;
                }

                // 死亡标记
                t.TriggerTimes += 1;

                HandleOutput(t.cacheTriggerConf);
            }
        }

        protected void OnMemberUnitDead(long deadEntityId)
        {
            int markMemberId = 1;
            foreach (var kv in RealRecord.MemberEntityMap)
            {
                if (kv.Value == deadEntityId)
                {
                    markMemberId = kv.Key;
                }
            }

            RealRecord.MemberEntityMap.Remove(markMemberId);
            RealRecord.DestroyedMemberIds.Add(markMemberId);
        }

    }

}

